using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource engineSource;
    private AudioSource fxSource;

    private AudioClip engineClip;
    private AudioClip fuelClip;
    private AudioClip crashClip;
    private AudioClip winClip;

    private const int SampleRate = 44100;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.volume = 0.35f;
        engineSource.spatialBlend = 0f;

        fxSource = gameObject.AddComponent<AudioSource>();
        fxSource.loop = false;
        fxSource.volume = 0.7f;
        fxSource.spatialBlend = 0f;

        engineClip = BuildEngineClip();
        fuelClip   = BuildFuelClip();
        crashClip  = BuildCrashClip();
        winClip    = BuildWinClip();

        engineSource.clip = engineClip;
        engineSource.pitch = 0.6f;
        engineSource.Play();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetEngineThrottle(float movement)
    {
        // movement: 0 = idle, 1 = full throttle
        float target = Mathf.Lerp(0.55f, 1.4f, Mathf.Abs(movement));
        engineSource.pitch = Mathf.MoveTowards(engineSource.pitch, target, Time.deltaTime * 3f);
        engineSource.volume = Mathf.Lerp(0.15f, 0.45f, Mathf.Abs(movement));
    }

    public void PlayFuelPickup()  => fxSource.PlayOneShot(fuelClip,  0.8f);
    public void PlayCrash()       => fxSource.PlayOneShot(crashClip, 1.0f);
    public void PlayWin()         => fxSource.PlayOneShot(winClip,   0.9f);

    private Coroutine engineFade;

    // (Re)start the looping engine — called whenever a gameplay scene loads.
    // Needed because this object survives scene loads (DontDestroyOnLoad), so
    // Awake (which first starts the engine) only ever runs once.
    public void StartEngine()
    {
        if (engineSource == null) return;
        if (engineFade != null) { StopCoroutine(engineFade); engineFade = null; }
        engineSource.volume = 0.35f;
        engineSource.pitch  = 0.6f;
        if (!engineSource.isPlaying) engineSource.Play();
    }

    // Fade the engine out — called on game over / win so the loop doesn't
    // keep droning under the menu (the "sound glitch" you heard).
    public void StopEngine()
    {
        if (engineSource == null || !engineSource.isPlaying) return;
        if (engineFade != null) StopCoroutine(engineFade);
        engineFade = StartCoroutine(FadeOutEngine());
    }

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

    // ── Procedural clip builders ──────────────────────────────────────────────

    // Engine: layered sine harmonics at 80 Hz, makes a mechanical rumble
    private AudioClip BuildEngineClip()
    {
        int len = SampleRate * 2;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / SampleRate;
            float s = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.25f
                    + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.15f
                    + Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.08f
                    + (Random.value * 2f - 1f) * 0.04f;
            data[i] = s * 0.7f;
        }
        // Smooth the loop point so there's no click
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

    // Fuel pickup: quick ascending arpeggio (C-E-G, 0.3 s)
    private AudioClip BuildFuelClip()
    {
        float duration = 0.3f;
        int len = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[len];
        float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5 E5 G5
        int segLen = len / freqs.Length;
        for (int seg = 0; seg < freqs.Length; seg++)
        {
            float f = freqs[seg];
            int start = seg * segLen;
            int end   = Mathf.Min(start + segLen, len);
            for (int i = start; i < end; i++)
            {
                float t = (float)(i - start) / segLen;
                float env = Mathf.Sin(Mathf.PI * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * ((float)i / SampleRate)) * env * 0.6f;
            }
        }
        var clip = AudioClip.Create("Fuel", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Crash: white noise burst with a fast attack and exponential decay (0.5 s)
    private AudioClip BuildCrashClip()
    {
        float duration = 0.5f;
        int len = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 6f);
            float noise = Random.value * 2f - 1f;
            // Add a low-frequency thud underneath
            float thud = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 10f);
            data[i] = (noise * 0.6f + thud * 0.4f) * env;
        }
        var clip = AudioClip.Create("Crash", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Win: short ascending fanfare (C-G-C, 0.6 s)
    private AudioClip BuildWinClip()
    {
        float duration = 0.6f;
        int len = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[len];
        float[] freqs = { 523.25f, 783.99f, 1046.5f }; // C5 G5 C6
        int segLen = len / freqs.Length;
        for (int seg = 0; seg < freqs.Length; seg++)
        {
            float f = freqs[seg];
            int start = seg * segLen;
            int end   = Mathf.Min(start + segLen, len);
            for (int i = start; i < end; i++)
            {
                float t = (float)(i - start) / segLen;
                float env = Mathf.Sin(Mathf.PI * t);
                float s = Mathf.Sin(2f * Mathf.PI * f       * ((float)i / SampleRate)) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * f * 2f  * ((float)i / SampleRate)) * 0.25f;
                data[i] = s * env * 0.7f;
            }
        }
        var clip = AudioClip.Create("Win", len, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
