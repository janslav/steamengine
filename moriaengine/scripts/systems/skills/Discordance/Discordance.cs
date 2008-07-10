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

		public DiscordanceSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
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

		protected override void On_Select(Character ch) {
			Character self = (Character) ch;
			self.currentSkillTarget2 = ((Item) self.BackpackAsContainer).FindType(T_Musical);
			((Player) self).Target(SingletonScript<Targ_Discordance>.Instance);
		}

		protected override void On_Start(Character self) {
			self.SysMessage("Pokousis se oslabit " + ((Character) self.currentSkillTarget1).Name + ".");
			DelaySkillStroke(self);
		}

		protected override void On_Stroke(Character self) {
			if (SkillValueOfChar(((Character) self.currentSkillTarget1)) != 0) {
				self.SysMessage("Tohle nelze oslabit.");
			} else if (Convert.ToInt32(self.currentSkillParam) == 1) {
				double targExperience = ((Character) self.currentSkillTarget1).Experience;
				if ((SkillValueOfChar(self) * 0.3) < targExperience) {
					self.SysMessage("Oslabeni tohoto cile presahuje tve moznosti.");
				} else {
					double discordancePower = ScriptUtil.EvalRandomFaktor(SkillValueOfChar(self), 0, 300);
					if (discordancePower > targExperience) {
						this.Success(self);
						return;
					} else {
						self.SysMessage("Oslabeni se nepovedlo.");
					}
				}
			} else {
				self.SysMessage("Oslabeni se nepovedlo.");
			}
			this.Fail(self);
		}

		internal static PluginKey effectPluginKey = PluginKey.Get("_discordanceEffect_");

		protected override void On_Success(Character self) {
			Character skillTarget = (Character) self.currentSkillTarget1;

			if (skillTarget.HasPlugin(effectPluginKey)) {
				self.SysMessage("Cil je jiz oslaben.");
			} else {
				self.SysMessage("Uspesne jsi oslabil cil.");
				DiscordanceEffectPlugin plugin = (DiscordanceEffectPlugin) DiscordanceEffectPlugin.defInstance.Create();
				plugin.discordEffectPower = SkillValueOfChar(self);
				skillTarget.AddPluginAsSimple(effectPluginKey, plugin);
			}
			self.currentSkill = null;
		}

		protected override void On_Fail(Character self) {
			self.Trigger_HostileAction(self);
			self.currentSkill = null;
		}

		protected override void On_Abort(Character self) {
			self.SysMessage("Oslabovani bylo predcasne preruseno.");
		}
	}

	public class Targ_Discordance : CompiledTargetDef {

		protected override void On_Start(Character self, object parameter) {
			self.SysMessage("Koho chces zkusit oslabit?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Character self, Character targetted, object parameter) {
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
			self.SelectSkill(SkillName.Musicianship);
			if ((int) self.currentSkillParam == 2) {
				return false;
			}
			self.currentSkillTarget1 = targetted;
			self.StartSkill(SkillName.Discordance);
			return false;
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
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
			lowed_ei = (ushort) DiscordanceValueLower(cont.SkillById((int)SkillName.EvalInt).RealValue, lowerConst);
			lowed_magery = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Magery).RealValue, lowerConst);
			lowed_resist = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.MagicResist).RealValue, lowerConst);
			lowed_poison = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Poisoning).RealValue, lowerConst);
			lowed_archer = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Archery).RealValue, lowerConst);
			lowed_twohand = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Swords).RealValue, lowerConst);
			lowed_mace = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Macing).RealValue, lowerConst);
			lowed_onehand = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Fencing).RealValue, lowerConst);
			lowed_wrestl = (ushort) DiscordanceValueLower(cont.SkillById((int) SkillName.Wrestling).RealValue, lowerConst);

			cont.Dex -= lowed_dex;
			cont.Str -= lowed_str;
			cont.Int -= lowed_int;
			cont.MaxHits -= lowed_hits;
			cont.MaxMana -= lowed_mana;
			cont.MaxStam -= lowed_stam;
			cont.SkillById((int) SkillName.EvalInt).RealValue -= lowed_ei;
			cont.SkillById((int) SkillName.Magery).RealValue -= lowed_magery;
			cont.SkillById((int) SkillName.MagicResist).RealValue -= lowed_resist;
			cont.SkillById((int) SkillName.Poisoning).RealValue -= lowed_poison;
			cont.SkillById((int) SkillName.Archery).RealValue -= lowed_archer;
			cont.SkillById((int) SkillName.Swords).RealValue -= lowed_twohand;
			cont.SkillById((int) SkillName.Macing).RealValue -= lowed_mace;
			cont.SkillById((int) SkillName.Fencing).RealValue -= lowed_onehand;
			cont.SkillById((int) SkillName.Wrestling).RealValue -= lowed_wrestl;

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
			cont.SkillById((int) SkillName.EvalInt).RealValue += lowed_ei;
			cont.SkillById((int) SkillName.Magery).RealValue += lowed_magery;
			cont.SkillById((int) SkillName.MagicResist).RealValue += lowed_resist;
			cont.SkillById((int) SkillName.Poisoning).RealValue += lowed_poison;
			cont.SkillById((int) SkillName.Archery).RealValue += lowed_archer;
			cont.SkillById((int) SkillName.Swords).RealValue += lowed_twohand;
			cont.SkillById((int) SkillName.Macing).RealValue += lowed_mace;
			cont.SkillById((int) SkillName.Fencing).RealValue += lowed_onehand;
			cont.SkillById((int) SkillName.Wrestling).RealValue += lowed_wrestl;			
		}

		private static int DiscordanceValueLower(int value, int lowerConst) {
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