using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Core
{
    /// <summary>
    /// Central game state machine. Drives the entire match loop.
    /// Never polls input directly — it receives events from IInputProvider implementations.
    ///
    /// Dependencies (assign in Inspector or via InputRouter):
    ///   - StoneSimulator
    ///   - IInputProvider for each team (PlayerInputProvider or AIInputProvider)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private StoneSimulator _stoneSimulator;

        [Header("Sheet geometry (must match StoneSimConfig)")]
        [SerializeField] private Vector2 _buttonCenter = Vector2.zero;
        [SerializeField] private float   _houseRadius  = 1.829f;
        [SerializeField] private float   _stoneRadius  = 0.145f;

        [Header("Timing")]
        [SerializeField] private float _endScoringDisplayTime = 3f;
        [SerializeField] private float _endTransitionTime     = 2f;

        // ── Public state ──────────────────────────────────────────────────────────
        public MatchState  CurrentMatch { get; private set; }
        public GamePhase   Phase        => CurrentMatch?.Phase ?? GamePhase.MatchSetup;

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<GamePhase>       OnPhaseChanged;
        public event Action<EndScoreResult>  OnEndScored;
        public event Action<MatchState>      OnMatchOver;

        // ── Private ───────────────────────────────────────────────────────────────
        private MatchConfig     _config;
        private IInputProvider  _redInput;
        private IInputProvider  _yellowInput;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _stoneSimulator.OnAllStonesStopped += HandleAllStonesStopped;
        }

        /// <summary>
        /// Called by the main menu or an InputRouter to start a match.
        /// redInput / yellowInput should already have their events wired up.
        /// </summary>
        public void StartMatch(MatchConfig config, IInputProvider redInput, IInputProvider yellowInput)
        {
            _config      = config;
            _redInput    = redInput;
            _yellowInput = yellowInput;

            WireInputEvents();

            CurrentMatch = new MatchState
            {
                CurrentEnd       = 1,
                CurrentThrowIndex = 0,
                HammerTeam       = config.FirstHammer,
                ThrowingTeam     = OppositeTeam(config.FirstHammer),
                Phase            = GamePhase.ThrowAim
            };

            _stoneSimulator.ResetSheet();
            BeginThrowPhase();
        }

        // ── Private: state transitions ────────────────────────────────────────────

        private void BeginThrowPhase()
        {
            SetPhase(GamePhase.ThrowAim);

            // Build a context ThrowData so the provider knows who is throwing
            var ctx = new ThrowData
            {
                Thrower    = CurrentMatch.ThrowingTeam,
                ThrowIndex = CurrentMatch.CurrentThrowIndex
            };

            GetInputFor(CurrentMatch.ThrowingTeam).BeginThrowInput(ctx);
        }

        private void HandleThrowCommitted(ThrowData data)
        {
            // Guard: only accept throws when we're in the aim phase
            if (CurrentMatch.Phase != GamePhase.ThrowAim) return;

            SetPhase(GamePhase.ThrowRelease);

            // Stone index = how many stones this team has thrown so far
            int stoneIndex = CurrentMatch.CurrentThrowIndex / 2;
            _stoneSimulator.LaunchStone(data, stoneIndex);

            SetPhase(GamePhase.StoneInMotion);

            // Notify the sweeper input to start tracking
            GetInputFor(CurrentMatch.ThrowingTeam).BeginSweepInput();
        }

        private void HandleSweepUpdate(SweepData data)
        {
            if (CurrentMatch.Phase != GamePhase.StoneInMotion) return;
            _stoneSimulator.ApplySweep(data);
        }

        private void HandleAllStonesStopped()
        {
            // Stop sweep input
            GetInputFor(CurrentMatch.ThrowingTeam).EndSweepInput();

            SetPhase(GamePhase.EndThrowEvaluation);

            CurrentMatch.CurrentThrowIndex++;

            // Alternate throwing team each throw
            CurrentMatch.ThrowingTeam = CurrentMatch.ThrowingTeam == CurrentMatch.HammerTeam
                ? CurrentMatch.NonHammerTeam
                : CurrentMatch.HammerTeam;

            if (CurrentMatch.IsEndComplete)
            {
                StartCoroutine(ScoreEnd());
            }
            else
            {
                BeginThrowPhase();
            }
        }

        private IEnumerator ScoreEnd()
        {
            SetPhase(GamePhase.EndScoring);

            var finalStones = _stoneSimulator.GetCurrentStoneStates();
            var result      = ScoringSystem.CalculateEndScore(
                finalStones, _buttonCenter, _houseRadius, _stoneRadius);
            result.EndNumber = CurrentMatch.CurrentEnd;
            result.FinalStonePositions = new List<StoneState>(finalStones);

            // Apply score to match state
            if (result.ScoringTeam == TeamId.Red)
            {
                CurrentMatch.RedScoreByEnd[CurrentMatch.CurrentEnd - 1] = result.PointsScored;
                CurrentMatch.TotalScore[0] += result.PointsScored;
            }
            else if (result.ScoringTeam == TeamId.Yellow)
            {
                CurrentMatch.YellowScoreByEnd[CurrentMatch.CurrentEnd - 1] = result.PointsScored;
                CurrentMatch.TotalScore[1] += result.PointsScored;
            }

            // Loser of end gets hammer next end (blank end: hammer stays)
            if (result.ScoringTeam != TeamId.None)
                CurrentMatch.HammerTeam = OppositeTeam(result.ScoringTeam);

            OnEndScored?.Invoke(result);

            yield return new WaitForSeconds(_endScoringDisplayTime);

            SetPhase(GamePhase.EndTransition);
            yield return new WaitForSeconds(_endTransitionTime);

            CurrentMatch.CurrentEnd++;
            CurrentMatch.CurrentThrowIndex = 0;

            if (CurrentMatch.CurrentEnd > _config.TotalEnds)
            {
                SetPhase(GamePhase.MatchOver);
                OnMatchOver?.Invoke(CurrentMatch);
            }
            else
            {
                _stoneSimulator.ResetSheet();

                // Non-hammer team always throws first
                CurrentMatch.ThrowingTeam = CurrentMatch.NonHammerTeam;
                BeginThrowPhase();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetPhase(GamePhase phase)
        {
            CurrentMatch.Phase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private IInputProvider GetInputFor(TeamId team)
            => team == TeamId.Red ? _redInput : _yellowInput;

        private static TeamId OppositeTeam(TeamId team)
            => team == TeamId.Red ? TeamId.Yellow : TeamId.Red;

        private void WireInputEvents()
        {
            _redInput.OnThrowCommitted  += HandleThrowCommitted;
            _redInput.OnSweepUpdate     += HandleSweepUpdate;
            _yellowInput.OnThrowCommitted += HandleThrowCommitted;
            _yellowInput.OnSweepUpdate    += HandleSweepUpdate;
        }

        private void OnDestroy()
        {
            if (_redInput != null)
            {
                _redInput.OnThrowCommitted    -= HandleThrowCommitted;
                _redInput.OnSweepUpdate       -= HandleSweepUpdate;
            }
            if (_yellowInput != null)
            {
                _yellowInput.OnThrowCommitted -= HandleThrowCommitted;
                _yellowInput.OnSweepUpdate    -= HandleSweepUpdate;
            }
        }
    }
}
