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
using BattleSnake.ApiModel;

namespace BattleSnake.InternalModel {

    public enum Status {
        Alive = 0,
        KilledHeadOnHead = 1,
        KilledEnemyBody = 2,
        KilledStarvation = 3,
        KilledOwnBody = 4,
        KilledWall = 5
    }

    /// <summary>
    /// Snake handle used with <see cref="FastWorld"/> to encode snake information
    /// </summary>
    public sealed class FastSnake {

        public Coord Tail { get; internal set; }
        internal readonly int index;
        public bool Alive { get {
            return Status == Status.Alive;
        } }

        public Status Status { get; private set; }

        public int MaxLength { get; internal set; }

        public int PendingMaxLength { get; internal set; }
        private FastWorld world;
        public Coord Head { get; internal set; }
        public int Health { get; internal set; }
        public int Length { get; internal set; }

        public Direction LastDirection {
            get {
                return world.fields[Head.Y, Head.X].direction;
            }
        }

        internal FastSnake(FastWorld world, int index, Coord initialHeadPosition) {
            this.world = world;
            this.index = index;
            MaxLength = 3;
            PendingMaxLength = MaxLength;
            Length = 1;
            Status = Status.Alive;
            Health = 100;
            Head = initialHeadPosition;
            Tail = initialHeadPosition;
        }

        internal FastSnake(FastSnake other, FastWorld world) {
            this.Tail = other.Tail;
            this.index = other.index;
            this.Status = other.Status;
            this.MaxLength = other.MaxLength;
            this.PendingMaxLength = other.PendingMaxLength;
            this.world = world;
            this.Head = other.Head;
            this.Health = other.Health;
            this.Length = other.Length;
        }

        /// <summary>
        /// Enumerate snake parts from tail to head. Enumerator only valid without simulating ticks.
        /// </summary>
        /// <returns>Enumeration of all parts of this snake</returns>
        public IEnumerable<Coord> EnumerateParts() {

            Coord current = Tail;

            for (;;) {
                Debug.Assert(world[current].occupant == FastWorld.Occupant.Snake);
                Debug.Assert(world[current].id == index);

                yield return current;

                if (current == Head) break;

                current = current.Advanced(world[current].direction);
            }

            yield break;
        }

        public void Grow() {
            PendingMaxLength += 1;
            Health = 101; // +1 because our implementations decreases after eating, Go doesn't
        }

        public void UpdatePostTick() {
            MaxLength = PendingMaxLength;
        }

        public void Kill(Status killReason) {
            if (killReason == Status.Alive) throw new ArgumentException();

            // Clean up from tail up
            Coord current = Tail;

            for (;;) {
                Debug.Assert(world.fields[current.Y, current.X].occupant == FastWorld.Occupant.Snake);
                Debug.Assert(world.fields[current.Y, current.X].id == index);

                Direction d = world.fields[current.Y, current.X].direction;

                world.fields[current.Y, current.X] = new FastWorld.Field(FastWorld.Occupant.Empty);

                if (current.Equals(Head)) {
                    break;
                }

                current = current.Advanced(d);
            }

            Status = killReason;
        }

        public bool WillGrowOnUpdate() {
            return Length != MaxLength;
        }

        public int PeekLength() {
            if (Length == MaxLength) {
                return Length;
            } else {
                return Length + 1;
            }
        }

        public void PerformTailMove() {
            Debug.Assert(Alive);

            if (Length == MaxLength) {
                // Must clean up tail, move tail pointer in direction current tail cell was left in
                // and then clear tail cell, also update tail pointer

                // Debug assert tail points to valid cell before moving tail
                Debug.Assert(world.fields[Tail.Y, Tail.X].occupant == FastWorld.Occupant.Snake);
                Debug.Assert(world.fields[Tail.Y, Tail.X].id == index);

                Coord newTail = Tail.Advanced(world.fields[Tail.Y, Tail.X].direction);

                world.fields[Tail.Y, Tail.X] = new FastWorld.Field(FastWorld.Occupant.Empty);

                Tail = newTail;

                --Length;

                // Debug assert tail points to valid cell after moving tail
                Debug.Assert(world.fields[Tail.Y, Tail.X].occupant == FastWorld.Occupant.Snake);
                Debug.Assert(world.fields[Tail.Y, Tail.X].id == index);
            }
        }

        public void PerformHeadMove(Direction d) {

            Debug.Assert(Alive);

            Debug.Assert(world.fields[Head.Y, Head.X].occupant == FastWorld.Occupant.Snake);
            Debug.Assert(world.fields[Head.Y, Head.X].id == index);

            // Write direction we left field in to old head position
            world.fields[Head.Y, Head.X] = world.fields[Head.Y, Head.X].WithModifiedDirection(d);

            Head = Head.Advanced(d);

            // Assert that the tile we are moving into is actually empty at this point
            Debug.Assert(world.fields[Head.Y, Head.X].occupant == FastWorld.Occupant.Empty);

            world.fields[Head.Y, Head.X] = new FastWorld.Field(FastWorld.Occupant.Snake, index, d);

            ++Length;
        }
    }
}