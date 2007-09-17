using System;
using System.Reflection;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public class ArmsLoreSkillDef : SkillDef {
		public ArmsLoreSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public override void Select(AbstractCharacter ch) {
			//todo: various state checks...
			Player self = ch as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ArmsLore>.Instance);
			}
		}

		internal override void Start(Character self) {
			if (!this.Trigger_Start(self)) {
				self.currentSkill = this;
				DelaySkillStroke(self);
			}
		}

		public override void Stroke(Character self) {
			//todo: various state checks...
			if (!this.Trigger_Stroke(self)) {
				if (CheckSuccess(self, Globals.dice.Next(700))) {
					Success(self);
				} else {
					Fail(self);
				}
			}
			self.currentSkill = null;
		}

		public override void Success(Character self) {
			self.SysMessage("SUKCEEES");// kontrolni hlaska, pozdeji odstranit!
			Item targetted = self.currentSkillTarget1 as Item;
			if (targetted != null) {
				self.SysMessage("Armor ani DMG este neumime, doplnime. Vypisujeme testovaci hlasku: " + targetted.Name + " model je: " + targetted.Model);
			} else {
				self.SysMessage("Zapomel jsi co zkoumas!"); // ztrata targetu
			}


		}

		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
				self.SysMessage("Fail");
			}
		}

		protected internal override void Abort(Character self) {
			if (!this.Trigger_Abort(self)) {
				self.SysMessage("Skill aborted.");
			}
			self.currentSkill = null;
		}
	}

	public class Targ_ArmsLore : CompiledTargetDef {
		protected override void On_Start(Character self, object parameter) {
			self.SysMessage("Co chces prohlednout?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Character self, Character targetted, object parameter) {
			self.SysMessage("Zameruj pouze zbrane a zbroje!");
			return false;
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter) {
			self.currentSkillTarget1 = targetted;
			self.StartSkill((int) SkillName.ArmsLore);
			return false;
		}

		protected override bool On_TargonStatic(Character self, Static targetted, object parameter) {
			self.SysMessage("Zameruj pouze zbrane a zbroje!");
			return true;
		}

		protected override bool On_TargonGround(Character self, IPoint3D targetted, object parameter) {
			self.SysMessage("Zameruj pouze zbrane a zbroje!");
			return true;
		}

		protected override void On_TargonCancel(Character self, object parameter) {
			self.SysMessage("Target zrusen");
		}
	}
}