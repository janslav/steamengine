using System;
using System.Reflection;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Networking;

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
            //TODO: dodìlat hlášku pro success 
			Character self = skillSeqArgs.Self;
            GameState stateSelf = self.GameState;
			Item targetted = (Item)skillSeqArgs.Target1;
            self.SysMessage(targetted.ToString()+ "," + targetted.IsDeleted);// kontrolni hlaska, pozdeji odstranit!
			if (targetted == null || !targetted.IsDeleted) {
                //if (stateSelf != null) {
                //    stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).ISuccess);
                //}
                self.SysMessage(targetted.Name + " se vyrabi z !RESOURCES!" + ", vazi " + targetted.Weight + " a barva je " + targetted.Color);
			} else {
                if (stateSelf != null) {
                    stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).TargetForgotten);
                }
			}
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
            GameState stateSelf = skillSeqArgs.Self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).IFailed);
            }
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
            GameState stateSelf = skillSeqArgs.Self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).ICanceled);
            }
		}
	}

	public class Targ_ItemID : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
            GameState stateSelf = self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).TargetWhat);
            }
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
    public class ItemIdLoc : CompiledLocStringCollection {
        internal readonly string TargetWhat = "Co chceš identifikovat?";
        internal readonly string TargetForgotten = "Zapomìl jsi, co máš identifikovat!";
        internal readonly string ICanceled = "Identifikace pøedmìtu pøerušena.";
        internal readonly string IFailed = "Identifikace pøedmìtu se nezdaøila.";
        internal readonly string ISuccess = "";
    }
}