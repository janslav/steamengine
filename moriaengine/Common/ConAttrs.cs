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
	public enum LogStyles : byte {
		Default=0, Warning, Error, Fatal, Critical, Debug,
		Highlight, Ident, FileLine, FilePos, File, Number
	}

	public interface ILogStrDisplay {
		void Write(string data, LogStyles style);
		void SetTitle(string data);
		void SetTitleToDefault();
	}

	//deprecated. To be replaced by LogStrParser
	public class ConAttrs {
		internal const char separatorChar = '\u001B';
		internal const char eosChar='e';
		internal const char titleChar = 't';
		internal const char styleChar = 's';
		internal const string separatorString = "\u001B";
		internal const string titleString = "t";
		internal const string styleString = "s";
		internal const string eosString = "e";
		public const string EOS = separatorString + eosString + separatorString;

		#region Static Methods
		static public string PrintStyle(LogStyles style) {
			return string.Concat(separatorString, styleString, ((int) style).ToString(), separatorString);
		}

		static public string PrintTitle(string title) {
			return string.Concat(separatorString, titleString, title, separatorString);
		}
		#endregion

#if !MONO
		#region Delegates
		public delegate void StyleChangedHandler(object sender, LogStyles style);
		public delegate void TitleChangedHandler(object sender, string title);
		public delegate void NextChunkHandler(object sender, string chunk);
		#endregion

		internal struct LogStyle {
			public Color styleColor;
			public FontStyle styleFont;
			public FontFamily styleFontFamily;
			public float styleSize;

			public LogStyle(Color col, FontStyle style, FontFamily family, float size) {
				styleColor=col;
				styleFont=style;
				styleFontFamily=family;
				styleSize=size;
			}
			public LogStyle(Color col, FontStyle style) {
				styleColor=col;
				styleFont=style;
				styleFontFamily=defaultFamily;
				styleSize=defaultSize;
			}
		}

		private const LogStyles lastStyleElement=LogStyles.Number;
		private const float defaultSize=8.25f;
		private const FontStyle defaultFontStyle=FontStyle.Regular;
		private static readonly FontFamily defaultFamily=new FontFamily(GenericFontFamilies.SansSerif);
		private static readonly Color defaultColor=Color.Black;
		private LogStyle[] logStyles=new LogStyle[((int) lastStyleElement)+1];

		#region Log Style Configuration
		public float DefaultSize {
			get { return GetSize(LogStyles.Default); }
		}
		public Color DefaultColor {
			get { return GetColor(LogStyles.Default); }
		}
		public string DefaultFontFamilyName {
			get { return GetFontFamilyName(LogStyles.Default); }
		}
		public FontFamily DefaultFontFamily {
			get { return GetFontFamily(LogStyles.Default); }
		}
		public FontStyle DefaultFontStyle {
			get { return GetFontStyle(LogStyles.Default); }
		}

		public void SetStyle(LogStyles style, Color color, FontStyle fnt, FontFamily family, float size) {
			SetColor(style, color);
			SetFontStyle(style, fnt);
			SetFontFamily(style, family);
			SetSize(style, size);
		}
		public void SetStyle(LogStyles style, Color color, FontStyle fnt, string family, float size) {
			SetColor(style, color);
			SetFontStyle(style, fnt);
			SetFontFamily(style, family);
			SetSize(style, size);
		}

		public void SetColor(LogStyles style, Color color) {
			logStyles[(int) style].styleColor=color;
		}
		public Color GetColor(LogStyles style) {
			return logStyles[(int) style].styleColor;
		}

		public void SetFontStyle(LogStyles style, FontStyle fnt) {
			logStyles[(int) style].styleFont=fnt;
		}
		public FontStyle GetFontStyle(LogStyles style) {
			return logStyles[(int) style].styleFont;
		}

		public void SetFontFamily(LogStyles style, string name) {
			try {
				FontFamily family=new FontFamily(name);
				logStyles[(int) style].styleFontFamily=family;
			} catch {
				// TODO: name of font family is invalid
			}
		}
		public void SetFontFamily(LogStyles style, FontFamily family) {
			logStyles[(int) style].styleFontFamily=family;
		}
		public string GetFontFamilyName(LogStyles style) {
			return logStyles[(int) style].styleFontFamily.Name;
		}
		public FontFamily GetFontFamily(LogStyles style) {
			return logStyles[(int) style].styleFontFamily;
		}

		public void SetSize(LogStyles style, float size) {
			logStyles[(int) style].styleSize=size;
		}
		public float GetSize(LogStyles style) {
			return logStyles[(int) style].styleSize;
		}
		#endregion


		#region Events
		public event StyleChangedHandler StyleChanged;
		public event NextChunkHandler NextChunk;
		public event TitleChangedHandler TitleChanged;

		protected virtual void OnStyleChanged(LogStyles style) {
			if (StyleChanged!=null)
				StyleChanged(this, style);
		}

		protected virtual void OnNextChunk(string chunk) {
			if (NextChunk != null && chunk.Length > 0)
				NextChunk(this, chunk);
		}

		protected virtual void OnTitleChanged(string title) {
			if (TitleChanged != null)
				TitleChanged(this, title);
		}

		#endregion
		public void DefaultSettings() {
			logStyles[(int) LogStyles.Default]		= new LogStyle(defaultColor, defaultFontStyle, defaultFamily, defaultSize);
			logStyles[(int) LogStyles.Warning]		= new LogStyle(Color.Red, defaultFontStyle);
			logStyles[(int) LogStyles.Error]		= new LogStyle(Color.Red, defaultFontStyle);
			logStyles[(int) LogStyles.Fatal]		= new LogStyle(Color.Red, FontStyle.Bold);
			logStyles[(int) LogStyles.Critical]		= new LogStyle(Color.Red, FontStyle.Bold);
			logStyles[(int) LogStyles.Debug]		= new LogStyle(Color.Gray, defaultFontStyle);
			logStyles[(int) LogStyles.FileLine]		= new LogStyle(Color.Blue, defaultFontStyle);
			logStyles[(int) LogStyles.Highlight]	= new LogStyle(Color.Orange, defaultFontStyle);
			logStyles[(int) LogStyles.FilePos]		= new LogStyle(defaultColor, FontStyle.Italic);
			logStyles[(int) LogStyles.File]			= new LogStyle(Color.Purple, defaultFontStyle);
			logStyles[(int) LogStyles.Number]		= new LogStyle(Color.Blue, defaultFontStyle);
			logStyles[(int) LogStyles.Ident]		= new LogStyle(Color.Blue, FontStyle.Bold);
		}

		public ConAttrs() {
			DefaultSettings();
		}
#endif
	}

	public class LogStrParser {
		private ILogStrDisplay display;
		private Stack<LogStyles> styleStack = new Stack<LogStyles>();

		private static char[] separatorArray = new char[] { ConAttrs.separatorChar };

		public LogStrParser(ILogStrDisplay display) {
			this.display = display;
		}

		public void ProcessLogStr(LogStr logStr) {
			ProcessLogStr(logStr.rawString);
		}

		private LogStyles CurrentStyle {
			get {
				if (styleStack.Count > 0) {
					return styleStack.Peek();
				}
				return LogStyles.Default;
			}
		}

		public void ProcessLogStr(string logStrEncoded) {
			string[] tokens = logStrEncoded.Split(separatorArray);
			int tokenLen = tokens.Length;
			if (tokenLen > 0) {
				for (int i = 0; i<tokenLen; i++) {
					string token = tokens[i];

					if (string.IsNullOrEmpty(token)) {
						continue;
					}
					if (i % 2 == 1) {
						switch (token[0]) {
							case ConAttrs.eosChar:
								if (styleStack.Count > 0) {
									styleStack.Pop();
								}
								continue;
							case ConAttrs.titleChar:
								string title = token.Substring(1);
								if (title.Length > 0) {
									this.display.SetTitle(title);
								} else {
									this.display.SetTitleToDefault();
								}
								continue;
							case ConAttrs.styleChar:
								int num = int.Parse(token.Substring(1));
								this.styleStack.Push((LogStyles) num);
								continue;
						}
					}

					this.display.Write(token, this.CurrentStyle);
				}
			}
		}
	}
}