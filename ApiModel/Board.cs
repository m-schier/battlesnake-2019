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
using Newtonsoft.Json;

namespace BattleSnake.ApiModel {
    public sealed class Board {
        [JsonProperty("width")]
        public readonly int Width;
        [JsonProperty("height")]
        public readonly int Height;
        [JsonProperty("food")]
        public List<Coord> Food;
        [JsonProperty("snakes")]
        public List<Snake> Snakes;

        public bool OnBoard(Coord c) {
            return OnBoard(c.X, c.Y);
        }

        public bool OnBoard(int x, int y) {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }
    }
}