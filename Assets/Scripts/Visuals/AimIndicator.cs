using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Visuals
{
    /// <summary>
    /// 3D world-space aim guide shown during the ThrowAim phase.
    /// Draws a dashed/solid LineRenderer from the active hack toward the estimated landing zone,
    /// plus a small disc marker at the predicted resting spot.
    ///
    /// Setup:
    ///   1. Create an empty GameObject "AimIndicator" in the scene.
    ///   2. Add this component.
    ///   3. Add a child GameObject "AimLine" with a LineRenderer (assign to _aimLine).
    ///      - Use a dashed/dotted material or a semi-transparent additive material.
    ///   4. Add a child GameObject "LandingMarker" (flat disc / ring mesh) (assign to _landingMarker).
    ///   5. Assign _redHack and _yellowHack (same transforms as on StoneSimulator).
    ///   6. Assign _playerInput (PlayerInputProvider in scene) and _config (StoneSimConfig asset).
    ///
    /// The component subscribes to PlayerInputProvider.OnAimUpdated and
    /// GameManager.OnPhaseChanged at Start, so no manual wiring is needed beyond the Inspector.
    /// </summary>
    public class AimIndicator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private Transform           _redHack;
        [SerializeField] private Transform           _yellowHack;
        [SerializeField] private PlayerInputProvider _playerInput;
        [SerializeField] private StoneSimConfig      _config;

        [Header("Visuals")]
        [SerializeField] private LineRenderer _aimLine;
        [SerializeField] private GameObject   _landingMarker;

        [Header("Line colours")]
        [SerializeField] private Color _redColor    = new Color(1.00f, 0.30f, 0.30f, 0.80f);
        [SerializeField] private Color _yellowColor = new Color(1.00f, 0.88f, 0.15f, 0.80f);

        [Header("Line shape")]
        [Tooltip("Number of points along the prediction line.")]
        [SerializeField] private int   _lineSegments = 24;
        [SerializeField] private float _lineWidth    = 0.04f;
        [SerializeField] private float _yOffset      = 0.015f;  // hover above ice

        // ── Private ───────────────────────────────────────────────────────────────
        private TeamId    _activeTeam;
        private Transform _activeHack;
        private bool      _isActive;

        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (_aimLine != null)
            {
                _aimLine.positionCount = _lineSegments + 1;
                _aimLine.startWidth    = _lineWidth;
                _aimLine.endWidth      = _lineWidth;
                _aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;

            if (_playerInput != null)
                _playerInput.OnAimUpdated += OnAimUpdated;

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;

            if (_playerInput != null)
                _playerInput.OnAimUpdated -= OnAimUpdated;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.ThrowAim)
            {
                var match   = GameManager.Instance?.CurrentMatch;
                _activeTeam = match?.ThrowingTeam ?? TeamId.Red;
                _activeHack = _activeTeam == TeamId.Red ? _redHack : _yellowHack;
                _isActive   = true;
                SetVisible(true);
            }
            else
            {
                _isActive = false;
                SetVisible(false);
            }
        }

        private void OnAimUpdated(float power, float aimAngleDeg, CurlDirection curl)
        {
            if (!_isActive || _activeHack == null || _aimLine == null || _config == null)
                return;

            Color col = _activeTeam == TeamId.Red ? _redColor : _yellowColor;
            _aimLine.startColor = col;
            _aimLine.endColor   = new Color(col.r, col.g, col.b, 0f);

            // ── Compute predicted travel distance ─────────────────────────────────
            // BaseDecelerationRate is subtracted per FixedUpdate frame, not per second.
            // Effective deceleration (m/s per second) = rate / fixedDeltaTime.
            float launchSpeed = Mathf.Lerp(_config.MinLaunchSpeed, _config.MaxLaunchSpeed, power);
            float decelPerSec = _config.BaseDecelerationRate / Mathf.Max(Time.fixedDeltaTime, 0.001f);
            // Kinematic: v² = 2·a·d  →  d = v² / (2·a)
            float travelDist  = decelPerSec > 0f
                ? (launchSpeed * launchSpeed) / (2f * decelPerSec)
                : 20f;
            travelDist = Mathf.Clamp(travelDist, 1f, 35f);

            // ── Throw direction ───────────────────────────────────────────────────
            // aimAngleDeg = 0 means straight toward the house (StoneController launch: +Z at angle=0)
            float rad = aimAngleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;

            // ── Build line points ─────────────────────────────────────────────────
            Vector3 origin = _activeHack.position;
            origin.y = _yOffset;

            for (int i = 0; i <= _lineSegments; i++)
            {
                float t = i / (float)_lineSegments;
                _aimLine.SetPosition(i, origin + dir * (t * travelDist));
            }

            // ── Landing marker ────────────────────────────────────────────────────
            if (_landingMarker != null)
            {
                Vector3 landingPos = origin + dir * travelDist;
                landingPos.y = _yOffset;
                _landingMarker.transform.position = landingPos;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_aimLine != null)       _aimLine.gameObject.SetActive(visible);
            if (_landingMarker != null) _landingMarker.SetActive(visible);
        }
    }
}
