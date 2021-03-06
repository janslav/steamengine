/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Persistence;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.CompiledScripts {

	[SaveableClass, DeepCopyableClass]
	public sealed class SpawnInfo {

		[SaveableData, CopyableData]
		public IThingFactory SpawnDef;

		[SaveableData, CopyableData]
		public short Amount;

		[SaveableData, CopyableData]
		public short MinTime;

		[SaveableData, CopyableData]
		public short MaxTime;

		[SaveableData, CopyableData]
		public short Homedist;


		[LoadingInitializer, DeepCopyImplementation]
		public SpawnInfo() {
		}

		public SpawnInfo(short amount, IThingFactory spawnDef, short minTime, short maxTime, short homedist) {
			this.Amount = amount;
			this.SpawnDef = spawnDef;
			this.MinTime = minTime;
			this.MaxTime = maxTime;
			this.Homedist = homedist;
		}
	}
}