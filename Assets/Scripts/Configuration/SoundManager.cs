using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Catálogo de audio")]
    public AudioLibrary library;

    [Header("AudioSources")]
    public AudioSource backgroundMusicSource; // Música “A”
    public AudioSource fxSource;

    // --- Internos para fading ---
    [Tooltip("Segundo canal para crossfades (si es null se crea en runtime).")]
    private AudioSource backgroundMusicSourceB;
    private Coroutine musicFadeCo;
    private Coroutine crossfadeCo;

    [SerializeField] private AudioMixer mixer;              // tu MainMixer
    [SerializeField] private string musicParam = "MusicVolume"; // nombre EXACTO del parámetro expuesto para música
    [SerializeField] private string sfxParam = "SFXVolume";   // nombre EXACTO del parámetro expuesto para SFX
    [SerializeField] private Vector2 mixerDbRange = new Vector2(-80f, 0f); // rango dB (silencio..máximo)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (library == null)
            Debug.LogWarning("[SoundManager] No hay AudioLibrary asignado.");
        if (backgroundMusicSource == null)
            Debug.LogWarning("[SoundManager] No hay AudioSource de música asignado.");
        if (fxSource == null)
            Debug.LogWarning("[SoundManager] No hay AudioSource de FX asignado.");

        // Garantiza el segundo canal para crossfades
        EnsureMusicBChannel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // =========================================================
    // Reproducción simple (tu API original)
    // =========================================================
    public static void PlayMusic(int index)
    {
        if (!IsReadyForMusic(index)) return;

        // Cancelar fades en curso
        if (Instance.musicFadeCo != null) { Instance.StopCoroutine(Instance.musicFadeCo); Instance.musicFadeCo = null; }
        if (Instance.crossfadeCo != null) { Instance.StopCoroutine(Instance.crossfadeCo); Instance.crossfadeCo = null; }

        Instance.backgroundMusicSource.Stop();
        Instance.backgroundMusicSource.clip = Instance.library.backgroundMusicClips[index];
        Instance.backgroundMusicSource.volume = 1f;
        Instance.backgroundMusicSource.Play();
    }

    public static void PlayFX(int index)
    {
        if (!IsReadyForFx(index)) return;
        Instance.fxSource.PlayOneShot(Instance.library.fxSounds[index]);
    }

    public static void StopMusic()
    {
        if (Instance?.backgroundMusicSource?.isPlaying == true)
            Instance.backgroundMusicSource.Stop();

        if (Instance?.backgroundMusicSourceB?.isPlaying == true)
            Instance.backgroundMusicSourceB.Stop();

        if (Instance != null)
        {
            if (Instance.musicFadeCo != null) Instance.StopCoroutine(Instance.musicFadeCo);
            if (Instance.crossfadeCo != null) Instance.StopCoroutine(Instance.crossfadeCo);
            Instance.musicFadeCo = null;
            Instance.crossfadeCo = null;
        }
    }

    public static void SetMusicVolume(float volume01)
    {
        if (Instance == null) return;
        Instance.SetMixerVolume(Instance.musicParam, volume01);
    }

    public static void SetFxVolume(float volume01)
    {
        if (Instance == null) return;
        Instance.SetMixerVolume(Instance.sfxParam, volume01);
    }

    private void SetMixerVolume(string exposedParam, float volume01)
    {
        if (mixer == null || string.IsNullOrEmpty(exposedParam)) return;

        volume01 = Mathf.Clamp01(volume01);

        // Mismo mapeo que usamos en la VolumeBar: lineal en dB
        float minDb = mixerDbRange.x; // p.ej. -80
        float maxDb = mixerDbRange.y; // p.ej. 0
        float db = (volume01 <= 0.0001f) ? minDb : Mathf.Lerp(minDb, maxDb, volume01);

        mixer.SetFloat(exposedParam, db);
    }


    // =========================================================
    // NUEVO: Fades
    // =========================================================

    /// <summary>Sube el volumen de la música actual desde su volumen actual hasta targetVolume en 'duration' segundos.</summary>
    public static void FadeInMusic(float duration)
    {
        if (Instance?.backgroundMusicSource == null) return;
        Instance.KillMusicFades();
        Instance.musicFadeCo = Instance.StartCoroutine(
            Instance.Co_FadeVolume(Instance.backgroundMusicSource, Instance.backgroundMusicSource.volume, Mathf.Clamp01(1), duration));
    }

    /// <summary>Baja el volumen de la música actual a 0 en 'duration' segundos. Si stopAfter=true, detiene al terminar.</summary>
    public static void FadeOutMusic(float duration, bool stopAfter = true)
    {
        if (Instance?.backgroundMusicSource == null) return;
        Instance.KillMusicFades();
        Instance.musicFadeCo = Instance.StartCoroutine(
            Instance.Co_FadeVolume(Instance.backgroundMusicSource, Instance.backgroundMusicSource.volume, 0f, duration, stopAfter));
    }

    /// <summary>Hace fade out de la pista actual y luego reproduce la nueva con fade in.</summary>
    public static void PlayMusicWithFade(int index, float fadeOutDuration = 0.5f, float fadeInDuration = 0.5f)
    {
        if (!IsReadyForMusic(index)) return;
        Instance.KillMusicFades();
        Instance.musicFadeCo = Instance.StartCoroutine(Instance.Co_PlayWithFade(index, fadeOutDuration, fadeInDuration));
    }

    /// <summary>Crossfade suave entre la música actual y la nueva en 'duration' segundos (usa segundo canal).</summary>
    public static void CrossfadeTo(int index, float duration = 1.0f)
    {
        if (!IsReadyForMusic(index)) return;
        Instance.KillMusicFades();
        Instance.crossfadeCo = Instance.StartCoroutine(Instance.Co_Crossfade(index, Mathf.Max(0.01f, duration)));
    }

    // =========================================================
    // Coroutines internas
    // =========================================================
    private IEnumerator Co_FadeVolume(AudioSource src, float from, float to, float duration, bool stopAtEndIfToZero = false)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;
        while (t < duration && src != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            src.volume = Mathf.Lerp(from, to, k);
            yield return null;
        }

        if (src != null)
        {
            src.volume = to;
            if (stopAtEndIfToZero && to <= 0.001f)
                src.Stop();
        }

        musicFadeCo = null;
    }

    private IEnumerator Co_PlayWithFade(int index, float fadeOut, float fadeIn)
    {
        var a = backgroundMusicSource;
        if (a == null) yield break;

        // Fade out de la actual
        yield return Co_FadeVolume(a, a.volume, 0f, Mathf.Max(0f, fadeOut), true);

        // Carga y fade in de la nueva
        a.clip = library.backgroundMusicClips[index];
        a.volume = 0f;
        a.Play();
        yield return Co_FadeVolume(a, 0f, 1f, Mathf.Max(0f, fadeIn), false);
    }

    private IEnumerator Co_Crossfade(int index, float duration)
    {
        EnsureMusicBChannel();

        var a = backgroundMusicSource;   // saliente
        var b = backgroundMusicSourceB;  // entrante

        if (a == null || b == null) yield break;

        // Prepara B con la nueva pista
        b.clip = library.backgroundMusicClips[index];
        b.volume = 0f;
        b.Play();

        float startAVol = a.isPlaying ? a.volume : 0f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            if (a != null) a.volume = Mathf.Lerp(startAVol, 0f, k);
            if (b != null) b.volume = Mathf.Lerp(0f, 1f, k);
            yield return null;
        }

        if (a != null)
        {
            a.Stop();
            a.volume = 1f; // resetea
        }
        // Intercambia roles: B pasa a ser el principal
        SwapMusicChannels();

        crossfadeCo = null;
    }

    private void SwapMusicChannels()
    {
        var temp = backgroundMusicSource;
        backgroundMusicSource = backgroundMusicSourceB;
        backgroundMusicSourceB = temp;
    }

    private void EnsureMusicBChannel()
    {
        if (backgroundMusicSourceB != null) return;

        // Crea un segundo canal bajo este mismo GameObject
        backgroundMusicSourceB = gameObject.AddComponent<AudioSource>();
        backgroundMusicSourceB.playOnAwake = false;
        backgroundMusicSourceB.loop = true;
        backgroundMusicSourceB.outputAudioMixerGroup = backgroundMusicSource != null ? backgroundMusicSource.outputAudioMixerGroup : null;
    }

    private void KillMusicFades()
    {
        if (musicFadeCo != null) { StopCoroutine(musicFadeCo); musicFadeCo = null; }
        if (crossfadeCo != null) { StopCoroutine(crossfadeCo); crossfadeCo = null; }
    }

    // =========================================================
    // Validaciones originales
    // =========================================================
    private static bool IsReadyForMusic(int index)
    {
        if (Instance == null || Instance.backgroundMusicSource == null || Instance.library == null)
            return false;

        var clips = Instance.library.backgroundMusicClips;
        if (clips == null || clips.Length == 0)
            return false;

        if (index < 0 || index >= clips.Length)
        {
            Debug.LogWarning($"[SoundManager] Índice de música fuera de rango: {index}");
            return false;
        }
        if (clips[index] == null)
        {
            Debug.LogWarning($"[SoundManager] AudioClip de música nulo en índice {index}");
            return false;
        }
        return true;
    }

    private static bool IsReadyForFx(int index)
    {
        if (Instance == null || Instance.fxSource == null || Instance.library == null)
            return false;

        var clips = Instance.library.fxSounds;
        if (clips == null || clips.Length == 0)
            return false;

        if (index < 0 || index >= clips.Length)
        {
            Debug.LogWarning($"[SoundManager] Índice de FX fuera de rango: {index}");
            return false;
        }
        if (clips[index] == null)
        {
            Debug.LogWarning($"[SoundManager] AudioClip de FX nulo en índice {index}");
            return false;
        }
        return true;
    }

    // Devuelve el volumen actual (0..1) leído del AudioMixer para Música
    public static float GetMusicVolume01()
    {
        if (Instance == null || Instance.mixer == null) return 0f;
        return Instance.TryGetVolume01(Instance.musicParam, out var v) ? v : 0f;
    }

    // Devuelve el volumen actual (0..1) leído del AudioMixer para SFX
    public static float GetSfxVolume01()
    {
        if (Instance == null || Instance.mixer == null) return 0f;
        return Instance.TryGetVolume01(Instance.sfxParam, out var v) ? v : 0f;
    }

    private bool TryGetVolume01(string exposedParam, out float value01)
    {
        value01 = 0f;
        if (string.IsNullOrEmpty(exposedParam)) return false;

        if (mixer.GetFloat(exposedParam, out var db))
        {
            float minDb = mixerDbRange.x; // p.ej. -80
            float maxDb = mixerDbRange.y; // p.ej. 0
            if (db <= minDb + 0.001f) { value01 = 0f; return true; }
            value01 = Mathf.InverseLerp(minDb, maxDb, db);
            return true;
        }
        return false;
    }

    public static void ResetMusic()
    {
        Instance.backgroundMusicSource.clip = null;
    }
}
