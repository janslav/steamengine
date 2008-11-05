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
			advRate = InitField_Typed("advRate", 0, typeof(double[]));
			delay = InitField_Typed("delay", 0, typeof(double[]));
			effect = InitField_Typed("effect", 0, typeof(double[]));
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
			return (SkillDef) ById((int) name);
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
				return arr[arr.Length-1];
			}
		}

		public ushort SkillValueOfChar(Character ch) {
			//return ch.Skills[this.id].RealValue;
			return ch.GetSkill(this.Id);
			//return ((Skill)ch.SkillsAbilities[this]).RealValue;
		}

		public static ushort SkillValueOfChar(Character ch, ushort id) {
			return ch.GetSkill(id);
		}

		public static ushort SkillValueOfChar(Character ch, SkillName id) {
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
				return arr[arr.Length-1];
			}
		}

		public double GetDelayForValue(ushort skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, Delay);
		}

		public double GetDelayForChar(Character ch) {
			if (ch.IsGM) {
				return 0;
			} else {
				return ScriptUtil.EvalRangePermille(SkillValueOfChar(ch), Delay);
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

		public double MinEffect {
			get {
				return Effect[0];
			}
		}

		public double MaxEffect {
			get {
				double[] arr = Effect;
				return arr[arr.Length-1];
			}
		}

		public double GetEffectForValue(ushort skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, Effect);
		}

		public double GetEffectForChar(Character ch) {
			return ScriptUtil.EvalRangePermille(SkillValueOfChar(ch), Effect);
		}

		public static bool CheckSuccess(ushort skillValue, int difficulty) {
			//TODO algorhitm
			return SkillUtils.CheckSuccess(skillValue, difficulty);
		}

		public bool CheckSuccess(Character ch, int difficulty) {
			if (ch.IsGM) {
				return true;
			} else {
				return SkillUtils.CheckSuccess(SkillValueOfChar(ch), difficulty);
			}
		}

		internal void Select(AbstractCharacter ch) {
			Character self = (Character) ch;
			if (!this.Trigger_Select(self)) {
				this.On_Select(self);
			}
		}

		protected abstract void On_Select(Character ch);

		private bool Trigger_Select(Character self) {
			if (!self.CheckAliveWithMessage())
				return true;
			bool cancel = false;
			ScriptArgs sa = new ScriptArgs(self, Id);
			cancel = this.TryCancellableTrigger(self, tkSelect, sa);
			if (!cancel) {
				cancel = self.TryCancellableTrigger(tkSkillSelect, sa);
				if (!cancel) {
					cancel = self.On_SkillSelect(Id);
				}
			}
			return cancel;
		}

		internal void Start(Character ch) {
			if (!this.Trigger_Start(ch)) {
				this.On_Start(ch);
			}
		}

		private bool Trigger_Start(Character self) {
			if (self==null) return false;
			bool cancel=false;
			ScriptArgs sa = new ScriptArgs(self, Id);
			cancel=TryCancellableTrigger(self, tkStart, sa);
			if (!cancel) {
				cancel=self.TryCancellableTrigger(tkSkillStart, sa);
				if (!cancel) {
					cancel=self.On_SkillStart(Id);
				}
			}
			return cancel;
		}

		[Summary("This method implements the start phase of the skill.")]
		protected abstract void On_Start(Character ch);

		[Summary("This method fires the @skillStroke triggers. "
		+ "Gets usually called by the SkillTimer.")]
		public void Stroke(Character ch){
			if (!this.Trigger_Stroke(ch)) {
				this.On_Stroke(ch);
			}
		}

		[Summary("This method implements the \"stroke\" of the skill, that means some important moment \"in the middle\".")]
		protected abstract void On_Stroke(Character ch);

		private bool Trigger_Stroke(Character self) {
			if (self == null) return false;
			bool cancel = false;
			ScriptArgs sa = new ScriptArgs(self, Id);
			cancel = TryCancellableTrigger(self, tkStroke, sa);
			if (!cancel) {
				cancel = self.TryCancellableTrigger(tkSkillStroke, sa);
				if (!cancel) {
					cancel = self.On_SkillStroke(Id);
				}
			}
			return cancel;
		}

		[Summary("This method fires the @skillFail triggers. "
		+ "Gets usually called when the skill chance fails, which is something else than being forced to abort")]
		public void Fail(Character ch) {
			if (!this.Trigger_Fail(ch)) {
				this.On_Fail(ch);
			}
		}

		[Summary("This method implements the failing of the skill. ")]
		protected abstract void On_Fail(Character ch);

		private bool Trigger_Fail(Character self) {
			if (self==null) return false;
			bool cancel=false;
			ScriptArgs sa = new ScriptArgs(self, Id);
			cancel=TryCancellableTrigger(self, tkFail, sa);
			if (!cancel) {
				cancel=self.TryCancellableTrigger(tkSkillFail, sa);
				if (!cancel) {
					cancel=self.On_SkillFail(Id);
				}
			}
			return cancel;
		}

		[Summary("This method fires the @skillGain triggers. "
		+ "Gets called when the Character`s about to gain in this skill, with the chance and skillcap as additional args")]
		public void Success(Character ch) {
			if (!this.Trigger_Success(ch)) {
				this.On_Success(ch);
			}
		}

		[Summary("This method implements the succes of the skill, a.e. skillgaiin and the success effect."
		+ "Usually calls Trigger_Success at some point")]
		protected abstract void On_Success(Character ch);

		private bool Trigger_Success(Character self) {
			if (self == null) return false;
			bool cancel = false;
			ScriptArgs sa = new ScriptArgs(self, Id);
			cancel = TryCancellableTrigger(self, tkSuccess, sa);
			if (!cancel) {
				cancel = self.TryCancellableTrigger(tkSkillSuccess, sa);
				if (!cancel) {
					cancel = self.On_SkillSuccess(Id);
				}
			}
			return cancel;
		}

		[Summary("This method fires the @skillAbort triggers. "
		+ "Gets usually called when the skill is interrupted \"from outside\" - no skillgain, etc.")]
		internal void Abort(Character self) {
			Trigger_Abort(self);
			On_Abort(self);
		}

		[Summary("This method implements the aborting of the skill. Unlike Fail, this happens before the regular end of the script delay, if there's any... "
		+"Usually calls Trigger_Abort at some point")]
		protected abstract void On_Abort(Character self);

		private void Trigger_Abort(Character self) {
			if (self==null) 
				return;

			ScriptArgs sa = new ScriptArgs(self, Id);
			TryTrigger(self, tkAbort, sa);
			self.TryTrigger(tkSkillAbort, sa);
			self.On_SkillAbort(Id);
		}

		public void DelaySkillStroke(double seconds, Character self) {
			self.DelaySkillStroke(seconds);
		}

		public void DelaySkillStroke(Character self) {
			self.DelaySkillStroke(GetDelayForChar(self));
		}

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
}