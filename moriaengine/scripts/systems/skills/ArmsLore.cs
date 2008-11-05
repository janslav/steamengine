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

		protected override void On_Select(Character ch) {
			//todo: various state checks...
			Player self = ch as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ArmsLore>.Instance);
			}
		}

		protected override void On_Start(Character self) {
			self.currentSkill = this;
			DelaySkillStroke(self);
		}

		protected override void On_Stroke(Character self) {
			//todo: various state checks...
			if (CheckSuccess(self, Globals.dice.Next(700))) {
				this.Success(self);
			} else {
				this.Fail(self);
			}

			self.currentSkill = null;
		}

		protected override void On_Success(Character self) {
			self.SysMessage("SUKCEEES");// kontrolni hlaska, pozdeji odstranit!
			Item targetted = self.currentSkillTarget1 as Item;
			if (targetted != null) {
				self.SysMessage("Armor ani DMG este neumime, doplnime. Vypisujeme testovaci hlasku: " + targetted.Name + " model je: " + targetted.Model);
			} else {
				self.SysMessage("Zapomel jsi co zkoumas!"); // ztrata targetu
			}
		}

		protected override void On_Fail(Character self) {
			self.SysMessage("Fail");
		}

		protected override void On_Abort(Character self) {
			self.SysMessage("Arms lore aborted.");
		}
	}

	public class Targ_ArmsLore : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Co chces prohlednout?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("Zameruj pouze zbrane a zbroje!");
			return false;
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			self.currentSkillTarget1 = targetted;
			self.StartSkill(SkillName.ArmsLore);
			return false;
		}

		protected override bool On_TargonStatic(Player self, Static targetted, object parameter) {
			self.SysMessage("Zameruj pouze zbrane a zbroje!");
			return true;
		}

		protected override bool On_TargonGround(Player self, IPoint4D targetted, object parameter) {
			self.SysMessage("Zameruj pouze zbrane a zbroje!");
			return true;
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			self.SysMessage("Target zrusen");
		}
	}
}