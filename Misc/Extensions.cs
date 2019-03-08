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
using System.Text;

namespace BattleSnake.Misc {
    /// <summary>
    /// Class containing extension methods for various standard framework classes.
    /// </summary>
    public static class Extensions {

        /// <summary>
        /// Create a new list of the given dimension with all elements set to
        /// the given value
        /// </summary>
        /// <param name="count">Number of initial elements</param>
        /// <param name="val">Value of all initial elements</param>
        /// <typeparam name="T">Type of element</typeparam>
        /// <returns>New filled list</returns>
        public static List<T> FilledList<T>(int count, T val) {
            var list = new List<T>(count);

            for (int i = 0; i < count; ++i) {
                list.Add(val);
            }

            return list;
        }

        /// <summary>
        /// Format the specified enumerable in a shallowly in a pythonic way, i.e.
        /// new int {1, 2, 3}.FormatEnumerable() => "[1, 2, 3]"
        /// </summary>
        /// <returns></returns>
        public static string FormatEnumerable<T>(this IEnumerable<T> list) {
            var sb = new StringBuilder();

            var e = list.GetEnumerator();
            bool first = true;

            sb.Append("[");

            while (e.MoveNext()) {
                if (first) {
                    first = false;
                } else {
                    sb.Append(", ");
                }
                sb.Append(e.Current);
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Perform Fisher-Yates Shuffle on the entire list using the given RNG.
        /// </summary>
        /// <param name="list">List to be shuffled</param>
        /// <param name="r">Random number generator</param>
        /// <typeparam name="T">Type of items in list</typeparam>
        public static void Shuffle<T>(this List<T> list, Random r) {
            Shuffle<T>(list, r, list.Count);
        }

        /// <summary>
        /// Perform Fisher-Yates Shuffle on a portion of the entire list.
        /// The first n items are shuffled against the entirety of the list,
        /// thus drawing n items from the entirety of the list in to the first
        /// n positions.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="r"></param>
        /// <param name="n"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this List<T> list, Random r, int n) {
            var k = list.Count;
            for (int i = 0; i < n - 1; ++i) {
                var j = r.Next(i, k);

                T temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }
    }
}