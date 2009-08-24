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

		public static void UnInstallCraftingPlugin(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				p.Delete();
			}
		}

		public static SimpleQueue<CraftingSelection> GetCraftingQueue(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				return p.craftingOrder.SelectionQueue;
			}
			return null;
		}

		public static void StartCrafting(Character self, CraftingOrder crOrd) {
			CraftingProcessPlugin p = InstallCraftingPlugin(self, crOrd);
			p.StartCrafting();
		}

		private void StartCrafting() {
			SkillSequenceArgs ssa = SkillSequenceArgs.Acquire((Character)Cont, craftingOrder.CraftingSkill);

			CraftingSelection csl = this.craftingOrder.SelectionQueue.Peek();
			ssa.Param1 = (csl != null ? csl.ItemDef : null);  //either selected ItemDef or null (both is acceptable)
			
			ssa.PhaseSelect();
		}

		[Summary("Static method called after finishing with one item crafting - either successfull or failed. "+
				"If success - lower the amount of items to be made and (if anything is yet to be made) start again. "+
				"If failed - start the craftmaking again immediatelly")]
		internal static void MakeFinished(SkillSequenceArgs skillSeqArgs) {
			Character cont = skillSeqArgs.Self;
			CraftingProcessPlugin pl = (CraftingProcessPlugin)cont.GetPlugin(craftingProcessPK);

			if (skillSeqArgs.Success) {
				CraftingSelection crSel = pl.craftingOrder.SelectionQueue.Peek(); //actual crafted item and its to-make count
				crSel.Count -= 1;//lower the amount of items to be made
				if (crSel.Count <= 0) {
					pl.craftingOrder.SelectionQueue.Dequeue(); //remove from the queue
				}
				if (pl.craftingOrder.SelectionQueue.Count > 0) { //still something to be made
					pl.StartCrafting(); //start again with next item
				} else {//nothing left, finish crafting
					cont.SysMessage(Loc<CraftSkillsLoc>.Get(cont.Language).FinishCrafting);
					pl.Delete(); //same as calling "uninstall plugin"
				}
			} else {//failed, start again now
				pl.StartCrafting();
			}
		}

		internal static void MakeImpossible(SkillSequenceArgs skillSeqArgs) {
			Character cont = skillSeqArgs.Self;
			CraftingProcessPlugin pl = (CraftingProcessPlugin) cont.GetPlugin(craftingProcessPK);
			
			ItemDef iDefToMake = (ItemDef)skillSeqArgs.Param1;

			pl.craftingOrder.SelectionQueue.Dequeue(); //remove the item from the queue
			if (pl.craftingOrder.SelectionQueue.Count > 0) { //still something to be made
				cont.SysMessage(String.Format(
					Loc<CraftSkillsLoc>.Get(cont.Language).ResourcesLackingButContinue,
					iDefToMake.Name));
				pl.StartCrafting(); //start again with next item(s)
			} else {
				cont.SysMessage(String.Format(
					Loc<CraftSkillsLoc>.Get(cont.Language).ResourcesLackingAndFinish,
					iDefToMake.Name));
				pl.Delete(); //same as calling "uninstall plugin"
			}
		}

		private static CraftingProcessPlugin InstallCraftingPlugin(Character self, CraftingOrder craftingOrder) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p == null) {
				p = (CraftingProcessPlugin) self.AddNewPlugin(craftingProcessPK, CraftingProcessPluginDef.instance);
			}
			p.craftingOrder = craftingOrder;
			return p;
		}
	}

	partial class CraftingProcessPluginDef {
		public static CraftingProcessPluginDef instance = new CraftingProcessPluginDef("p_craftingProcess", "C# scripts", -1);
	}
}
