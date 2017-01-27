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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Interpretation;

namespace SteamEngine.Scripting.Objects {
	public class ScriptedSpeechDef : AbstractSpeechDef {
		private static readonly ConcurrentDictionary<string, Regex> regexes =
			new ConcurrentDictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);
		//pattern(string) - Regex pairs

		private readonly Shielded<SpeechTrigger[]> triggers = new Shielded<SpeechTrigger[]>();

		public ScriptedSpeechDef(string defname)
			: base(defname) {
		}

		private class SpeechTrigger {
			internal LScriptHolder holder;
			internal readonly Regex re;

			internal SpeechTrigger(string pattern) {
				this.re = GetRegex(pattern);
			}
		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			SeShield.AssertInTransaction();

			var name = input.HeaderName.ToLower();
			var s = AbstractScript.GetByDefname(name);
			ScriptedSpeechDef ssd;
			if (s != null) {
				ssd = s as ScriptedSpeechDef;
				if (ssd == null) {//is not scripted, so can not be overriden
					throw new SEException(input.Filename, input.HeaderLine, "A script called " + LogStr.Ident(name) + " already exists!");
				}
			} else {
				ssd = new ScriptedSpeechDef(name);
				ssd.Register();
			}

			//now do load the trigger code. 
			var triggerCount = input.TriggerCount;
			var triggers = new SpeechTrigger[triggerCount];
			var sameCodeTriggers = new List<SpeechTrigger>();
			for (var i = 0; i < triggerCount; i++) {
				var trigger = input.GetTrigger(i);
				var st = new SpeechTrigger(trigger.TriggerName);
				sameCodeTriggers.Add(st);
				triggers[i] = st;

				if (!IsEmptyCode(trigger.Code.ToString())) {
					var holder = new LScriptHolder(trigger);
					foreach (var same in sameCodeTriggers) {
						same.holder = holder;
					}
					sameCodeTriggers.Clear();
				}
			}

			ssd.triggers.Value = triggers;

			return ssd;
		}

		public override void Unload() {
			SeShield.AssertInTransaction();

			base.Unload();
			this.triggers.Value = null;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			SeShield.AssertInTransaction();

			var message = speechArgs.Speech;
			foreach (var st in this.triggers.Value) {
				var m = st.re.Match(message);
				if (m.Success) {
					var o = st.holder.Run(listener, speechArgs);
					return (SpeechResult) Convert.ToInt32(o);
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			SeShield.AssertInTransaction();

			var message = speechArgs.Speech;
			foreach (var st in this.triggers.Value) {
				var m = st.re.Match(message);
				if (m.Success) {
					var o = st.holder.TryRun(listener, speechArgs);
					try {
						return (SpeechResult) Convert.ToInt32(o);
					} catch { }
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		private static Regex GetRegex(string triggername) {
			return regexes.GetOrAdd(GeneratePattern(triggername), key =>
				new Regex(key, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled));
		}

		private static string GeneratePattern(string triggername) {
			triggername = triggername.Trim();
			if (triggername.StartsWith("<<") && triggername.EndsWith(">>")) {
				return triggername.Substring(2, triggername.Length - 4);
			}
			triggername = Regex.Escape(triggername);
			return triggername.Replace(@"\*", ".*");
		}

		private static bool IsEmptyCode(string code) {
			string line;
			var reader = new StringReader(code);
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if ((line.Length != 0) && (!line.StartsWith("//"))) {
					return false;
				}
			}
			return true;
		}

		public new static void Bootstrap() {
			ScriptLoader.RegisterScriptType(new[] { "Speech", "SpeechDef" }, LoadFromScripts, false);
		}
	}
}