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

using BattleSnake.InternalModel;

namespace BattleSnake.AI {
    public class CachedMetricState {
        public readonly FastWorld World;
        public readonly int OwnIndex;
        public readonly int EnemyIndex;

        public CachedMetricState(FastWorld world, int ownIndex, int enemyIndex) {
            this.World = world;
            this.OwnIndex = ownIndex;
            this.EnemyIndex = enemyIndex;

            this.ownSnake = world.Snakes[ownIndex];
            this.enemySnake = world.Snakes[enemyIndex];

            this.adversarialFill = new Lazy<Util.AdversarialFillResult>(() => Util.GenerateFloodFillBoard(world));
            this.ownFill = new Lazy<Util.FillResult>(() => Util.CountBasicFloodFill(world, ownSnake.Head));
            this.enemyFill = new Lazy<Util.FillResult>(() => Util.CountBasicFloodFill(world, enemySnake.Head));
        }

        private FastSnake ownSnake;
        private FastSnake enemySnake;

        private Lazy<Util.AdversarialFillResult> adversarialFill;
        private Lazy<Util.FillResult> ownFill;
        private Lazy<Util.FillResult> enemyFill;


        public FastSnake OwnSnake { get { return ownSnake; }}
        public FastSnake EnemySnake { get { return enemySnake; }}

        public Util.AdversarialFillResult AdversarialFill { get { return adversarialFill.Value; }}
        public Util.FillResult OwnFill { get { return ownFill.Value; }}
        public Util.FillResult EnemyFill { get { return enemyFill.Value; }}
    }
}