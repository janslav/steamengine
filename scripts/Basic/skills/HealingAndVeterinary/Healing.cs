using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class HealingSkillDef : BandageSkillDef {

		public HealingSkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

		//public static SkillDef defHealing = SkillDef.ById(SkillName.Healing);

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;
			var target = skillSeqArgs.Target1 as Thing;

			var targetChar = target as Character;
			if (targetChar != null) {
				//pokud ma cil maximum zivotu,a nekrvaci
				if ((targetChar.Hits >= targetChar.MaxHits) && (!BleedingEffectPluginDef.IsBleeding(targetChar))) {
					if (target == self) {
						self.WriteLine(Loc<HealingLoc>.Get(self.Language).YoureAtMaxHitpoints);
					} else {
						self.WriteLine(string.Format(CultureInfo.InvariantCulture,
							Loc<HealingLoc>.Get(self.Language).TargetIsAtMaxHitpoints, target.Name));
					}
					return TriggerResult.Cancel;
				}

				if (!CharModelInfo.IsHumanModel(targetChar.Model)) {
					self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingSkillNotApplicable);
					return TriggerResult.Cancel;
				}

			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}

			return base.On_Select(skillSeqArgs); //BandageSkillDef implementation, common for both Healing and Veterinary
		}


		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;
			var target = skillSeqArgs.Target1 as Thing;

			var targetChar = target as Character;

			if (targetChar != null) {
				if (targetChar == self) {
					self.WriteLine(Loc<HealingLoc>.Get(self.Language).YoureStartingToHealYourself);
				} else {
					self.WriteLine(string.Format(CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(self.Language).YoureStartingToHealTarget, targetChar.Name));
				}
				skillSeqArgs.Tool.Consume(1);
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}

			return base.On_Start(skillSeqArgs); //BandageSkillDef implementation. Starts using separate timer
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;

			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(200));

			return base.On_Stroke(skillSeqArgs); //BandageSkillDef implementation. Similar checks as in @select
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;
			var target = skillSeqArgs.Target1 as Thing;

			var targetChar = target as Character;
			if (targetChar != null) {
				// TODO? Vytvorit vzorec pro maximum a minimum pridanych HP

				if (BleedingEffectPluginDef.IsBleeding(targetChar)) {
					BleedingEffectPluginDef.StopBleeding(targetChar);

					self.WriteLine(Loc<HealingLoc>.Get(self.Language).SuccessfullyStoppedBleeding);
				} else {
					targetChar.Hits += (short) ScriptUtil.EvalRangePermille(self.GetSkill(SkillName.Healing) + self.GetSkill(SkillName.Anatomy), this.Effect);

					if (targetChar.Hits > targetChar.MaxHits) {
						targetChar.Hits = targetChar.MaxHits;
					}

					self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingSucceeded);
				}
				//tell the target hes been healed?


				var usedBandages = skillSeqArgs.Tool;
				if ((usedBandages != null) && (usedBandages.Type == SingletonScript<t_bandage_blood>.Instance)) {
					AddCleanBandage(self);
				} else {
					AddBloodyBandage(self);
				}
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;
			var target = skillSeqArgs.Target1 as Thing;

			var targetChar = skillSeqArgs.Target1 as Character;
			if (targetChar != null) {
				self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingFailed);

				if (targetChar != self) {
					targetChar.WriteLine(string.Format(CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(targetChar.Language).HealerFailedToHealYou, self.Name));
				}
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;
			self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingAborted);
		}
	}

	public class HealingLoc : CompiledLocStringCollection<HealingLoc> {
		internal readonly string YoureAtMaxHitpoints = "Jsi zcela zdr�v!";
		internal readonly string TargetIsAtMaxHitpoints = "{0} je zcela zdr�v!";
		internal readonly string YoureStartingToHealTarget = "Za��n� l��it {0}.";
		internal readonly string YoureStartingToHealYourself = "Za��n� se l��it.";
		internal readonly string HealingSucceeded = "L��en� se ti povedlo!";
		internal readonly string HealingFailed = "L��en� se ti nezda�ilo!";
		internal readonly string HealerFailedToHealYou = "{0} selhal p�i pokusu t� o�et�it!";
		internal readonly string HealingAborted = "L��en� p�eru�eno";

		internal readonly string SuccessfullyStoppedBleeding = "Povedlo se ti zastavit krv�cen�";

		internal readonly string HealingSkillNotApplicable = "Nelze l��it skillem Healing";
	}
}