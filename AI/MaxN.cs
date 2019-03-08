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
using System.Diagnostics;
using System.Linq;
using System.Threading;

using BattleSnake.AI.Heuristics;
using BattleSnake.InternalModel;
using BattleSnake.Misc;
using BattleSnake.ApiModel;

namespace BattleSnake.AI {
    public class MaxN {

        public class Result {

            public Result(int nodesSimulated, int snakesSimulated, int depth, Direction move, float score, List<float> scoresAbs, List<float> scoresRel, List<Direction> directions) {
                NodesSimulated = nodesSimulated;
                SnakesSimulated = snakesSimulated;
                Depth = depth;
                Move = move;
                Score = score;
                ScoresRel = scoresRel;
                ScoresAbs = scoresAbs;
                Directions = directions;
                UsedAlphaBetaFallback = false;
            }

            public Result(DeepeningSearch.Result r) {
                // TODO
                Depth = r.Depth;
                Score = r.Score;
                Move = r.Move;
                UsedAlphaBetaFallback = true;
            }

            public int NodesSimulated { get; }
            public int SnakesSimulated { get; }
            public int Depth { get; }
            public Direction Move { get; }
            public float Score { get; }
            public List<float> ScoresRel { get; }
            public List<float> ScoresAbs { get; }
            public List<Direction> Directions { get; }

            public bool UsedAlphaBetaFallback { get; }

            public override string ToString() {
                return $"Move={Move}, Score={Score}, Depth={Depth}, Snakes={SnakesSimulated}, Nodes={NodesSimulated}, ScoresAbs={(ScoresAbs == null ? null : ScoresAbs.FormatEnumerable())}, ScoresRel={(ScoresRel == null ? null : ScoresRel.FormatEnumerable())}, Directions={(Directions == null ? null : Directions.FormatEnumerable())}, abFallback={UsedAlphaBetaFallback}";
            }
        }

        private static bool HasCloserPart(FastSnake s, Coord target, int maxDistance) {
            // Enumerate all parts
            foreach (var part in s.EnumerateParts()) {
                // Check manhattan distance to threshold
                if (Coord.ManhattanDistance(target, part) <= maxDistance) {
                    return true;
                }
            }

            return false;
        }

        private static List<bool> FindMaskForDepth(FastWorld w, int ownIndex, int depth) {
            // Initially assume all masked
            var result = Extensions.FilledList(w.Snakes.Count, true);

            // Want at most this distance to any part of each snake to fully simulate it
            var maximumDesiredDistance = depth * 2;

            Coord ownHead = w.Snakes[ownIndex].Head;

            for (int i = 0; i < w.Snakes.Count; ++i) {
                // Self is never masked
                if (i == ownIndex) {
                    result[i] = false;
                    continue;
                }

                // What is dead may never die
                if (!w.Snakes[i].Alive) continue;

                result[i] = !HasCloserPart(w.Snakes[i], ownHead, maximumDesiredDistance);
            }

            return result;
        }

        public static Result Search(Configuration c, FastWorld w, int ownIndex) {
            if (c.SearchLimit.LimitType == DeepeningSearch.LimitType.Depth) {
                return SearchSelectingStrategy(c, w, ownIndex, c.SearchLimit.Limit, new DeepeningSearch.Stop());
            } else {
                return BestFixedTime(c, w, ownIndex);
            }
        }

        private static Result SearchSelectingStrategy(Configuration c, FastWorld w, int ownIndex, int depth, DeepeningSearch.Stop stop) {
            var reflexMask = c.ReflexMask ?? FindMaskForDepth(w, ownIndex, depth);
            var simulatedCount = reflexMask.Count(el => el == false);
            var ordering = MaxN.CalculateSteppingOrdering(w, ownIndex, reflexMask);

            if (simulatedCount == 2 && c.AlphaBetaFallbackHeuristic != null) {
                // If alpha beta fallback heuristic is provided and we have two simulated snakes, use alpha beta search
                var abConf = new DeepeningSearch.Configuration(c.AlphaBetaFallbackHeuristic(ordering[0], ordering[1]), ordering[0], ordering[1]);
                abConf.Stop = stop;
                var (direction, score) = new AlphaBeta().Search(abConf, w, depth);
                return new Result(new DeepeningSearch.Result(score, direction, depth));
            } else {
                var internalConfiguration = new InternalConfiguration {
                    ReflexMask = reflexMask,
                    Depth = depth,
                    OwnIndex = ownIndex,
                    StopHandler = stop };

                var (scoresAbs, scoresRel, directions) = PseudoPlyStep(w, internalConfiguration, c.MaxNHeuristicProducer(ownIndex), 0, 0, ordering, null);

                return new Result(
                    internalConfiguration.steps,
                    internalConfiguration.ReflexMask.Count(el => el == false),
                    internalConfiguration.Depth,
                    directions[ownIndex],
                    scoresRel[ownIndex],
                    scoresAbs,
                    scoresRel,
                    directions
                    );
            }
        }

