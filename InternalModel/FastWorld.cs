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
using System.Threading;

using BattleSnake.Misc;
using BattleSnake.ApiModel;


namespace BattleSnake.InternalModel {

    /// <summary>
    /// World representation using a fixed size field array instead of dynamically sized data structures like the API model.
    /// This representation offers constant time update, cloning and collision checking speeds and generally outperforms the
    /// model used in the API.
    /// </summary>
    public sealed class FastWorld : ICloneable {

        public enum Occupant {
            Empty,
            Snake,
            Fruit,
            Wall,
            Flood
        }

        public struct Field {
            /// <summary>
            /// Occupant type on this field
            /// </summary>
            public readonly Occupant occupant;

            /// <summary>
            /// If occupied by snake, index of snake. Else no meaning.
            /// </summary>
            public readonly int id;

            /// <summary>
            /// If field under head pointer, direction this field was entered in. Else if field under any other snake part, direction this
            /// field was left in. Else if not part of a tail, no inherent meaning.
            /// </summary>
            public readonly Direction direction;

            public Field(Occupant o, int id, Direction d) {
                occupant = o;
                this.id = id;
                direction = d;
            }

            public Field(Occupant o) {
                occupant = o;
                // Don't care
                id = 0;
                direction = Direction.North;
            }

            public Field WithModifiedDirection(Direction d) {
                return new Field(occupant, id, d);
            }
        }

        // Ensure we aren't writing a thread-hostile class by making the static
        // random generator thread local
        private static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

        public readonly Field[,] fields;

        public ref Field this[Coord key] {
            get {
                return ref fields[key.Y, key.X];
            }
        }

        public int Turn {get; internal set;}

        public int Width { get; }
        public int Height { get; }

        public bool IsDecided {
            get {
                int alive = 0;

                for (int i = 0; i < Snakes.Count; ++i) {
                    if (Snakes[i].Alive) {
                        ++alive;
                    }
                }

                return alive <= 1;
            }
        }

        private List<Coord> desiredNewPosition;

        private HashSet<Coord> fruits;

        public IEnumerable<Coord> Fruits {
            get => fruits;
        }

        public List<FastSnake> Snakes { get; }

        // Food statistics related fields
        private int TurnsSinceLastFoodSpawn;
        private int MaxTurnsToNextFoodSpawn;

        private FastWorld(int width, int height) {
            if (width < 3 || height < 3) {
                throw new ArgumentException("World size must be at least 3x3");
            }

            Width = width;
            Height = height;
            Turn = 0;

            fields = new Field[Height, Width];

            Snakes = new List<FastSnake>();
            desiredNewPosition = new List<Coord>();
            fruits = new HashSet<Coord>();
            TurnsSinceLastFoodSpawn = 0;
            MaxTurnsToNextFoodSpawn = 20;
        }

        #region ICloneable

        private FastWorld(FastWorld other) {
            Width = other.Width;
            Height = other.Height;
            Turn = other.Turn;
            TurnsSinceLastFoodSpawn = other.TurnsSinceLastFoodSpawn;
            MaxTurnsToNextFoodSpawn = other.MaxTurnsToNextFoodSpawn;

            fields = other.fields.Clone() as Field[,];

            Snakes = new List<FastSnake>();
            desiredNewPosition = new List<Coord>();
            fruits = new HashSet<Coord>();

            foreach (var s in other.Snakes) Snakes.Add(new FastSnake(s, this));
            foreach (var f in other.fruits) fruits.Add(f);
        }

        public Object Clone() {
            return new FastWorld(this);
        }
        #endregion

