/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
 */

using System;
using System.Reflection;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public abstract class SkillDef : AbstractSkillDef {

		//this is from spheretables: all these should or could be defined here...
		//CSKILLDEFPROP(AdvRate,		0, "")	<--- done
		//CSKILLDEFPROP(Bonus_Dex,	0, "
		//CSKILLDEFPROP(Bonus_Int,	0, "")
		//CSKILLDEFPROP(Bonus_Str,	0, "")
		//CSKILLDEFPROP(BonusStats,	0, "")
		//CSKILLDEFPROP(Delay,		0, "")	<--- done
		//CSKILLDEFPROP(Effect,		0, "")	<--- done
		//CSKILLDEFPROP(Key,			0, "")	<--- is in AbstractSkillDef already
		//CSKILLDEFPROP(PromptMsg,	0, "")
		//CSKILLDEFPROP(Stat_Dex,		0, "")
		//CSKILLDEFPROP(Stat_Int,		0, "")
		//CSKILLDEFPROP(Stat_Str,		0, "")
		//CSKILLDEFPROP(Title,		0, "")
		//CSKILLDEFPROP(Values,		0, "")

		private FieldValue advRate;
		private FieldValue delay;
		private FieldValue effect;

		public SkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			advRate = InitTypedField("advRate", 0, typeof(double[]));
			delay = InitTypedField("delay", 0, typeof(double[]));
			effect = InitTypedField("effect", 0, typeof(double[]));
		}

		//		protected override void LoadLine(string param, string args) {
		//			switch(param) {
		//				case "advrate":
		//					advRate = SetFieldMemory("advRate", args, typeof(double[]));
		//					break;
		//				case "delay":
		//					delay = SetFieldMemory("delay", args, typeof(double[]));
		//					break;
		//				default:
		//					base.LoadLine(param, args);
		//					break;
		//			}
		//		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);//the AbstractThingDef Loadline
		}

		public static SkillDef ById(SkillName name) {
			return (SkillDef) GetById((int) name);
		}

		public double[] AdvRate {
			get {
				return (double[]) advRate.CurrentValue;
			}
			set {
				advRate.CurrentValue = value;
			}
		}

		public double MinAdvRate {
			get {
				return AdvRate[0];
			}
		}

		public double MaxAdvRate {
			get {
				double[] arr = AdvRate;
				return arr[arr.Length - 1];
			}
		}

		public int SkillValueOfChar(Character ch) {
			//return ch.Skills[this.id].RealValue;
			return ch.GetSkill(this.Id);
			//return ((Skill)ch.SkillsAbilities[this]).RealValue;
		}

		public static int SkillValueOfChar(Character ch, ushort id) {
			return ch.GetSkill(id);
		}

		public static int SkillValueOfChar(Character ch, SkillName id) {
			return ch.GetSkill((int) id);
		}

		public double AdvRateForValue(ushort skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, AdvRate);
		}

		public double AdvRateOfChar(Character ch) {
			return ScriptUtil.EvalRangePermille(SkillValueOfChar(ch), AdvRate);
		}

		public double[] Delay {
			get {
				return (double[]) delay.CurrentValue;
			}
			set {
				delay.CurrentValue = value;
			}
		}

		public double MinDelay {
			get {
				return Delay[0];
			}
		}

		public double MaxDelay {
			get {
				double[] arr = Delay;
				return arr[arr.Length - 1];
			}
		}

		public double GetDelayForValue(ushort skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, this.Delay);
		}

		public double GetDelayForChar(Character ch) {
			//GM is always immediate
			if (ch.IsGM) {
				return 0;
			} else {
				return ScriptUtil.EvalRangePermille(this.SkillValueOfChar(ch), this.Delay);
			}
		}

		public double[] Effect {
			get {
				return (double[]) effect.CurrentValue;
			}
			set {
				effect.CurrentValue = value;
			}
		}

		public double GetEffectForValue(int skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, this.Effect);
		}

		public double GetEffectForChar(Character ch) {
			//GM is treated as having the maximal skill
			if (ch.IsGM) {
				return ScriptUtil.EvalRangePermille(1000.0, this.Effect);
			} else {
				return ScriptUtil.EvalRangePermille(SkillValueOfChar(ch), this.Effect);
			}
		}

		public static bool CheckSuccess(int skillValue, int difficulty) {
			return SkillUtils.CheckSuccess(skillValue, difficulty);
		}

		public bool CheckSuccess(Character ch, int difficulty) {
			//GM is always immediate
			if (ch.IsGM) {
				return true;
			} else {
				return SkillUtils.CheckSuccess(SkillValueOfChar(ch), difficulty);
			}
		}

		internal bool Trigger_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage())
				return true;
			bool cancel = self.TryCancellableTrigger(tkSkillSelect, skillSeqArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = self.On_SkillSelect(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(self, tkSelect, skillSeqArgs.scriptArgs);
					if (!cancel) {
						try {
							cancel = this.On_Select(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return cancel;
		}

		internal bool Trigger_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			bool cancel = self.TryCancellableTrigger(tkSkillStart, skillSeqArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = self.On_SkillStart(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(self, tkStart, skillSeqArgs.scriptArgs);
					if (!cancel) {
						try {
							cancel = this.On_Start(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return cancel;
		}

		internal bool Trigger_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			bool cancel = self.TryCancellableTrigger(tkSkillStroke, skillSeqArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = self.On_SkillStroke(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(self, tkStroke, skillSeqArgs.scriptArgs);
					if (!cancel) {
						try {
							cancel = this.On_Stroke(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return cancel;
		}

		internal bool Trigger_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			bool cancel = self.TryCancellableTrigger(tkSkillFail, skillSeqArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = self.On_SkillFail(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(self, tkFail, skillSeqArgs.scriptArgs);
					if (!cancel) {
						try {
							cancel = this.On_Fail(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return cancel;
		}

		internal bool Trigger_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			bool cancel = self.TryCancellableTrigger(tkSkillSuccess, skillSeqArgs.scriptArgs);
			if (!cancel) {
				try {
					cancel = self.On_SkillSuccess(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (!cancel) {
					cancel = this.TryCancellableTrigger(self, tkSuccess, skillSeqArgs.scriptArgs);
					if (!cancel) {
						try {
							cancel = this.On_Success(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return cancel;
		}

		internal void Trigger_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.TryCancellableTrigger(tkSkillAbort, skillSeqArgs.scriptArgs);
			try {
				self.On_SkillAbort(skillSeqArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.TryCancellableTrigger(self, tkAbort, skillSeqArgs.scriptArgs);
			try {
				this.On_Abort(skillSeqArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[Summary("This method implements the Select phase of the skill.")]
		protected abstract bool On_Select(SkillSequenceArgs skillSeqArgs);

		[Summary("This method implements the Start phase of the skill.")]
		protected abstract bool On_Start(SkillSequenceArgs skillSeqArgs);

		[Summary("This method implements the \"stroke\" of the skill, that means some important moment \"in the middle\".")]
		protected abstract bool On_Stroke(SkillSequenceArgs skillSeqArgs);

		[Summary("This method implements the failing of the skill. ")]
		protected abstract bool On_Fail(SkillSequenceArgs skillSeqArgs);

		[Summary("This method implements the succes of the skill, a.e. skillgaiin and the success effect.")]
		protected abstract bool On_Success(SkillSequenceArgs skillSeqArgs);

		[Summary("This method implements the aborting of the skill. Unlike Fail, this happens before the regular end of the script delay, if there's any... ")]
		protected abstract void On_Abort(SkillSequenceArgs skillSeqArgs);

		//[Summary("This method fires the @skillMakeItem triggers. "
		//+"Gets usually called after an item has been crafted, with the AbstractItem as additional argument")]
		//protected bool Trigger_MakeItem(Character self, AbstractItem i) {
		//	if (self==null) return false;
		//	bool cancel=false;
		//	ScriptArgs sa = new ScriptArgs(self, i, Id);
		//	cancel=TryCancellableTrigger(self, tkMakeItem, sa);
		//	if (!cancel) {
		//		cancel=self.TryCancellableTrigger(self, tkSkillMakeItem, sa);
		//		if (!cancel) {
		//			cancel=self.On_SkillMakeItem(Id, i);
		//		}
		//	}
		//	return cancel;
		//}

		public static readonly TriggerKey tkAbort = TriggerKey.Get("Abort");
		public static readonly TriggerKey tkSkillAbort = TriggerKey.Get("SkillAbort");
		public static readonly TriggerKey tkFail = TriggerKey.Get("Fail");
		public static readonly TriggerKey tkSkillFail = TriggerKey.Get("SkillFail");
		public static readonly TriggerKey tkMakeItem = TriggerKey.Get("MakeItem");
		public static readonly TriggerKey tkSkillMakeItem = TriggerKey.Get("SkillMakeItem");
		public static readonly TriggerKey tkSelect = TriggerKey.Get("Select");
		public static readonly TriggerKey tkSkillSelect = TriggerKey.Get("SkillSelect");
		public static readonly TriggerKey tkStart = TriggerKey.Get("Start");
		public static readonly TriggerKey tkSkillStart = TriggerKey.Get("SkillStart");
		public static readonly TriggerKey tkStroke = TriggerKey.Get("Stroke");
		public static readonly TriggerKey tkSkillStroke = TriggerKey.Get("SkillStroke");
		public static readonly TriggerKey tkSuccess = TriggerKey.Get("Success");
		public static readonly TriggerKey tkSkillSuccess = TriggerKey.Get("SkillSuccess");
		//public static readonly TriggerKey tkGain = TriggerKey.Get("Gain");
		//public static readonly TriggerKey tkSkillGain = TriggerKey.Get("SkillGain");
	}

	[Persistence.SaveableClass]
	public class SkillSequenceArgs {
		private Character self; //set when calling @Select
		private SkillDef skillDef; //set when calling @Select
		private IPoint3D target1, target2; //set in @Select or before it
		private object param1, param2; //set in @Select or before it
		private Item tool;
		private TimeSpan delay; //set in @Start
		private bool success;

		public readonly ScriptArgs scriptArgs;

		[Persistence.LoadingInitializer]
		public SkillSequenceArgs() {

			this.scriptArgs = new ScriptArgs(this);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillDef skillDef) {
			SkillSequenceArgs args = new SkillSequenceArgs();
			args.self = self;
			args.skillDef = skillDef;
			args.target1 = null;
			args.target2 = null;
			args.param1 = null;
			args.param2 = null;
			args.tool = null;
			args.success = false;
			args.delay = Timers.Timer.negativeOneSecond;
			return args;
		}

		public static SkillSequenceArgs Acquire(Character self, SkillDef skillDef, IPoint4D target1, IPoint4D target2, Item tool, object param1, object param2) {
			SkillSequenceArgs args = new SkillSequenceArgs();
			args.self = self;
			args.skillDef = skillDef;
			args.target1 = target1;
			args.target2 = target2;
			args.param1 = param1;
			args.param2 = param2;
			args.tool = tool;
			args.success = false;
			args.delay = Timers.Timer.negativeOneSecond;
			return args;
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName) {
			return Acquire(self, (SkillDef) SkillDef.GetById((int) skillName));
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName, Item tool) {
			return Acquire(self, (SkillDef) SkillDef.GetById((int) skillName), null, null, tool, null, null);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName, Item tool, object param1) {
			return Acquire(self, (SkillDef) SkillDef.GetById((int) skillName), null, null, tool, param1, null);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName, IPoint4D target1, IPoint4D target2, Item tool, object param1, object param2) {
			return Acquire(self, (SkillDef) SkillDef.GetById((int) skillName), target1, target2, tool, param1, param2);
		}

		public Character Self {
			get {
				return this.self;
			}
		}

		public SkillDef SkillDef {
			get {
				return this.skillDef;
			}
		}

		[Persistence.Save]
		public void Save(SteamEngine.Persistence.SaveStream output) {
			if (this.self != null) {
				output.WriteValue("self", this.self);
			}
			if (this.skillDef != null) {
				output.WriteValue("skillDef", this.skillDef);
			}
		}

		[Persistence.LoadLine]
		public void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "self":
					Persistence.ObjectSaver.Load(valueString, delegate(object loaded, string f, int l) {
						this.self = (Character) loaded;
					}, filename, line);
					break;
				case "skilldef":
					this.skillDef = (SkillDef) Persistence.ObjectSaver.OptimizedLoad_Script(valueName);
					break;
			}
		}

		[Persistence.SaveableData]
		public IPoint3D Target1 {
			get {
				return this.target1;
			}
			set {
				this.target1 = value;
			}
		}

		[Persistence.SaveableData]
		public IPoint3D Target2 {
			get {
				return this.target2;
			}
			set {
				this.target2 = value;
			}
		}

		[Persistence.SaveableData]
		public object Param1 {
			get {
				return this.param1;
			}
			set {
				this.param1 = value;
			}
		}

		[Persistence.SaveableData]
		public object Param2 {
			get {
				return this.param2;
			}
			set {
				this.param2 = value;
			}
		}

		[Persistence.SaveableData]
		public Item Tool {
			get {
				return this.tool;
			}
			set {
				this.tool = value;
			}
		}

		[Persistence.SaveableData]
		public TimeSpan DelaySpan {
			get {
				return this.delay;
			}
			set {
				this.delay = value;
			}
		}

		public double DelayInSeconds {
			get {
				return this.delay.TotalSeconds;
			}
			set {
				this.delay = TimeSpan.FromSeconds(value);
			}
		}

		[Persistence.SaveableData]
		public bool Success {
			get {
				return this.success;
			}
			set {
				this.success = value;
			}
		}

		public void PhaseSelect() {
			if (!this.skillDef.Trigger_Select(this)) {
				this.PhaseStart();
			}
		}

		public void PhaseStart() {
			if (!this.skillDef.Trigger_Start(this)) {
				AbortSkill(this.self);

				this.DelayInSeconds = this.skillDef.GetDelayForChar(this.self);
				this.DelayStroke();
			}
		}

		public void DelayStroke() {
			if (this.delay < TimeSpan.Zero) {
				this.PhaseStroke();
			} else {
				this.self.AddTimer(skillTimerKey, new SkillStrokeTimer(this)).DueInSpan = this.delay;
			}
		}

		[Summary("This method fires the @skillStroke triggers. "
		+ "Gets usually called by the SkillTimer.")]
		public void PhaseStroke() {
			if (this.self.IsAliveAndValid) {
				if (!this.skillDef.Trigger_Stroke(this)) {
					if (this.success) {
						this.PhaseSuccess();
					} else {
						this.PhaseFail();
					}
				}
			}
		}

		[Summary("This method fires the @skillFail triggers. Gets usually called from the Stroke phase")]
		[Remark("Failing is something else than being forced to abort")]
		public void PhaseFail() {
			if (!this.skillDef.Trigger_Fail(this)) {
				//this.Dispose();
			}
		}

		[Summary("This method fires the @Success triggers. Gets usually called from the Stroke phase")]
		public void PhaseSuccess() {
			if (!this.skillDef.Trigger_Success(this)) {
				//this.Dispose();
			}
		}

		[Summary("This method fires the @skillAbort triggers. "
		+ "Gets usually called when the skill is interrupted \"from outside\" - no skillgain, etc.")]
		public void PhaseAbort() {
			this.skillDef.Trigger_Abort(this);
			//this.Dispose();
		}

		public static void AbortSkill(Character self) {
			SkillStrokeTimer timer = (SkillStrokeTimer) self.RemoveTimer(skillTimerKey);
			if (timer != null) {
				timer.skillSeqArgs.PhaseAbort();
				timer.skillSeqArgs = null;
				timer.Delete();
			}
		}

		public static SkillSequenceArgs GetSkillSequenceArgs(Character self) {
			SkillStrokeTimer timer = (SkillStrokeTimer) self.GetTimer(skillTimerKey);
			if (timer != null) {
				return timer.skillSeqArgs;
			}
			return null;
		}

		private static Timers.TimerKey skillTimerKey = Timers.TimerKey.Get("_skillTimer_");

		[Persistence.SaveableClass]
		[DeepCopyableClass]
		public class SkillStrokeTimer : Timers.BoundTimer {

			[CopyableData]
			[Persistence.SaveableData]
			public SkillSequenceArgs skillSeqArgs;

			[Persistence.LoadingInitializer]
			[DeepCopyImplementation]
			public SkillStrokeTimer() {
			}

			public SkillStrokeTimer(SkillSequenceArgs skillSeqArgs) {
				Sanity.IfTrueThrow(skillSeqArgs == null, "skillSeqArgs == null");
				this.skillSeqArgs = skillSeqArgs;
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Logger.WriteDebug("SkillStrokeTimer OnTimeout on " + this.Cont);
				this.skillSeqArgs.PhaseStroke();
			}
		}
	}
}