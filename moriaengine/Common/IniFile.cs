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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

namespace SteamEngine.Common {

	//

	public class IniFile {

		private Dictionary<string, IniFileSection> sectionsByName = new Dictionary<string, IniFileSection>(StringComparer.OrdinalIgnoreCase);
		private List<IniFileSection> allSections = new List<IniFileSection>();
		string filename;

		private static readonly string verticalLine = Environment.NewLine + "------------------------------------------------------------------------------" + Environment.NewLine;

		public IniFile(string filename) {
			this.filename = filename;

			if (File.Exists(filename)) {
				using (StreamReader reader = new StreamReader(filename)) {

					foreach (IniFileSection section in Parse(filename, reader)) {
						allSections.Add(section);
						sectionsByName[section.Name] = section;
					}
				}
			}
		}

		public bool FileExists {
			get {
				return File.Exists(this.filename);
			}
		}

		public void WriteToFile() {
			using (StreamWriter writer = new StreamWriter(this.filename)) {
				foreach (IIniFilePart section in allSections) {
					section.WriteOut(writer);
				}
			}
		}

		public IniFileSection GetNewOrParsedSection(string sectionName) {
			IniFileSection section;
			if (!sectionsByName.TryGetValue(sectionName, out section)) {

				section = new IniFileSection(sectionName, verticalLine);
				allSections.Add(section);
				sectionsByName[section.Name] = section;
			}
			return section;
		}

		public IniFileSection GetNewSection(string sectionName) {
			IniFileSection section = new IniFileSection(sectionName, verticalLine);
			allSections.Add(section);
			sectionsByName[section.Name] = section;
			return section;
		}

		public IniFileSection GetSection(string sectionName) {
			IniFileSection section;
			if (!sectionsByName.TryGetValue(sectionName, out section)) {
				throw new SEException("Missing section " + sectionName + " from the ini file.");
			}
			return section;
		}

		public bool HasSection(string name) {
			return this.sectionsByName.ContainsKey(name);
		}

		public IEnumerable<IniFileSection> GetSections(string sectionName) {
			foreach (IniFileSection section in allSections) {
				if (sectionName.Equals(section.Name, StringComparison.OrdinalIgnoreCase)) {
					yield return section;
				}
			}
		}

		public void RemoveSection(IniFileSection section) {
			allSections.Remove(section);

			IniFileSection oldSection;
			if (sectionsByName.TryGetValue(section.Name, out oldSection)) {
				if (oldSection == section) {
					sectionsByName.Remove(section.Name);
					foreach (IniFileSection s in allSections) {
						if (string.Equals(s.Name, section.Name, StringComparison.OrdinalIgnoreCase)) {
							sectionsByName[s.Name] = s;
							return;
						}
					}
				}
			}
		}

