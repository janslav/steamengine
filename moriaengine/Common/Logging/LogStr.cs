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
using System.Text;
using System.IO;
using SteamEngine;

namespace SteamEngine.Common {
	public class LogStr {
		private static string[] prefixStrings = new string[LogStrBase.logStylesCount];
		private const string EOS = LogStrBase.separatorString + LogStrBase.eosString + LogStrBase.separatorString;

		internal string rawString;
		internal string niceString;

		public string RawString { get { return rawString; } }
		public string NiceString { get { return niceString; } }

		static LogStr() {
			for (int i = 0, n = prefixStrings.Length; i < n; i++) {
				prefixStrings[i] = String.Concat(LogStrBase.separatorString, LogStrBase.styleString, i.ToString(), LogStrBase.separatorString);
			}
		}

		internal protected LogStr(string raw, string nice) {
			rawString = raw;
			niceString = nice;
		}

		#region Operators
		public static LogStr operator +(string str1, LogStr str2) {
			str2.rawString = str1 + str2.rawString;
			str2.niceString = str1 + str2.niceString;

			return str2;
		}

		public static explicit operator LogStr(string str) {
			return LogStr.Raw(str);
		}

		public static LogStr operator +(LogStr str1, string str2) {
			str1.rawString += str2;
			str1.niceString += str2;

			return str1;
		}

		public static LogStr operator +(LogStr str1, LogStr str2) {
			str1.rawString += str2.rawString;
			str1.niceString += str2.niceString;

			return str1;
		}
		#endregion

		public override string ToString() {
			return rawString;
		}

		public void Append(LogStr str) {
			this.rawString += str.rawString;
			this.niceString += str.niceString;
		}

		public static bool ParseFileLine(string fileline, out string file, out int line) {
			file = "";
			line = 0;
			int idx = fileline.IndexOf(',', 0);
			if (idx <= 0)
				return false;
			file = fileline.Substring(0, idx).Trim();
			string line_str = fileline.Substring(idx + 1, fileline.Length - idx - 1).Trim();
			try {
				line = int.Parse(line_str);
			} catch {
				return false;
			}
			return true;
		}

		#region Static methods
		public static string ToStringFor(object obj) {
			string result;
			if (obj == null) {
				result = "null";
			} else if (obj is object[]) {
				result = ArrayToString((object[]) obj);
			} else {
				result = obj.ToString();
			}
			return result;
		}
		public static string ArrayToString(object[] array) {
			string arrstr = "";
			int len = array.Length;
			for (int a = 0; a < len; a++) {
				object o = array[a];
				if (arrstr.Length == 0) {
					arrstr = o.ToString();
				} else {
					arrstr += ", " + o.ToString();
				}
			}
			return "{" + arrstr + "}";
		}

