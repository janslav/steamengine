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
	public class AnimalLoreSkillDef : SkillDef {
		public AnimalLoreSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_AnimalLore>.Instance, skillSeqArgs);
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
				stateSelf.WriteLine(Loc<AnimalLoreLoc>.Get(stateSelf.Language).ASuccess1);
			}
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			GameState stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<AnimalLoreLoc>.Get(stateSelf.Language).AFailed);
			}
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			GameState stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<AnimalLoreLoc>.Get(stateSelf.Language).ACanceled);
			}
		}
	}

	public class Targ_AnimalLore : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<AnimalLoreLoc>.Get(self.Language).TargetWho);
			base.On_Start(self, parameter);
		}

		protected override bool On_TargonChar(Player self, Character targetted, object parameter) {
			if (!self.CanInteractWithMessage(targetted)) {
				return false;
			}

			if (targetted.IsAnimal) {
				SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
				skillSeq.Target1 = targetted;
				skillSeq.PhaseStart();
				return false;
			} else {
				self.SysMessage(Loc<AnimalLoreLoc>.Get(self.Language).TargetOnlyAnimals);
				return false;
			}
		}

		protected override bool On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage(Loc<AnimalLoreLoc>.Get(self.Language).TargetOnlyAnimals);
			return true;
		}
	}

	public class AnimalLoreLoc : CompiledLocStringCollection {
		internal readonly string TargetWho = "Co chceš zkoumat?";
		internal readonly string TargetOnlyAnimals = "Zamìøuj pouze zvìø!";
		internal readonly string ACanceled = "Animal Lore bylo pøerušeno.";
		internal readonly string AFailed = "Tvé zkoumání se nezdaøilo.";
		internal readonly string ASuccess1 = "Animal Lore se ti povedlo.";
	}
}