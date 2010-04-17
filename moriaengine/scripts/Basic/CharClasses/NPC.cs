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
using SteamEngine.Regions;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	//temporary? Mozna to bude neco jako MovementBrain, kazdopadne to chci takhle na testovani
	[ViewableClass]
	[SaveableClass, DeepCopyableClass]
	public sealed class DynamicMovementSettings : IMovementSettings {

		[SaveableData, CopyableData]
		public bool canCrossLand;

		[SaveableData, CopyableData]
		public bool canSwim;

		[SaveableData, CopyableData]
		public bool canCrossLava;

		[SaveableData, CopyableData]
		public bool canFly;

		[SaveableData, CopyableData]
		public bool ignoreDoors;

		[SaveableData, CopyableData]
		public int climbPower;

		[LoadingInitializer, DeepCopyImplementation]
		public DynamicMovementSettings() {
			this.canCrossLand = true;
			this.canSwim = false;
			this.canCrossLava = false;
			this.canFly = false;
			this.ignoreDoors = false;
			this.climbPower = 2;
		}

		public void ShowValues() {
			Globals.SrcWriteLine("canCrossLand: " + canCrossLand);
			Globals.SrcWriteLine("canSwim: " + canSwim);
			Globals.SrcWriteLine("canCrossLava: " + canCrossLava);
			Globals.SrcWriteLine("canFly: " + canFly);
			Globals.SrcWriteLine("ignoreDoors: " + ignoreDoors);
			Globals.SrcWriteLine("climbPower: " + climbPower);
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

		bool IMovementSettings.CanCrossLand {
			get {
				return canCrossLand;
			}
		}
		bool IMovementSettings.CanSwim {
			get {
				return canSwim;
			}
		}
		bool IMovementSettings.CanCrossLava {
			get {
				return canCrossLava;
			}
		}
		bool IMovementSettings.CanFly {
			get {
				return canFly;
			}
		}
		bool IMovementSettings.IgnoreDoors {
			get {
				return ignoreDoors;
			}
		}
		int IMovementSettings.ClimbPower {
			get {
				return climbPower;
			}
		} //max positive difference in 1 step
	}

	[ViewableClass]
	public partial class NPCDef {
	}

	[ViewableClass]
	public partial class NPC : Character {

		public override short MaxHits {
			get {
				return maxHitpoints;
			}
			set {
				if (value != maxHitpoints) {
					CharSyncQueue.AboutToChangeHitpoints(this);
					this.maxHitpoints = value;

					//check the hitpoints regeneration
					RegenerationPlugin.TryInstallPlugin(this, this.Hits, this.maxHitpoints, this.HitsRegenSpeed);
				}
			}
		}

		public override short MaxMana {
			get {
				return maxMana;
			}
			set {
				if (value != maxMana) {
					CharSyncQueue.AboutToChangeMana(this);
					this.maxMana = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Mana, this.maxMana, this.ManaRegenSpeed);
					
					//meditation finish
					if (this.Mana >= MaxMana) {
						this.DeletePlugin(MeditationPlugin.meditationPluginKey);
					}
				}
			}
		}

		public override short MaxStam {
			get {
				return maxStamina;
			}
			set {
				if (value != maxStamina) {
					CharSyncQueue.AboutToChangeStamina(this);
					this.maxStamina = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Stam, this.maxStamina, this.StamRegenSpeed);
				}
			}
		}

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