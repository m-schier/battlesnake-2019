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
using BattleSnake.ApiModel;
using Newtonsoft.Json;

namespace BattleSnake.Service {
    /// <summary>
    /// Reponse object for API move requests
    /// </summary>
    class ResponseMove {
        [JsonProperty("move")]
        public readonly string Move;

        public ResponseMove(Direction d) {
            Move = FormatDirection(d);
        }

        /// <summary>
        /// Get the string encoded direction as used by the API for the given direction enum
        /// </summary>
        public static string FormatDirection(Direction d) {
            switch (d) {
            case Direction.North:
                return "up";
            case Direction.East:
                return "right";
            case Direction.South:
                return "down";
            case Direction.West:
                return "left";
            default:
                throw new ArgumentException();
            }
        }
    }
}