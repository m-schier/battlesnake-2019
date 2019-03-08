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

using BattleSnake.AI.Heuristics;
using BattleSnake.InternalModel;
using BattleSnake.ApiModel;

using System;
using System.Diagnostics;

namespace BattleSnake.AI.Agents {
    public class PrimaryAgent : IFastModelController
    {

        public string Name
        {
            get; internal set;
        } = "SHIELD";

        public Func<int, int, IHeuristic<FastWorld>> AlphaBetaHeuristicProducer = (index, enemyIndex) => new PrimaryDuellingHeuristic(index, enemyIndex);

        public Func<int, IMultiHeuristic> MaxNHeuristicProducer = (index) => new PrimaryMultiHeuristic(index);

        public void Start(FastWorld w, int ownIndex)
        {
            // No op
        }

        public Direction Move(FastWorld w, int ownIndex, DeepeningSearch.SearchLimit limit)
        {

            var watch = Stopwatch.StartNew();

            var conf = new MaxN.Configuration
            {
                MaxNHeuristicProducer = this.MaxNHeuristicProducer,
                AlphaBetaFallbackHeuristic = this.AlphaBetaHeuristicProducer,
                SearchLimit = limit
            };
            
            var result = MaxN.Search(conf, w, ownIndex);
            // Console.Error.WriteLine($"{ownIndex} TURN {w.Turn:D3} NMax done {watch.Elapsed.TotalMilliseconds} ms, {result}");
            Trace.Assert(result != null);
            return result.Move;
        }

        public void End(FastWorld w, int ownIndex)
        {
            // No op
        }
    }
}