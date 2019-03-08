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
using BattleSnake.ApiModel;

namespace BattleSnake.Service {
    /// <summary>
    /// Implementation of a service controller that instantiates and manages instance controllers in a per controller thread safe way
    /// </summary>
    public sealed class ConcurrentServiceController<C> : ISnakeServiceController where C : ISnakeInstanceController {

        private Dictionary<string, C> controllers;

        public string Name {
            get { return producer().Name; }
        }

        private Func<C> producer;

        public ConcurrentServiceController(Func<C> producer) {
            this.producer = producer;
            controllers = new Dictionary<string, C>();
        }

        public void Ping() {
            string config = "Release";
            int count;

            #if DEBUG
            config = "Debug";
            #endif

            lock (controllers) {
                count = controllers.Count;
            }

            Console.Error.WriteLine("🏓 Ping ok, Configuration: {0}, Controllers: {1}", config, count);
        }

        public string Start(GameState s) {
            var controller = producer();

            lock (controllers) {
                controllers[s.You.ID] = controller;
            }

            return controller.Start(s);
        }

        public Direction Move(GameState s) {
            C controller;

            lock (controllers) {
                controller = controllers[s.You.ID];
            }

            return controller.Move(s);
        }

        public void End(GameState s) {
            C controller;

            lock (controllers) {
                controller = controllers[s.You.ID];
            }

            controller.End(s);

            lock (controllers) {
                controllers.Remove(s.You.ID);
            }
        }
    }
}