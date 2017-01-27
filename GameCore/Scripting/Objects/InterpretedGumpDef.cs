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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Interpretation;

namespace SteamEngine.Scripting.Objects {

	//this is the class that gets instantiated for every LScript DIALOG/GUMP script
	public sealed class InterpretedGumpDef : GumpDef {

		private readonly Shielded<State> shieldedState = new Shielded<State>();

		private struct State {
			internal LScriptHolder layoutScript;
			internal LScriptHolder textsScript;
			internal ResponseTrigger[] responseTriggers;
		}

		private InterpretedGumpDef(string name)
				: base(name) {
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "stack0"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static InterpretedGumpDef Load(PropsSection input) {
			SeShield.AssertInTransaction();

			var headers = input.HeaderName.Split(new[] { ' ', '\t' }, 2);
			var name = headers[0];//d_something
			var gump = GetByDefname(name);
			InterpretedGumpDef sgd;
			if (gump != null) {
				sgd = gump as InterpretedGumpDef;
				if (sgd == null) {//is not scripted, so can not be overriden
					throw new SEException(LogStr.FileLine(input.Filename, input.HeaderLine) + "GumpDef/Dialog " + LogStr.Ident(name) + " already exists!");
				}
			} else {
				sgd = new InterpretedGumpDef(name);
				sgd.Register();
			}
			if (headers.Length == 1) {//layout section
				if ((sgd.shieldedState.Value.layoutScript != null) && (!sgd.IsUnloaded)) {//already loaded
					throw new SEException("GumpDef/Dialog " + LogStr.Ident(name) + " already exists!");
				}
				var sc = new LScriptHolder(input.GetTrigger(0));
				if (sc.IsUnloaded) {//in case the compilation failed (syntax error)
					sgd.Unload(); //IsUnloaded = true;
					return null;
				}
				sgd.shieldedState.Modify((ref State s) => s.layoutScript = sc);
				sgd.UnUnload();
				return sgd;
			}
			if (headers.Length == 2) {//buttons or texts section
				var type = headers[1].ToLowerInvariant();
				switch (type) {
					case "text":
					case "texts":
						if ((sgd.shieldedState.Value.textsScript != null) && (!sgd.IsUnloaded)) {//already loaded
							throw new SEException("TEXT section for GumpDef/Dialog called " + LogStr.Ident(name) + " already exists!");
						}
						var trigger = input.GetTrigger(0);
						var stream = new StringReader(trigger.Code.ToString());
						var modifiedCode = new StringBuilder();
						while (true) {
							var curLine = stream.ReadLine();
							if (curLine != null) {
								curLine = curLine.Trim();
								if ((curLine.Length == 0) || (curLine.StartsWith("//"))) {
									continue;
								}
								curLine = Utility.Uncomment(curLine);
								modifiedCode.Append("AddString(\"");
								modifiedCode.Append(curLine);
								modifiedCode.Append("\")");
								modifiedCode.Append(Environment.NewLine);
							} else {
								break;
							}
						}
						trigger.Code = modifiedCode;
						var sc = new LScriptHolder(trigger);
						if (sc.IsUnloaded) {//in case the compilation failed (syntax error)
							sgd.Unload(); //IsUnloaded = true;
							return null;
						}
						sgd.shieldedState.Modify((ref State s) => s.textsScript = sc);
						return sgd;
					case "button":
					case "buttons":
					case "triggers":
					case "trigger":
						if ((sgd.shieldedState.Value.responseTriggers != null) && !sgd.IsUnloaded) {//already loaded
							throw new SEException("BUTTON section for GumpDef/Dialog called " + LogStr.Ident(name) + " already exists!");
						}

						var n = input.TriggerCount;
						var responsesList = new List<ResponseTrigger>(n);
						for (var i = 1; i < n; i++) {//starts from 1 because 0 is the "default" script, which is igored in this section
							trigger = input.GetTrigger(i);
							var triggerName = trigger.TriggerName;
							if (StringComparer.OrdinalIgnoreCase.Equals(triggerName, "anybutton")) {
								responsesList.Add(new ResponseTrigger(0, int.MaxValue, trigger));
								continue;
							}
							try {
								var index = ConvertTools.ParseInt32(triggerName);
								responsesList.Add(new ResponseTrigger(index, index, trigger));
								continue;
							} catch {
								var boundStrings = triggerName.Split(' ', '\t', ',');
								if (boundStrings.Length == 2) {
									try {
										var lowerBound = ConvertTools.ParseInt32(boundStrings[0].Trim());
										var upperBound = ConvertTools.ParseInt32(boundStrings[1].Trim());
										responsesList.Add(new ResponseTrigger(lowerBound, upperBound, trigger));
										continue;
									} catch { }
								}
							}
							Logger.WriteError("String '" + LogStr.Ident(triggerName) + "' is not valid as gump/dialog response trigger header");
						}
						sgd.shieldedState.Modify((ref State s) => s.responseTriggers = responsesList.ToArray());
						return sgd;
				}
			}
			throw new SEException("Invalid GumpDef/Dialog header");
		}

		internal static void LoadingFinished() {
			foreach (var script in AllScripts) {
				var sgd = script as InterpretedGumpDef;
				if (sgd != null) {
					SeShield.InTransaction(sgd.CheckValidity);
				}
			}
		}

		private void CheckValidity() {//check method, used as delayed
			SeShield.AssertInTransaction();

			if (this.shieldedState.Value.layoutScript == null) {
				Logger.WriteWarning("Dialog " + LogStr.Ident(this.Defname) + " missing the main (layout) section?");
				this.Unload();
				return;
			}
			if (this.IsUnloaded && (this.shieldedState.Value.layoutScript != null)) {
				Logger.WriteWarning("Dialog " + LogStr.Ident(this.Defname) + " resynced incompletely?");
			}
		}

		public override void Unload() {
			this.shieldedState.Modify((ref State s) => {
				s.layoutScript = null;
				s.responseTriggers = null;
				s.textsScript = null;
			});

			base.Unload();
		}

		internal override Gump InternalConstruct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			SeShield.AssertInTransaction();

			this.ThrowIfUnloaded();
			var instance = new InterpretedGump(this);
			var sa = new ScriptArgs(instance, sendTo); //instance and recipient are stored everytime
			if (args != null) {
				instance.InputArgs = args; //store the Dialog Args to the instance				
			} else {
				instance.InputArgs = new DialogArgs(); //prepare the empty DialogArgs object (no params)
			}

			Exception exception = null;
			var shieldedStateValue = this.shieldedState.Value;

			shieldedStateValue.textsScript?.TryRun(focus, sa, out exception);
			if (exception != null) {
				return null;
			}

			shieldedStateValue.layoutScript?.TryRun(focus, sa, out exception);

			var returnedInstance = sa.Argv[0] as InterpretedGump;
			if (returnedInstance == null) {
				returnedInstance = instance;
			}

			if (exception != null) {
				returnedInstance.FinishCompilingPacketData(focus, sendTo);
				return returnedInstance;
			}
			return null;
		}

