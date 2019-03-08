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

using System.Collections.Generic;

using BattleSnake.AI.Heuristics;
using BattleSnake.InternalModel;
using BattleSnake.ApiModel;
using BattleSnake.Misc;

namespace BattleSnake.AI {
    public static class Util {

        public static Coord[] Moves = {
            new Coord( 1, 0),
            new Coord(-1, 0),
            new Coord( 0, 1),
            new Coord( 0,-1)
        };

        public static Direction[] Directions = {
            Direction.North,
            Direction.West,
            Direction.South,
            Direction.East
        };

        public struct FillTile {
            public readonly int? Snake;
            public readonly int Distance;

            public FillTile(int? snake, int distance) {
                Snake = snake; Distance = distance;
            }
        }

        private struct FillItem {
            public readonly int Snake;
            public readonly int Distance;
            public readonly Coord Coord;

            public FillItem(int snake, int distance, Coord coord) {
                Snake = snake; Distance = distance; Coord = coord;
            }
        }

        private struct BasicFillItem {
            public readonly Coord Coord;
            public readonly int Distance;

            public BasicFillItem(Coord coord, int distance) {
                Coord = coord; Distance = distance;
            }
        }

        /// <summary>
        /// More intelligent reflex based evasion. Scores all 4 available actions
        /// without simulating the next step, returning the best choice.
        /// This function is supposed to be very fast.
        /// </summary>
        /// <param name="board">Current world</param>
        /// <param name="index">Index of snake to simulate</param>
        /// <returns></returns>
        public static Direction ImprovedReflexBasedEvade(FastWorld board, int index) {
            Direction best = Direction.North;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < Directions.Length; ++i) {
                var d = Directions[i];

                float score = ReflexEvasionHeuristic.Score(board, index, d);

                if (score > bestScore) {
                    bestScore = score;
                    best = d;
                }
            }

            return best;
        }

        public class FillResult {
            public readonly int Count;
            public readonly int WhiteCount;
            public readonly int BlackCount;
            public readonly int[,] Distances;

            internal FillResult(int count, int whiteCount, int blackCount, int[,] distances) {
                Count = count;
                WhiteCount = whiteCount;
                BlackCount = blackCount;
                Distances = distances;
            }
        }

        public static FillResult CountBasicFloodFill(FastWorld board, Coord start) {
            var visited = new int[board.Height, board.Width];
            var queue = new Queue<BasicFillItem>();
            int count = 0;
            int blackCount = 0;
            int whiteCount = 0;

            queue.Enqueue(new BasicFillItem(start, 0));

            while (queue.TryDequeue(out BasicFillItem current)) {
                // Iterate all possible moves from current position
                for (int i = 0; i < Moves.Length; ++i) {
                    var next = current.Coord + Util.Moves[i];
                    var nextDistance = current.Distance + 1;

                    // Skip out of bounds
                    if (next.X < 0 || next.Y < 0 || next.X >= board.Width || next.Y >= board.Height) {
                        continue;
                    }

                    // Skip already visited
                    if (visited[next.Y, next.X] > 0) {
                        continue;
                    }
                    // Fill neighbour increasing distance and add neighbour to queue
                    visited[next.Y, next.X] = nextDistance;

                    // Skip unpassable
                    var occupant = board.fields[next.Y, next.X].occupant;

                    if (occupant != FastWorld.Occupant.Empty && occupant != FastWorld.Occupant.Fruit) {
                        continue;
                    }

                    queue.Enqueue(new BasicFillItem(next, nextDistance));

                    // Check parity to determine whether on white or black chessboard tile
                    if (next.IsChessBoardWhite) {
                        ++whiteCount;
                    } else {
                        ++blackCount;
                    }

                    ++count;
                }
            }

            return new FillResult(count, whiteCount, blackCount, visited);
        }

        public class AdversarialFillResult {
            public readonly FillTile[,] Tiles;
            public readonly List<int> EmptyCounts;

            internal AdversarialFillResult(FillTile[,] Tiles, List<int> EmptyCounts) {
                this.Tiles = Tiles;
                this.EmptyCounts = EmptyCounts;
            }
        }

        public static AdversarialFillResult GenerateFloodFillBoard(FastWorld board) {
            var fillBoard = new FillTile[board.Height, board.Width];

            // Count true empty tiles filled first by snake
            var emptyTileFillCounts = Extensions.FilledList(board.Snakes.Count, 0);

            var queue = new Queue<FillItem>();
            var list = new List<FillItem>();

            // Fill heads as ours
            for (int i = 0; i < board.Snakes.Count; ++i) {
                list.Add(new FillItem(board.Snakes[i].index, 0, board.Snakes[i].Head));
            }

            // Sort by length to ensure longest snake can expand most freely
            list.Sort((a, b) => board.Snakes[b.Snake].MaxLength.CompareTo(board.Snakes[a.Snake].MaxLength));

            // Add initial head position to flood fill queue
            for (int i = 0; i < list.Count; ++i) {
                queue.Enqueue(list[i]);
            }

            while (queue.TryDequeue(out FillItem current)) {
                // Iterate all possible moves from current position
                for (int i = 0; i < Moves.Length; ++i) {
                    var next = current.Coord + Util.Moves[i];

                    // Skip out of bounds
                    if (next.X < 0 || next.Y < 0 || next.X >= board.Width || next.Y >= board.Height) {
                        continue;
                    }

                    // Skip already filled neighbours
                    if (fillBoard[next.Y, next.X].Snake != null) {
                        continue;
                    }

                    // Fill neighbour increasing distance and add neighbour to queue
                    fillBoard[next.Y, next.X] = new FillTile(current.Snake, current.Distance + 1);

                    // Now skip if blocked on original board, because we cant continue filling from here
                    // Still want to set fill tile earlier to determine if we can reach this snake part first
                    if (board[next].occupant == FastWorld.Occupant.Snake) {
                        continue;
                    }

                    emptyTileFillCounts[current.Snake]++;

                    queue.Enqueue(new FillItem(current.Snake, current.Distance + 1, next));
                }
            }

            return new AdversarialFillResult(fillBoard, emptyTileFillCounts);
        }
    }
}