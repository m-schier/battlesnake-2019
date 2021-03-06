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

using BattleSnake.ApiModel;

namespace BattleSnake.Service {
    /// <summary>
    /// Interface that must be implemented by all controllers registered with the HTTP snake service.
    /// A service controller is instantiated per service instance, thus controlling multiple snake instances.
    /// </summary>
    public interface ISnakeServiceController {

        string Name { get; }

        void Ping();

        string Start(GameState s);

        Direction Move(GameState s);

        void End(GameState s);
    }
}