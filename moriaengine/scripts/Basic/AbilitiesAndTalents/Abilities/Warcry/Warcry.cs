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
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[Summary("War's warcry")]
	[ViewableClass]
	public class WarcryDef : ImmediateAbilityDef {
		public WarcryDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		#region triggerMethods
		protected override bool On_DenyActivate(DenyAbilityArgs args) {
			bool retVal = false;
			//TODO - zde je�t� implementovat to jestli doty�nej �ije/ne�ije atd.

			retVal = base.On_DenyActivate(args); //call superclass for common checks - including resources consuming etc
			return retVal;
		}

		[Summary("Functional implementation of warcry ability")]
		protected override bool On_Activate(Character chr, Ability ab) {
			//TODO - taky nejak zarvat nebo co !

			double power = ab.Def.EffectPower * ab.ModifiedPoints; //
			TimeSpan duration = TimeSpan.FromSeconds(ab.Def.EffectDuration);

			foreach (Player target in chr.GetMap().GetPlayersInRange(chr.X, chr.Y, (ushort) ComputeRange(chr))) {
				if (chr == target) {
					continue; //dont do selfwarcry ;)
				}

				WarcryEffectPlugin warcryEffect = (WarcryEffectPlugin) WarcryEffectPlugin.defInstance.Create();
				warcryEffect.Init(chr, EffectFlag.FromAbility | EffectFlag.HarmfulEffect, power, duration, this);
				target.AddPlugin(WarcryEffectPlugin.warcyEffectPluginKey, warcryEffect);
			}
			return false; //no cancel needed
		}
		#endregion triggerMethods

		[Summary("Compute the warcry range using the information from character (using i.e char's level" +
				" and the ability points...). Consider that 18 steps should be maximum (client limits)")]
		private int ComputeRange(Character chr) {
			//TODO - udelat to nejak sofistikovaneji			
			return chr.GetAbility(this);
		}

		[SteamFunction("Warcry")]
		[Summary("Running the warcry (if the player has the ability)")]
		public static void WarcryFunction(Character chr, ScriptArgs args) {
			WarcryDef wcrDef = SingletonScript<WarcryDef>.Instance;
			wcrDef.Activate(chr);
		}
	}

	[ViewableClass]
	public partial class WarcryEffectPlugin {

		public static readonly WarcryEffectPluginDef defInstance = 
			(WarcryEffectPluginDef) new WarcryEffectPluginDef("p_warcryEffect", "C# scripts", -1).Register();
		internal static PluginKey warcyEffectPluginKey = PluginKey.Acquire("_warcryEffect_");

		public bool On_SkillSelect(SkillSequenceArgs ssa) {
			if (ssa.SkillDef.Id == (int) SkillName.Magery) {
				((Character) this.Cont).RedMessage("Jsi v �oku a nem��e� kouzlit!");
				return true;
			}
			return false;
		}

		public bool On_SkillStart(SkillSequenceArgs ssa) {
			if (ssa.SkillDef.Id == (int) SkillName.Magery) {
				((Character) this.Cont).RedMessage("Jsi v �oku a nem��e� kouzlit!");
				return true;
			}
			return false;
		}

		public void On_Assign() {
			((Character) Cont).RedMessage("Jsi v �oku!");
		}

		public override void On_UnAssign(Character formerCont) {
			formerCont.SysMessage("Neblah� ��inky warcry pominuly");
			//base.On_UnAssign(formerCont);
		}
	}
}