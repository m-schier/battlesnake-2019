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
using BattleSnake.ApiModel;

namespace BattleSnake.AI.Heuristics {
    /// <summary>
    /// This heuristic predicts the outcome of the given action and the current state
    /// without simulating the new state. Therefore it does not implement IHeuristic
    /// and cannot be used as a base heuristic for tree search.
    /// </summary>
    public class ReflexEvasionHeuristic {
        public static float Score(FastWorld w, int ownIndex, Direction move) {
            // If known to be deadly return minimum
            if (w.CertainlyDeadly(ownIndex, move)) return -1000.0f;

            var newHead = w.Snakes[ownIndex].Head.Advanced(move);

            float enemyCollisionScore = 0.0f;

            // If stepping on snake, punish badly
            // This always means stepping on another snake, as
            // CertainlyDeadly already checks self
            if (w[newHead].occupant == FastWorld.Occupant.Snake) {
                enemyCollisionScore -= 1.0f;
            }

            float fruitScore = 0.0f;

            // Reward stepping on fruit
            if (w[newHead].occupant == FastWorld.Occupant.Fruit) {
                fruitScore = 1.0f;
            }

            float potentialCollisionScore = 0.0f;

            // Punish proximity to enemy that may kill us
            for (int i = 0; i < w.Snakes.Count; ++i) {
                if (!w.Snakes[i].Alive || i == ownIndex) continue;

                var manhattan = Coord.ManhattanDistance(w.Snakes[i].Head, newHead);

                if (manhattan == 1) {
                    // TODO: Delta does not take into account growing yet
                    var delta = w.Snakes[ownIndex].Length - w.Snakes[i].Length;
                    if (delta == 0) {
                        // Punish potential tie collision
                        potentialCollisionScore -= 0.5f;
                    } else if (delta > 0) {
                        // Reward potential collision with smaller snake
                        // less than potential tie
                        potentialCollisionScore += 0.3f;
                    } else {
                        // Punish potential collision with larger snake
                        potentialCollisionScore -= 1.0f;
                    }
                }
            }

            // Asume holding current direction
            float holdScore = move == w.Snakes[ownIndex].LastDirection ? 1.0f : 0.0f;

            return holdScore * 1.0f
                + fruitScore * 3.0f
                + potentialCollisionScore * 10.0f
                + enemyCollisionScore * 30.0f;
        }
    }
}