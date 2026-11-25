using System;
using UnityEngine;

// Provisional local speech for interventions without a recorded AudioClip.
internal sealed class CasinoDeviceSpeech : IDisposable
{
    float deadline;
    bool warned;
    public bool Speaking { get; private set; }
    public bool Pending { get; private set; }
    public bool Failed { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    sealed class InitListener : AndroidJavaProxy
    {
        public volatile int Status = int.MinValue;
        public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
        [UnityEngine.Scripting.Preserve]
        public void onInit(int status) { Status = status; }
    }

    AndroidJavaObject android;
    InitListener init;
    IntPtr speakMethod;
    bool ready;
    string waitingText;
    float waitingVolume, startGrace;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    System.Diagnostics.Process windows;
#endif

    public CasinoDeviceSpeech()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
            init = new InitListener();
            android = new AndroidJavaObject("android.speech.tts.TextToSpeech", context, init);
        }
        catch (Exception exception) { Warn(exception.Message); }
#endif
    }

    public bool Start(string text, float volume)
    {
        Stop();
        Failed = false;
        deadline = Time.unscaledTime + Mathf.Clamp(text.Length / 11f + 4f, 7f, 30f);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ready) return StartAndroid(text, volume);
        if (init == null) return false;
        waitingText = text;
        waitingVolume = volume;
        startGrace = Time.unscaledTime + 3.5f;
        Pending = true;
        return true;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            string script = "$ErrorActionPreference = 'Stop'; $speaker = New-Object -ComObject SAPI.SpVoice; " +
                "$spanish = $speaker.GetVoices() | Where-Object {($_.GetAttribute('Language') -split ';') -match '^[0-9a-f]*0a$'} | Select-Object -First 1; " +
                "if ($null -eq $spanish) {exit 3}; $speaker.Voice = $spanish; $speaker.Rate = -1; " +
                "$speaker.Volume = " + Mathf.RoundToInt(volume * 100f) + "; " +
                "$null = $speaker.Speak([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + payload + "')))";
            string command = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            windows = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + command)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            Pending = windows != null;
            return Pending;
        }
        catch (Exception exception) { Warn(exception.Message); return false; }
#else
        return false;
#endif
    }

    public void Tick()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CompleteAndroidSetup();
        if (waitingText != null && (ready || Time.unscaledTime >= startGrace))
        {
            string text = waitingText;
            waitingText = null;
            if (!ready || !StartAndroid(text, waitingVolume)) Failed = true;
        }
        try
        {
            Speaking = ready && deadline > 0f && android.Call<bool>("isSpeaking");
            if (Speaking) startGrace = 0f;
            else if (waitingText == null && startGrace > 0f && Time.unscaledTime >= startGrace) Failed = true;
            Pending = waitingText != null || (ready && !Speaking && Time.unscaledTime < startGrace);
        }
        catch (Exception exception)
        {
            Warn(exception.Message);
            ready = false;
            Failed = true;
            Stop();
        }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (windows != null)
        {
            if (windows.HasExited)
            {
                if (windows.ExitCode != 0)
                {
                    Failed = true;
                    Warn("Comprueba que Windows tenga una voz de español instalada.");
                }
                windows.Dispose();
                windows = null;
            }
            Speaking = windows != null;
        }
        Pending = false;
#endif
        if (deadline > 0f && Time.unscaledTime >= deadline)
        {
            Failed |= Speaking || Pending;
            Stop();
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void CompleteAndroidSetup()
    {
        if (init == null || init.Status == int.MinValue) return;
        int status = init.Status;
        init = null;
        try
        {
            if (status == 0 && ChooseOfflineSpanish())
            {
                speakMethod = AndroidJNI.GetMethodID(android.GetRawClass(), "speak",
                    "(Ljava/lang/CharSequence;ILandroid/os/Bundle;Ljava/lang/String;)I");
                android.Call<int>("setSpeechRate", .9f);
                ready = speakMethod != IntPtr.Zero;
            }
        }
        catch (Exception exception) { Warn(exception.Message); }
        if (!ready) Warn("Instala o activa una voz de español sin conexión en los ajustes de texto a voz de Android.");
    }

    bool ChooseOfflineSpanish()
    {
        using var choices = android.Call<AndroidJavaObject>("getVoices");
        if (choices == null) return false;
        using var iterator = choices.Call<AndroidJavaObject>("iterator");
        int best = -1;
        while (iterator.Call<bool>("hasNext"))
        {
            using var voice = iterator.Call<AndroidJavaObject>("next");
            using var locale = voice.Call<AndroidJavaObject>("getLocale");
            if (locale.Call<string>("getLanguage") != "es" || voice.Call<bool>("isNetworkConnectionRequired")) continue;
            using var features = voice.Call<AndroidJavaObject>("getFeatures");
            if (features != null && features.Call<bool>("contains", "notInstalled")) continue;
            string country = locale.Call<string>("getCountry");
            int priority = country == "PE" ? 4 : country == "MX" || country == "419" ? 3 : country == "ES" ? 1 : 2;
            int score = priority * 1000 + voice.Call<int>("getQuality");
            if (score > best && android.Call<int>("setVoice", voice) == 0) best = score;
        }
        return best >= 0;
    }

    bool StartAndroid(string text, float volume)
    {
        try
        {
            using var message = new AndroidJavaObject("java.lang.String", text);
            using var options = new AndroidJavaObject("android.os.Bundle");
            using var id = new AndroidJavaObject("java.lang.String", "banco-andino-guia");
            options.Call("putFloat", "volume", volume);
            options.Call("putInt", "stream", 3); // STREAM_MUSIC
            var args = new[]
            {
                new jvalue { l = message.GetRawObject() },
                new jvalue { i = 0 }, // QUEUE_FLUSH
                new jvalue { l = options.GetRawObject() },
                new jvalue { l = id.GetRawObject() }
            };
            if (AndroidJNI.CallIntMethod(android.GetRawObject(), speakMethod, args) == 0)
            {
                startGrace = Time.unscaledTime + 1.2f;
                Pending = true;
                return true;
            }
        }
        catch (Exception exception) { Warn(exception.Message); }
        Warn("El motor de voz no pudo reproducir la indicación.");
        startGrace = 0f;
        Pending = false;
        return false;
    }
#endif

    void Warn(string message)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning("Guía de voz local. " + message);
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        waitingText = null;
        startGrace = 0f;
        if (ready && deadline > 0f)
        {
            try { android.Call<int>("stop"); }
            catch (Exception exception) { ready = false; Warn(exception.Message); }
        }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (windows != null)
        {
            try { if (!windows.HasExited) windows.Kill(); }
            catch (InvalidOperationException) { }
            finally { windows.Dispose(); windows = null; }
        }
#endif
        deadline = 0f;
        Speaking = false;
        Pending = false;
    }

    public void Dispose()
    {
        Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (android == null) return;
        try { android.Call("shutdown"); }
        catch (Exception exception) { Warn(exception.Message); }
        finally { android.Dispose(); android = null; }
#endif
    }
}
