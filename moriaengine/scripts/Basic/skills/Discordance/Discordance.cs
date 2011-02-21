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

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Musical instrument = skillSeqArgs.Tool as Musical;
			if (instrument != null) {
				if (instrument.TopObj() != self) {
					instrument = null;
				}
			}
			if (instrument == null) {
				instrument = (Musical) self.Backpack.FindByClass(typeof(Musical));
			}
			if (instrument == null) {
				self.SysMessage("Nemáš u sebe hudební nástroj.");
				skillSeqArgs.Success = false;
				return TriggerResult.Cancel;
			}
			skillSeqArgs.Tool = instrument;

			Character target = skillSeqArgs.Target1 as Character;
			if (target == null) {
				Player selfAsPlayer = self as Player;
				if (selfAsPlayer != null) {
					selfAsPlayer.Target(SingletonScript<Targ_Discordance>.Instance, skillSeqArgs);
					return TriggerResult.Cancel;
				} else {
					throw new SEException("Discordance target not set for nonplayer");
				}
			}

			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			SkillSequenceArgs musicianship = SkillSequenceArgs.Acquire(self, SkillName.Musicianship, skillSeqArgs.Tool, true); //true = parameter for the musicianship @Stroke, it won't proceed with the skill
			musicianship.PhaseStroke();

			if (musicianship.Tool != null) {
				skillSeqArgs.Success = musicianship.Success;

				self.SysMessage("Pokousis se oslabit " + ((Character) skillSeqArgs.Target1).Name + ".");
				return TriggerResult.Continue;
			} else {
				//skillSeqArgs.Dispose();
				return TriggerResult.Cancel; //we lost the instrument or something
			}
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			if (skillSeqArgs.Tool.IsDeleted || skillSeqArgs.Tool.TopObj() != self) {
				self.SysMessage("Nemáš u sebe hudební nástroj.");
				skillSeqArgs.PhaseAbort();
				return TriggerResult.Cancel;
			}

			Character target = (Character) skillSeqArgs.Target1;

			if (this.SkillValueOfChar(target) > 0) {
				self.SysMessage("Tohle nelze oslabit.");
			} else if (skillSeqArgs.Success) { //set by Musicianship in @Start
				//TODO experience revamp

				//double targExperience = target.Experience;
				//int mySkillValue = this.SkillValueOfChar(self);
				//if ((mySkillValue * 0.3) < targExperience) {
				//    self.SysMessage("Oslabeni tohoto cile presahuje tve moznosti.");
				//} else {
				//    double discordancePower = ScriptUtil.EvalRandomFaktor(mySkillValue, 0, 300);
				//    if (discordancePower <= targExperience) {
				//        skillSeqArgs.Success = false;
				//    } //else success stays true
				//}
			}
			return TriggerResult.Continue;
		}

		internal static PluginKey effectPluginKey = PluginKey.Acquire("_discordanceEffect_");

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;

			if (target.HasPlugin(effectPluginKey)) {
				self.SysMessage("Cíl je již oslaben.");
			} else {
				self.SysMessage("Úspìšnì jsi oslabil cíl.");
				DiscordanceEffectPlugin plugin = (DiscordanceEffectPlugin) DiscordanceEffectPlugin.defInstance.Create();
				plugin.discordEffectPower = this.SkillValueOfChar(self);
				target.AddPluginAsSimple(effectPluginKey, plugin);
				self.Trigger_HostileAction(self);
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			self.SysMessage("Oslabení se nepovedlo.");
			self.Trigger_HostileAction(self);
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Oslabování bylo pøedcasnì pøerušeno.");
		}
	}

	public class Targ_Discordance : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Koho chceš zkusit oslabit?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;

			if (targetted.IsPlayer) {
				self.SysMessage("Zamìøuj jenom monstra!");
				return TargetResult.RestartTargetting;
			} else if (targetted.HasPlugin(DiscordanceSkillDef.effectPluginKey)) {
				self.SysMessage("Cíl je již oslaben.");
				return TargetResult.Done;
			}

			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();

			return TargetResult.Done;
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
			lowed_ei = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.EvalInt), lowerConst);
			lowed_magery = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Magery), lowerConst);
			lowed_resist = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.MagicResist), lowerConst);
			lowed_poison = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Poisoning), lowerConst);
			lowed_archer = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Archery), lowerConst);
			lowed_twohand = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Swords), lowerConst);
			lowed_mace = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Macing), lowerConst);
			lowed_onehand = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Fencing), lowerConst);
			lowed_wrestl = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Wrestling), lowerConst);

			cont.Dex -= lowed_dex;
			cont.Str -= lowed_str;
			cont.Int -= lowed_int;
			cont.MaxHits -= lowed_hits;
			cont.MaxMana -= lowed_mana;
			cont.MaxStam -= lowed_stam;
			cont.ModifySkillValue(SkillName.EvalInt, -lowed_ei);
			cont.ModifySkillValue(SkillName.Magery, -lowed_magery);
			cont.ModifySkillValue(SkillName.MagicResist, -lowed_resist);
			cont.ModifySkillValue(SkillName.Poisoning, -lowed_poison);
			cont.ModifySkillValue(SkillName.Archery, -lowed_archer);
			cont.ModifySkillValue(SkillName.Swords, -lowed_twohand);
			cont.ModifySkillValue(SkillName.Macing, -lowed_mace);
			cont.ModifySkillValue(SkillName.Fencing, -lowed_onehand);
			cont.ModifySkillValue(SkillName.Wrestling, -lowed_wrestl);

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
			cont.ModifySkillValue(SkillName.EvalInt, lowed_ei);
			cont.ModifySkillValue(SkillName.Magery, lowed_magery);
			cont.ModifySkillValue(SkillName.MagicResist, lowed_resist);
			cont.ModifySkillValue(SkillName.Poisoning, lowed_poison);
			cont.ModifySkillValue(SkillName.Archery, lowed_archer);
			cont.ModifySkillValue(SkillName.Swords, lowed_twohand);
			cont.ModifySkillValue(SkillName.Macing, lowed_mace);
			cont.ModifySkillValue(SkillName.Fencing, lowed_onehand);
			cont.ModifySkillValue(SkillName.Wrestling, lowed_wrestl);
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

	[Dialogs.ViewableClass]
	public partial class DiscordanceEffectPluginDef {
	}
}