        /// <summary>
        /// Search the game tree multiple times with increasing depth until the wall clock time limit
        /// specified by the configuration is exhausted. Take best result found at that point and
        /// cancel search.
        /// </summary>
        private static Result BestFixedTime(Configuration c, FastWorld w, int ownIndex) {

            Debug.Assert(c.SearchLimit.LimitType == DeepeningSearch.LimitType.Milliseconds);

            // Keep in mind on thread safety:
            // The thread that calls Abort might block if the thread that is being aborted is in a protected 
            // region of code, such as a catch block, finally block, or constrained execution region. If the 
            // thread that calls Abort holds a lock that the aborted thread requires, a deadlock can occur.
            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.thread.abort?view=netframework-4.7.2

            var myLock = new object();
            Result best = null;

            var stop = new DeepeningSearch.Stop();

            Thread t = new Thread(() => {
                try {
                    for (int i = 1; ; ++i) {
                        var result = SearchSelectingStrategy(c, w, ownIndex, i, stop);

                        lock (myLock) {
                            best = result;
                        }
                    }
                }
                catch (DeepeningSearch.StopSearchException) {
                    // Perfectly normal to throw this exception upon stopping deepening
                    return;
                }
            });

            t.Start();

            Thread.Sleep(c.SearchLimit.Limit);

            stop.RequestStop();

            // Should be no more contention since t is aborted and joined
            // But I guess it doesn't hurt
            lock (myLock) {
                return best;
            }
        }

        /// <summary>
        /// Configuration for NMax search
        /// </summary>
        public class Configuration {
            public List<bool> ReflexMask { get; set; } = null;

            public Func<int, IMultiHeuristic> MaxNHeuristicProducer { get; set; }

            public Func<int, int, IHeuristic<FastWorld>> AlphaBetaFallbackHeuristic { get; set; } = null;

            public DeepeningSearch.SearchLimit SearchLimit { get; set; } = new DeepeningSearch.SearchLimit();
        }

        /// <summary>
        /// Internal configuration holds regular configuration as well as additional diagnostic
        /// and control members
        /// </summary>
        private class InternalConfiguration {
            public List<bool> ReflexMask;

            public int OwnIndex;

            public int Depth;

            public DeepeningSearch.Stop StopHandler;

            internal int steps = 0;
        }

        private static Tuple<List<float>, List<float>> ScoreAdvantage(CachedMultiMetricState w, InternalConfiguration c, IMultiHeuristic heuristic) {
            var count = w.World.Snakes.Count;

            var phiAbs = new List<float>(count);

            float sum = 0.0f;

            for (int i = 0; i < count; ++i) {
                if (c.ReflexMask[i]) {
                    // Reflex based agents always have a score of 0
                    // This way if we are only simulating against reflex based agents, we still
                    // have advantage for better moves (opposed to only considering fully simulated
                    // agents, where advantage would always be 0 compared to average of simulated for one simulated).
                    phiAbs.Add(0.0f);
                    // Don't need to add to sum for 0
                }
                else {
                    // Fully expanded agents estimate their score by the provided heuristic
                    float val = heuristic.Score(w, i);

                    Debug.Assert(!float.IsNaN(val));

                    phiAbs.Add(val);
                    sum += val;
                }
            }

            // Take average to score advantage
            float avg = sum / count;

            var phiRel = new List<float>(count);

            // Subtract average from all heuristic scores to get comparative advantage
            for (int i = 0; i < count; ++i) {
                phiRel.Add(phiAbs[i] - avg);
            }

            Debug.Assert(!phiAbs.Any(f => float.IsNaN(f)));

            return Tuple.Create(phiAbs, phiRel);
        }

