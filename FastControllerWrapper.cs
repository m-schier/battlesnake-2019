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
using System.Security.Cryptography;
using System.Text;

using BattleSnake.AI;
using BattleSnake.InternalModel;
using BattleSnake.ApiModel;
using BattleSnake.Service;

namespace BattleSnake {
    public class FastControllerWrapper<T> : ISnakeInstanceController where T : IFastModelController {
        T innerController;

        public FastControllerWrapper(T innerController) {
            this.innerController = innerController;
            this.SearchLimit = new DeepeningSearch.SearchLimit();
        }

        public DeepeningSearch.SearchLimit SearchLimit {
            get; set;
        }

        public string Name {
            get {
                return "FastControllerWrapper_" + innerController.Name;
            }
        }

        public static byte[] sha256(string s) {
            using (var sha = SHA256.Create()) {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            }
        }

        public string Start(GameState s) {
            FastWorld w = FastWorld.FromApiModel(s);
            int ownIndex = w.FindSnakeIndexForHead(s.You.Head);

            innerController.Start(w, ownIndex);

            // For color just take hash of inner type name
            var hash = sha256(innerController.GetType().FullName);
            return String.Format("#{0:X2}{1:X2}{2:X2}", hash[0], hash[1], hash[2]);
        }

        public Direction Move(GameState s) {
            FastWorld w = FastWorld.FromApiModel(s);
            int ownIndex = w.FindSnakeIndexForHead(s.You.Head);

            return innerController.Move(w, ownIndex, SearchLimit);
        }

        public void End(GameState s) {
            FastWorld w = FastWorld.FromApiModel(s);
            int ownIndex = w.FindSnakeIndexForHead(s.You.Head);

            innerController.End(w, ownIndex);
        }
    }
}