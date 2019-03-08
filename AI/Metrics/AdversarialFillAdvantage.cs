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

namespace BattleSnake.AI.Metrics {
    class AdversarialFillAdvantage : IMetric {
        public float ScoreCached(CachedMetricState state) {
            var floodBoard = state.AdversarialFill.Tiles;

            int friendlyCounter = 0;
            int enemyCounter = 0;

            for (int i = 0; i < floodBoard.GetLength(0); ++i) {
                for (int j = 0; j < floodBoard.GetLength(1); ++j) {
                    if (floodBoard[i, j].Snake == state.OwnIndex) friendlyCounter++;
                    else if (floodBoard[i, j].Snake == state.EnemyIndex) enemyCounter++;
                }
            }

            return (((float) friendlyCounter / (enemyCounter + friendlyCounter)) - 0.5f) * 2.0f;
        }
    }
}