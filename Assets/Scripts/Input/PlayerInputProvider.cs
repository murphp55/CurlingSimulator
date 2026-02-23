using System;
using System.Collections.Generic;
using UnityEngine;
using CurlingSimulator.Core;

namespace CurlingSimulator.Input
{
    /// <summary>
    /// Human mouse input for throwing and sweeping.
    ///
    /// Throw mechanic:
    ///   Hold LMB + push mouse forward (screen Y) to build power.
    ///   Horizontal mouse movement while holding steers the aim angle.
    ///   Release LMB to commit the throw.
    ///   Tab toggles curl direction before throwing.
    ///
    /// Sweep mechanic:
    ///   Rapid left-right (X axis) mouse reversals are measured in a rolling 0.5 s window.
    ///   12 reversals/s = 100% sweep intensity.
    /// </summary>
    public class PlayerInputProvider : MonoBehaviour, IInputProvider
    {
        // ── Inspector tunables ────────────────────────────────────────────────────
        [Header("Throw")]
        [Tooltip("How many screen pixels of upward mouse movement = maximum power.")]
        [SerializeField] private float _powerMaxPixels = 300f;

        [Tooltip("How sensitive horizontal movement is when aiming. Degrees per pixel.")]
        [SerializeField] private float _directionSensitivity = 0.1f;

        [Tooltip("Maximum aim deviation from centre line (degrees).")]
        [SerializeField] private float _maxAimAngle = 8f;

        [Tooltip("Minimum horizontal mouse delta (screen units) to register as aim input.")]
        [SerializeField] private float _directionDeadzone = 0.5f;

        [Header("Sweep")]
        [Tooltip("Rolling window length in seconds for counting reversals.")]
        [SerializeField] private float _sweepWindowSeconds = 0.5f;

        [Tooltip("Reversals per second that yield 100% sweep intensity.")]
        [SerializeField] private float _maxEffectiveReversalRate = 12f;

        [Tooltip("Minimum absolute mouse delta to count as intentional sweep movement.")]
        [SerializeField] private float _sweepDirectionThreshold = 0.5f;

        // ── IInputProvider ────────────────────────────────────────────────────────
        public event Action<ThrowData> OnThrowCommitted;
        public event Action<SweepData> OnSweepUpdate;
        public bool IsSweepActive => _sweepEnabled;

        // ── Private state ─────────────────────────────────────────────────────────
        private bool   _throwEnabled;
        private bool   _sweepEnabled;

        // Throw tracking
        private bool   _isCharging;
        private float  _chargeStartY;
        private float  _power;           // 0–1
        private float  _aimAngle;        // degrees

        // Curl selection
        private CurlDirection _selectedCurl = CurlDirection.InTurn;

        // Context filled in by BeginThrowInput
        private ThrowData _throwContext;

        // Sweep tracking
        private float   _prevMouseX;
        private float   _scrubIntensity;
        private readonly Queue<float> _reversalTimestamps = new Queue<float>();

        // ── Events the HUD can subscribe to ──────────────────────────────────────
        /// <summary>Fires every frame while charging with current (power, angle).</summary>
        public event Action<float, float, CurlDirection> OnAimUpdated;

        // ─────────────────────────────────────────────────────────────────────────

        public void BeginThrowInput(ThrowData context)
        {
            _throwContext  = context;
            _throwEnabled  = true;
            _isCharging    = false;
            _power         = 0f;
            _aimAngle      = 0f;
        }

        public void BeginSweepInput()
        {
            _sweepEnabled    = true;
            _scrubIntensity  = 0f;
            _prevMouseX      = 0f;
            _reversalTimestamps.Clear();
        }

        public void EndSweepInput()
        {
            _sweepEnabled   = false;
            _scrubIntensity = 0f;
        }

        private void Update()
        {
            // Toggle curl direction before throwing
            if (_throwEnabled && UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                _selectedCurl = _selectedCurl == CurlDirection.InTurn
                    ? CurlDirection.OutTurn
                    : CurlDirection.InTurn;
            }

            if (_throwEnabled)  UpdateThrow();
            if (_sweepEnabled)  UpdateSweep();
        }

        private void UpdateThrow()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _isCharging   = true;
                _chargeStartY = UnityEngine.Input.mousePosition.y;
                _power        = 0f;
                _aimAngle     = 0f;
            }

            if (_isCharging)
            {
                float rawPush = UnityEngine.Input.mousePosition.y - _chargeStartY;
                _power = Mathf.Clamp01(rawPush / _powerMaxPixels);

                float deltaX = UnityEngine.Input.GetAxis("Mouse X");
                if (Mathf.Abs(deltaX) > _directionDeadzone)
                {
                    _aimAngle = Mathf.Clamp(
                        _aimAngle + deltaX * _directionSensitivity,
                        -_maxAimAngle, _maxAimAngle);
                }

                OnAimUpdated?.Invoke(_power, _aimAngle, _selectedCurl);
            }

            if (UnityEngine.Input.GetMouseButtonUp(0) && _isCharging)
            {
                _isCharging   = false;
                _throwEnabled = false;

                var throwData = new ThrowData
                {
                    Power          = _power,
                    DirectionAngle = _aimAngle,
                    Curl           = _selectedCurl,
                    Thrower        = _throwContext.Thrower,
                    ThrowIndex     = _throwContext.ThrowIndex,
                    Timestamp      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                OnThrowCommitted?.Invoke(throwData);
            }
        }

        private void UpdateSweep()
        {
            float mouseX    = UnityEngine.Input.GetAxis("Mouse X");
            float absMouseX = Mathf.Abs(mouseX);

            // Detect direction reversal
            bool reversed = absMouseX > _sweepDirectionThreshold
                         && _prevMouseX != 0f
                         && Mathf.Sign(mouseX) != Mathf.Sign(_prevMouseX);

            if (reversed)
                _reversalTimestamps.Enqueue(Time.time);

            // Expire old entries outside the rolling window
            while (_reversalTimestamps.Count > 0 &&
                   Time.time - _reversalTimestamps.Peek() > _sweepWindowSeconds)
            {
                _reversalTimestamps.Dequeue();
            }

            // Compute intensity
            float reversalRate  = _reversalTimestamps.Count / _sweepWindowSeconds;
            float rawIntensity  = Mathf.Clamp01(reversalRate / _maxEffectiveReversalRate);

            _scrubIntensity = Mathf.Lerp(_scrubIntensity, rawIntensity, Time.deltaTime * 8f);

            // Decay when mouse is still
            if (absMouseX < _sweepDirectionThreshold)
                _scrubIntensity = Mathf.MoveTowards(_scrubIntensity, 0f, Time.deltaTime * 2f);

            _prevMouseX = mouseX;

            var sweepData = new SweepData
            {
                Intensity  = _scrubIntensity,
                DeltaTime  = Time.deltaTime,
                Timestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            OnSweepUpdate?.Invoke(sweepData);
        }
    }
}
