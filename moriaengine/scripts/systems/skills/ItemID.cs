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

		public override void Select(AbstractCharacter ch) {
			//todo: various state checks...
			Player self = ch as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ItemID>.Instance);
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
				self.SysMessage(targetted.Name + " se vyrabi z !RESOURCES!" + ", vazi " + targetted.Weight + " a barva je " + targetted.Color);
			} else {
				self.SysMessage("Zapomel jsi co mas identifikovat!"); // ztrata targetu
			}
		}

		public override void Fail(Character self) {
			if (!this.Trigger_Fail(self)) {
				self.SysMessage("Fail");
			}
		}

		protected internal override void Abort(Character self) {
			this.Trigger_Abort(self);
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
			self.StartSkill((int) SkillName.ItemID);

			return false;
		}
	}
}