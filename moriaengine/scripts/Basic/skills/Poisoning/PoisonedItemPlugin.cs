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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {


	[Dialogs.ViewableClass]
	public partial class PoisonedItemPlugin {
		static PluginKey poisonPK = PluginKey.Acquire("_poison_");


		internal static PoisonedItemPlugin Acquire(PoisonPotion potion) {
			PoisonedItemPlugin plugin = (PoisonedItemPlugin) PoisonedItemPluginDef.instance.Create();
			plugin.poisonDuration = potion.PoisonDuration;
			plugin.poisonPower = potion.PoisonPower;
			plugin.poisonTickInterval = potion.PoisonTickInterval;
			plugin.poisonType = potion.PoisonType;
			return plugin;
		}

		public static PoisonedItemPlugin GetPoisonPlugin(Item item) {
			return item.GetPlugin(poisonPK) as PoisonedItemPlugin;
		}

		public PoisonEffectPluginDef PoisonType {
			get {
				return this.poisonType;
			}
		}

		public int PoisonPower {
			get {
				return this.poisonPower;
			}
		}

		public TimeSpan PoisonTickInterval {
			get {
				return this.poisonTickInterval;
			}
		}

		public int PoisonDuration {
			get {
				return this.poisonDuration;
			}
		}

		public int PoisonDoses {
			get {
				return this.poisonDoses;
			}
		}
	}

	[Dialogs.ViewableClass]
	public partial class PoisonedItemPluginDef {
		public static readonly PoisonedItemPluginDef instance = (PoisonedItemPluginDef)
			new PoisonedItemPluginDef("p_poisoned_item", "C# scripts", -1).Register();
	}
}


