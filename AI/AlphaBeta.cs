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
using System.Collections.Generic;
using BattleSnake.InternalModel;
using BattleSnake.ApiModel;

namespace BattleSnake.AI {

    public class AlphaBeta : DeepeningSearch {

        public static Tuple<Direction, float> BestWithHeuristic(Configuration c, FastWorld w, int maxDepth, int currentDepth, float alpha, float betaInitial) {

            // Fail fast if terminal or depth exhausted
            bool terminal = c.Heuristic.IsTerminal(w);
            bool limitReached = currentDepth >= maxDepth;

            if (terminal || limitReached) {
                var score = c.Heuristic.Score(w);
                return Tuple.Create(Direction.North, score);
            }

            Direction bestOwnDirection = Direction.North;

            var desiredMoves = new List<Direction>(w.Snakes.Count);
            for (int i = 0; i < w.Snakes.Count; ++i) {
                // Use the given decision functions for all snakes first
                desiredMoves.Add(c.UntargetedDecisionFunction(w, i));
            }

            // Initially have not checked any own moves. We must always check at least one
            // available action to allow the heuristic to evaluate one leaf, even if we know
            // it leads to death. Otherwise would return the theoretical heuristic min value (-Inf)
            // not the practical lower bound as implemented
            bool checkedOwnMove = false;

            for (int i = 0; i < Util.Directions.Length; ++i) {

                // If this is the last available action and we have not evaluated any actions,
                // must evaluate.
                bool mustEvaluateOwn = !checkedOwnMove && i == Util.Directions.Length - 1;

                // Skip guaranteed deadly immediately
                if (!mustEvaluateOwn && w.CertainlyDeadly(c.OwnIndex, Util.Directions[i])) {
                    continue;
                }

                checkedOwnMove = true;

                // Must reset beta to initial beta value of this node, otherwise using updated beta from different sub tree
                // Beta stores the best move possible for the opponent
                var beta = betaInitial;

                bool checkedEnemyMove = true;

                for (int j = 0; j < Util.Directions.Length; ++j) {

                    // Check stop request in inner loop
                    if (c.Stop.StopRequested) throw new StopSearchException();

                    bool mustEvaluateEnemy = !checkedEnemyMove && j == Util.Directions.Length - 1;

                    // Skip guaranteed deadly immediately
                    if (!mustEvaluateEnemy && w.CertainlyDeadly(c.EnemyIndex, Util.Directions[j])) {
                        continue;
                    }

                    desiredMoves[c.OwnIndex] = Util.Directions[i];
                    desiredMoves[c.EnemyIndex] = Util.Directions[j];

                    var worldInstance = w.Clone() as FastWorld;
                    worldInstance.UpdateMovementTick(desiredMoves);

                    var tuple = BestWithHeuristic(c, worldInstance, maxDepth, currentDepth + 1, alpha, beta);

                    if (tuple.Item2 < beta) {
                        beta = tuple.Item2;
                    }

                    // If the best move possible is worse for the first player than the current worst,
                    // stop, no need to find even worse moves
                    if (alpha >= beta) {
                        // Alpha cut-off
                        break;
                    }
                }

                if (beta > alpha) {
                    alpha = beta;
                    bestOwnDirection = Util.Directions[i];
                }

                // If our best move is even better than the current choice of the opponent
                // stop, no need to find even better moves
                // Of course have to compare to initial beta value here
                if (alpha >= betaInitial) {
                    // Beta cut-off
                    break;
                }
            }

            return Tuple.Create(bestOwnDirection, alpha);
        }

        public override Tuple<Direction, float> Search(Configuration c, FastWorld root, int depth) {
            return BestWithHeuristic(c, root, depth, 0, float.NegativeInfinity, float.PositiveInfinity);
        }
    }
}