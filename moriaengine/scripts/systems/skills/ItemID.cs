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

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ItemID>.Instance, skillSeqArgs);
			}
			return true;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: various state checks...
			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
			return false;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage("ItemId SUKCEEES");// kontrolni hlaska, pozdeji odstranit!
			Item targetted = (Item) skillSeqArgs.Target1;
			if (targetted == null || targetted.IsDeleted) {
				self.SysMessage(targetted.Name + " se vyrabi z !RESOURCES!" + ", vazi " + targetted.Weight + " a barva je " + targetted.Color);
			} else {
				self.SysMessage("Zapomel jsi co mas identifikovat!"); // ztrata targetu
			}
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Fail");
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Identification aborted.");
		}
	}

	public class Targ_ItemID : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Co chces identifikovat ?");
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			if (!self.CanReachWithMessage(targetted)) {
				return false;
			}

			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return false;
		}
	}
}