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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	partial class CraftingProcessPlugin {
		private static PluginKey craftingProcessPK = PluginKey.Get("_craftingProcess_");

		public static void InstallCraftingPlugin(Character self, SimpleQueue<CraftingSelection> craftingOrder, CraftingSkillDef craftingSkill) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p == null) {
				p = (CraftingProcessPlugin) self.AddNewPlugin(craftingProcessPK, CraftingProcessPluginDef.instance);
			}
			p.craftingOrder = craftingOrder;
			p.craftingSkill = craftingSkill;
		}

		public static void UnInstallCraftingPlugin(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				p.Delete();
			}
		}

		public static SimpleQueue<CraftingSelection> GetCraftingQueue(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				return p.craftingOrder;
			}
			return null;
		}

		public static void StartCrafting(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				p.StartCrafting();
			}
		}

		private void StartCrafting() {
			SkillSequenceArgs ssa = SkillSequenceArgs.Acquire((Character)Cont, craftingSkill);
			ssa.PhaseStart(); //start making...
		}
	}

	partial class CraftingProcessPluginDef {
		public static CraftingProcessPluginDef instance = new CraftingProcessPluginDef("p_craftingProcess", "C# scripts", -1);
	}
}
