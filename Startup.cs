﻿/**
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
using System.Diagnostics;

using BattleSnake.AI.Agents;
using BattleSnake.Service;

namespace BattleSnake {
    class Startup
    {

        static void Main(string[] args)
        {

            // Configure tracing to print to console for heroku
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Error));

            // Get port from heroku environment or set to default of 5050
            var port = Environment.GetEnvironmentVariable("PORT") ?? "5050";

            // Ignore MSDN warning
            // Top-level wildcard bindings (http://*:8080/ and http://+:8080) should not be used
            var root = "http://*:" + port + "/";

            using (var server = new Server(root))
            {
                Console.Error.WriteLine("Starting server on endpoint {0}", root);
                server.AddEndpoint("primary", new ConcurrentServiceController<FastControllerWrapper<PrimaryAgent>>(() => new FastControllerWrapper<PrimaryAgent>(new PrimaryAgent())));
                server.Run();
            }
        }
    }
}