		public static LogStr Raw(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, obj.ToString());
		}
		public static LogStr Warning(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Warning, str));
		}
		public static LogStr Error(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Error, str));
		}
		public static LogStr Critical(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Critical, str));
		}
		public static LogStr Fatal(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Fatal, str));
		}
		public static LogStr Debug(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Debug, str));
		}
		public static LogStr Highlight(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Highlight, str));
		}
		public static LogStr Title(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(null, LogStr.GetTitleSettingMessage(str));
		}
		//public static LogStr SetStyle(LogStyles style) {
		//    return new LogStr(null, LogStr.GetStyleMessage(style));
		//}
		public static LogStr Style(object obj, LogStyles style) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(style, str));
		}
		public static LogStr Number(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Number, str));
		}
		public static LogStr Ident(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Ident, str));
		}
		public static LogStr FilePos(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.FilePos, str));
		}
		public static LogStr File(object obj) {
			string str = ToStringFor(obj);
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.File, str));
		}
		public static LogStr Raw(string str) {
			return new LogStr(str, str);
		}
		public static LogStr Warning(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Warning, str));
		}
		public static LogStr Error(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Error, str));
		}
		public static LogStr Critical(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Critical, str));
		}
		public static LogStr Fatal(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Fatal, str));
		}
		public static LogStr Debug(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Debug, str));
		}
		public static LogStr FileLine(string file, int line) {
			string str = TranslatePath(file) + ", " + line.ToString();
			return new LogStr("(" + str + ") ", "(" + LogStr.GetStyleMessage(LogStyles.FileLine, str) + ") ");
		}
		public static LogStr Highlight(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Highlight, str));
		}
		public static LogStr Title(string str) {
			return new LogStr(null, LogStr.GetTitleSettingMessage(str));
		}
		public static LogStr Style(string str, LogStyles style) {
			return new LogStr(str, LogStr.GetStyleMessage(style, str));
		}
		public static LogStr Number(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Number, str));
		}
		public static LogStr Ident(string str) {
			return new LogStr(str, LogStr.GetStyleMessage(LogStyles.Ident, str));
		}
		public static LogStr FilePos(string str) {
			return new LogStr(TranslatePath(str), LogStr.GetStyleMessage(LogStyles.FilePos, str));
		}
		public static LogStr File(string str) {
			return new LogStr(TranslatePath(str), LogStr.GetStyleMessage(LogStyles.File, str));
		}
		public static LogStr Code(string s) {
			return LogStr.Debug("[") + LogStr.Ident(s) + LogStr.Debug("]");
		}
		public static LogStr Concat(LogStr arg0, LogStr arg1) {
			return new LogStr(
				String.Concat(arg0.rawString, arg1.rawString),
				String.Concat(arg0.niceString, arg1.niceString));
		}

		public static LogStr Concat(LogStr arg0, LogStr arg1, LogStr arg2) {
			return new LogStr(
				String.Concat(arg0.rawString, arg1.rawString, arg2.rawString),
				String.Concat(arg0.niceString, arg1.niceString, arg2.niceString));
		}

		public static LogStr Concat(LogStr arg0, LogStr arg1, LogStr arg2, LogStr arg3) {
			return new LogStr(
				String.Concat(arg0.rawString, arg1.rawString, arg2.rawString, arg3.rawString),
				String.Concat(arg0.niceString, arg1.niceString, arg2.niceString, arg3.niceString));
		}

		public static LogStr Concat(params LogStr[] args) {
			int n = args.Length;
			string[] rawstrings = new string[n];
			string[] nicestrings = new string[n];
			for (int i = 0; i < n; i++) {
				rawstrings[i] = args[i].rawString;
				nicestrings[i] = args[i].niceString;
			}

			return new LogStr(
				String.Concat(rawstrings),
				String.Concat(nicestrings));
		}

		public static string GetStyleStartPrefix(LogStyles style) {
			return prefixStrings[(int) style];
		}

		private static string GetStyleMessage(LogStyles style, string text) {
			return string.Concat(prefixStrings[(int) style], text, EOS);
		}

		private static string GetTitleSettingMessage(string title) {
			return string.Concat(LogStrBase.separatorString, LogStrBase.titleString, title, LogStrBase.separatorString);
		}

		internal static readonly string defaultDir = Path.GetFullPath(".");
		internal static readonly string separatorAndDot = string.Concat(Path.DirectorySeparatorChar, ".");

		//adds /./ to the position of process default dir
		private static string TranslatePath(string path) {
			try {
				if (Path.IsPathRooted(path)) {
					if (path.StartsWith(defaultDir, StringComparison.OrdinalIgnoreCase)) {
						path = string.Concat(defaultDir, separatorAndDot, path.Substring(defaultDir.Length));
					}
				}
			} catch { }
			return path;
		}

		#endregion
	}

	public class LogStrBuilder {
		StringBuilder nice = new StringBuilder();
		StringBuilder raw = new StringBuilder();

		public LogStrBuilder() {
		}

		public LogStrBuilder Append(LogStr arg) {
			nice.Append(arg.niceString);
			raw.Append(arg.rawString);
			return this;
		}

		public LogStrBuilder Append(string arg) {
			nice.Append(arg);
			raw.Append(arg);
			return this;
		}

		public LogStrBuilder Append(object arg) {
			string str = arg as string;
			if (str != null) {
				Append(str);
				return this;
			}
			LogStr ls = arg as LogStr;
			if (ls != null) {
				Append(ls);
				return this;
			}
			str = arg.ToString();
			Append(str);
			return this;
		}

		public override string ToString() {
			return raw.ToString();
		}

		public LogStr ToLogStr() {
			return new LogStr(raw.ToString(), nice.ToString());
		}
	}

}