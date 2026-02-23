using System;
using System.Collections.Generic;
using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;

namespace CurlingSimulator.Simulation
{
    /// <summary>
    /// Manages all stones on the sheet: launching, sweeping, collision detection, and
    /// out-of-play detection. Owns the list of active StoneControllers.
    /// GameManager drives this; nothing else should call LaunchStone or ApplySweep directly.
    /// </summary>
    public class StoneSimulator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private StoneSimConfig _config;
        [SerializeField] private GameObject     _redStonePrefab;
        [SerializeField] private GameObject     _yellowStonePrefab;
        [SerializeField] private Transform      _redHack;
        [SerializeField] private Transform      _yellowHack;

        // ── Public state ──────────────────────────────────────────────────────────
        public bool IsAnyStoneMoving { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action                              OnAllStonesStopped;
        public event Action<StoneController>             OnStoneOutOfPlay;
        public event Action<StoneController, StoneController> OnStoneCollision;

        // ── Private ───────────────────────────────────────────────────────────────
        private readonly List<StoneController> _allStones    = new List<StoneController>();
        private readonly List<StoneController> _movingStones = new List<StoneController>();

        // Stone pools: 8 per team, pre-instantiated and parked off-sheet
        private readonly List<StoneController> _redPool    = new List<StoneController>();
        private readonly List<StoneController> _yellowPool = new List<StoneController>();

        private static readonly Vector3 ParkPosition = new Vector3(100f, 0f, 100f);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            for (int i = 0; i < 8; i++)
            {
                _redPool.Add(SpawnStone(_redStonePrefab, TeamId.Red, i));
                _yellowPool.Add(SpawnStone(_yellowStonePrefab, TeamId.Yellow, i));
            }
        }

        private StoneController SpawnStone(GameObject prefab, TeamId team, int index)
        {
            var go = Instantiate(prefab, ParkPosition, Quaternion.identity, transform);
            go.name = $"Stone_{team}_{index:00}";
            var ctrl = go.GetComponent<StoneController>();

            var initialState = new StoneState
            {
                Position  = new Vector2(ParkPosition.x, ParkPosition.z),
                Velocity  = Vector2.zero,
                Owner     = team,
                StoneIndex = index,
                IsMoving  = false,
                IsInPlay  = false
            };
            ctrl.Initialize(initialState, _config);
            ctrl.OnStopped += HandleStoneStopped;
            return ctrl;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Activates the next stone for the given team and launches it from the hack.
        /// The stone index used is equal to (throwIndex / 2) — each team throws 8 stones.
        /// </summary>
        public void LaunchStone(ThrowData throwData, int stoneIndexForTeam)
        {
            var pool = throwData.Thrower == TeamId.Red ? _redPool : _yellowPool;
            var ctrl = pool[stoneIndexForTeam];

            // Bring it to the hack position
            Transform hack     = throwData.Thrower == TeamId.Red ? _redHack : _yellowHack;
            Vector3   hackPos  = hack.position;

            float launchSpeed = Mathf.Lerp(_config.MinLaunchSpeed, _config.MaxLaunchSpeed, throwData.Power);

            // Determine curl sign:
            // Red throwing toward far house: InTurn curls left (−X), OutTurn curls right (+X)
            // Yellow throwing from the other hack: mirrored
            float curlSign;
            if (throwData.Thrower == TeamId.Red)
                curlSign = throwData.Curl == CurlDirection.InTurn ? -1f : 1f;
            else
                curlSign = throwData.Curl == CurlDirection.InTurn ? 1f : -1f;

            // Initialise position at the hack then launch
            var state = new StoneState
            {
                Position   = new Vector2(hackPos.x, hackPos.z),
                Velocity   = Vector2.zero,
                Owner      = throwData.Thrower,
                StoneIndex = stoneIndexForTeam,
                IsMoving   = true,
                IsInPlay   = true
            };
            ctrl.Initialize(state, _config);
            ctrl.Launch(launchSpeed, throwData.DirectionAngle, curlSign);

            _allStones.Add(ctrl);
            _movingStones.Add(ctrl);
            IsAnyStoneMoving = true;
        }

        /// <summary>
        /// Passes sweep intensity to all currently moving stones.
        /// Called each frame by GameManager while phase == StoneInMotion.
        /// </summary>
        public void ApplySweep(SweepData sweepData)
        {
            foreach (var stone in _movingStones)
                stone.SetSweepIntensity(sweepData.Intensity);
        }

        /// <summary>Returns a snapshot of all stone states currently in play.</summary>
        public List<StoneState> GetCurrentStoneStates()
        {
            var result = new List<StoneState>(_allStones.Count);
            foreach (var s in _allStones)
                if (s.State.IsInPlay)
                    result.Add(s.State);
            return result;
        }

        /// <summary>Parks all stones off-sheet and clears the in-play list for a new end.</summary>
        public void ResetSheet()
        {
            foreach (var stone in _allStones)
            {
                stone.ForceStop();
                stone.transform.position = ParkPosition;
            }
            _allStones.Clear();
            _movingStones.Clear();
            IsAnyStoneMoving = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            if (_movingStones.Count == 0) return;

            CheckBoundaries();
            CheckCollisions();
        }

        private void CheckBoundaries()
        {
            for (int i = _movingStones.Count - 1; i >= 0; i--)
            {
                var stone = _movingStones[i];
                var pos   = stone.State.Position;

                bool outOfBounds = pos.y < _config.BackLineDistance
                                || Mathf.Abs(pos.x) > _config.SheetHalfWidth;

                if (outOfBounds)
                {
                    stone.ForceStop();
                    _movingStones.RemoveAt(i);
                    _allStones.Remove(stone);
                    OnStoneOutOfPlay?.Invoke(stone);
                }
            }
        }

        private void CheckCollisions()
        {
            for (int i = 0; i < _movingStones.Count; i++)
            {
                var moving = _movingStones[i];

                // Check against all other stones (both moving and stationary)
                foreach (var other in _allStones)
                {
                    if (other == moving) continue;

                    if (!CollisionResolver.AreColliding(
                            moving.State.Position, other.State.Position, _config.StoneRadius))
                        continue;

                    // Resolve
                    var (v1, v2) = CollisionResolver.Resolve(
                        moving.State.Position, moving.State.Velocity,
                        other.State.Position,  other.State.Velocity,
                        _config.CollisionRestitution);

                    var (p1, p2) = CollisionResolver.Separate(
                        moving.State.Position, other.State.Position, _config.StoneRadius);

                    moving.SetStateFromCollision(p1, v1);
                    other.SetStateFromCollision(p2, v2);

                    // If the struck stone is now moving, track it
                    if (!_movingStones.Contains(other) && other.State.IsMoving)
                        _movingStones.Add(other);

                    OnStoneCollision?.Invoke(moving, other);
                }
            }
        }

        private void HandleStoneStopped(StoneController stone)
        {
            _movingStones.Remove(stone);

            if (_movingStones.Count == 0)
            {
                IsAnyStoneMoving = false;
                OnAllStonesStopped?.Invoke();
            }
        }
    }
}
