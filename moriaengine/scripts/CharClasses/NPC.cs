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

using System;
using System.Collections;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	//temporary? Mozna to bude neco jako MovementBrain, kazdopadne to chci takhle na testovani
	[SaveableClass]
	[ViewableClass]
	public class DynamicMovementSettings : IMovementSettings {
		[SaveableData]
		public bool canCrossLand;
		[SaveableData]
		public bool canSwim;
		[SaveableData]
		public bool canCrossLava;
		[SaveableData]
		public bool canFly;
		[SaveableData]
		public bool ignoreDoors;
		[SaveableData]
		public int climbPower;

		[LoadingInitializer]
		public DynamicMovementSettings() {
			this.canCrossLand = true;
			this.canSwim = false;
			this.canCrossLava = false;
			this.canFly = false;
			this.ignoreDoors = false;
			this.climbPower = 2;
		}

		public void ShowValues() {
			Globals.SrcWriteLine("canCrossLand: "+canCrossLand);
			Globals.SrcWriteLine("canSwim: "+canSwim);
			Globals.SrcWriteLine("canCrossLava: "+canCrossLava);
			Globals.SrcWriteLine("canFly: "+canFly);
			Globals.SrcWriteLine("ignoreDoors: "+ignoreDoors);
			Globals.SrcWriteLine("climbPower: "+climbPower);
		}

		public DynamicMovementSettings(bool canCrossLand, bool canSwim, bool canCrossLava, 
					bool canFly, bool ignoreDoors, int climbPower) {
			this.canCrossLand = canCrossLand;
			this.canSwim = canSwim;
			this.canCrossLava = canCrossLava;
			this.canFly = canFly;
			this.ignoreDoors = ignoreDoors;
			this.climbPower = climbPower;
		}

		bool IMovementSettings.CanCrossLand { get { 
			return canCrossLand; 
		} }
		bool IMovementSettings.CanSwim { get { 
			return canSwim; 
		} }
		bool IMovementSettings.CanCrossLava { get { 
			return canCrossLava; 
		} }
		bool IMovementSettings.CanFly { get { 
			return canFly; 
		} }
		bool IMovementSettings.IgnoreDoors { get { 
			return ignoreDoors; 
		} }
		int IMovementSettings.ClimbPower { get {
			return climbPower;
		} } //max positive difference in 1 step
	}

	[ViewableClass]
	public partial class NPCDef {
	}

	[ViewableClass]
	public partial class NPC : Character {

		public override IMovementSettings MovementSettings {
			get {
				if (movementSettings == null) {
					return base.MovementSettings;
				} else {
					return movementSettings;
				}
			}
			set {
				movementSettings = value;
			}
		}
	}
}