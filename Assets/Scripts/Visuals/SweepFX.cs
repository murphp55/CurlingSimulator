using UnityEngine;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Visuals
{
    /// <summary>
    /// Drives ice-scraping particles on a stone while it is being swept.
    /// Attach to the stone prefab alongside StoneController.
    ///
    /// Setup in prefab:
    ///   - Add a child GameObject with a ParticleSystem.
    ///   - Configure the PS for a short-lived, low-velocity ice-chip burst
    ///     (e.g. start speed 0.3, start size 0.03, start lifetime 0.4, white/cyan tint).
    ///   - Assign the ParticleSystem to _particles in this component.
    /// </summary>
    [RequireComponent(typeof(StoneController))]
    public class SweepFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particles;

        [Tooltip("Emission rate (particles/sec) at full sweep intensity.")]
        [SerializeField] private float _maxEmissionRate = 80f;

        private StoneController                _ctrl;
        private ParticleSystem.EmissionModule  _emission;
        private bool                           _psInitialised;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _ctrl = GetComponent<StoneController>();
            if (_particles != null)
            {
                _emission      = _particles.emission;
                _psInitialised = true;
                SetRate(0f);
            }
        }

        private void Update()
        {
            if (!_psInitialised || _ctrl == null) return;

            bool  moving = _ctrl.State.IsMoving;
            float rate   = moving ? _ctrl.SweepIntensity * _maxEmissionRate : 0f;

            SetRate(rate);

            if (moving && !_particles.isPlaying)
                _particles.Play();
            else if (!moving && _particles.isPlaying)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void SetRate(float rate)
        {
            _emission.rateOverTime = rate;
        }
    }
}
