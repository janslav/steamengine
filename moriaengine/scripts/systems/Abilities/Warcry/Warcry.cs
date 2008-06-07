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
	public class Warcry : ImmediateAbilityDef {

		[Summary("Number of seconds the warcry effect will last on the hit player")]
		private FieldValue effectDuration;
		
		public Warcry(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			effectDuration = InitField_Typed("effectDuration", 10, typeof(int));
		}
		
		#region triggerMethods
		[Summary("This method implements firing the ability. Will be implemened ond special children")]
		protected override void On_Fire(Character chr) {
			foreach (Player plr in chr.GetMap().GetPlayersInRange(chr.X, chr.Y, ComputeRange(chr))) {
				if(chr == plr) {
					continue; //dont do selfwarcry ;)
				}
				//first try to get the plugin from the playr (he may be under the warcry effect from someone other)
				WarcryEffectPlugin wepl = plr.GetPlugin(WarcryEffectPlugin.warcyEffectPluginKey) as WarcryEffectPlugin;
				if(wepl == null) {
					wepl = (WarcryEffectPlugin)plr.AddNewPlugin(WarcryEffectPlugin.warcyEffectPluginKey, WarcryEffectPlugin.defInstance);				
				}
				//anyways, set the duration of the warcry effect (either on the newly added plugin or the old one)
				if(wepl.Timer < EffectDuration) {
					//but dont make it shorter >:-)
					wepl.Timer = EffectDuration;
				}
			}
		}		
		#endregion triggerMethods

		[InfoField("Usage delay")]
		public override int UseDelay {			
			set {
				useDelay.CurrentValue = value;		
			}
		}

		[InfoField("Warcry effect duration")]
		public int EffectDuration {
			get {
				return (int)effectDuration.CurrentValue;
			}
			set {
				effectDuration.CurrentValue = value;
			}
		}

		[Summary("Compute the warcry range using the information from character (using i.e char's level"+
				" and the ability points...). Consider that 18 steps should be maximum (client limits)")]
		private ushort ComputeRange(Character chr) {
			//TODO - udelat to nejak sofistikovaneji
			Ability ab = chr.GetAbility(this);
			return ab.Points;
		}

		[SteamFunction("Warcry")]
		[Summary("Running the warcry (if the player has the ability)")]
		public static void WarcryFunction(Character chr, ScriptArgs args) {
			Warcry wcrDef = SingletonScript<Warcry>.Instance;
			if(AbilityDef.CanUseAbility(chr,wcrDef)) {
				wcrDef.Activate(chr);
			}
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
