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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public class PropsFileParser {
		//possible targets:
		//GameAccount.Load
		//Thing.Load
		//ThingDef.LoadDefsSection
		//Globals.LoadGlobals

		//regular expressions for stream loading
		//[type name]//comment
		public static readonly Regex headerRE = new Regex(@"^\[\s*(?<type>.*?)(\s+(?<name>.*?))?\s*\]\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//"triggerkey=@triggername//comment"
		//"triggerkey @triggername//comment"
		//triggerkey can be "ON", "ONTRIGGER", "ONBUTTON", or ""
		internal static readonly Regex triggerRE = new Regex(@"^\s*(?<triggerkey>(on|ontrigger|onbutton))((\s*=\s*)|(\s+))@?\s*(?<triggername>\w*)\s*(//(?<comment>.*))?(?<ignored>.*)$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//private static int line;

		//private static void Warning(string s) {
		//	Logger.WriteWarning(WorldSaver.CurrentFile,line,s);
		//}
		//
		//private static void Error(string s) {
		//	Logger.WriteError(WorldSaver.CurrentFile,line,s);
		//}

		public static IEnumerable<PropsSection> Load(string filename, TextReader stream, CanStartAsScript isScript) {
			int line = 0;
			PropsSection curSection = null;
			TriggerSection curTrigger = null; //these are also added to curSection...

			while (true) {
				string curLine = stream.ReadLine();
				line++;
				if (curLine != null) {
					curLine = curLine.Trim();
					if ((curLine.Length == 0) || (curLine.StartsWith("//"))) {
						//it is a comment or a blank line
						if (curTrigger != null) {
							//in script compiler do also blank lines count, so we can`t ignore them.
							curTrigger.AddLine(curLine);
						}//else the comment gets lost... :\
						continue;
					}
					Match m = headerRE.Match(curLine);
					if (m.Success) {
						if (curSection != null) {//send the last section
							yield return curSection;
						}
						GroupCollection gc = m.Groups;
						curSection = new PropsSection(filename, gc["type"].Value, gc["name"].Value, line, gc["comment"].Value);
						if (isScript(curSection.headerType)) {
							//if it is something like [function xxx]
							curTrigger = new TriggerSection(filename, line, curSection.headerType, curSection.headerName, gc["comment"].Value);
							curSection.AddTrigger(curTrigger);
						} else {
							curTrigger = null;
						}
						continue;
					}
					m = triggerRE.Match(curLine);
					//on=@blah
					if (m.Success) {
						//create a new triggersection
						GroupCollection gc = m.Groups;
						curTrigger = new TriggerSection(filename, line, gc["triggerkey"].Value, gc["triggername"].Value, gc["comment"].Value);
						if (curSection == null) {
							//a trigger section without real section?
							Logger.WriteWarning(filename, line, "No section for this trigger section...?");
						} else {
							curSection.AddTrigger(curTrigger);
						}
						continue;
					}
					if (curTrigger != null) {
						if (curSection != null) {
							curTrigger.AddLine(curLine);
						} else {
							//this shouldnt be, a trigger without section...?
							Logger.WriteWarning(filename, line, "Skipping line '" + curLine + "'.");
						}
						continue;
					}
					m = Loc.valueRE.Match(curLine);
					if (m.Success) {
						if (curSection != null) {
							GroupCollection gc = m.Groups;
							curSection.AddPropsLine(gc["name"].Value, gc["value"].Value, line, gc["comment"].Value);
						} else {
							//this shouldnt be, a property without header...?
							Logger.WriteWarning(filename, line, "No section for this value. Skipping line '" + curLine + "'.");
						}
						continue;
					}
					Logger.WriteError(filename, line, "Unrecognizable data '" + curLine + "'.");
				} else {
					//end of file
					if (curSection != null) {
						yield return curSection;
					}
					break;
				}
			} //end of (while (true)) - for each line of the file
		}
	}

	public class PropsSection {
		public readonly string headerComment;
		public readonly string headerType;
		public string headerName;//[headerType headerName]
		public readonly int headerLine;
		public readonly string filename;
		internal Dictionary<string, PropsLine> props;//table of PropsLines
		private List<TriggerSection> triggerSections;//list of TriggerSections

		internal PropsSection(string filename, string type, string name, int line, string comment) {
			this.filename = filename;
			this.headerType = type;
			this.headerName = name;
			this.headerLine = line;
			this.headerComment = comment;
			this.props = new Dictionary<string, PropsLine>(StringComparer.OrdinalIgnoreCase);
			this.triggerSections = new List<TriggerSection>();
		}

		public TriggerSection GetTrigger(int index) {
			return triggerSections[index];
		}

		public TriggerSection GetTrigger(string name) {
			foreach (TriggerSection s in triggerSections) {
				if (string.Compare(name, s.triggerName, true) == 0) {
					return s;
				}
			}
			return null;
		}

		public TriggerSection PopTrigger(string name) {
			int i = 0, n = triggerSections.Count;
			TriggerSection s = null;
			for (; i < n; i++) {
				s = triggerSections[i];
				if (string.Compare(name, s.triggerName, true) == 0) {
					n = -1;
					break;
				}
			}
			if (n == -1) {
				triggerSections.RemoveAt(i);
				return s;
			}
			return null;
		}

		public int TriggerCount {
			get {
				return triggerSections.Count;
			}
		}

		internal void AddTrigger(TriggerSection value) {
			triggerSections.Add(value);
		}

		internal void AddPropsLine(string name, string value, int line, string comment) {
			PropsLine p = new PropsLine(name, value, line, comment);
			string origKey = name;
			string key = origKey;
			for (int a = 0; props.ContainsKey(key); a++) {
				key = origKey + a.ToString();
				//duplicite properties get a counted name
				//like if there is more "events=..." lines, they are in the hashtable with keys
				//events, events0, events1, etc. 
				//these entries wont be probably looked up by their name anyways.
			}
			props[key] = p;
		}

		public PropsLine TryPopPropsLine(string name) {
			PropsLine line;
			props.TryGetValue(name, out line);
			props.Remove(name);
			return line;
		}

		public PropsLine PopPropsLine(string name) {
			PropsLine line;
			props.TryGetValue(name, out line);
			props.Remove(name);
			if (line == null) {
				throw new SEException(LogStr.FileLine(this.filename, this.headerLine) + "There is no '" + name + "' line!");
			}
			return line;
		}

		public ICollection<PropsLine> GetPropsLines() {
			return props.Values;
		}

		public override string ToString() {
			return string.Format("[{0} {1}]", headerType, headerName);
		}
	}

	public class TriggerSection {
		public readonly string triggerComment;
		public readonly string triggerKey;	//"on", "ontrigger", "onbutton", or just ""
		public readonly string triggerName;	//"create", etc
		public readonly int startline;
		public readonly string filename;
		public StringBuilder code;	//code

		internal TriggerSection(string filename, int startline, string key, string name, string comment) {
			this.filename = filename;
			this.triggerKey = key;
			this.triggerName = name;
			this.startline = startline;
			this.code = new StringBuilder();
			this.triggerComment = comment;
		}

		internal void AddLine(string data) {
			code.Append(data).Append(Environment.NewLine);
		}

		//		internal void AddLine() {
		//			code.Append(Environment.NewLine);
		//		}

		public override string ToString() {
			return string.Concat(triggerKey, "=@", triggerName);
		}
	}

	public class PropsLine {
		public readonly string comment;
		public readonly string name;
		public readonly string value;//name=value
		public readonly int line;

		public PropsLine(string name, string value, int line, string comment) {
			this.name = name;
			this.value = value;
			this.line = line;
			this.comment = comment;
		}

		public override string ToString() {
			return name + " = " + value;
		}
	}
}
