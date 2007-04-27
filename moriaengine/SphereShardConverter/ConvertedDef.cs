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
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using SteamEngine.Common;
using System.Configuration;

namespace SteamEngine.Converter {
	public delegate void LineImpl(ConvertedDef def, PropsLine line);

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
		protected ArrayList writtenData = new ArrayList();
		private ConvertedFile convertFile;
		public string headerType = null;
		public string headerName = null;

		private bool dontDump = false;
		
		protected ArrayList firstStageImplementations = new ArrayList(5);
		protected ArrayList secondStageImplementations = new ArrayList(5);
		protected ArrayList thirdStageImplementations = new ArrayList(5);
		
		public ConvertedDef(PropsSection input) {
			this.origData = input;
			this.origFile = input.filename;
			this.convertFile = ConverterMain.currentIFile;
			
			headerType = input.headerType;
			headerName = input.headerName;
		}

		public void Set(PropsLine line) {
			if ((line.comment == null) || (line.comment.Length == 0)) {
				writtenData.Add(String.Format("{0} = {1}", line.name, line.value));
			} else {
				writtenData.Add(String.Format("{0} = {1} //{2} ", line.name, line.value, line.comment));
			}
		}
				
		public void Set(string key, string value, string comment) {
			if ((comment == null) || (comment.Length == 0)) {
				writtenData.Add(String.Format("{0} = {1}", key, value));
			} else {
				writtenData.Add(String.Format("{0} = {1} //{2} ", key, value, comment));
			}
		}
		
		public virtual void Dump(TextWriter writer) {
			writer.WriteLine();
			string header = String.Concat("[", headerType, " ", headerName, "]");

			if (origData.headerComment.Length > 0) {
				header = header + " //"+origData.headerComment;
			}
			if (dontDump) {
				header = "//"+header+" //(commented out by Converter) ";
			}
			writer.WriteLine(header);

			foreach (string line in writtenData) {
				writer.WriteLine(dontDump?("//"+line):line);
			}
		}

		private void BasicStageImpl(ArrayList implementations) {
			foreach (LineImplTask[] arr in implementations) {
				foreach (LineImplTask task in arr) {
					string origKey = task.fieldName;
					LineImpl deleg = task.deleg;

					string key = origKey;
					PropsLine line = origData.TryPopPropsLine(key);

					for (int a=0;(line!=null);a++) {
						deleg(this, line);

						key=origKey+a.ToString();
						line = origData.TryPopPropsLine(key);
					}
				}
			}
		}

		public void DontDump() {
			dontDump = true;
		}
		
		public virtual void FirstStage() {
			bool needspace = false;
			PropsLine line = origData.TryPopPropsLine("category");
			if (line != null) {
				Set(line); needspace = true;
			}
			line = origData.TryPopPropsLine("subsection");
			if (line != null) {
				Set(line); needspace = true;
			}
			line = origData.TryPopPropsLine("description");
			if (line != null) {
				Set(line); needspace = true;
			}
			if (needspace) {
				writtenData.Add("");
			}


			BasicStageImpl(firstStageImplementations);
		}

		public virtual void SecondStage() {
			BasicStageImpl(secondStageImplementations);
		}

		public virtual void ThirdStage() {
			BasicStageImpl(thirdStageImplementations);
			
			foreach (PropsLine line in origData.GetPropsLines()) {
				if (line.name.ToLower().StartsWith("tag.")) {
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
				Console.WriteLine("Info: "+LogStr.FileLine(this.convertFile.origPath, linenum)+LogStr.Highlight(message));
			}
		}

		public void Warning(int linenum, string message) {
			Logger.WriteWarning(this.convertFile.origPath, linenum, message);
		}

		public void Error(int linenum, string message) {
			Logger.WriteError(this.convertFile.origPath, linenum, message);
		}

//generic line implementations
		protected static void WriteAsIs(ConvertedDef def, PropsLine line) {
			def.Set(line);
		}
		
		protected static void WriteInQuotes(ConvertedDef def, PropsLine line) {
			string value;
			Match ma = TagMath.stringRE.Match(line.value);
			if (ma.Success) {
				value = String.Intern(ma.Groups["value"].Value);
			} else {
				value = line.value;
			}
			value = "\""+value+"\"";
			def.Set(line.name, value, line.comment);
		}

		protected static void WriteAsComment(ConvertedDef def, PropsLine line) {
			def.Set("//"+line.name, line.value, line.comment+" commented out by Converter");
		}

		protected static void MayBeInt_IgnorePoint(ConvertedDef def, PropsLine line) {
			object number;
			try {
				number = TagMath.ParseSphereNumber(line.value.Replace(".", ""));
			} catch (Exception) {
				WriteAsIs(def, line);
				return;
			}
			def.Set(line.name, number.ToString(), line.comment);
		}

		protected static void MayBeHex_IgnorePoint(ConvertedDef def, PropsLine line) {
			object number;
			try {
				number = TagMath.ParseSphereNumber(line.value.Replace(".", ""));
			} catch (Exception) {
				WriteAsIs(def, line);
				return;
			}
			long i = Convert.ToInt64(number);
			def.Set(line.name, "0x"+i.ToString("x"), line.comment);
		}
	}
}