        public static FastWorld FromApiModel(BattleSnake.ApiModel.GameState state) {
            var board = state.Board;
            var w = new FastWorld(board.Width, board.Height);
            w.Turn = state.Turn;

            foreach (var f in board.Food) {
                if (w.fields[f.Y, f.X].occupant != Occupant.Empty) {
                    // API is a bit dumb sometimes
                    Console.Error.WriteLine("Trying to place fruit on occupied tile, all fruits are {0}", board.Food.FormatEnumerable());

                    continue;
                }

                w.fields[f.Y, f.X] = new Field(Occupant.Fruit);
                bool added = w.fruits.Add(f);
                Debug.Assert(added);
            }

            for (int i = 0; i < board.Snakes.Count; ++i) {
                var s = board.Snakes[i];
                var fastSnake = new FastSnake(w, i, s.Head);

                // Add head to fields
                if (w.fields[s.Head.Y, s.Head.X].occupant != Occupant.Empty) {
                    throw new ArgumentException("Trying to place snake part on occupied tile");
                }

                var headDirection = Direction.North;

                // If we can reconstruct last head direction, do so
                if (s.EffectiveLength > 1) {
                    headDirection = Coord.GetSingleStepDirection(s.Body[1], s.Body[0]);
                }

                w.fields[s.Head.Y, s.Head.X] = new Field(Occupant.Snake, i, headDirection);

                // Add tails correctly
                for (int j = 1; j < s.EffectiveLength; ++j) {
                    if (w.fields[s.Body[j].Y, s.Body[j].X].occupant != Occupant.Empty) {
                        throw new ArgumentException("Trying to place snake part on occupied tile");
                    }

                    var directionLeft = Coord.GetSingleStepDirection(s.Body[j], s.Body[j - 1]);
                    w.fields[s.Body[j].Y, s.Body[j].X] = new Field(Occupant.Snake, i, directionLeft);
                }

                // Set meta data
                fastSnake.Health = s.Health;
                fastSnake.Length = s.EffectiveLength;
                fastSnake.MaxLength = s.Body.Count;
                fastSnake.PendingMaxLength = s.Body.Count;
                fastSnake.Tail = s.Tail;

                w.Snakes.Add(fastSnake);
            }

            return w;
        }

        public int FindSnakeIndexForHead(Coord pos) {
            for (int i = 0; i < Snakes.Count; ++i) {
                if (Snakes[i].Head == pos) {
                    return i;
                }
            }

            throw new IndexOutOfRangeException("No such position");
        }

        public bool CertainlyDeadly(int index, Direction d) {
            var self = Snakes[index];
            var next = self.Head.Advanced(d);

            // Moving out of bounds is certainly gonna kill the snake
            if (next.X < 0 || next.Y < 0 || next.X >= Width || next.Y >= Height) return true;

            // Also running into a part of ourselves that is not our tail is certainly deadly
            var field = fields[next.Y, next.X];

            // Running into anything but a snake or running into any snake that isn't us
            // may always be safe, as other snakes may die
            if (field.occupant != Occupant.Snake || field.id != index) return false;

            // If tail pointer matches next position, running into ourself may be safe
            // (actually, should always be safe, because we can't grow as no fruit spawns on top of our tail)
            // Otherwise running into body, which will kill us
            return self.Tail != next;
        }