		//regular expressions for stream loading
		//[type name]//comment
		private static Regex headerRE = new Regex(@"^\[\s*(?<name>.*?)\s*\]\s*(((//)|(#))(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		//name=value //comment
		private static Regex valueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(((//)|(#))(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static IEnumerable<IniFileSection> Parse(string filename, TextReader stream) {
			int line = 0;
			IniFileSection curSection = null;
			StringBuilder comments = new StringBuilder();

			while (true) {
				string curLine = stream.ReadLine();
				line++;
				if (curLine != null) {
					curLine = curLine.Trim();
					if (curLine.Length == 0) {
						comments.AppendLine();
						continue;
					} else if (curLine.StartsWith("//") || curLine.StartsWith("# ")) {
						comments.AppendLine(curLine.Substring(2));
						continue;
					} else if (curLine.StartsWith("#")) {
						comments.AppendLine(curLine.Substring(1));
						continue;
					} else if (curLine.Trim('-').Length == 0) {
						comments.AppendLine(verticalLine);
						continue;
					}
					Match m = headerRE.Match(curLine);
					if (m.Success) {
						if (curSection != null) {//send the last section
							yield return curSection;
						}
						GroupCollection gc = m.Groups;
						curSection = new IniFileSection(gc["name"].Value, comments.ToString(), gc["comment"].Value);
						comments.Length = 0;

						continue;
					}
					m = valueRE.Match(curLine);
					if (m.Success) {
						if (curSection != null) {
							GroupCollection gc = m.Groups;
							IniFileValueLine valueLine = new IniFileValueLine(gc["name"].Value, gc["value"].Value,
								comments.ToString(), false, gc["comment"].Value);
							comments.Length = 0;
							curSection.SetParsedValue(valueLine);

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
						curSection.AddComment(comments.ToString());
						yield return curSection;
					}
					break;
				}
			} //end of (while (true)) - for each line of the file
		}
	}

	internal interface IIniFilePart {
		void WriteOut(TextWriter stream);
	}

	public class CommentedIniFilePart {
		internal readonly IniFileComment commentAbove;
		internal readonly IniFileComment commentNext;

		internal CommentedIniFilePart(string commentAbove, bool wrapcommentAbove, string commentNext) {
			this.commentAbove = new IniFileComment(commentAbove, wrapcommentAbove);
			this.commentNext = new IniFileComment(commentNext, false);
		}
	}

	public class IniFileSection : CommentedIniFilePart, IIniFilePart {
		private readonly string name;
		private Dictionary<string, IniFileValueLine> props = new Dictionary<string, IniFileValueLine>(StringComparer.OrdinalIgnoreCase);
		private List<IIniFilePart> parts = new List<IIniFilePart>();

		internal IniFileSection(string name, string commentAbove, string commentNext)
			: base(commentAbove, false, commentNext) {
			this.name = name;
		}

		public IniFileSection(string name, string comment)
			: base(comment, true, null) {

			this.name = name;
		}

		public string Name {
			get { return this.name; }
		}

		public ICollection<IniFileValueLine> Lines {
			get { return this.props.Values; }
		}

		internal void SetParsedValue(IniFileValueLine valueLine) {
			string valueName = valueLine.name;
			if (props.ContainsKey(valueName)) {
				Logger.WriteWarning("One section can't have more values of the same name (section [" + this.name + "], value name '" + valueName + "'. Ignoring.");
				return;
			}
			props[valueName] = valueLine;
			parts.Add(valueLine);
		}

		public void AddComment(string comment) {
			parts.Add(new IniFileComment(comment, false));
		}

		public T GetValue<T>(string valueName, T defaultValue, string comment) {
			IniFileValueLine value;
			if (props.TryGetValue(valueName, out value)) {
				return value.GetValue<T>();
			} else {
				comment = string.Concat(Environment.NewLine, "( ", valueName, ": ", comment, " )", Environment.NewLine);
				value = new IniFileValueLine(valueName, string.Concat(defaultValue), comment, true, null);
				props[valueName] = value;
				parts.Add(value);
				return defaultValue;
			}
		}

		public void SetValue<T>(string valueName, T value, string comment) {
			IniFileValueLine valueLine;
			if (this.props.TryGetValue(valueName, out valueLine)) {
				valueLine.SetValue(string.Concat(value));
			} else {
				comment = string.Concat(Environment.NewLine, "( ", valueName, ": ", comment, " )", Environment.NewLine);
				valueLine = new IniFileValueLine(valueName, string.Concat(value), comment, true, null);
				props[valueName] = valueLine;
				parts.Add(valueLine);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public T GetValue<T>(string valueName) {
			IniFileValueLine value;
			if (this.props.TryGetValue(valueName, out value)) {
				return value.GetValue<T>();
			} else {
				throw new SEException("Missing value " + valueName + " from the ini file.");
			}
		}

		public bool TryGetValue<T>(string valueName, out T retVal) {
			IniFileValueLine value;
			if (this.props.TryGetValue(valueName, out value)) {
				retVal = value.GetValue<T>();
				return true;
			} else {
				retVal = default(T);
				return false;
			}
		}

		public bool HasValue(string valueName) {
			return this.props.ContainsKey(valueName);
		}

		public void RemoveValue(string valueName) {
			this.props.Remove(valueName);
		}

		void IIniFilePart.WriteOut(TextWriter stream) {
			this.commentAbove.WriteOut(stream);
			stream.Write("[");
			stream.Write(this.name);
			stream.WriteLine("]");
			this.commentNext.WriteOut(stream);

			foreach (IIniFilePart part in this.parts) {
				part.WriteOut(stream);
			}
		}
	}

	public class IniFileValueLine : CommentedIniFilePart, IIniFilePart {
		internal string name;
		internal string valueString;//name=value

		internal bool valueSet;
		internal object value;

		internal IniFileValueLine(string name, string valueString, string commentAbove, bool wrap, string commentNext)
			: base(commentAbove, wrap, commentNext) {

			if (name.Trim(Tools.whitespaceChars).IndexOfAny(Tools.whitespaceChars) > -1) {
				throw new SEException("No whitespace characters allowed in value name");
			}
			this.name = name;
			this.valueString = valueString;
			//this.valueSet = false;
		}

		public string Name {
			get { return this.name; }
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "valueString")]
		public void SetValue(string valueString) {
			this.valueString = valueString;
			this.valueSet = false;
		}

		public T GetValue<T>() {
			if (!this.valueSet) {
				if (typeof(T) != typeof(string) && string.IsNullOrEmpty(this.valueString)) {
					this.value = default(T);
				} else {
					this.value = ConvertTools.ConvertTo<T>(this.valueString);
				}
				this.valueSet = true;
			}
			return ConvertTools.ConvertTo<T>(value);
		}

		void IIniFilePart.WriteOut(TextWriter stream) {
			this.commentAbove.WriteOut(stream);
			stream.Write(this.name);
			stream.Write(" = ");
			stream.WriteLine(this.valueString);
			this.commentNext.WriteOut(stream);
		}
	}

	internal class IniFileComment : IIniFilePart {
		private string comment = "";
		private bool wrap;

		internal IniFileComment(string comment, bool wrap) {
			this.comment = comment;
			this.wrap = wrap;
		}

		public void WriteOut(TextWriter stream) {
			if (string.IsNullOrEmpty(this.comment)) {
				return;
			}

			StringReader reader = new StringReader(this.comment);

			string line;
			while ((line = reader.ReadLine()) != null) {
				if (line.Trim().Length == 0) {
					stream.WriteLine();
					continue;
				}

				string strOut = "# " + line;
				if (this.wrap) {
					while (strOut.Length > 80) {
						int space = strOut.LastIndexOf(' ', 80);
						if (space > -1) {
							if (space < 2) {
								space = strOut.IndexOf(' ', 80);
								if (space < 0) {
									break;
								}
							}
							string s = strOut.Substring(0, space);
							strOut = "# " + strOut.Substring(space);
							stream.WriteLine(s);

						} else {
							break;
						}
					}
				}
				stream.WriteLine(strOut);
			}

		}
	}
}