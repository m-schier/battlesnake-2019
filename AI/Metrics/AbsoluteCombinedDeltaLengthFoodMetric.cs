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
using System.Diagnostics;

using BattleSnake.InternalModel;

namespace BattleSnake.AI.Metrics {
    public class AbsoluteCombinedDeltaLengthFoodMetric {

        public float FruitMultiplierFactor { get; set;} = 1.0f;
        
        public float DeltaXScale { get; set;} = 0.2f;

        public float FoodDistanceDecay { get; set; } = 0.2f;

        private float ScoreDeltaAdvantage(float delta) {
            return (float) ((1.0 / (1.0 + Math.Exp(-(delta * DeltaXScale))) - 0.5) * 2.0);
        }

        private float FoodDistanceMetric(FastWorld w, int ownIndex, Util.FillResult fill) {

            int bestOwnDistance = int.MaxValue;

            foreach (var fruit in w.Fruits) {
                var dist = fill.Distances[fruit.Y, fruit.X];

                if (dist <= 0) continue;

                bestOwnDistance = Math.Min(bestOwnDistance, dist);
            }

            if (bestOwnDistance == int.MaxValue) {
                return 0;
            }

            return (float) Math.Exp(-FoodDistanceDecay * bestOwnDistance);
        }

        public float Score(FastWorld w, int ownIndex, Util.FillResult fill) {

            int maxLength = 0;
            int sumLength = 0;
            int otherCount = 0;

            for (int i = 0; i < w.Snakes.Count; ++i) {
                if (i == ownIndex) continue;
                if (!w.Snakes[i].Alive) continue;

                sumLength += w.Snakes[i].Length;
                otherCount += 1;

                if (w.Snakes[i].Length > maxLength) {
                    maxLength = w.Snakes[i].Length;
                }
            }

            float averageLength = sumLength / (float) otherCount;

            float interpLength = 0.9f * maxLength + 0.1f * averageLength;

            float deltaAdvantage = w.Snakes[ownIndex].Length - interpLength;

            float deltaMetric = ScoreDeltaAdvantage(deltaAdvantage);
            float deltaOnEatMetric = ScoreDeltaAdvantage(deltaAdvantage + 1);

            // If food distance metric is 1, want to be exactly as good as eating one
            // fruit. Since food distance metric is always less than 1, ensures that eating
            // is always better than guarding with proximity of 1.
            float foodMetricMultiplier = (deltaOnEatMetric - deltaMetric) * FruitMultiplierFactor;

            float score = deltaMetric + FoodDistanceMetric(w, ownIndex, fill) * foodMetricMultiplier;

            Debug.Assert(!float.IsNaN(score));

            return score;
        }
    }
}