		internal void OnResponse(InterpretedGump instance, int pressedButton, int[] selectedSwitches, ResponseText[] returnedTexts, ResponseNumber[] responseNumbers) {
			var valueResponseTriggers = this.shieldedState.Value.responseTriggers;

			if (valueResponseTriggers != null) {
				for (int i = 0, n = valueResponseTriggers.Length; i < n; i++) {
					var rt = valueResponseTriggers[i];
					if (rt.IsInBounds(pressedButton)) {
						var sa = new ScriptArgs(
							instance,                           //0
							instance.Cont,                      //1
							pressedButton,                      //2
							new ArgTxtHolder(returnedTexts),    //3
							new ArgChkHolder(selectedSwitches), //4
							new ArgNumHolder(responseNumbers)   //5
						);
						rt.script.TryRun(instance.Focus, sa);
						return;
					}
				}
			}
		}

		private class ResponseTrigger {
			private readonly int lowerBound;
			private readonly int upperBound;
			internal readonly LScriptHolder script;
			internal ResponseTrigger(int lowerBound, int upperBound, TriggerSection trigger) {
				this.lowerBound = lowerBound;
				this.upperBound = upperBound;
				this.script = new LScriptHolder(trigger);
			}

			internal bool IsInBounds(int index) {
				return ((this.lowerBound <= index) && (index <= this.upperBound));
			}
		}

		public override string ToString() {
			return "InterpretedGumpDef " + this.Defname;
		}
	}

	public class ArgChkHolder {
		private readonly int[] selectedSwitches;
		internal ArgChkHolder(int[] selectedSwitches) {
			this.selectedSwitches = selectedSwitches;
		}

		public int this[int id] {
			get {
				for (int i = 0, n = this.selectedSwitches.Length; i < n; i++) {
					if (this.selectedSwitches[i] == id) {
						return 1;
					}
				}
				return 0;
			}
		}
	}

	public class ArgTxtHolder {
		private readonly ResponseText[] responseTexts;
		internal ArgTxtHolder(ResponseText[] responseTexts) {
			this.responseTexts = responseTexts;
		}

		public string this[int id] {
			get {
				for (int i = 0, n = this.responseTexts.Length; i < n; i++) {
					var rt = this.responseTexts[i];
					if (rt.Id == id) {
						return rt.Text;
					}
				}
				return "";
			}
		}
	}

	public class ArgNumHolder {
		private readonly ResponseNumber[] responseNumbers;
		internal ArgNumHolder(ResponseNumber[] responseNumbers) {
			this.responseNumbers = responseNumbers;
		}

		public decimal this[int id] {
			get {
				for (int i = 0, n = this.responseNumbers.Length; i < n; i++) {
					var rn = this.responseNumbers[i];
					if ((rn != null) && (rn.Id == id)) {
						return rn.Number;
					}
				}
				return 0;
			}
		}
	}
}