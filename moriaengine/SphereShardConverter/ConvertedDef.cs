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
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;

namespace SteamEngine.Converter {
	public delegate string LineImpl(ConvertedDef def, PropsLine line);

	public class LineImplTask {
		public readonly string fieldName;
		public readonly LineImpl deleg;

		public LineImplTask(string fieldName, LineImpl deleg) {
			this.fieldName = fieldName;
			this.deleg = deleg;
		}
	}

	public class ConvertedDef {
		protected PropsSection origData;
		protected string origFile;
		protected List<string> writtenData = new List<string>();
		private ConvertedFile convertFile;
		public string headerType = null;
		public string headerName = null;

		private bool dontDump = false;

		protected List<LineImplTask[]> firstStageImplementations = new List<LineImplTask[]>();
		protected List<LineImplTask[]> secondStageImplementations = new List<LineImplTask[]>();
		protected List<LineImplTask[]> thirdStageImplementations = new List<LineImplTask[]>();

		public ConvertedDef(PropsSection input) {
			this.origData = input;
			this.origFile = input.Filename;
			this.convertFile = ConverterMain.currentIFile;

			this.headerType = input.HeaderType;
			headerName = input.HeaderName;
		}

		public void Set(PropsLine line) {
			if ((line.Comment == null) || (line.Comment.Length == 0)) {
				this.writtenData.Add(String.Format("{0} = {1}", line.Name, line.Value));
			} else {
				this.writtenData.Add(String.Format("{0} = {1} //{2} ", line.Name, line.Value, line.Comment));
			}
		}

		public void Set(string key, string value, string comment) {
			if ((comment == null) || (comment.Length == 0)) {
				this.writtenData.Add(String.Format("{0} = {1}", key, value));
			} else {
				this.writtenData.Add(String.Format("{0} = {1} //{2} ", key, value, comment));
			}
		}

		public virtual void Dump(TextWriter writer) {
			writer.WriteLine();
			string header = String.Concat("[", this.headerType, " ", headerName, "]");

			if (this.origData.HeaderComment.Length > 0) {
				header = header + " //" + this.origData.HeaderComment;
			}
			if (dontDump) {
				header = "//" + header + " //(commented out by Converter) ";
			}
			writer.WriteLine(header);

			foreach (string line in this.writtenData) {
				writer.WriteLine(dontDump ? ("//" + line) : line);
			}
		}

		private void BasicStageImpl(List<LineImplTask[]> implementations) {
			foreach (LineImplTask[] arr in implementations) {
				foreach (LineImplTask task in arr) {
					string origKey = task.fieldName;
					LineImpl deleg = task.deleg;

					string key = origKey;
					PropsLine line = this.origData.TryPopPropsLine(key);

					for (int a = 0; (line != null); a++) {
						deleg(this, line);

						key = origKey + a.ToString();
						line = this.origData.TryPopPropsLine(key);
					}
				}
			}
		}

		public void DontDump() {
			dontDump = true;
		}

		public virtual void FirstStage() {
			bool needspace = false;
			PropsLine line = this.origData.TryPopPropsLine("category");
			if (line != null) {
				Set(line); needspace = true;
			}
			line = this.origData.TryPopPropsLine("subsection");
			if (line != null) {
				Set(line); needspace = true;
			}
			line = this.origData.TryPopPropsLine("description");
			if (line != null) {
				Set(line); needspace = true;
			}
			if (needspace) {
				this.writtenData.Add("");
			}


			BasicStageImpl(firstStageImplementations);
		}

		public virtual void SecondStage() {
			BasicStageImpl(secondStageImplementations);
		}

		public virtual void ThirdStage() {
			BasicStageImpl(thirdStageImplementations);

			foreach (PropsLine line in this.origData.PropsLines) {
				if (line.Name.ToLower().StartsWith("tag.")) {
					WriteAsIs(this, line);
				} else {
					WriteAsComment(this, line);
				}
			}
		}

		//public override string tostring() {
		//	return "converteddef";
		//}

		public void Info(int linenum, string message) {
			if (ConverterMain.AdditionalConverterMessages) {
				Console.WriteLine("Info: " + LogStr.FileLine(this.convertFile.origPath, linenum) + LogStr.Highlight(message));
			}
		}

		public void Warning(int linenum, string message) {
			Logger.WriteWarning(this.convertFile.origPath, linenum, message);
		}

		public void Error(int linenum, string message) {
			Logger.WriteError(this.convertFile.origPath, linenum, message);
		}

		//generic line implementations
		protected static string WriteAsIs(ConvertedDef def, PropsLine line) {
			def.Set(line);
			return line.Value;
		}

		protected static string WriteInQuotes(ConvertedDef def, PropsLine line) {
			string value;
			Match ma = TagMath.stringRE.Match(line.Value);
			if (ma.Success) {
				value = String.Intern(ma.Groups["value"].Value);
			} else {
				value = line.Value;
			}
			value = "\"" + value + "\"";
			def.Set(line.Name, value, line.Comment);
			return value;
		}

		//protected static void WriteAsBool(ConvertedDef def, PropsLine line) {
		//    string value;
		//    Match ma = TagMath.stringRE.Match(line.value);
		//    if (ma.Success) {
		//        value = String.Intern(ma.Groups["value"].Value);
		//    } else {
		//        value = line.value;
		//    }
		//    value = "\""+value+"\"";
		//    def.Set(line.name, value, line.comment);
		//}

		protected static string WriteAsComment(ConvertedDef def, PropsLine line) {
			def.Set("//" + line.Name, line.Value, line.Comment + " commented out by Converter");
			return line.Value;
		}

		protected static string MayBeInt_IgnorePoint(ConvertedDef def, PropsLine line) {
			string retVal = line.Value;
			try {
				object number = ConvertTools.ParseAnyNumber(line.Value.Replace(".", ""));
				retVal = number.ToString();
			} catch (Exception) {
			}
			def.Set(line.Name, retVal, line.Comment);
			return retVal;
		}

		protected static string MayBeHex_IgnorePoint(ConvertedDef def, PropsLine line) {
			string retVal = line.Value;
			try {
				object number = ConvertTools.ParseAnyNumber(line.Value.Replace(".", ""));
				long i = Convert.ToInt64(number);
				retVal = "0x" + i.ToString("x");
			} catch (Exception) {
			}
			def.Set(line.Name, retVal, line.Comment);
			return retVal;
		}
	}
}