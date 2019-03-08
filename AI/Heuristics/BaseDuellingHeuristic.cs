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

using BattleSnake.InternalModel;

namespace BattleSnake.AI.Heuristics {

    public abstract class BaseDuellingHeuristic : IHeuristic<FastWorld> {
        protected readonly int snakeIndex;
        protected readonly int enemyIndex;

        public BaseDuellingHeuristic(int snakeIndex, int enemyIndex) {
            this.snakeIndex = snakeIndex;
            this.enemyIndex = enemyIndex;
        }

        public bool IsTerminal(FastWorld state) {
            return !state.Snakes[snakeIndex].Alive || !state.Snakes[enemyIndex].Alive;
        }

        public static int ScoreKillReason(InternalModel.Status status) {
            // Higher is worse
            switch (status) {
                case Status.KilledHeadOnHead: return 1;
                case Status.KilledStarvation: return 2;
                case Status.KilledEnemyBody: return 2;
                case Status.KilledOwnBody: return 3;
                case Status.KilledWall: return 3;
                default: return 0;
            }
        }

        public float Score(FastWorld state) {
            var me = state.Snakes[snakeIndex];
            var enemy = state.Snakes[enemyIndex];

            // Try to win as early as possible and lose or draw as late as possible
            // Also try to prefer death reasons which are not completely deterministic
            if (!me.Alive && !enemy.Alive) return -900.0f + state.Turn / 100.0f;
            if (!me.Alive) return -1000.0f + state.Turn / 100.0f - ScoreKillReason(me.Status);
            if (!enemy.Alive) return 1000.0f - state.Turn / 100.0f;

            var cache = new CachedMetricState(state, snakeIndex, enemyIndex);

            return ScoreDetail(cache);
        }

        protected abstract float ScoreDetail(CachedMetricState state);
    }
}