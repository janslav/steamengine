using System;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Persistence;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	[Dialogs.ViewableClass]
	public class HealingSkillDef : BandageSkillDef {

		public HealingSkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

		//public static SkillDef defHealing = SkillDef.ById(SkillName.Healing);

		protected override bool On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = target as Character;
			if (targetChar != null) {
				//pokud ma cil maximum zivotu
				if (targetChar.Hits >= targetChar.MaxHits) {
					if (target == self) {
						self.WriteLine(Loc<HealingLoc>.Get(self.Language).YoureAtMaxHitpoints);
					} else {
						self.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
							Loc<HealingLoc>.Get(self.Language).TargetIsAtMaxHitpoints, target.Name));
					}
					return true;
				}

				if (!CharModelInfo.IsHumanModel(targetChar.Model)) {
					self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingSkillNotApplicable);
					return true;
				}

			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}

			return base.On_Select(skillSeqArgs); //BandageSkillDef implementation, common for both Healing and Veterinary
		}


		protected override bool On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = target as Character;

			if (targetChar != null) {
				if (targetChar == self) {
					self.WriteLine(Loc<HealingLoc>.Get(self.Language).YoureStartingToHealYourself);
				} else {
					self.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(self.Language).YoureStartingToHealTarget, targetChar.Name));
				}
				skillSeqArgs.Tool.Consume(1);
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}

			return base.On_Start(skillSeqArgs); //BandageSkillDef implementation. Starts using separate timer
		}

		protected override bool On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(200));

			return base.On_Stroke(skillSeqArgs); //BandageSkillDef implementation. Similar checks as in @select
		}

		protected override bool On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = target as Character;
			if (targetChar != null) {
				// TODO? Vytvorit vzorec pro maximum a minimum pridanych HP

				targetChar.Hits += (short) ScriptUtil.EvalRangePermille(self.GetSkill(SkillName.Healing) + self.GetSkill(SkillName.Anatomy), this.Effect);

				if (targetChar.Hits > targetChar.MaxHits) {
					targetChar.Hits = targetChar.MaxHits;
				}

				self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingSucceeded);

				Item usedBandages = skillSeqArgs.Tool;
				if ((usedBandages != null) && (usedBandages.Type == SingletonScript<t_bandage_blood>.Instance)) {
					AddCleanBandage(self);
				} else {
					AddBloodyBandage(self);
				}
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}
			return false;
		}

		protected override bool On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = skillSeqArgs.Target1 as Character;
			if (targetChar != null) {
				self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingFailed);

				if (targetChar != self) {
					targetChar.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(targetChar.Language).HealerFailedToHealYou, self.Name));
				}
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}
			return false;
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingAborted);
		}
	}

	public class HealingLoc : CompiledLocStringCollection {
		internal readonly string YoureAtMaxHitpoints = "Jsi zcela zdr�v!";
		internal readonly string TargetIsAtMaxHitpoints = "{0} je zcela zdr�v!";
		internal readonly string YoureStartingToHealTarget = "Za��n� l��it {0}.";
		internal readonly string YoureStartingToHealYourself = "Za��n� se l��it.";
		internal readonly string HealingSucceeded = "L��en� se ti povedlo!";
		internal readonly string HealingFailed = "L��en� se ti nezda�ilo!";
		internal readonly string HealerFailedToHealYou = "{0} selhal p�i pokusu t� o�et�it!";
		internal readonly string HealingAborted = "L��en� p�eru�eno";

		internal readonly string HealingSkillNotApplicable = "Nelze l��it skillem Healing";
	}
}