        private static List<int> CalculateSteppingOrdering(FastWorld w, int ownIndex, List<bool> reflexMask) {

            var result = new List<int>();

            // Always evaluate self out most
            result.Add(ownIndex);

            for (int i = 0; i < w.Snakes.Count; ++i) {
                // Ignore reflex based since they can't base their decision on other snakes anyways
                if (reflexMask[i]) continue;

                // Ignore self
                if (i == ownIndex) continue;

                // Add fully simulated other snakes
                // TODO: Could order by distance to player, i.e. the closest snake makes the best move
                result.Add(i);
            }

            return result;
        }

        private static Tuple<List<float>, List<float>, List<Direction>> PseudoPlyStep(FastWorld w, InternalConfiguration c, IMultiHeuristic heuristic, int currentDepth, int currentPlyDepth, List<int> steppingOrder, List<Direction> moves) {
            // Immediately check stop search on each draw (but not ply)
            if (c.StopHandler.StopRequested) {
                throw new DeepeningSearch.StopSearchException();
            }

            if (currentPlyDepth == steppingOrder.Count) {
                // Increase diagnostic step counter
                c.steps++;

                // All plys played, perform move
                var newW = w.Clone() as FastWorld;
                newW.UpdateMovementTick(moves);

                var cachedMetricState = new CachedMultiMetricState(newW);

                int nextDepth = currentDepth + 1;

                var oldMoves = new List<Direction>(moves);
                List<float> phiAbs, phiRel;

                if (nextDepth == c.Depth || heuristic.IsTerminal(cachedMetricState)) {
                    (phiAbs, phiRel) = ScoreAdvantage(cachedMetricState, c, heuristic);
                }
                else {
                    (phiAbs, phiRel, _) = PseudoPlyStep(newW, c, heuristic, nextDepth, 0, steppingOrder, null);
                }

                return Tuple.Create(phiAbs, phiRel, oldMoves);
            } else if (currentPlyDepth == 0) {
                // First to play on this draw is responsible for setting up move cache by definition
                // Must create new move list to not mess with parent plys (and because by definition we did not receive the parent move list)
                moves = new List<Direction>(w.Snakes.Count);

                for (int i = 0; i < w.Snakes.Count; ++i) {
                    if (w.Snakes[i].Alive && c.ReflexMask[i]) {
                        moves.Add(Util.ImprovedReflexBasedEvade(w, i));
                    } else {
                        moves.Add(Direction.North);
                    }
                }
            }

            int ownIndex = steppingOrder[currentPlyDepth];

            float alpha = float.NegativeInfinity;
            List<float> phiRelMax = null;
            List<float> phiAbsMax = null;
            List<Direction> dirMax = null;

            for (int i = 0; i < Util.Directions.Length; ++i) {

                // Must always evaluate at least one move, even if it is known to be deadly,
                // to have correct heuristics bounds
                bool mustEvaluate = phiRelMax == null && i == Util.Directions.Length - 1;

                // Skip certainly deadly moves if we do not have to evaluate
                if (!mustEvaluate && w.CertainlyDeadly(ownIndex, Util.Directions[i])) continue;

                moves[ownIndex] = Util.Directions[i];

                var (phiStarAbs, phiStarRel, directions) = PseudoPlyStep(w, c, heuristic, currentDepth, currentPlyDepth + 1, steppingOrder, moves);
                var phiStarP = phiStarRel[ownIndex];

                if (alpha < phiStarP) {

                    alpha = phiStarP;
                    phiRelMax = phiStarRel;
                    phiAbsMax = phiStarAbs;
                    dirMax = directions;
                }

                Debug.Assert(alpha != float.NegativeInfinity);
            }

            Trace.Assert(phiRelMax != null);
            Trace.Assert(dirMax != null);

            return Tuple.Create(phiAbsMax, phiRelMax, dirMax);
        }
    }
}