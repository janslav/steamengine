using System;
using System.Reflection;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class ArmsLoreSkillDef : SkillDef {
		public ArmsLoreSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ArmsLore>.Instance, skillSeqArgs);
			}
			return true;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.CanReachWithMessage((Item) skillSeqArgs.Target1)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
				return false;
			} else {
				return true;
			}
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage("Arms lore SUKCEEES");// kontrolni hlaska, pozdeji odstranit!
			Destroyable targetted = (Destroyable) skillSeqArgs.Target1;

			self.SysMessage("Armor už umíme, doplníme. Vypisujeme testovaci hlasku: " + targetted.Name + " model je: " + targetted.Model);

			//Destroyable = zbran (Weapon) nebo brneni/obleceni (Wearable)

			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Arms lore se nepovedlo.");
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Arms lore pøerušeno.");
		}
	}

	public class Targ_ArmsLore : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Co chceš prohlédnout?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("Zamìøuj pouze zbranì a zbroje!");
			return true;
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			if (!self.CanReachWithMessage(targetted)) {
				return false;
			}

			if (targetted is Destroyable) {
				SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
				skillSeq.Target1 = targetted;
				skillSeq.PhaseStart();
				return false;
			} else {
				self.SysMessage("Zamìøuj pouze zbranì a zbroje!");
				return false;
			}
		}
	}
}