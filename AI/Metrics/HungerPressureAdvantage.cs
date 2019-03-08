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

using System;

namespace BattleSnake.AI.Metrics {
    class HungerPressureAdvantage : IMetric {

        public int SatisfactionThreshold { get; set; } = 75;

        public float ScoreCached(CachedMetricState state) {

            var floodBoard = state.AdversarialFill.Tiles;

            int bestOwnDistance = int.MaxValue;
            int bestEnemyDistance = int.MaxValue;

            foreach (var fruit in state.World.Fruits) {
                ref var tile = ref floodBoard[fruit.Y, fruit.X];
                if (tile.Snake == state.OwnIndex) {
                    bestOwnDistance = Math.Min(bestOwnDistance, tile.Distance);
                } else if (tile.Snake == state.EnemyIndex) {
                    bestEnemyDistance = Math.Min(bestEnemyDistance, tile.Distance);
                }
            }

            float ownFruitPressure = Math.Min(state.OwnSnake.Health - bestOwnDistance * 1.2f - 10.0f, 0);
            float enemyFruitPressure = Math.Min(state.EnemySnake.Health - bestEnemyDistance * 1.2f - 10.0f, 0);

            // Always satisfied with rather high health, even if no fruit available, to prevent guarding when we want to eat
            if (state.OwnSnake.Health > SatisfactionThreshold) ownFruitPressure = 0.0f;
            if (state.EnemySnake.Health > SatisfactionThreshold) enemyFruitPressure = 0.0f;

            if (ownFruitPressure == enemyFruitPressure) return 0.0f;

            // Lower is worse
            return (enemyFruitPressure / (ownFruitPressure + enemyFruitPressure) - 0.5f) * 2.0f;
        }
    }
}