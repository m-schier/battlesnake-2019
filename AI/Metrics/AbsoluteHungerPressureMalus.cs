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

namespace BattleSnake.AI.Metrics {
    public class AbsoluteHungerPressureMalus {

        public float BaseReserve { get; set; } = 10.0f;

        public float DistanceMultiplier { get; set; } = 1.2f;

        public int SatisfactionThreshold { get; set; } = 75;

        public float Score(CachedMultiMetricState s, int index) {

            if (s.World.Snakes[index].Health > SatisfactionThreshold) return 1.0f;

            int bestOwnDistance = int.MaxValue;

            foreach (var fruit in s.World.Fruits) {
                ref var tile = ref s.AdversarialFill.Tiles[fruit.Y, fruit.X];
                if (tile.Snake == index) {
                    bestOwnDistance = Math.Min(bestOwnDistance, tile.Distance);
                }
            }

            float ownFruitPressure = Math.Min(s.World.Snakes[index].Health - bestOwnDistance * DistanceMultiplier - BaseReserve, 0);

            // Map [-Inf, 0] -> [0, 0.5] to [-1, 0] on logistic function
            float logistic = (float)Math.Pow(0.85, -ownFruitPressure);
            float score = (logistic - 0.5f) * 2.0f;

            Debug.Assert(!float.IsNaN(score));

            return score;
        }
    }
}