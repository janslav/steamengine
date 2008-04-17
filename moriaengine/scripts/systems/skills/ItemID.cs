using System;
using System.Reflection;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class ItemIDSkillDef : SkillDef {
		public ItemIDSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_Select(Character ch) {
			//todo: various state checks...
			Player self = ch as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ItemID>.Instance);
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
				self.SysMessage(targetted.Name + " se vyrabi z !RESOURCES!" + ", vazi " + targetted.Weight + " a barva je " + targetted.Color);
			} else {
				self.SysMessage("Zapomel jsi co mas identifikovat!"); // ztrata targetu
			}
		}

		protected override void On_Fail(Character self) {
			self.SysMessage("Fail");
		}

		protected override void On_Abort(Character self) {
			self.SysMessage("Identification aborted.");
		}
	}

	public class Targ_ItemID : CompiledTargetDef {

		protected override void On_Start(Character self, object parameter) {
			self.SysMessage("Co chces identifikovat ?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Character self, Item targetted, object parameter) {

			self.currentSkillTarget1 = targetted;
			self.StartSkill(SkillName.ItemID);

			return false;
		}
	}
}