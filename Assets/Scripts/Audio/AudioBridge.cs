using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Audio
{
    /// <summary>
    /// Wires GameManager, StoneSimulator, and PlayerInputProvider events to AudioManager calls.
    /// AudioManager holds the AudioSources and clips; this component is the glue.
    ///
    /// Attach to the same GameObject as AudioManager (or any scene object).
    /// Assign _simulator and _playerInput in the Inspector.
    ///
    /// All audio calls are guarded by AudioManager.Instance null checks,
    /// so the bridge is safe even if AudioManager is temporarily absent.
    /// </summary>
    public class AudioBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private StoneSimulator     _simulator;
        [SerializeField] private PlayerInputProvider _playerInput;

        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (_simulator != null)
            {
                _simulator.OnStoneCollision   += OnCollision;
                _simulator.OnAllStonesStopped += OnAllStopped;
            }

            if (_playerInput != null)
            {
                _playerInput.OnThrowCommitted += OnThrowCommitted;
                _playerInput.OnSweepUpdate    += OnSweepUpdate;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
                GameManager.Instance.OnEndScored    += OnEndScored;
            }
        }

        private void OnDestroy()
        {
            if (_simulator != null)
            {
                _simulator.OnStoneCollision   -= OnCollision;
                _simulator.OnAllStonesStopped -= OnAllStopped;
            }

            if (_playerInput != null)
            {
                _playerInput.OnThrowCommitted -= OnThrowCommitted;
                _playerInput.OnSweepUpdate    -= OnSweepUpdate;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnEndScored    -= OnEndScored;
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────────

        private void OnCollision(StoneController a, StoneController b)
        {
            // Use combined incoming speed as a proxy for impact loudness
            float speed = (a.State.Velocity.magnitude + b.State.Velocity.magnitude) * 0.5f;
            AudioManager.Instance?.PlayStoneCollision(speed);
        }

        private void OnAllStopped()
        {
            AudioManager.Instance?.StopSweeping();
            AudioManager.Instance?.PlayStoneComeToRest();
        }

        private void OnThrowCommitted(ThrowData data)
        {
            AudioManager.Instance?.PlayThrowRelease(data.Power);
        }

        private void OnSweepUpdate(SweepData data)
        {
            AudioManager.Instance?.PlaySweeping(data.Intensity);
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            // Safety net: ensure sweep loop is stopped whenever we leave motion phase
            if (phase != GamePhase.StoneInMotion)
                AudioManager.Instance?.StopSweeping();
        }

        private void OnEndScored(EndScoreResult result)
        {
            AudioManager.Instance?.PlayEndScore(result.PointsScored);
        }
    }
}
