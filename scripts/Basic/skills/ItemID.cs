using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class ItemIDSkillDef : SkillDef {
		public ItemIDSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			var self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ItemID>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			var self = skillSeqArgs.Self;
			if (self.CanReachWithMessage((Item) skillSeqArgs.Target1)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
				return TriggerResult.Continue;
			}
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			//TODO: dodìlat hlášku pro success 
			var self = skillSeqArgs.Self;
			var stateSelf = self.GameState;
			var targetted = (Item) skillSeqArgs.Target1;
			self.SysMessage(targetted + "," + targetted.IsDeleted);// kontrolni hlaska, pozdeji odstranit!

			//if (stateSelf != null) {
			//    stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).ISuccess);
			//}
			self.SysMessage(targetted.Name + " se vyrabi z !RESOURCES!" + ", vazi " + targetted.Weight + " a barva je " + targetted.Color);
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			var stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).IFailed);
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			var stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).ICanceled);
			}
		}
	}

	public class Targ_ItemID : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			var stateSelf = self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<ItemIdLoc>.Get(stateSelf.Language).TargetWhat);
			}
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (!self.CanReachWithMessage(targetted)) {
				return TargetResult.RestartTargetting;
			}

			var skillSeq = (SkillSequenceArgs) parameter;
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}
	}
	public class ItemIdLoc : CompiledLocStringCollection<ItemIdLoc> {
		internal readonly string TargetWhat = "Co chceš identifikovat?";
		internal readonly string ICanceled = "Identifikace pøedmìtu pøerušena.";
		internal readonly string IFailed = "Identifikace pøedmìtu se nezdaøila.";
		internal readonly string ISuccess = "";
	}
}