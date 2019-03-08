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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using BattleSnake.ApiModel;
using Newtonsoft.Json;

namespace BattleSnake.Service {
    /// <summary>
    /// Crude HTTP REST server for serving snake API requests
    /// </summary>
    public sealed class Server : IDisposable {
        private HttpListener listener;

        private JsonSerializerSettings serializerSettings;

        private Dictionary<string, ISnakeServiceController> endpoints;

        public Server(string root) {
            listener = new HttpListener();
            listener.Prefixes.Add(root);

            endpoints = new Dictionary<string, ISnakeServiceController>();

            // Configure serializer
            serializerSettings = DefaultSerializerSettings;
        }

        public static JsonSerializerSettings DefaultSerializerSettings {
            get {
                var opt = new JsonSerializerSettings();
                opt.MissingMemberHandling = MissingMemberHandling.Error;
                opt.ContractResolver = new ServerContractResolver();
                return opt;
            }
        }

        public void AddEndpoint(string name, ISnakeServiceController controller) {
            // Add a slash because .NET framework splits URLs this way
            endpoints.Add(name + "/", controller);
        }

        public void HandleRequest(HttpListenerContext ctx) {
            string payload = "";
            try {
                // Initially assume internal error
                ctx.Response.StatusCode = 503;

                var path = ctx.Request.Url.AbsolutePath;
                var segments = ctx.Request.Url.Segments;

                GameState state = null;
                StreamReader reader = null;

                try {
                    reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                    payload = reader.ReadToEnd();
                    state = ParseGameState(payload);
                } catch {
                    // Swallow
                } finally {
                    if (reader != null) reader.Dispose();
                }

                // Expecting fragments
                // 0: /
                // 1: <name>/
                // 2: <method>
                if (segments.Length != 3 || segments[0] != "/") {
                    throw new StopServingException(404);
                }

                var name = segments[1];
                var method = segments[2];

                ISnakeServiceController controller;

                if (!endpoints.TryGetValue(name, out controller)) {
                    throw new StopServingException(404);
                }

                // TODO: Limit HTTP methods
                if (method == "ping") {
                    controller.Ping();
                    ctx.Response.StatusCode = 200;
                } else if (method == "start") {
                    var json = JsonConvert.SerializeObject(new ResponseStart(controller.Start(state)));
                    ReplyJson(ctx.Response, 200, json);
                } else if (method == "move") {
                    var json = JsonConvert.SerializeObject(new ResponseMove(controller.Move(state)));

                    ReplyJson(ctx.Response, 200, json);
                } else if (method == "end") {
                    controller.End(state);
                    ctx.Response.StatusCode = 200;
                } else {
                    throw new StopServingException(404);
                }

            } catch (StopServingException sse) {
                ctx.Response.StatusCode = sse.StatusCode;
            } catch (Exception e) {
                Console.Error.WriteLine($"SERVER: Uncaught exception while handling HTTP request: {e}");
            } finally {
                ctx.Response.OutputStream.Close();
            }
        }

        public void Run() {
            listener.Start();

            while (listener.IsListening) {
                var ctx = listener.GetContext();
                ThreadPool.QueueUserWorkItem(c => HandleRequest(c), ctx, false);
            }
        }

        private GameState ParseGameState(string payload) {
            var value = JsonConvert.DeserializeObject<GameState>(payload, serializerSettings);
            if (value == null) throw new NullReferenceException();
            return value;
        }

        private GameState ParseGameState(HttpListenerRequest r) {
            var reader = new StreamReader(r.InputStream, Encoding.UTF8);
            return ParseGameState(reader.ReadToEnd());
        }

        private static void ReplyJson(HttpListenerResponse r, int code, string content) {
            r.StatusCode = code;
            r.ContentType = "application/json";
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            r.ContentLength64 = buffer.Length;
            r.OutputStream.Write(buffer, 0, buffer.Length);
        }

        #region Disposable
        // Initially not disposed
        bool disposed = false;

        public void Dispose() {
            Dispose(true);
            // Manually disposing, do not finalize
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool manuallyDisposing) {
            // Cannot dispose more than once
            if (disposed) return;

            if (manuallyDisposing) {
                // If manually disposing, GC has not disposed any managed child members yet,
                // we are disposing top-down so we are responsible for disposing managed children

                // DISPOSE MANAGED MEMBERS HERE
                if (listener.IsListening) {
                    listener.Stop();
                }
                listener.Close();
            }

            // DIPOSE UNMANAGED MEMBERS HERE

            // Done disposing
            disposed = true;
        }

        ~Server() {
            // Automatically disposing
            Dispose(false);
        }
        #endregion
    }
}