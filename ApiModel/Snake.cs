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
using Newtonsoft.Json;

namespace BattleSnake.ApiModel {
    public sealed class Snake {
        [JsonProperty("id")]
        public readonly string ID;
        [JsonProperty("name")]
        public readonly string Name;
        [JsonProperty("health")]
        public readonly int Health;
        [JsonProperty("body")]
        public readonly List<Coord> Body;

        [JsonIgnore]
        public int EffectiveLength {
            get {
                return Body.Count - GrowthLeft;
            }
        }

        [JsonIgnore]
        public Coord Head {
            get { 
                if (Body.Count == 0) throw new InvalidOperationException("This snake is empty");
                return Body[0]; 
            }
        }

        [JsonIgnore]
        public Coord Tail {
            get { return Body[Body.Count - 1]; }
        }

        [JsonIgnore]
        public int GrowthLeft {
            get {
                // API stores duplicate body parts at end of snake if growing,
                // thus can determine growth left

                int result = 0;
                var tail = Tail;

                for (int i = Body.Count - 2; i >= 0; --i) {
                    if (Body[i] == tail) ++result;
                    else break;
                }
                
                return result;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("<Snake, ID=");
            sb.Append(ID);
            sb.Append(", Health=");
            sb.Append(Health);
            sb.Append(", GrowthLeft=");
            sb.Append(GrowthLeft);
            sb.Append(", Body=[");

            for (int i = 0; i < Body.Count; ++i) {
                if (i != 0) sb.Append(", ");
                sb.Append(Body[i]);
            }

            sb.Append("]>");
            return sb.ToString();
        }
    }
}