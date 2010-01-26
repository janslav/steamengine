using System;
using System.Reflection;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Networking;


namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class AnatomySkillDef : SkillDef {
        public AnatomySkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_Anatomy>.Instance, skillSeqArgs);
			}
			return true;
		}

		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			return false;
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
			return false;
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character targetted = skillSeqArgs.Target1 as Character;
            GameState stateSelf = self.GameState;
			if (targetted == null || targetted.IsDeleted) {
                if (stateSelf != null) {
                    self.IsFemale == true ? stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).TargetForgottenF) : stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).TargetForgottenM);
                }
            } else if (self.CanReachWithMessage(targetted)) {
                //TODO: Hlasky pro ruzne intervaly STR/STAM
                if (stateSelf != null) {
                    stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).ASuccess1);
                }
			}
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
            GameState stateSelf = skillSeqArgs.Self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).AFailed);
            }
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
            GameState stateSelf = skillSeqArgs.Self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).ACanceled);
            }
		}
	}

    public class Targ_Anatomy : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			GameState stateSelf = self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).TargetWho);
            }
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
            if (!self.CanReachWithMessage(targetted)){
                return false;
            }

            if (targetted is Character) {
                SkillSequenceArgs skillSeq = (SkillSequenceArgs)parameter;
                skillSeq.Target1 = targetted;
                skillSeq.PhaseStart();
                return false;
            } else {
                GameState stateSelf = self.GameState;
                if (stateSelf != null) {
                    stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).TargetOnlyHuman);
                }
                return false;
            }
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
            GameState stateSelf = self.GameState;
            if (stateSelf != null) {
                stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).TargetOnlyHuman);
            }
            return false;
		}
	}

    public class AnatomyLoc : CompiledLocStringCollection {
        internal readonly string TargetWho = "Koho chceš zkoumat?";
        internal readonly string TargetOnlyHuman = "Zamìøuj pouze osoby!";
        internal readonly string ACanceled = "Anatomie byla pøerušena.";
        internal readonly string AFailed = "Tvé zkoumání se nezdaøilo.";
        internal readonly string TargetForgottenF = "Zapomìla jsi koho chceš zkoumat!";
        internal readonly string TargetForgottenM = "Zapomìl jsi koho chceš zkoumat!";
        internal readonly string ASuccess1 = "Anatomie se ti povedla.";
    }
}