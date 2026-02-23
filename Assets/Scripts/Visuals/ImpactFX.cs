using UnityEngine;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Visuals
{
    /// <summary>
    /// Spawns a one-shot particle burst at the point of stone-on-stone collisions.
    ///
    /// Attach to any scene GameObject. Assign _simulator and _impactPrefab in the Inspector.
    ///
    /// Setup:
    ///   - Create a ParticleSystem prefab: short burst (duration 0.1, maxParticles ~30),
    ///     start speed 1–3, start size 0.05, white/cyan tint, Stop Action = Destroy.
    ///   - Assign that prefab to _impactPrefab here.
    ///   - Assign the StoneSimulator scene object to _simulator.
    /// </summary>
    public class ImpactFX : MonoBehaviour
    {
        [SerializeField] private StoneSimulator _simulator;
        [SerializeField] private GameObject     _impactPrefab;

        [Tooltip("Y offset above the ice so the burst appears at stone-contact height.")]
        [SerializeField] private float _yOffset = 0.07f;

        // ─────────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_simulator != null)
                _simulator.OnStoneCollision += HandleCollision;
        }

        private void OnDisable()
        {
            if (_simulator != null)
                _simulator.OnStoneCollision -= HandleCollision;
        }

        private void HandleCollision(StoneController a, StoneController b)
        {
            if (_impactPrefab == null) return;

            // Spawn at the midpoint between the two stone centres
            Vector3 pos   = (a.transform.position + b.transform.position) * 0.5f;
            pos.y        += _yOffset;

            var go = Instantiate(_impactPrefab, pos, Quaternion.identity);

            // Auto-destroy: prefer the particle system's own duration if available
            var ps = go.GetComponent<ParticleSystem>();
            float lifetime = ps != null
                ? ps.main.duration + ps.main.startLifetime.constantMax + 0.2f
                : 2f;

            Destroy(go, lifetime);
        }
    }
}
