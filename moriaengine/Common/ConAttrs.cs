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
		private string RawString;
		private string chunk;
		private int ProcessingIndex;
		private string title;
		private LogStyles logStyle;
		private Stack styleStack;

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
			RawString="";
			ProcessingIndex=0;
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

			logStyle=LogStyles.Default;
		}

		public ConAttrs() {
			DefaultSettings();
			styleStack=new Stack();
		}
		#region Public Methods
		public String Strip() {
			string str="";
			int idx, idx2;

			idx=0;
			try {
				while (idx>=0 && idx<RawString.Length) {
					while (idx>=0 && RawString[idx]==separatorChar) {
						idx++;
						idx2=RawString.IndexOf(separatorChar, idx);

						if (idx2>idx)
							idx=idx2+1;
					}
					idx2=RawString.IndexOf(separatorChar, idx);

					if (idx2>idx) {
						str+=RawString.Substring(idx, idx2-idx);
					} else {
						str+=RawString.Substring(idx, RawString.Length-idx);
					}

					idx=idx2;
				}
			} catch (Exception) {
			}

			return str;
		}

		public void Process() {
			logStyle=LogStyles.Default;
			OnStyleChanged(logStyle);
			styleStack.Clear();
			while (ProcessChunk()) { }
		}

		public void SetString(string str) {
			RawString=str;
			ProcessingIndex=0;
		}

		public bool ProcessChunk() {
			int idx;

			if (ProcessingIndex<0 || ProcessingIndex>=RawString.Length)
				return false;

			try {
				while (RawString[ProcessingIndex]==separatorChar) {
					ProcessingIndex++;
					idx=RawString.IndexOf(separatorChar, ProcessingIndex);

					if (idx>ProcessingIndex) {
						switch (RawString[ProcessingIndex++]) {
							case eosChar:
								if (styleStack.Count>0) {
									logStyle=(LogStyles) styleStack.Pop();
									OnStyleChanged(logStyle);
								} else {
									OnStyleChanged(LogStyles.Default);
								}
								break;
							case titleChar:
								title=RawString.Substring(ProcessingIndex, idx-ProcessingIndex);
								OnTitleChanged(title);
								break;
							case styleChar:
								try {
									int i;
									LogStyles old=logStyle;

									i = int.Parse(RawString.Substring(ProcessingIndex, idx - ProcessingIndex));
									if (i<=(int) lastStyleElement && i>=0) {
										logStyle=(LogStyles) i;
									} else {
										logStyle = LogStyles.Default;
									}

									if (logStyle!=old) {
										styleStack.Push(old);
										OnStyleChanged(logStyle);
									}
								} catch (Exception) {
									// something goes wrong, setting default text style
									logStyle = LogStyles.Default;
								}
								break;
							default:
								// Non-standard tag
								break;
						}
						ProcessingIndex=idx+1;
					}
				}
				idx=RawString.IndexOf(separatorChar, ProcessingIndex);

				if (idx>ProcessingIndex) {
					chunk=RawString.Substring(ProcessingIndex, idx-ProcessingIndex);
				} else {
					chunk=RawString.Substring(ProcessingIndex, RawString.Length-ProcessingIndex);
				}

				OnNextChunk(chunk);

				ProcessingIndex=idx;
			} catch (Exception) {
			}

			return true;
		}
		#endregion
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
				foreach (string token in tokens) {
					if (string.IsNullOrEmpty(token)) {
						continue;
					}
					switch (token[0]) {
						case ConAttrs.eosChar:
							if (styleStack.Count > 0) {
								styleStack.Pop();
							}
							break;
						case ConAttrs.titleChar:
							string title = token.Substring(1);
							if (title.Length > 0) {
								this.display.SetTitle(title);
							} else {
								this.display.SetTitleToDefault();
							}
							break;
						case ConAttrs.styleChar:
							int i = int.Parse(token.Substring(1));
							this.styleStack.Push((LogStyles) i);
							break;
						default:
							this.display.Write(token, this.CurrentStyle);
							break;
					}
				}
			}
		}
	}
}