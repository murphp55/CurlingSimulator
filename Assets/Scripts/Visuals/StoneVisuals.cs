using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Visuals
{
    /// <summary>
    /// Per-stone visual component. Attach to the stone prefab alongside StoneController.
    ///
    /// Handles:
    ///   - Applying the correct team material (instances it so emission can vary per-stone).
    ///   - Spinning the visual mesh via State.AngularProgress.
    ///   - Enabling / disabling the TrailRenderer while the stone is moving.
    ///   - Highlight mode (boosted emission) when the stone is the scoring stone.
    ///
    /// Setup in prefab:
    ///   - Assign _redMaterial / _yellowMaterial (URP Lit with _EMISSION keyword).
    ///   - Assign _stoneRenderer (the MeshRenderer on the stone mesh child).
    ///   - Assign _spinRoot (same transform or a child that should rotate around Y).
    ///   - Optionally assign _trail (TrailRenderer child).
    /// </summary>
    [RequireComponent(typeof(StoneController))]
    public class StoneVisuals : MonoBehaviour
    {
        [Header("Team Materials")]
        [SerializeField] private Material _redMaterial;
        [SerializeField] private Material _yellowMaterial;

        [Header("References")]
        [SerializeField] private Renderer      _stoneRenderer;
        [SerializeField] private Transform     _spinRoot;       // rotates around Y each Update
        [SerializeField] private TrailRenderer _trail;

        [Header("Trail Colours")]
        [SerializeField] private Color _redTrailColor    = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color _yellowTrailColor = new Color(1f, 0.85f, 0.10f, 1f);

        [Header("Emission")]
        [SerializeField] private float _baseEmissionScale      = 1.0f;
        [SerializeField] private float _highlightEmissionScale = 4.0f;

        // ── Private ───────────────────────────────────────────────────────────────
        private StoneController _ctrl;
        private Material        _instanceMat;   // per-stone material instance
        private bool            _highlighted;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _ctrl = GetComponent<StoneController>();
        }

        private void Start()
        {
            // StoneController.Initialize() is called during StoneSimulator.Awake(),
            // so State.Owner is valid by the time Start() runs.
            ApplyTeamVisuals();
        }

        private void Update()
        {
            if (_ctrl == null) return;

            // Spin the visual mesh to reflect angular progress from physics
            if (_spinRoot != null)
                _spinRoot.localRotation = Quaternion.Euler(0f, _ctrl.State.AngularProgress, 0f);

            // Trail emits only while the stone is in motion
            if (_trail != null)
                _trail.emitting = _ctrl.State.IsMoving;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by scoring visualiser to mark this stone as the closest to the button.
        /// Boosts emission to make it stand out.
        /// </summary>
        public void SetHighlight(bool on)
        {
            if (_highlighted == on) return;
            _highlighted = on;

            if (_instanceMat != null)
                _instanceMat.SetColor("_EmissionColor",
                    BaseEmissionColor() * (_highlighted ? _highlightEmissionScale : _baseEmissionScale));
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyTeamVisuals()
        {
            if (_ctrl == null || _stoneRenderer == null) return;

            var sourceMat = _ctrl.State.Owner == TeamId.Red ? _redMaterial : _yellowMaterial;
            if (sourceMat == null) return;

            // Instance so each stone controls its own emission independently
            _instanceMat = new Material(sourceMat) { name = sourceMat.name + "_Instance" };
            _stoneRenderer.material = _instanceMat;

            _instanceMat.EnableKeyword("_EMISSION");
            _instanceMat.SetColor("_EmissionColor", BaseEmissionColor() * _baseEmissionScale);

            if (_trail != null)
            {
                Color tc = _ctrl.State.Owner == TeamId.Red ? _redTrailColor : _yellowTrailColor;
                _trail.startColor = tc;
                _trail.endColor   = new Color(tc.r, tc.g, tc.b, 0f);
            }
        }

        private Color BaseEmissionColor()
            => _ctrl.State.Owner == TeamId.Red
                ? new Color(0.60f, 0.04f, 0.04f)
                : new Color(0.70f, 0.55f, 0.00f);
    }
}
