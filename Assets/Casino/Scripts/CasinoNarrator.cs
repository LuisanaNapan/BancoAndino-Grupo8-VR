using UnityEngine;

public sealed class CasinoNarrator : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    sealed class ReadyListener : AndroidJavaProxy
    {
        public volatile int status = int.MinValue;
        public ReadyListener() : base("android.speech.tts.TextToSpeech$OnInitListener") {}
        [UnityEngine.Scripting.Preserve]
        public void onInit(int result) { status = result; }
    }

    AndroidJavaObject speaker;
    ReadyListener listener;
    bool ready;
    string pending;

    void Awake()
    {
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            listener = new ReadyListener();
            speaker = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
        }
        catch (System.Exception error) { Debug.LogWarning("Narración: " + error.Message); }
    }

    void Update()
    {
        if (listener == null || listener.status == int.MinValue) return;
        int status = listener.status;
        listener = null;
        if (speaker == null || status != 0) return;
        using var spanish = new AndroidJavaObject("java.util.Locale", "es", "ES");
        ready = speaker.Call<int>("setLanguage", spanish) >= 0;
        if (!ready) Debug.LogWarning("Narración: instala una voz de texto a voz en español.");
        if (ready && pending != null) Speak(pending);
        pending = null;
    }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    System.Diagnostics.Process speech;
#endif

    public void Speak(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ready)
        {
            if (listener != null) pending = text;
            return;
        }
        try
        {
            using var options = new AndroidJavaObject("android.os.Bundle");
            speaker.Call<int>("speak", text, 0, options, "casino-guia");
        }
        catch (System.Exception error) { Debug.LogWarning("Narración: " + error.Message); }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        StopSpeech();
        try
        {
            string payload = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            string script = "Add-Type -AssemblyName System.Speech; " +
                "$voice = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                "$spanish = $voice.GetInstalledVoices() | Where-Object {$_.Enabled -and $_.VoiceInfo.Culture.TwoLetterISOLanguageName -eq 'es'} | Select-Object -First 1; " +
                "if ($spanish) {$voice.SelectVoice($spanish.VoiceInfo.Name); " +
                "$voice.Speak([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + payload + "')))}";
            string command = System.Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            speech = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch (System.Exception error) { Debug.LogWarning("Narración: " + error.Message); }
#endif
    }

    void StopSpeech()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        pending = null;
        if (ready) speaker?.Call<int>("stop");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (speech == null) return;
        try { if (!speech.HasExited) speech.Kill(); }
        catch (System.InvalidOperationException) {}
        finally { speech.Dispose(); speech = null; }
#endif
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) StopSpeech();
    }

    void OnDestroy()
    {
        StopSpeech();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (speaker == null) return;
        speaker.Call("shutdown");
        speaker.Dispose();
#endif
    }
}
