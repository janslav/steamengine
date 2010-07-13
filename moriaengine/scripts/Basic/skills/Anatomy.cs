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
			if (self.CanInteractWithMessage(skillSeqArgs.Target1)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
				return false;
			} else {
				return true;
			}
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			GameState stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				//TODO: Hlasky pro ruzne intervaly STR/STAM
				stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).ASuccess1);
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
			self.WriteLine(Loc<AnatomyLoc>.Get(self.Language).TargetWho);
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
			if (!self.CanInteractWithMessage(targetted)) {
				return false;
			}

			if (targetted.IsHuman) {
				SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
				skillSeq.Target1 = targetted;
				skillSeq.PhaseStart();
				return false;
			} else {
				self.WriteLine(Loc<AnatomyLoc>.Get(self.Language).TargetOnlyHuman);
				return false;
			}
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			self.WriteLine(Loc<AnatomyLoc>.Get(self.Language).TargetOnlyHuman);
			return true;
		}
	}

	public class AnatomyLoc : CompiledLocStringCollection {
		internal readonly string TargetWho = "Koho chceš zkoumat?";
		internal readonly string TargetOnlyHuman = "Zamìøuj pouze osoby!";
		internal readonly string ACanceled = "Anatomie byla pøerušena.";
		internal readonly string AFailed = "Tvé zkoumání se nezdaøilo.";
		internal readonly string ASuccess1 = "Anatomie se ti povedla.";
	}
}