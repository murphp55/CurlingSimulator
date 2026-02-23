using System;
using UnityEngine;
using CurlingSimulator.Core;

namespace CurlingSimulator.Simulation
{
    /// <summary>
    /// Per-stone MonoBehaviour. Attached to each stone prefab.
    /// Owns the kinematic Rigidbody integration loop: deceleration, curl, and position update.
    /// StoneSimulator drives this component; nothing else should call SetVelocity or ForceStop directly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class StoneController : MonoBehaviour
    {
        // ── Public state ──────────────────────────────────────────────────────────
        public StoneState State          { get; private set; }
        public float      SweepIntensity => _sweepIntensity;

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<StoneController> OnStopped;

        // ── Private ───────────────────────────────────────────────────────────────
        private Rigidbody        _rb;
        private StoneSimConfig   _config;

        private float  _speed;           // current scalar speed (m/s)
        private float  _launchAngle;     // degrees from +Z axis (sheet centre line)
        private float  _curlSign;        // +1 or -1 depending on team + curl direction
        private float  _curlAccumulator; // total lateral drift accumulated so far
        private float  _sweepIntensity;  // 0–1, set each FixedUpdate by StoneSimulator

        // Visual spin
        private float  _spinRateDegreesPerMetre = 180f;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.useGravity = false;
        }

        /// <summary>
        /// Initialises this stone from a pure-data StoneState.
        /// Called by StoneSimulator after instantiation or when loading a snapshot.
        /// </summary>
        public void Initialize(StoneState initialState, StoneSimConfig config)
        {
            _config = config;
            State   = initialState;

            _speed           = initialState.Velocity.magnitude;
            _launchAngle     = Mathf.Atan2(initialState.Velocity.x, initialState.Velocity.y) * Mathf.Rad2Deg;
            _curlAccumulator = 0f;
            _sweepIntensity  = 0f;

            // Position in world space: StoneState.Position is XZ, Y = stone resting height
            _rb.position = new Vector3(initialState.Position.x, transform.position.y, initialState.Position.y);
        }

        /// <summary>
        /// Launches the stone with the given world-space velocity (XZ components).
        /// launchAngle: degrees from +Z (sheet direction). Curl sign: +1 = curves right, -1 = left.
        /// </summary>
        public void Launch(float speed, float launchAngleDegrees, float curlSign)
        {
            _speed           = speed;
            _launchAngle     = launchAngleDegrees;
            _curlSign        = curlSign;
            _curlAccumulator = 0f;
            _sweepIntensity  = 0f;

            var s = State;
            s.IsMoving = true;
            State = s;
        }

        /// <summary>
        /// Set by StoneSimulator each FixedUpdate while the stone is in motion.
        /// Takes the max of the incoming value and the current value so a brief frame gap doesn't kill it.
        /// </summary>
        public void SetSweepIntensity(float intensity)
        {
            _sweepIntensity = Mathf.Max(_sweepIntensity, intensity);
        }

        /// <summary>Immediately stops the stone (e.g. out-of-bounds, game reset).</summary>
        public void ForceStop()
        {
            _speed = 0f;
            var s  = State;
            s.IsMoving  = false;
            s.IsInPlay  = false;
            State       = s;
        }

        /// <summary>
        /// Repositions the stone to the given position and sets its velocity — used by CollisionResolver.
        /// </summary>
        public void SetStateFromCollision(Vector2 newPosition, Vector2 newVelocity)
        {
            var s        = State;
            s.Position   = newPosition;
            s.Velocity   = newVelocity;
            State        = s;

            _speed           = newVelocity.magnitude;
            _launchAngle     = Mathf.Atan2(newVelocity.x, newVelocity.y) * Mathf.Rad2Deg;
            _curlAccumulator = 0f; // restart curl from new trajectory
            s.IsMoving       = _speed > _config.MinSpeedThreshold;
            State            = s;

            _rb.position = new Vector3(newPosition.x, _rb.position.y, newPosition.y);
        }

        private void FixedUpdate()
        {
            if (!State.IsMoving) return;

            float dt = Time.fixedDeltaTime;

            // ── Apply sweep modifiers ─────────────────────────────────────────────
            float effectiveFriction = _config.BaseDecelerationRate
                * (1f - _sweepIntensity * _config.SweepFrictionReduction);
            float effectiveCurlRate = _config.BaseCurlRate
                * (1f - _sweepIntensity * _config.SweepCurlReduction);

            // ── Decelerate ────────────────────────────────────────────────────────
            _speed = Mathf.Max(0f, _speed - effectiveFriction);

            // ── Accumulate curl (lateral drift grows with distance, not time) ─────
            _curlAccumulator += effectiveCurlRate * _speed * _curlSign;

            // ── Integrate position ────────────────────────────────────────────────
            // Forward direction is along the launch angle in the XZ plane
            float rad     = _launchAngle * Mathf.Deg2Rad;
            Vector3 fwd   = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Vector3 right = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));

            Vector3 newPos = _rb.position
                + fwd   * _speed              * dt
                + right * _curlAccumulator    * dt;

            _rb.MovePosition(newPos);

            // ── Visual spin ───────────────────────────────────────────────────────
            float spinDelta = _speed * dt * _spinRateDegreesPerMetre * _curlSign;
            var   s         = State;
            s.AngularProgress += spinDelta;

            // ── Sync state struct ─────────────────────────────────────────────────
            s.Position  = new Vector2(newPos.x, newPos.z);
            s.Velocity  = new Vector2(fwd.x, fwd.z) * _speed;
            State       = s;

            // ── Check stop ────────────────────────────────────────────────────────
            if (_speed < _config.MinSpeedThreshold)
            {
                _speed     = 0f;
                s          = State;
                s.IsMoving = false;
                State      = s;
                OnStopped?.Invoke(this);
            }

            // ── Decay sweep intensity between packets ─────────────────────────────
            _sweepIntensity = Mathf.MoveTowards(_sweepIntensity, 0f, dt * 3f);
        }
    }
}
