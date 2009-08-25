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
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public abstract class CraftingSkillDef : SkillDef {

		public CraftingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		[Summary("This trigger is used only when clicked the skill-list blue radio button for some crafting skill."+
				"opens the craftmenu for the given skill")]
		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!CheckPrerequisities(skillSeqArgs) || !DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return true;//something wrong, finish now
			}

			if (skillSeqArgs.Param1 != null) {
				return false; //do not stop, we have some item here, we can start crafting now
			} else {//no item pre-selected, open the craftmenu
				self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[(SkillName) this.Id]));
				//do not continue, the rest will be solved from the craftmenu
				return true; //F = continue to @start, T = stop
			}
		}

		[Summary("This trigger is called when OK button is clicked from the craftmenu. One item from the queue is picked and its making process "+
			"begins here (i.e. the making success and the number of strokes is pre-computed here")]
		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!CheckPrerequisities(skillSeqArgs) || !DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return true;//something wrong, finish now
			}

			ItemDef iDefToMake = (ItemDef)skillSeqArgs.Param1;//the on_start trigger runs only if there is something here...
				//get the strokes count (i.e. number of animations and skillmaking sounds before the item is made)
			double[] itemStrokes = iDefToMake.Strokes; //always 2-item array
				//compute the actual strokes count to make the selected item for "self" char
			int strokes = (int)Math.Round(ScriptUtil.EvalRangePermille(this.SkillValueOfChar(self), itemStrokes));
			//check the Success now / TODO> item difficulty
			skillSeqArgs.Success = this.CheckSuccess(self, iDefToMake.Difficulty);
			if (!skillSeqArgs.Success) {//the item making will fail
				skillSeqArgs.Param2 = (int)Math.Round(ScriptUtil.GetRandInRange(1, strokes));//fail will occur after a few strokes (not necessary immediatelly)
			} else {//item will be created, with pre-computed number of strokes
				skillSeqArgs.Param2 = strokes;
			}
			return false; //continue to delay, then @stroke
		}

		[Summary("This trigger is run a pre-computed number of times before the item is either created or failed."+
				"The item-making animations and sounds are run from here")]
		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!CheckPrerequisities(skillSeqArgs) || !DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(skillSeqArgs.Self);
				return true;//something wrong, finish now
			}

			int strokesCnt = Convert.ToInt32(skillSeqArgs.Param2);
			if (strokesCnt > 1) {
				//do the animation, sound and repeat the stroke
				DoStroke(skillSeqArgs);
				skillSeqArgs.Param2 = strokesCnt - 1;
				skillSeqArgs.DelayStroke();
				return true; //stop here for now (we are waiting for the next stroke round...)
			}
			DoStroke(skillSeqArgs);//do the animation, sound and finish
			return false; //continue to @success or @fail (the result is already prepared from the "@start" phase
		}

		[Summary("This trigger is run in case the item making succeeds. It consumes the desired number of resources, "+
				"creates the item instance and checks if there are any other items to be created in the queue. If so, it "+
				"re-runs the @start trigger")]
		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Player self = (Player) skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!CheckPrerequisities(skillSeqArgs) || !DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return true;//something wrong, finish now
			}

			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;

			IResourceListItem missingResource;
			bool canMake = true;
			//first check the necessary resources to have (GM needn't have anything) (if any resources are needed)
			if (!self.IsGM && iDefToMake.SkillMake != null && !iDefToMake.SkillMake.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingResource)) {
				ResourcesList.SendResourceMissingMsg(self, missingResource);
				canMake = false;
			}

			if (canMake) {//if still OK try to consume the consumable resources (if any) otherwise step over this block
				if (!self.IsGM && iDefToMake.Resources != null && !iDefToMake.Resources.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missingResource)) {
					ResourcesList.SendResourceMissingMsg(self, missingResource);
					canMake = false;
				} else {//resources consumed or we are GM or no resources needed, create the item and place it to the pre-defined (or default) location
					Item newItem = (Item) iDefToMake.Create(self.ReceivingContainer);
					//self.ClilocSysMessage(500638);//You create the item and put it in your backpack.
					self.SysMessage(String.Format(
								Loc<CraftSkillsLoc>.Get(self.Language).ItemMadeAndPutInRecCont,
								iDefToMake.Name, self.ReceivingContainer.Name));
					DoSuccess(skillSeqArgs, newItem);

					CraftingProcessPlugin.MakeFinished(skillSeqArgs);
				}
			}

			//deal with failure - not enough resources 
			if (!canMake) {
				CraftingProcessPlugin.MakeImpossible(skillSeqArgs);
				return true; //stop the trigger, the rest will be decided by the CraftingProcessPlugin
			}

			return false;
		}

		[Summary("This trigger is run in case the item making fails. It consumes some of the desired number of resources (i.e. these are wasted), " +
				"and checks if there are any other items to be created in the queue. If so, it " +
				"re-runs the @start trigger")]
		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) { //checking alive will be enough
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return true;//no message needed, it's been already sent in the called method
			}

			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;

			iDefToMake.Resources.ConsumeSomeResources(self, ResourcesLocality.Backpack);
			self.SysMessage(String.Format(
						Loc<CraftSkillsLoc>.Get(self.Language).ItemMakingFailed,
						iDefToMake.Name));
			CraftingProcessPlugin.MakeFinished(skillSeqArgs);
			return true; //stop the trigger, the rest will be decided by the CraftingProcessPlugin
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<CraftSkillsLoc>.Get(self.Language).CraftingAborted);
			CraftingProcessPlugin.UnInstallCraftingPlugin(self);
		}

		[Summary("This method is intended to be overriden by particular CraftingSkillDef classes. " +
				"Its purpose is to make something related to the stroke of the particular skill (such as playing some sound...)")]
		protected virtual void DoStroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(skillSeqArgs.SkillDef.Defname + " stroke");
		}

		[Summary("This method is intended to be overriden by particular CraftingSkillDef classes. " +
				"Its purpose is to make something related to the success of the particular skill (such as playing sound,"+
				"computing some special item characteristics etc.)")]
		protected virtual void DoSuccess(SkillSequenceArgs skillSeqArgs, Item newItem) {
			//TODO pocitat nejake ty "exceptional" vlastnosti, podepisovani predmetu atp.
			Character self = skillSeqArgs.Self;
			self.SysMessage(skillSeqArgs.SkillDef.Defname + " success");
		}		

		[Summary("This method is intended to be overriden by particular CraftingSkillDef classes. " +
				"Its purpose is to check any additional pre-requisities for succesfull skill usage "+
				"(e.g. for BS check if the anvil or forge is near etc...)."+
				"Return false if the trigger above should be cancelled or true if we can continue")]
		protected virtual bool DoCheckSpecials(SkillSequenceArgs skillSeqArgs) {
			return true;
		}

		[Remark("Check if we are alive, have enough stats etc.... Return false if the trigger above" +
				" should be cancelled or true if we can continue")]
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Stam <= self.MaxStam / 10) {
				self.ClilocSysMessage(501991);//You are too fatigued to even lift a finger.
				return false; //stop
			}
			return true;
		}
	}

	internal class CraftSkillsLoc : CompiledLocStringCollection {
		public string ResourcesLackingButContinue = "Not enough resources to make {0}, continuing with other items.";
		public string ResourcesLackingAndFinish = "Not enough resources to make {0}, crafting is finished.";
		public string ItemMakingFailed = "You have failed to make {0} and lost some of the resources";
		public string FinishCrafting = "Crafting is finished";
		public string CraftingAborted = "Crafting aborted";
		//500638 - You create the item and put it in your backpack.
		public string ItemMadeAndPutInRecCont = "You create the {0} and put it in your {1}";
	}
}