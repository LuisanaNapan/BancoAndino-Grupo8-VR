using UnityEngine;

public sealed class CasinoNarrator : MonoBehaviour
{
    const string ClipFolder = "CasinoNarracion/";
    UIManager ui;
    AudioManager mix;
    AudioSource voice;
    CasinoDeviceSpeech deviceSpeech;
    string lastId, lastText;
    (string id, string text)[] introduction;
    int introIndex;
    float introMinimumEnd, introNextAt;
    (string id, string text) deferred;
    float volume;
    bool muted;
    bool resumeCue;

    public float Volume => volume;
    public bool Muted => muted;
    public bool IsSpeaking => (voice != null && voice.isPlaying) || (deviceSpeech != null && deviceSpeech.Speaking);
    public string PlaybackBackend { get; private set; }
    public bool Introducing => introduction != null && introIndex < introduction.Length;
    public bool FirstInstructionReady => !Introducing || introIndex >= introduction.Length - 2;

    public void Initialize(UIManager display, AudioManager audioManager)
    {
        ui = display;
        mix = audioManager;
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0;
        deviceSpeech = new CasinoDeviceSpeech();
        volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Casino.VoiceVolume", .75f));
        muted = PlayerPrefs.GetInt("Casino.VoiceMuted") != 0;
        voice.volume = volume;
    }

    public void Speak(string id, string text)
    {
        if (Introducing)
        {
            deferred = (id, text);
            return;
        }
        lastId = id;
        lastText = text;
        PlayLine(id, text, false);
    }

    public void Welcome(params (string id, string text)[] lines)
    {
        introduction = lines;
        introIndex = 0;
        deferred = default;
        lastText = null;
        PlayIntroductionLine();
    }

    public void StopWelcome()
    {
        if (!Introducing) return;
        introIndex = introduction.Length;
        deferred = default;
        ui.Caption("", 0f);
        StopAudio();
    }

    void PlayIntroductionLine()
    {
        var line = introduction[introIndex];
        introNextAt = 0f;
        introMinimumEnd = Time.unscaledTime + PlayLine(line.id, line.text, true);
    }

    float PlayLine(string id, string text, bool singleCaption)
    {
        StopAudio();
        var clip = string.IsNullOrEmpty(id) ? null : Resources.Load<AudioClip>(ClipFolder + id);
        bool enabled = !muted && volume > 0f && !AudioListener.pause && AudioListener.volume > 0f;
        bool play = clip != null && enabled;
        float readingTime = Mathf.Max(2.6f, text.Length / 16f);
        float duration = play ? clip.length : readingTime;
        if (play)
        {
            voice.clip = clip;
            voice.Play();
            PlaybackBackend = "AudioClip";
        }
        else if (clip == null && enabled && deviceSpeech.Start(text, volume * AudioListener.volume))
            PlaybackBackend = "Voz del dispositivo";
        ui.Caption(text, singleCaption ? float.PositiveInfinity : duration, singleCaption,
            PlaybackBackend == "Subtítulos");
        return duration;
    }

    public void Replay()
    {
        if (Introducing) PlayIntroductionLine();
        else if (!string.IsNullOrEmpty(lastText)) Speak(lastId, lastText);
        else if (introduction != null) Welcome(introduction);
    }

    public void AdjustVolume(float delta)
    {
        bool wasPlaying = IsSpeaking || deviceSpeech.Pending;
        volume = Mathf.Clamp01(volume + delta);
        voice.volume = volume;
        PlayerPrefs.SetFloat("Casino.VoiceVolume", volume);
        if (wasPlaying && (volume == 0f || deviceSpeech.Speaking || deviceSpeech.Pending)) Replay();
    }

    public void ToggleMute()
    {
        bool wasPlaying = IsSpeaking || deviceSpeech.Pending;
        muted = !muted;
        PlayerPrefs.SetInt("Casino.VoiceMuted", muted ? 1 : 0);
        if (!muted || wasPlaying) Replay();
    }

    void Update()
    {
        if (mix == null) return;
        deviceSpeech.Tick();
        if (PlaybackBackend == "Voz del dispositivo" && deviceSpeech.Failed) ShowFailedSpeech();
        mix.DuckMusic(IsSpeaking);
        if (Introducing) AdvanceIntroduction();
    }

    void ShowFailedSpeech()
    {
        string text = Introducing ? introduction[introIndex].text : lastText;
        StopAudio();
        float readingTime = Mathf.Max(2.6f, text.Length / 16f);
        ui.Caption(text, Introducing ? float.PositiveInfinity : readingTime, Introducing);
        if (Introducing) introMinimumEnd = Time.unscaledTime + readingTime;
    }

    void AdvanceIntroduction()
    {
        float now = Time.unscaledTime;
        if (IsSpeaking || deviceSpeech.Pending || now < introMinimumEnd) return;
        if (introNextAt == 0f)
        {
            ui.Caption("", 0f);
            introNextAt = now + .5f;
            return;
        }
        if (now < introNextAt) return;
        introIndex++;
        if (Introducing)
        {
            PlayIntroductionLine();
            return;
        }
        var next = deferred;
        deferred = default;
        if (next.text != null) Speak(next.id, next.text);
    }

    void StopAudio()
    {
        if (voice != null) voice.Stop();
        deviceSpeech?.Stop();
        PlaybackBackend = "Subtítulos";
        if (mix != null) mix.DuckMusic(false);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            resumeCue = IsSpeaking || (deviceSpeech != null && deviceSpeech.Pending);
            StopAudio();
        }
        else if (resumeCue)
        {
            resumeCue = false;
            Replay();
        }
    }

    void OnDestroy()
    {
        StopAudio();
        deviceSpeech?.Dispose();
    }
}
