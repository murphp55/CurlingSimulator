using System;
using System.Collections;
using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Implements IInputProvider using an IAIStrategy.
    /// Fires OnThrowCommitted after a random think delay to feel more human.
    /// Continuously emits sweep data while sweeping is enabled.
    ///
    /// Assign in the Inspector:
    ///   - StoneSimulator (to read current sheet state for the AI)
    ///   - GameManager (to read MatchState)
    ///   - StoneSimConfig (for house geometry)
    ///   - AITeam
    ///   - Difficulty (sets strategy on Awake)
    /// </summary>
    public class AIInputProvider : MonoBehaviour, IInputProvider
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private TeamId           _aiTeam;
        [SerializeField] private AIDifficulty     _difficulty;
        [SerializeField] private StoneSimulator   _stoneSimulator;
        [SerializeField] private StoneSimConfig   _config;

        [Header("Think Time")]
        [SerializeField] private float _minThinkSeconds = 0.5f;
        [SerializeField] private float _maxThinkSeconds = 1.5f;

        // ── IInputProvider ────────────────────────────────────────────────────────
        public event Action<ThrowData> OnThrowCommitted;
        public event Action<SweepData> OnSweepUpdate;
        public bool IsSweepActive => _sweepEnabled;

        // ── Private ───────────────────────────────────────────────────────────────
        private BaseAIStrategy _strategy;
        private bool           _sweepEnabled;
        private ThrowData      _pendingContext;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _strategy = CreateStrategy(_difficulty);
            _strategy.InjectSheetGeometry(
                Vector2.zero,
                _config != null ? _config.HouseRadius : 1.829f,
                _config != null ? _config.StoneRadius  : 0.145f);
        }

        // ── IInputProvider ────────────────────────────────────────────────────────

        public void BeginThrowInput(ThrowData context)
        {
            _pendingContext = context;
            StartCoroutine(ThinkAndThrow());
        }

        public void BeginSweepInput()  => _sweepEnabled = true;
        public void EndSweepInput()    => _sweepEnabled = false;

        // ── Private ───────────────────────────────────────────────────────────────

        private IEnumerator ThinkAndThrow()
        {
            yield return new WaitForSeconds(
                UnityEngine.Random.Range(_minThinkSeconds, _maxThinkSeconds));

            SheetState sheet = BuildSheetState();
            ThrowData  data  = _strategy.CalculateThrow(sheet, ThrowIntent.Draw);

            data.Thrower    = _pendingContext.Thrower;
            data.ThrowIndex = _pendingContext.ThrowIndex;
            data.Timestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            OnThrowCommitted?.Invoke(data);
        }

        private void FixedUpdate()
        {
            if (!_sweepEnabled) return;

            SheetState sheet = BuildSheetState();

            // Find the moving stone owned by the AI team (or anyone — AI sweeps for its own stones)
            StoneState movingStone = default;
            bool found = false;
            foreach (var stone in sheet.Stones)
            {
                if (stone.IsMoving) { movingStone = stone; found = true; break; }
            }

            if (!found) return;

            SweepData sweep = _strategy.CalculateSweep(movingStone, sheet);
            OnSweepUpdate?.Invoke(sweep);
        }

        private SheetState BuildSheetState()
        {
            var gm     = GameManager.Instance;
            var match  = gm?.CurrentMatch;

            int[] score = match != null
                ? new[] { match.TotalScore[0], match.TotalScore[1] }
                : new[] { 0, 0 };

            return new SheetState
            {
                Stones                  = _stoneSimulator != null
                    ? _stoneSimulator.GetCurrentStoneStates()
                    : new System.Collections.Generic.List<StoneState>(),
                StonesRemainingThisEnd  = match != null ? 16 - match.CurrentThrowIndex : 16,
                AITeam                  = _aiTeam,
                OpponentTeam            = _aiTeam == TeamId.Red ? TeamId.Yellow : TeamId.Red,
                CurrentEnd              = match?.CurrentEnd ?? 1,
                CurrentScore            = score,
                AIHasHammer             = match?.HammerTeam == _aiTeam,
                ButtonCenter            = Vector2.zero,
                HouseRadius             = _config != null ? _config.HouseRadius : 1.829f,
                StoneRadius             = _config != null ? _config.StoneRadius  : 0.145f
            };
        }

        private static BaseAIStrategy CreateStrategy(AIDifficulty difficulty) =>
            difficulty switch
            {
                AIDifficulty.Easy   => new EasyAIStrategy(),
                AIDifficulty.Hard   => new HardAIStrategy(),
                _                   => new MediumAIStrategy()
            };
    }
}
