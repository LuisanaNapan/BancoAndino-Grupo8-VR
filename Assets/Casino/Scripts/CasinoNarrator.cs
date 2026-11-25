using UnityEngine;

public sealed class CasinoNarrator : MonoBehaviour
{
    const string ClipFolder = "CasinoNarracion/";
    UIManager ui;
    AudioManager mix;
    AudioSource voice;
    string lastId, lastText;
    (string id, string text)[] introduction;
    int introIndex;
    float introMinimumEnd, introNextAt;
    (string id, string text) deferred;
    float volume;
    bool muted;

    public float Volume => volume;
    public bool Muted => muted;
    public bool IsSpeaking => voice != null && voice.isPlaying;
    public bool Introducing => introduction != null && introIndex < introduction.Length;

    public void Initialize(UIManager display, AudioManager audioManager)
    {
        ui = display;
        mix = audioManager;
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0;
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
        bool play = clip != null && !muted && volume > 0f;
        float readingTime = Mathf.Max(2.6f, text.Length / 16f);
        float duration = play ? clip.length : readingTime;
        ui.Caption(text, singleCaption ? float.PositiveInfinity : duration, singleCaption);
        if (play)
        {
            voice.clip = clip;
            voice.Play();
        }
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
        volume = Mathf.Clamp01(volume + delta);
        voice.volume = volume;
        PlayerPrefs.SetFloat("Casino.VoiceVolume", volume);
        if (volume == 0f) StopAudio();
    }

    public void ToggleMute()
    {
        muted = !muted;
        PlayerPrefs.SetInt("Casino.VoiceMuted", muted ? 1 : 0);
        if (muted) StopAudio();
        else Replay();
    }

    void Update()
    {
        if (mix == null) return;
        mix.DuckMusic(IsSpeaking);
        if (Introducing) AdvanceIntroduction();
    }

    void AdvanceIntroduction()
    {
        float now = Time.unscaledTime;
        if (IsSpeaking || now < introMinimumEnd) return;
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
        if (mix != null) mix.DuckMusic(false);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) StopAudio();
    }

    void OnDestroy() { StopAudio(); }
}
