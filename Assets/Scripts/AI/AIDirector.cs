using System.Collections.Generic;
using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Selects the best ThrowIntent for the AI given the current sheet situation.
    /// Used by Medium and Hard difficulty strategies.
    /// Easy always draws regardless of sheet state.
    /// </summary>
    public class AIDirector
    {
        public ThrowIntent SelectIntent(SheetState state)
        {
            int aiStoneCount  = CountStonesInHouse(state, state.AITeam);
            int oppStoneCount = CountStonesInHouse(state, state.OpponentTeam);
            int stonesLeft    = state.StonesRemainingThisEnd;
            bool hasHammer    = state.AIHasHammer;

            // Last stone: if scoring, protect lead; if losing, take out opponent's stone
            if (stonesLeft <= 2)
            {
                if (aiStoneCount > 0 && aiStoneCount >= oppStoneCount)
                    return hasHammer ? ThrowIntent.Draw : ThrowIntent.Guard;
                if (oppStoneCount > 0)
                    return ThrowIntent.Takeout;
                return ThrowIntent.Draw;
            }

            // Mid-end: if opponent has lead stone, try to remove it
            if (IsOpponentLeading(state))
            {
                if (stonesLeft > 6) return ThrowIntent.Guard;
                return ThrowIntent.Takeout;
            }

            // AI is leading or even: protect with a guard or draw behind
            if (aiStoneCount > 0)
                return ThrowIntent.Guard;

            return ThrowIntent.Draw;
        }

        private bool IsOpponentLeading(SheetState state)
        {
            float aiNearest  = float.MaxValue;
            float oppNearest = float.MaxValue;

            foreach (var stone in state.Stones)
            {
                if (!stone.IsInPlay) continue;
                float dist = ScoringSystem.DistanceToButton(stone.Position, state.ButtonCenter);
                if (!ScoringSystem.IsStoneInHouse(stone.Position, state.ButtonCenter,
                        state.HouseRadius, state.StoneRadius))
                    continue;

                if (stone.Owner == state.AITeam)
                    aiNearest = Mathf.Min(aiNearest, dist);
                else
                    oppNearest = Mathf.Min(oppNearest, dist);
            }

            return oppNearest < aiNearest;
        }

        private int CountStonesInHouse(SheetState state, TeamId team)
        {
            int count = 0;
            foreach (var stone in state.Stones)
            {
                if (!stone.IsInPlay || stone.Owner != team) continue;
                if (ScoringSystem.IsStoneInHouse(stone.Position, state.ButtonCenter,
                        state.HouseRadius, state.StoneRadius))
                    count++;
            }
            return count;
        }
    }
}
