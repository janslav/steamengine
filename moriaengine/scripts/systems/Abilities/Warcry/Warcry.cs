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
		private FieldValue effectDuration;

		public WarcryDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			effectDuration = InitField_Typed("effectDuration", 10, typeof(double));
		}

		#region triggerMethods
		protected override bool On_DenyUse(DenyAbilityArgs args) {
			bool retVal = false;
			//TODO - zde ještì implementovat to jestli dotyènej žije/nežije atd.

			retVal = base.On_DenyUse(args); //call superclass for common checks - including resources consuming etc
			return retVal;
		}

		[Summary("Functional implementation of warcry ability")]
		protected override bool On_Activate(Character chr) {
			//TODO - taky nejak zarvat nebo co !
			foreach (Player plr in chr.GetMap().GetPlayersInRange(chr.X, chr.Y, (ushort) ComputeRange(chr))) {
				if (chr == plr) {
					continue; //dont do selfwarcry ;)
				}
				//first try to get the plugin from the player (he may be under the warcry effect from someone else)
				WarcryEffectPlugin wepl = plr.GetPlugin(WarcryEffectPlugin.warcyEffectPluginKey) as WarcryEffectPlugin;
				if (wepl == null) {
					wepl = (WarcryEffectPlugin) plr.AddNewPlugin(WarcryEffectPlugin.warcyEffectPluginKey, WarcryEffectPlugin.defInstance);
				}
				//anyways, set the duration of the warcry effect (either on the newly added plugin or the old one)
				if (wepl.Timer < EffectDuration) {
					//but dont make it shorter >:-)
					wepl.Timer = EffectDuration;
				}
			}
			return false; //no cancel needed
		}
		#endregion triggerMethods

		[InfoField("Effect duration")]
		[Summary("Number of seconds the warcry effect will last on the hit player")]
		public double EffectDuration {
			get {
				return (double) effectDuration.CurrentValue;
			}
			set {
				effectDuration.CurrentValue = value;
			}
		}

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

		public static readonly WarcryEffectPluginDef defInstance = new WarcryEffectPluginDef("p_warcryEffect", "C#scripts", -1);
		internal static PluginKey warcyEffectPluginKey = PluginKey.Get("_warcryEffect_");

		//TODO - az bde magie tak sem udelat nejakj on_spellcast zrusit kouzlo... 

		public void On_Assign(Character cont) {
			cont.RedMessage("Jsi v šoku!");
		}

		public void On_UnAssign(Character cont) {
			cont.SysMessage("Neblahé úèinky warcry pominuly");
		}

		public void On_Timer() {
			this.Delete();
		}
	}
}
