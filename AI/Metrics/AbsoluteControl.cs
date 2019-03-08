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

using System.Diagnostics;

namespace BattleSnake.AI.Metrics {
    public class AbsoluteControl
    {
        public float Score(CachedMultiMetricState s, int index)
        {

            var adversarialFill = s.AdversarialFill.EmptyCounts;

            int friendlyCounter = adversarialFill[index];
            int sum = 0;

            for (int i = 0; i < adversarialFill.Count; ++i) {
                sum += adversarialFill[i];
            }

            if (sum == 0) return 0.0f;

            float score = ((friendlyCounter / (float)sum) - 0.5f) * 2.0f;

            Debug.Assert(!float.IsNaN(score));

            return score;
        }
    }
}