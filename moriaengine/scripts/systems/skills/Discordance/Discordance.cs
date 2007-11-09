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
using SteamEngine;
using SteamEngine.Persistence;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class DiscordanceSkillDef : SkillDef {

		public DiscordanceSkillDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
		}

		private static TriggerGroup t_Musical;

		public TriggerGroup T_Musical {
			get {
				if (t_Musical == null) {
					t_Musical = TriggerGroup.Get("t_musical");
				}
				return t_Musical;
			}
		}

		public override void Select(AbstractCharacter ch) {
			Character self = (Character) ch;
			self.currentSkillTarget2 = ((Item)self.BackpackAsContainer).FindType(T_Musical);
			((Player)self).Target(SingletonScript<Targ_Discordance>.Instance);
		}

		internal override void Start(Character self) {
			if (!this.Trigger_Start(self)) {
				self.SysMessage("Pokousis se oslabit " + ((Character) self.currentSkillTarget1).Name + ".");
				DelaySkillStroke(self);
			}
		}
		
		public override void Stroke(Character self) {
			if (!this.Trigger_Stroke(self)) {
				if (SkillValueOfChar(((Character) self.currentSkillTarget1)) != 0) {
					self.SysMessage("Tohle nelze oslabit.");
				} else if (Convert.ToInt32(self.currentSkillParam) == 1) {
					double targExperience = ((Character) self.currentSkillTarget1).Experience;
					if ((SkillValueOfChar(self) * 0.3) < targExperience) {
						self.SysMessage("Oslabeni tohoto cile presahuje tve moznosti.");
					} else {
						double discordancePower = ScriptUtil.EvalRandomFaktor(SkillValueOfChar(self), 0, 300);
						if (discordancePower > targExperience) {
							Success(self);
							return;
						} else {
							self.SysMessage("Oslabeni se nepovedlo.");
						}
					}
				} else {
					self.SysMessage("Oslabeni se nepovedlo.");
				}
				Fail(self);
			}
		}

		internal static PluginKey effectPluginKey = PluginKey.Get("_discordanceEffect_");

		public override void Success(Character self) {
			if (!this.Trigger_Success(self)) {
				Character skillTarget = (Character) self.currentSkillTarget1;

				if (skillTarget.HasPlugin(effectPluginKey)) {
					self.SysMessage("Cil je jiz oslaben.");
				} else {
					self.SysMessage("Uspesne jsi oslabil cil.");
					DiscordanceEffectPlugin plugin = (DiscordanceEffectPlugin) DiscordanceEffectPlugin.defInstance.Create();
					plugin.discordEffectPower = SkillValueOfChar(self);
					skillTarget.AddPluginAsSimple(effectPluginKey, plugin);
				}
			}
			self.currentSkill = null;
		}
		
		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
				self.Trigger_HostileAction(self);
			}
			self.currentSkill = null;
		}
		
		protected internal override void Abort(Character self) {
			this.Trigger_Abort(self);
			self.SysMessage("Oslabovani bylo predcasne preruseno.");
		}
	}

	public class Targ_Discordance : CompiledTargetDef {

		protected override void On_Start(Character self, object parameter)
		{
			self.SysMessage("Koho chces zkusit oslabit?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Character self, Character targetted, object parameter)
		{
			if (targetted.IsPlayer) {
				self.SysMessage("Zameruj jenom monstra!");
				return false;
			} else if (self.currentSkill != null) {
				self.ClilocSysMessage(500118);                    //You must wait a few moments to use another skill.
				return false;
			} else if (targetted.HasPlugin(DiscordanceSkillDef.effectPluginKey)) {
				self.SysMessage("Cil je jiz oslaben.");
				self.currentSkill = null;
				return false;
			}
			self.SelectSkill((int)SkillName.Musicianship);
			if ((int)self.currentSkillParam == 2) {
				return false;
			}
			self.currentSkillTarget1 = targetted;
			self.StartSkill((int)SkillName.Discordance);
			return false;
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter)
		{
			self.SysMessage("Predmety nelze oslabit.");
			return false;
		}
	}

	[Dialogs.ViewableClass]
	public partial class DiscordanceEffectPlugin {

		public static readonly DiscordanceEffectPluginDef defInstance = new DiscordanceEffectPluginDef("p_discordanceEffect_", "C#scripts", -1);

		public void On_Assign() {
			Character cont = (Character) this.Cont;
			
			int lowerConst = discordEffectPower / 4;

			lowed_dex = (short) DiscordanceValueLower(cont.Dex, lowerConst);
			lowed_str = (short) DiscordanceValueLower(cont.Str, lowerConst);
			lowed_int = (short) DiscordanceValueLower(cont.Int, lowerConst);
			lowed_hits = (short) DiscordanceValueLower(cont.MaxHits, lowerConst);
			lowed_mana = (short) DiscordanceValueLower(cont.MaxMana, lowerConst);
			lowed_stam = (short) DiscordanceValueLower(cont.MaxStam, lowerConst);
			lowed_ei = (ushort) DiscordanceValueLower(cont.Skills[16].RealValue, lowerConst);
			lowed_magery = (ushort) DiscordanceValueLower(cont.Skills[25].RealValue, lowerConst);
			lowed_resist = (ushort) DiscordanceValueLower(cont.Skills[26].RealValue, lowerConst);
			lowed_poison = (ushort) DiscordanceValueLower(cont.Skills[30].RealValue, lowerConst);
			lowed_archer = (ushort) DiscordanceValueLower(cont.Skills[31].RealValue, lowerConst);
			lowed_twohand = (ushort) DiscordanceValueLower(cont.Skills[40].RealValue, lowerConst);
			lowed_mace = (ushort) DiscordanceValueLower(cont.Skills[41].RealValue, lowerConst);
			lowed_onehand = (ushort) DiscordanceValueLower(cont.Skills[42].RealValue, lowerConst);
			lowed_wrestl = (ushort) DiscordanceValueLower(cont.Skills[43].RealValue, lowerConst);

			cont.Dex -= lowed_dex;
			cont.Str -= lowed_str;
			cont.Int -= lowed_int;
			cont.MaxHits -= lowed_hits;
			cont.MaxMana -= lowed_mana;
			cont.MaxStam -= lowed_stam;
			cont.Skills[16].RealValue -= lowed_ei;
			cont.Skills[25].RealValue -= lowed_magery;
			cont.Skills[26].RealValue -= lowed_resist;
			cont.Skills[30].RealValue -= lowed_poison;
			cont.Skills[31].RealValue -= lowed_archer;
			cont.Skills[40].RealValue -= lowed_twohand;
			cont.Skills[41].RealValue -= lowed_mace;
			cont.Skills[42].RealValue -= lowed_onehand;
			cont.Skills[43].RealValue -= lowed_wrestl;

			if (cont.Hits > cont.MaxHits) {
				cont.Hits = cont.MaxHits;
			}
			if (cont.Mana > cont.MaxMana) {
				cont.Mana = cont.MaxMana;
			}
			if (cont.Stam > cont.MaxStam) {
				cont.Stam = cont.MaxStam;
			}
			this.Timer = ScriptUtil.EvalRangePermille(discordEffectPower, 10, 15);
		}

		public void On_UnAssign(Character cont) {
			cont.Dex += lowed_dex;
			cont.Str += lowed_str;
			cont.Int += lowed_int;
			cont.MaxHits += lowed_hits;
			cont.MaxMana += lowed_mana;
			cont.MaxStam += lowed_stam;
			cont.Skills[16].RealValue += lowed_ei;
			cont.Skills[25].RealValue += lowed_magery;
			cont.Skills[26].RealValue += lowed_resist;
			cont.Skills[30].RealValue += lowed_poison;
			cont.Skills[31].RealValue += lowed_archer;
			cont.Skills[40].RealValue += lowed_twohand;
			cont.Skills[41].RealValue += lowed_mace;
			cont.Skills[42].RealValue += lowed_onehand;
			cont.Skills[43].RealValue += lowed_wrestl;
		}

		private static int DiscordanceValueLower(int value,int lowerConst) {
			int lowedVal = ((value * lowerConst) / 1000);
			if (value < lowedVal) {
				lowedVal = value;
			}
			return lowedVal;
		}
		
		public void On_Timer() {
			this.Delete();	
		}
	}
}