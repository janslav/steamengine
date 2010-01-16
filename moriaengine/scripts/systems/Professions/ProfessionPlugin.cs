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
using System.Timers;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass("Profession")]
	[Summary("This class holds the profession assigned to one character (cont field). " +
			"it holds a reference to the ProfessionDef, dispatches all trigger calls etc.")]
	public partial class ProfessionPlugin {
		internal static PluginKey professionKey = PluginKey.Acquire("_profession_");

		private ProfessionDef def;

		[Summary("Initialize the profession on the character")]
		public void On_Assign(Player newCont) {
			Sanity.IfTrueThrow(this.def == null, "this.def == null on ProfessionPlugin assigning");

			//maybe we should do this on the first use of said skill/spell/ability...? Or maybe just in the startroom stone script?
			//changing of profession should be available for testing on players, without messing them up right away...

			//SkillName oneName;
			//foreach (ISkill skill in ((Player) Cont).Skills) {//the Cont of the Profession is always Player...
			//    oneName = (SkillName) skill.Id;
			//    skill.RealValue = Math.Min(def.BasicSkill(oneName), def.MaxSkill(oneName));
			//}
		}

		public ProfessionDef ProfessionDef {
			get {
				return def;
			}
		}

		//we need a "deny" version of skillstart/select triggers

		//public virtual bool On_SkillSelect(SkillSequenceArgs skillSeqArgs) {
		//    return this.CheckCancelSkill((Player) this.Cont, skillSeqArgs);
		//}

		//public virtual bool On_SkillStart(SkillSequenceArgs skillSeqArgs) {
		//    return this.CheckCancelSkill((Player) this.Cont, skillSeqArgs);
		//}

		//private bool CheckCancelSkill(Player player, SkillSequenceArgs skillSeqArgs) {
		//    throw new Exception("The method or operation is not implemented.");
		//}

		public virtual bool On_AbilityDenyAssign(DenyAbilityArgs args) {
			return this.CheckDenyAbility((Player) this.Cont, args);
		}

		public virtual bool On_AbilityDenyUse(DenyAbilityArgs args) {
			return this.CheckDenyAbility((Player) this.Cont, args);
		}

		private bool CheckDenyAbility(Player player, DenyAbilityArgs args) {
			if (!player.IsGM) {
				if (!def.CanUseAbility(args.ranAbilityDef)) {
					args.Result = DenyResultAbilities.Deny_NotAllowedToHaveThisAbility;
					return true;
				}
			}
			return false; //do not cancel
		}

		public static ProfessionPlugin GetInstalledPlugin(Player player) {
			return (ProfessionPlugin) player.GetPlugin(professionKey);
		}

		public static void InstallProfessionPlugin(Player player, ProfessionDef value) {
			if (value != null) {
				ProfessionPlugin newPlugin = (ProfessionPlugin) value.ProfessionPluginDef.Create();
				newPlugin.def = value;
				player.AddPlugin(professionKey, newPlugin); //if there was a previous one, it's deleted now
			} else {
				player.DeletePlugin(professionKey); //null professiondef = no professionplugin
			}
		}
	}
}