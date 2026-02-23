using UnityEngine;

namespace CurlingSimulator.Audio
{
    /// <summary>
    /// Central audio manager. Uses three AudioSources:
    ///   _sfxSource    — one-shot sound effects
    ///   _sweepSource  — looping sweep sound (volume driven by intensity)
    ///   _musicSource  — background music loop
    ///
    /// Assign AudioClips in the Inspector.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _sweepSource;
        [SerializeField] private AudioSource _musicSource;

        [Header("Clips")]
        [SerializeField] private AudioClip _throwReleaseClip;
        [SerializeField] private AudioClip _sweepingLoopClip;
        [SerializeField] private AudioClip _stoneCollisionClip;
        [SerializeField] private AudioClip _stoneRestClip;
        [SerializeField] private AudioClip[] _endScoreClips; // index by points (0=blank,1=1pt,…)

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (_musicSource != null && !_musicSource.isPlaying)
                _musicSource.Play();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void PlayThrowRelease(float power)
        {
            if (_throwReleaseClip == null) return;
            _sfxSource.pitch  = Mathf.Lerp(0.9f, 1.2f, power);
            _sfxSource.PlayOneShot(_throwReleaseClip);
        }

        public void PlaySweeping(float intensity)
        {
            if (_sweepSource == null || _sweepingLoopClip == null) return;
            if (!_sweepSource.isPlaying)
            {
                _sweepSource.clip = _sweepingLoopClip;
                _sweepSource.loop = true;
                _sweepSource.Play();
            }
            _sweepSource.volume = Mathf.Clamp01(intensity);
        }

        public void StopSweeping()
        {
            if (_sweepSource != null && _sweepSource.isPlaying)
                _sweepSource.Stop();
        }

        public void PlayStoneCollision(float impactSpeed)
        {
            if (_stoneCollisionClip == null) return;
            _sfxSource.pitch  = Mathf.Lerp(0.8f, 1.1f, impactSpeed / 4.5f);
            _sfxSource.PlayOneShot(_stoneCollisionClip);
        }

        public void PlayStoneComeToRest()
        {
            if (_stoneRestClip != null)
                _sfxSource.PlayOneShot(_stoneRestClip);
        }

        public void PlayEndScore(int points)
        {
            if (_endScoreClips == null || _endScoreClips.Length == 0) return;
            int idx = Mathf.Clamp(points, 0, _endScoreClips.Length - 1);
            if (_endScoreClips[idx] != null)
                _sfxSource.PlayOneShot(_endScoreClips[idx]);
        }
    }
}
