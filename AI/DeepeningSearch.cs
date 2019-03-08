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
using System.Threading;

using BattleSnake.AI.Heuristics;
using BattleSnake.InternalModel;
using BattleSnake.ApiModel;

namespace BattleSnake.AI {
    public abstract class DeepeningSearch {

        internal class StopSearchException : Exception { }


        internal class Stop {
            private volatile bool stop = false;

            public bool StopRequested {
                get { return stop;  }
            }

            public void RequestStop() {
                stop = true;
            }
        }

        public enum LimitType {
            Milliseconds,
            Depth
        }

        public class SearchLimit {

            public LimitType LimitType { get; set; } = LimitType.Milliseconds;

            public int Limit { get; set; } = 400;
        }

        public class Configuration {
            public int OwnIndex;
            public int EnemyIndex;
            public IHeuristic<FastWorld> Heuristic;

            /// <summary>
            /// Decision function evaluated for all snakes that are not deeply simulated
            /// during game tree search
            /// </summary>
            /// <value></value>
            public Func<FastWorld, int, Direction> UntargetedDecisionFunction { get; set; } = 
            Util.ImprovedReflexBasedEvade;

            public SearchLimit SearchLimit { get; set; }

            // Quite nasty because this is not part of configuration parameters, but just put it here for now
            internal Stop Stop = new Stop();

            public Configuration(IHeuristic<FastWorld> heuristic, int ownIndex, int enemyIndex) {
                Heuristic = heuristic; OwnIndex = ownIndex; EnemyIndex = enemyIndex;
                SearchLimit = new SearchLimit();
            }
        }

        public class Result {
            public readonly float Score;
            public readonly Direction Move;
            public readonly int Depth;

            internal Result(float score, Direction move, int depth) {
                Score = score; Move = move; Depth = depth;
            }
        }

        public abstract Tuple<Direction, float> Search(Configuration c, FastWorld root, int depth);

        public Result Best(Configuration c, FastWorld w) {
            switch (c.SearchLimit.LimitType) {
            case LimitType.Depth:
                return BestFixedDepth(c, w);
            case LimitType.Milliseconds:
                return BestFixedTime(c, w);
            default:
                throw new ArgumentException();
            }
        }

        private Result BestFixedDepth(Configuration c, FastWorld w) {
            Debug.Assert(c.SearchLimit.LimitType == LimitType.Depth);

            var tuple = Search(c, w, c.SearchLimit.Limit);

            return new Result(tuple.Item2, tuple.Item1, c.SearchLimit.Limit);
        }

        private Result BestFixedTime(Configuration c, FastWorld w) {

            Debug.Assert(c.SearchLimit.LimitType == LimitType.Milliseconds);

            // Keep in mind on thread safety:
            // The thread that calls Abort might block if the thread that is being aborted is in a protected 
            // region of code, such as a catch block, finally block, or constrained execution region. If the 
            // thread that calls Abort holds a lock that the aborted thread requires, a deadlock can occur.
            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.thread.abort?view=netframework-4.7.2

            // Always reset stop latch first
            c.Stop = new Stop();

            var myLock = new object();
            Result best = null;

            Thread t = new Thread(() => {
                try {
                    for (int i = 1; ; ++i) {
                        var current = Search(c, w, i);

                        lock (myLock) {
                            best = new Result(current.Item2, current.Item1, i);
                        }
                    }
                }
                catch (StopSearchException) {
                    // Perfectly normal to throw this exception upon stopping deepening
                    return;
                }
            });

            t.Start();

            Thread.Sleep(c.SearchLimit.Limit);

            c.Stop.RequestStop();

            // Should be no more contention since t is aborted and joined
            // But I guess it doesn't hurt
            lock (myLock) {
                return best;
            }
        }
    }
}
