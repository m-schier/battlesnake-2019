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
    class CombinedDeltaLengthFoodMetric : IMetric {

        public float DeltaXScale { get; set;} = 0.2f;

        public float FoodDistanceDecay { get; set; } = 0.2f;

        private float ScoreDeltaAdvantage(float delta) {
            return (float) ((1.0 / (1.0 + Math.Exp(-(delta * DeltaXScale))) - 0.5) * 2.0);
        }

        private float FoodDistanceMetric(CachedMetricState state) {
            var floodBoard = state.OwnFill;

            int bestOwnDistance = int.MaxValue;

            foreach (var fruit in state.World.Fruits) {
                var dist = floodBoard.Distances[fruit.Y, fruit.X];

                if (dist <= 0) continue;

                bestOwnDistance = Math.Min(bestOwnDistance, dist);
            }

            if (bestOwnDistance == int.MaxValue) {
                return 0;
            }

            return (float) Math.Exp(-FoodDistanceDecay * bestOwnDistance);
        }

        public float ScoreCached(CachedMetricState state) {
            float deltaAdvantage = state.OwnSnake.Length - state.EnemySnake.Length;

            float deltaMetric = ScoreDeltaAdvantage(deltaAdvantage);
            float deltaOnEatMetric = ScoreDeltaAdvantage(deltaAdvantage + 1);

            // If food distance metric is 1, want to be exactly as good as eating one
            // fruit. Since food distance is always less than 1, ensures that eating
            // is always better than guarding with proximity of 1.
            float foodMetricMultiplier = deltaOnEatMetric - deltaMetric;

            return deltaMetric + FoodDistanceMetric(state) * foodMetricMultiplier;
        }
    }
}