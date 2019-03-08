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
using Newtonsoft.Json;

namespace BattleSnake.ApiModel {
    public struct Coord : IEquatable<Coord> {
        [JsonProperty("x")]
        public readonly int X;
        [JsonProperty("y")]
        public readonly int Y;

        [JsonIgnore]
        public bool IsChessBoardWhite {
            get {
                return (X & 1) == (Y & 1);
            }
        }

        public Coord(int x, int y) {
            X = x; Y = y;
        }

        public override string ToString() {
            return "(" + X + ", " + Y + ")";
        }

        public static Coord operator+(Coord a, Coord b) {
            return new Coord(a.X + b.X, a.Y + b.Y);
        }

        public Coord Advanced(Direction d) {
            switch (d) {
                case Direction.North:
                    return new Coord(X, Y - 1);
                case Direction.East:
                    return new Coord(X + 1, Y);
                case Direction.South:
                    return new Coord(X, Y + 1);
                case Direction.West:
                    return new Coord(X - 1, Y);
                default:
                    throw new ArgumentException("Invalid value for Direction");
            }
        }

        public static int ManhattanDistance(Coord c1, Coord c2) {
            return Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y);
        }

        public static Direction GetSingleStepDirection(Coord from, Coord to) {
            if (ManhattanDistance(from, to) != 1) {
                throw new ArgumentException(string.Format("Not a single step from {0} to {1}", from, to));
            }

            if (from.X < to.X) {
                return Direction.East;
            } else if (from.X > to.X) {
                return Direction.West;
            } else if (from.Y < to.Y) {
                return Direction.South;
            } else {
                return Direction.North;
            }
        }

        #region IEquatable
        public override bool Equals(object other) {
            if (other is Coord p) {
                return Equals(p);
            } else {
                return false;
            }
        }

        public bool Equals(Coord other) {
            return X == other.X && Y == other.Y;
        }

        public static bool operator==(Coord a, Coord b) {
            return a.Equals(b);
        }

        public static bool operator!=(Coord a, Coord b) {
            return !a.Equals(b);
        }

        // Also always implement new hashcode if equals updated
        public override int GetHashCode() {
            return X * 113 + Y;
        }
        #endregion IEquatable
    }
}