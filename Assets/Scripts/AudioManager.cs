using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource engineSource;
    private AudioSource fxSource;
    private AudioSource warningSource;  // low-fuel beep loop

    private AudioClip engineClip;
    private AudioClip fuelClip;
    private AudioClip crashClip;
    private AudioClip winClip;
    private AudioClip checkpointClip;
    private AudioClip lifeLostClip;
    private AudioClip nitroClip;
    private AudioClip lowFuelBeepClip;

    private const int SampleRate = 44100;
    private bool lowFuelWarningActive;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        engineSource        = gameObject.AddComponent<AudioSource>();
        engineSource.loop   = true;
        engineSource.volume = 0.35f;
        engineSource.spatialBlend = 0f;

        fxSource        = gameObject.AddComponent<AudioSource>();
        fxSource.loop   = false;
        fxSource.volume = 0.7f;
        fxSource.spatialBlend = 0f;

        warningSource        = gameObject.AddComponent<AudioSource>();
        warningSource.loop   = false;
        warningSource.volume = 0.5f;
        warningSource.spatialBlend = 0f;

        engineClip      = BuildEngineClip();
        fuelClip        = BuildFuelClip();
        crashClip       = BuildCrashClip();
        winClip         = BuildWinClip();
        checkpointClip  = BuildCheckpointClip();
        lifeLostClip    = BuildLifeLostClip();
        nitroClip       = BuildNitroClip();
        lowFuelBeepClip = BuildLowFuelBeepClip();

        engineSource.clip = engineClip;
        engineSource.pitch = 0.6f;
        engineSource.Play();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetEngineThrottle(float t)
    {
        engineSource.pitch  = Mathf.MoveTowards(engineSource.pitch,  Mathf.Lerp(0.55f, 1.4f, Mathf.Abs(t)), Time.deltaTime * 3f);
        engineSource.volume = Mathf.Lerp(0.15f, 0.45f, Mathf.Abs(t));
    }

    public void SetLowFuelWarning(bool active)
    {
        if (active == lowFuelWarningActive) return;
        lowFuelWarningActive = active;
        if (active)
            StartCoroutine(LowFuelBeepLoop());
    }

    public void PlayFuelPickup()   => fxSource.PlayOneShot(fuelClip,       0.8f);
    public void PlayCrash()        => fxSource.PlayOneShot(crashClip,      1.0f);
    public void PlayWin()          => fxSource.PlayOneShot(winClip,        0.9f);
    public void PlayCheckpoint()   => fxSource.PlayOneShot(checkpointClip, 0.75f);
    public void PlayLifeLost()     => fxSource.PlayOneShot(lifeLostClip,   0.9f);
    public void PlayNitroActivate()=> fxSource.PlayOneShot(nitroClip,      0.85f);

    private Coroutine engineFade;

    public void StartEngine()
    {
        if (engineSource == null) return;
        if (engineFade != null) { StopCoroutine(engineFade); engineFade = null; }
        engineSource.volume = 0.35f;
        engineSource.pitch  = 0.6f;
        if (!engineSource.isPlaying) engineSource.Play();
    }

    public void StopEngine()
    {
        if (engineSource == null || !engineSource.isPlaying) return;
        if (engineFade != null) StopCoroutine(engineFade);
        engineFade = StartCoroutine(FadeOutEngine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator FadeOutEngine()
    {
        float startVol = engineSource.volume;
        float t = 0f;
        const float dur = 0.2f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            engineSource.volume = Mathf.Lerp(startVol, 0f, t / dur);
            yield return null;
        }
        engineSource.Stop();
        engineFade = null;
    }

    private IEnumerator LowFuelBeepLoop()
    {
        while (lowFuelWarningActive)
        {
            warningSource.PlayOneShot(lowFuelBeepClip, 0.6f);
            yield return new WaitForSecondsRealtime(0.45f);
        }
    }

    // ── Procedural Clip Builders ──────────────────────────────────────────────

    private AudioClip BuildEngineClip()
    {
        int len = SampleRate * 2;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SampleRate;
            float s = Mathf.Sin(2f * Mathf.PI * 80f  * t) * 0.50f
                    + Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.25f
                    + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.15f
                    + Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.08f
                    + (Random.value * 2f - 1f) * 0.04f;
            data[i] = s * 0.7f;
        }
        int fade = SampleRate / 20;
        for (int i = 0; i < fade; i++)
        {
            float t = (float)i / fade;
            data[i] *= t;
            data[len - 1 - i] *= t;
        }
        var clip = AudioClip.Create("Engine", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildFuelClip()
    {
        float dur = 0.3f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        float[] freqs = { 523.25f, 659.25f, 783.99f };
        int seg = len / freqs.Length;
        for (int s = 0; s < freqs.Length; s++)
        {
            int start = s * seg, end = Mathf.Min(start + seg, len);
            for (int i = start; i < end; i++)
            {
                float t = (float)(i - start) / seg;
                data[i] = Mathf.Sin(2f * Mathf.PI * freqs[s] * ((float)i / SampleRate))
                        * Mathf.Sin(Mathf.PI * t) * 0.6f;
            }
        }
        var clip = AudioClip.Create("Fuel", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildCrashClip()
    {
        float dur = 0.5f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t   = (float)i / len;
            float env = Mathf.Exp(-t * 6f);
            float thud = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 10f);
            data[i] = ((Random.value * 2f - 1f) * 0.6f + thud * 0.4f) * env;
        }
        var clip = AudioClip.Create("Crash", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildWinClip()
    {
        float dur = 0.6f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        float[] freqs = { 523.25f, 783.99f, 1046.5f };
        int seg = len / freqs.Length;
        for (int s = 0; s < freqs.Length; s++)
        {
            int start = s * seg, end = Mathf.Min(start + seg, len);
            for (int i = start; i < end; i++)
            {
                float t   = (float)(i - start) / seg;
                float env = Mathf.Sin(Mathf.PI * t);
                float f   = freqs[s];
                float v   = Mathf.Sin(2f * Mathf.PI * f      * ((float)i / SampleRate)) * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * f * 2f * ((float)i / SampleRate)) * 0.25f;
                data[i] = v * env * 0.7f;
            }
        }
        var clip = AudioClip.Create("Win", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Checkpoint: clean two-note chime (D5 → A5)
    private AudioClip BuildCheckpointClip()
    {
        float dur = 0.22f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        float[] freqs = { 587.33f, 880f };
        int seg = len / 2;
        for (int s = 0; s < 2; s++)
        {
            int start = s * seg, end = Mathf.Min(start + seg, len);
            for (int i = start; i < end; i++)
            {
                float t = (float)(i - start) / seg;
                data[i] = Mathf.Sin(2f * Mathf.PI * freqs[s] * ((float)i / SampleRate))
                        * Mathf.Sin(Mathf.PI * t) * 0.55f;
            }
        }
        var clip = AudioClip.Create("Checkpoint", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Life lost: descending glide B4 → B3 with slight distortion
    private AudioClip BuildLifeLostClip()
    {
        float dur = 0.45f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t    = (float)i / len;
            float freq = Mathf.Lerp(493.88f, 246.94f, t);
            float env  = 1f - t;
            float s    = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / SampleRate));
            // Slight clip distortion
            data[i] = Mathf.Clamp(s * 1.3f, -0.9f, 0.9f) * env * 0.65f;
        }
        var clip = AudioClip.Create("LifeLost", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Nitro: high-pitch rising whine F4 → A5
    private AudioClip BuildNitroClip()
    {
        float dur = 0.5f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t    = (float)i / len;
            float freq = Mathf.Lerp(349f, 880f, t * t);
            float env  = t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / 0.9f * 0.6f;
            data[i] = (Mathf.Sin(2f * Mathf.PI * freq * ((float)i / SampleRate)) * 0.6f
                     + Mathf.Sin(2f * Mathf.PI * freq * 2f * ((float)i / SampleRate)) * 0.25f)
                     * env * 0.7f;
        }
        var clip = AudioClip.Create("Nitro", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Low-fuel beep: short 440 Hz sine pulse
    private AudioClip BuildLowFuelBeepClip()
    {
        float dur = 0.08f;
        int len = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            data[i] = Mathf.Sin(2f * Mathf.PI * 440f * ((float)i / SampleRate))
                    * Mathf.Sin(Mathf.PI * t) * 0.5f;
        }
        var clip = AudioClip.Create("LowFuelBeep", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
