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

//using System;
//using System.IO;
//using System.Text.RegularExpressions;
//using System.Collections;
//using SteamEngine;
//using SteamEngine.LScript;
//using SteamEngine.Common;
//
//namespace SteamEngine.CompiledScripts {
//	public abstract class Speech : AbstractScript {
//		protected Speech() : base() {
//		}
//		
//		protected Speech(string name) : base(name) {
//		}
//
//		protected abstract object Handle(Thing self, string message);
//		protected abstract object TryHandle(Thing self, string message);
//		
//		public static new Speech Get(string defname) {
//			return byName[defname] as Speech;
//		}
//	}
//
//	public abstract class CompiledSpeech : Speech {
//		public CompiledSpeech() {
//		}
//
//		internal override string GetName() {
//			return this.GetType().Name;
//		}
//	}
//	
//	public class ScriptedSpeech : Speech {
//		private static readonly Hashtable regexes = new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer());
//		//pattern(string) - Regex pairs
//
//		SpeechTrigger[] triggers;
//
//		public ScriptedSpeech(string defname) : base (defname) {
//		}
//		
//		private class SpeechTrigger {
//			internal LScriptHolder holder;
//			internal Regex re;
//			
//			internal SpeechTrigger(string pattern) {
//				this.re = GetRegex(pattern);
//			}
//		}
//
//		internal static IUnloadable LoadFromScripts(PropsSection input) {
//			string name = input.headerName.ToLower();
//			AbstractScript s = AbstractScript.Get(name);
//			ScriptedSpeech ss;
//			if (s != null) {
//				ss = s as ScriptedSpeech;
//				if (ss == null) {//is not scripted, so can not be overriden
//					throw new SEException(LogStr.FileLine(WorldSaver.currentfile, input.headerLine)+"A script called "+LogStr.Ident(name)+" already exists!");
//				}
//			} else {
//				ss = new ScriptedSpeech(name);
//			}
//					
//			//now do load the trigger code. 
//			int triggerCount = input.TriggerCount;
//			ss.triggers = new SpeechTrigger[triggerCount];
//			ArrayList sameCodeTriggers = new ArrayList();
//			for (int i = 0; i<triggerCount; i++) {
//				TriggerSection trigger = input.GetTrigger(i);
//				SpeechTrigger st = new SpeechTrigger(trigger.triggerName);
//				sameCodeTriggers.Add(st);
//				ss.triggers[i] = st;
//
//				if (!IsEmptyCode(trigger.code.ToString())) {
//					LScriptHolder holder = new LScriptHolder(trigger);
//					foreach (SpeechTrigger same in sameCodeTriggers) {
//						same.holder = holder;
//					}
//					sameCodeTriggers.Clear();
//				}
//			}
//
//			return ss;
//		}
//
//		protected override object Handle(Thing self, string message) {
//			foreach (SpeechTrigger st in triggers) {
//				Match m = st.re.Match(message);
//				if (m.Success) {
//					return st.holder.Run(self, message);
//				}
//			}
//			return null;
//		}
//
//		protected override object TryHandle(Thing self, string message) {
//			foreach (SpeechTrigger st in triggers) {
//				Match m = st.re.Match(message);
//				if (m.Success) {
//					return st.holder.TryRun(self, message);
//				}
//			}
//			return null;
//		}
//
//		private static Regex GetRegex(string triggername) {
//			string pattern = GeneratePattern(triggername);
//			Regex regex = (Regex) regexes[pattern];
//			if (regex == null) {
//				regex = new Regex(pattern, 
//					RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
//				regexes[pattern] = regex;
//			}
//			return regex;
//		}
//
//		private static string GeneratePattern(string triggername) {
//			triggername = triggername.Trim();
//			if (triggername.StartsWith("<<") && triggername.EndsWith(">>")) {
//				return triggername.Substring(2, triggername.Length-4);
//			} else {
//				triggername = Regex.Escape(triggername);
//				return triggername.Replace(@"\*", ".*");
//			}
//		}
//		
//		private static bool IsEmptyCode(string code) {
//			string line;
//			StringReader reader = new StringReader(code);
//			while ((line = reader.ReadLine()) != null) {
//				line = line.Trim();
//				if ((line.Length!=0) && (!line.StartsWith("//"))) {
//					return false;
//				}
//			}
//			return true;
//		}
//		
//		public static void Bootstrap() {
//			ScriptLoader.RegisterScriptType(new string[] {"Speech", "SpeechDef"}, new LoadSection(LoadFromScripts), false);
//		}
//	}
//}