        /// <summary>
        /// Perform one simulation tick without spawning additional food
        /// </summary>
        /// <param name="desiredDirections">Directions for all snakes to move in</param>
        public void UpdateMovementTick(List<Direction> desiredDirections) {
            if (desiredDirections.Count != Snakes.Count) {
                throw new ArgumentException("Dimensions must agree");
            }

            desiredNewPosition.Clear();

            for (int i = 0; i < Snakes.Count; ++i) {
                var s = Snakes[i];

                if (s.Alive) {
                    desiredNewPosition.Add(s.Head.Advanced(desiredDirections[i]));
                } else {
                    // Add invalid position
                    desiredNewPosition.Add(new Coord(-1, -1));
                }
            }

            // Kill on out of bounds
            for (int i = 0; i < Snakes.Count; ++i) {
                if (!Snakes[i].Alive) {
                    continue;
                }

                var pos = desiredNewPosition[i];

                if (pos.X < 0 || pos.Y < 0 || pos.X >= Width || pos.Y >= Height) {
                    Snakes[i].Kill(Status.KilledWall);
                }
            }

            // Check head on head collisions first, do this first to ensure
            // that fruits will always be consumed by the snake deserving to do so
            // Now handles three way collision
            for (int i = 0; i < Snakes.Count; ++i) {

                if (!Snakes[i].Alive) {
                    continue;
                }

                var target = desiredNewPosition[i];
                var matches = 1;

                // What's the longest tail
                var longest = Snakes[i].PeekLength();

                // Count how many snakes have lengths matching longest
                var longestCount = 1;

                // Preflight to determine longest snake and collisions
                // for given target
                for (int j = i + 1; j < Snakes.Count; ++j) {
                    if (!Snakes[j].Alive) {
                        continue;
                    }

                    if (desiredNewPosition[j] == target) {
                        ++matches;

                        // If longest excelled, reset longest counter
                        if (longest < Snakes[j].PeekLength()) {
                            longest = Snakes[j].PeekLength();
                            longestCount = 1;
                        }
                        // Else if longest matched, increase longest counter
                        else if (longest == Snakes[j].PeekLength()) {
                            longestCount++;
                        }
                    }
                }

                // If no collision, skip
                if (matches == 1) continue;

                // Kill all that would be killed by multi-way collision
                for (int j = i; j < Snakes.Count; ++j) {
                    if (!Snakes[j].Alive) {
                        continue;
                    }

                    // Kill if shorter than longest or if more than 1 longest
                    if (desiredNewPosition[j] == target && (Snakes[j].PeekLength() < longest || longestCount > 1)) {
                        Snakes[j].Kill(Status.KilledHeadOnHead);
                    }
                }
            }

            // Now consume fruits, do this before checking tail collision to ensure
            // that we cannot clip into a growing snake
            for (int i = 0; i < Snakes.Count; ++i) {
                if (!Snakes[i].Alive) {
                    continue;
                }

                var newHead = desiredNewPosition[i];

                if (fields[newHead.Y, newHead.X].occupant == Occupant.Fruit) {
                    bool removed = fruits.Remove(newHead);

                    Debug.Assert(removed);

                    fields[newHead.Y, newHead.X] = new Field(Occupant.Empty);
                    Snakes[i].Grow();
                }
            }

            // Check head on tail collision, must also take into account growing
            for (int i = 0; i < Snakes.Count; ++i) {
                if (!Snakes[i].Alive) {
                    continue;
                }

                var newHead = desiredNewPosition[i];
                var f = fields[newHead.Y, newHead.X];

                if (f.occupant == Occupant.Wall) {
                    Snakes[i].Kill(Status.KilledWall);
                } else if (f.occupant == Occupant.Snake) {
                    // If colliding into the end of another snake, need to check
                    // whether the other snake will grow, in which case this kills
                    // the current snake, or if the other snake will not grow, freeing
                    // the tile simultaneously
                    if (!Snakes[f.id].Tail.Equals(newHead) || Snakes[f.id].WillGrowOnUpdate()) {
                        if (f.id == i) {
                            Snakes[i].Kill(Status.KilledOwnBody);
                        } else {
                            Snakes[i].Kill(Status.KilledEnemyBody);
                        }
                    }
                }
            }

            // Kill all starved snakes
            for (int i = 0; i < Snakes.Count; ++i) {
                if (!Snakes[i].Alive) {
                    continue;
                }

                if (--Snakes[i].Health <= 0) {
                    Snakes[i].Kill(Status.KilledStarvation);
                }
            }

            // Now actually move all snakes, first move tails out of the way so that heads
            // may simulataneously enter old tails tile without comprising information
            for (int i = 0; i < Snakes.Count; ++i) {
                if (Snakes[i].Alive) {
                    Snakes[i].PerformTailMove();
                }
            }
            for (int i = 0; i < Snakes.Count; ++i) {
                if (Snakes[i].Alive) {
                    Snakes[i].PerformHeadMove(desiredDirections[i]);
                }
            }

            // Update post tick statistics (transfer new max lenght)
            for (int i = 0; i < Snakes.Count; ++i) {
                Snakes[i].UpdatePostTick();
            }

            ++Turn;
        }
    }
}