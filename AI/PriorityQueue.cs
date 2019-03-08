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

namespace BattleSnake.AI {
    class PriorityQueue<T> where T : IComparable<T> {
        private readonly List<T> data;

        public int Count {
            get {
                return data.Count;
            }
        }

        public PriorityQueue() {
            data = new List<T>();
        }

        public void Clear() {
            data.Clear();
        }

        public T Dequeue() {
            // Extract smallest element
            T front = data[0];

            // Swap largest element to place of smallest
            int lastIndex = data.Count - 1;
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);

            // Rebuild heap from top down
            --lastIndex;
            int parentIndex = 0;

            for (;;) {
                // Set child index to left child first
                int childIndex = parentIndex * 2 + 1;

                // If no left child, no right child either, can't bubble further
                if (childIndex > lastIndex) {
                    break;
                }

                // If right child exists and right child smaller left child, continue with right child
                // to always continue with smaller child
                int rightChildIndex = childIndex + 1;
                if (rightChildIndex <= lastIndex && data[rightChildIndex].CompareTo(data[childIndex]) < 0) {
                    childIndex = rightChildIndex;
                }

                // If parent already smaller or equal to smallest child, nothing to do, heap is heapified
                if (data[parentIndex].CompareTo(data[childIndex]) <= 0) {
                    break;
                }

                // Else had a smaller child, swap child and parent and continue downwards
                T tmp = data[parentIndex];
                data[parentIndex] = data[childIndex];
                data[childIndex] = tmp;

                // Take swapped child as no parent, other child remains heapified
                parentIndex = childIndex;
            }

            return front;
        }

        public void Enqueue(T item) {
            // Add item as new leave to end of heap
            data.Add(item);

            // Set child index to just inserted item
            int childIndex = data.Count - 1;

            // While we haven't reached the root
            while (childIndex > 0) {
                // Calculate parent index of current child
                int parentIndex = (childIndex - 1) / 2;

                // If the child is larger or equal to the parent, already heapified,
                // nothing more to do
                if (data[childIndex].CompareTo(data[parentIndex]) >= 0) {
                    break;
                }

                // Else swap parent and child
                T tmp = data[childIndex];
                data[childIndex] = data[parentIndex];
                data[parentIndex] = tmp;

                // Select parent node as new child
                childIndex = parentIndex;
            }
        }

        public T Peek() {
            return data[0];
        }
    }
}
