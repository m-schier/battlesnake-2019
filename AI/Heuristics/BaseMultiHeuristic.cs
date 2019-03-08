/**
 *  BattleSnake 2019 submission, AI program for multi agent snake game
 *  Copyright (C) 2019 Maximilian Schier, Frederick Schubert and Niclas Wüstenbecker
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

namespace BattleSnake.AI.Heuristics {

    public abstract class BaseMultiHeuristic : IMultiHeuristic {
        
        protected int BountySnake;

        public BaseMultiHeuristic(int bountySnake) {
            BountySnake = bountySnake;
        }

        public bool IsTerminal(CachedMultiMetricState state) {
            return state.World.IsDecided || !state.World.Snakes[BountySnake].Alive;
        }

        public float Score(CachedMultiMetricState state, int index) {
            var me = state.World.Snakes[index];

            float score = 0.0f;

            if (!state.World.Snakes[BountySnake].Alive && index != BountySnake) {
                // Be really pessimistic everybody hates us
                score += 2000.0f;
            }

            // Try to win as early as possible and lose or draw as late as possible
            // Also try to prefer death reasons which are not completely deterministic
            if (!me.Alive) {
                return score - 1000.0f + state.World.Turn / 100.0f - BaseDuellingHeuristic.ScoreKillReason(me.Status);
            } else if (IsTerminal(state)) {
                // High score if we are the sole survivor, try to win earlier
                return score + 1000.0f - state.World.Turn / 100.0f;
            }

            // Else evaluate detailed heuristic if no terminal
            return ScoreDetail(state, index);
        }

        protected abstract float ScoreDetail(CachedMultiMetricState state, int index);
    }
}