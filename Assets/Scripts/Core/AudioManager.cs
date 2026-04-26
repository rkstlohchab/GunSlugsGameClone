using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace GunSlugsClone.Core
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam = "MusicVolume";
        [SerializeField] private string sfxParam = "SfxVolume";

        [Header("SFX Pool")]
        [SerializeField] private AudioSource sfxPrefab;
        [SerializeField] private int sfxPoolDefault = 8;
        [SerializeField] private int sfxPoolMax = 32;

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;

        private ObjectPool<AudioSource> _sfxPool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxPool = new ObjectPool<AudioSource>(
                createFunc: () => Instantiate(sfxPrefab, transform),
                actionOnGet: src => src.gameObject.SetActive(true),
                actionOnRelease: src => { src.Stop(); src.clip = null; src.gameObject.SetActive(false); },
                actionOnDestroy: src => Destroy(src.gameObject),
                collectionCheck: false,
                defaultCapacity: sfxPoolDefault,
                maxSize: sfxPoolMax);
        }

        public void Initialise()
        {
            ApplySavedVolumes();
        }

        public void PlaySfx(AudioClip clip, Vector3 position = default, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = _sfxPool.Get();
            src.transform.position = position;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();
            StartCoroutine(ReleaseAfter(src, clip.length / Mathf.Max(0.01f, pitch)));
        }

        private IEnumerator ReleaseAfter(AudioSource src, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _sfxPool.Release(src);
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeSeconds = 1f)
        {
            if (musicSource == null || clip == null) return;
            StopAllCoroutines();
            StartCoroutine(MusicCrossfade(clip, loop, fadeSeconds));
        }

        private IEnumerator MusicCrossfade(AudioClip clip, bool loop, float fadeSeconds)
        {
            var startVol = musicSource.volume;
            var t = 0f;
            while (t < fadeSeconds && musicSource.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeSeconds);
                yield return null;
            }
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
            t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, 1f, t / fadeSeconds);
                yield return null;
            }
            musicSource.volume = 1f;
        }

        public void ApplySavedVolumes()
        {
            var d = SaveSystem.Data;
            SetMixer(masterParam, d.MasterVolume);
            SetMixer(musicParam, d.MusicVolume);
            SetMixer(sfxParam, d.SfxVolume);
        }

        private void SetMixer(string param, float linear01)
        {
            if (mixer == null || string.IsNullOrEmpty(param)) return;
            var db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
            mixer.SetFloat(param, db);
        }
    }
}
