using System;
using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Shared base for all AI difficulty tiers.
    ///
    /// Computes an ideal ThrowData for a given intent, then applies Gaussian noise
    /// scaled by AccuracyBias (0 = perfect, 1 = very noisy).
    ///
    /// Subclasses only need to override AccuracyBias and optionally SweepQuality.
    /// </summary>
    public abstract class BaseAIStrategy : IAIStrategy
    {
        /// <summary>0 = perfect accuracy, 1 = maximum noise. Set per difficulty tier.</summary>
        protected abstract float AccuracyBias { get; }

        /// <summary>0–1 fraction of optimal sweep intensity the AI applies.</summary>
        protected virtual float SweepQuality => 0f;

        // Sheet geometry — injected by AIInputProvider
        protected Vector2 ButtonCenter;
        protected float   HouseRadius;
        protected float   StoneRadius;

        public void InjectSheetGeometry(Vector2 buttonCenter, float houseRadius, float stoneRadius)
        {
            ButtonCenter = buttonCenter;
            HouseRadius  = houseRadius;
            StoneRadius  = stoneRadius;
        }

        // ─────────────────────────────────────────────────────────────────────────

        public virtual ThrowData CalculateThrow(SheetState sheetState, ThrowIntent intent)
        {
            ThrowData ideal = ComputeIdealThrow(sheetState, intent);
            return ApplyNoise(ideal, AccuracyBias);
        }

        public SweepData CalculateSweep(StoneState movingStone, SheetState sheetState)
        {
            // Simple: AI sweeps at SweepQuality if stone is heading roughly toward the house
            float intensity = SweepQuality * ComputeSweepNeed(movingStone, sheetState);

            return new SweepData
            {
                Intensity  = intensity,
                DeltaTime  = Time.fixedDeltaTime,
                Timestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        // ── Ideal throw computation ───────────────────────────────────────────────

        private ThrowData ComputeIdealThrow(SheetState state, ThrowIntent intent)
        {
            switch (intent)
            {
                case ThrowIntent.Draw:
                    return BuildDrawThrow(state, targetOffset: Vector2.zero);

                case ThrowIntent.Guard:
                    // Place stone 4 m in front of the house button
                    return BuildDrawThrow(state, targetOffset: new Vector2(0f, 4f));

                case ThrowIntent.Takeout:
                    return BuildTakeoutThrow(state);

                case ThrowIntent.Peel:
                    // Aggressive — faster takeout
                    var peel = BuildTakeoutThrow(state);
                    peel.Power = 1f;
                    return peel;

                case ThrowIntent.Freeze:
                    return BuildFreezeThrow(state);

                default:
                    return BuildDrawThrow(state, Vector2.zero);
            }
        }

        private ThrowData BuildDrawThrow(SheetState state, Vector2 targetOffset)
        {
            Vector2 target = ButtonCenter + targetOffset;

            // Angle from the centre line to the target
            float angle = Mathf.Atan2(target.x, target.y) * Mathf.Rad2Deg;

            // Medium power draw
            float power = Mathf.Clamp01(0.55f + UnityEngine.Random.Range(-0.05f, 0.05f));

            // Prefer InTurn for Red, OutTurn for Yellow (arbitrary convention)
            var curl = state.AITeam == TeamId.Red
                ? CurlDirection.InTurn
                : CurlDirection.OutTurn;

            return new ThrowData
            {
                Power          = power,
                DirectionAngle = angle,
                Curl           = curl,
                Thrower        = state.AITeam,
                Timestamp      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private ThrowData BuildTakeoutThrow(SheetState state)
        {
            // Find the closest opponent stone in the house
            Vector2 targetPos = ButtonCenter;
            float   nearest   = float.MaxValue;

            foreach (var stone in state.Stones)
            {
                if (!stone.IsInPlay || stone.Owner == state.AITeam) continue;
                float d = Vector2.Distance(stone.Position, ButtonCenter);
                if (d < nearest)
                {
                    nearest   = d;
                    targetPos = stone.Position;
                }
            }

            float angle = Mathf.Atan2(targetPos.x, targetPos.y) * Mathf.Rad2Deg;
            var curl    = state.AITeam == TeamId.Red
                ? CurlDirection.InTurn
                : CurlDirection.OutTurn;

            return new ThrowData
            {
                Power          = 0.85f,   // hard enough to remove but not max
                DirectionAngle = angle,
                Curl           = curl,
                Thrower        = state.AITeam,
                Timestamp      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private ThrowData BuildFreezeThrow(SheetState state)
        {
            // Find the team's closest stone already in the house; draw to be touching it
            Vector2 targetPos = ButtonCenter;
            float   nearest   = float.MaxValue;

            foreach (var stone in state.Stones)
            {
                if (!stone.IsInPlay || stone.Owner != state.AITeam) continue;
                float d = Vector2.Distance(stone.Position, ButtonCenter);
                if (d < nearest)
                {
                    nearest   = d;
                    // Stop one diameter away from the existing stone toward the hack
                    targetPos = stone.Position + Vector2.up * (StoneRadius * 2f);
                }
            }

            float angle = Mathf.Atan2(targetPos.x, targetPos.y) * Mathf.Rad2Deg;
            var curl    = state.AITeam == TeamId.Red
                ? CurlDirection.InTurn
                : CurlDirection.OutTurn;

            return new ThrowData
            {
                Power          = 0.50f,
                DirectionAngle = angle,
                Curl           = curl,
                Thrower        = state.AITeam,
                Timestamp      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        // ── Noise ─────────────────────────────────────────────────────────────────

        private static ThrowData ApplyNoise(ThrowData ideal, float bias)
        {
            ideal.Power          = Mathf.Clamp01(ideal.Power + GaussNoise(bias * 0.15f));
            ideal.DirectionAngle = Mathf.Clamp(
                ideal.DirectionAngle + GaussNoise(bias * 3f), -8f, 8f);

            // Rare handle misread — flip curl
            if (UnityEngine.Random.value < bias * 0.05f)
                ideal.Curl = ideal.Curl == CurlDirection.InTurn
                    ? CurlDirection.OutTurn
                    : CurlDirection.InTurn;

            return ideal;
        }

        /// <summary>Approximate Gaussian noise using Box-Muller (polar form).</summary>
        private static float GaussNoise(float stdDev)
        {
            float u, v, s;
            do
            {
                u = UnityEngine.Random.Range(-1f, 1f);
                v = UnityEngine.Random.Range(-1f, 1f);
                s = u * u + v * v;
            }
            while (s >= 1f || s == 0f);

            return u * Mathf.Sqrt(-2f * Mathf.Log(s) / s) * stdDev;
        }

        // ── Sweep decision ────────────────────────────────────────────────────────

        private float ComputeSweepNeed(StoneState stone, SheetState sheet)
        {
            // Sweep if the stone is moving toward the house and likely to stop short
            if (!stone.IsMoving) return 0f;

            float distToButton = Vector2.Distance(stone.Position, sheet.ButtonCenter);
            float speed        = stone.Velocity.magnitude;

            // Simple heuristic: sweep if stone is within 10 m of the button and slow
            return (distToButton < 10f && speed < 1.5f) ? 1f : 0f;
        }
    }
}
