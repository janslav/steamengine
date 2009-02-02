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
#if !MONO
using System.Drawing;
using System.Drawing.Text;
#endif
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace SteamEngine.Common {
	public enum LogStyles : int {
		Default = 0, 
		Warning, Error, Fatal, Critical, Debug,
		Highlight, Ident, FileLine, FilePos, File, Number
	}

	public interface ILogStrDisplay {
		void Write(string data, LogStyleInfo style);
		void SetTitle(string data);
		void SetTitleToDefault();
	}

	public class LogStyleInfo {
		public readonly Color textColor;
		public readonly FontStyle fontStyle;
		public readonly FontFamily fontFamily;
		public readonly float fontSize;
		public readonly Font font;
		public readonly bool isLink;

		public LogStyleInfo(Color color, FontStyle fontStyle, FontFamily fontFamily, float fontSize, bool isLink) {
			this.textColor = color;
			this.fontStyle = fontStyle;
			this.fontFamily = fontFamily;
			this.fontSize = fontSize;
			this.isLink = isLink;
			this.font = new Font(this.fontFamily, this.fontSize, this.fontStyle);
		}

		public LogStyleInfo(Color color, FontStyle fontStyle)
			: this(color, fontStyle, LogStrBase.defaultFamily, LogStrBase.defaultSize, false) {

		}
	}

	public static class LogStrBase {
		internal const char separatorChar = '\u001B';
		internal const char eosChar = 'e';
		internal const char titleChar = 't';
		internal const char styleChar = 's';
		internal const string separatorString = "\u001B";
		internal const string titleString = "t";
		internal const string styleString = "s";
		internal const string eosString = "e";

		public static readonly int logStylesCount = Enum.GetValues(typeof(LogStyles)).Length;

		public const float defaultSize = 8.25f;
		public const FontStyle defaultFontStyle = FontStyle.Regular;
		public static readonly FontFamily defaultFamily = new FontFamily(GenericFontFamilies.SansSerif);
		public static readonly Color defaultColor = Color.Black;

		private static readonly LogStyleInfo[] logStyles = new LogStyleInfo[logStylesCount];

		static LogStrBase() {
			logStyles[(int) LogStyles.Default] = new LogStyleInfo(defaultColor, defaultFontStyle, defaultFamily, defaultSize, false);
			logStyles[(int) LogStyles.Warning] = new LogStyleInfo(Color.Red, defaultFontStyle);
			logStyles[(int) LogStyles.Error] = new LogStyleInfo(Color.Red, defaultFontStyle);
			logStyles[(int) LogStyles.Fatal] = new LogStyleInfo(Color.Red, FontStyle.Bold);
			logStyles[(int) LogStyles.Critical] = new LogStyleInfo(Color.Red, FontStyle.Bold);
			logStyles[(int) LogStyles.Debug] = new LogStyleInfo(Color.Gray, defaultFontStyle);
			logStyles[(int) LogStyles.FileLine] = new LogStyleInfo(Color.Blue, defaultFontStyle, defaultFamily, defaultSize, true);
			logStyles[(int) LogStyles.Highlight] = new LogStyleInfo(Color.Orange, defaultFontStyle);
			logStyles[(int) LogStyles.FilePos] = new LogStyleInfo(defaultColor, FontStyle.Italic, defaultFamily, defaultSize, true);
			logStyles[(int) LogStyles.File] = new LogStyleInfo(Color.Purple, defaultFontStyle, defaultFamily, defaultSize, true);
			logStyles[(int) LogStyles.Number] = new LogStyleInfo(Color.Blue, defaultFontStyle);
			logStyles[(int) LogStyles.Ident] = new LogStyleInfo(Color.Blue, FontStyle.Bold);
		}

		public static LogStyleInfo DefaultLogStyleInfo {
			get { return logStyles[(int) LogStyles.Default]; }
		}

		public static LogStyleInfo GetLogStyleInfo(LogStyles style) {
			return logStyles[(int) style];
		}
	}
}