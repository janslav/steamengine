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

namespace SteamEngine.Common {
	public enum LogStyles : byte {
		Default=0, Warning, WarningData, Error, ErrorData, Fatal, FatalData,
		Critical, CriticalData, Debug, DebugData, FileLine, Highlight,
		Ident, FilePos, File, Number
	}

	public class ConAttrs {
		public const char charSeparator='\u001B';
		public const char charEOS='e';
		public const char charTitle='t';
		public const char charStyle='s';
		public static readonly string EOS=string.Format("{0}{1}{0}", charSeparator.ToString(), charEOS);

		#region Static Methods
		static public string PrintStyle(LogStyles style) {
			return string.Format("{0}{1}{2}{0}", charSeparator, charStyle, (int) style);
		}

		static public string PrintTitle(string title) {
			return string.Format("{0}{1}{2}{0}", charSeparator, charTitle, title);
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
			logStyles[(int) LogStyles.WarningData]	= new LogStyle(defaultColor, defaultFontStyle);
			logStyles[(int) LogStyles.Error]			= new LogStyle(Color.Red, defaultFontStyle);
			logStyles[(int) LogStyles.ErrorData]		= new LogStyle(defaultColor, defaultFontStyle);
			logStyles[(int) LogStyles.Fatal]			= new LogStyle(Color.Red, FontStyle.Bold);
			logStyles[(int) LogStyles.FatalData]		= new LogStyle(defaultColor, defaultFontStyle);
			logStyles[(int) LogStyles.Critical]		= new LogStyle(Color.Red, FontStyle.Bold);
			logStyles[(int) LogStyles.CriticalData]	= new LogStyle(defaultColor, defaultFontStyle);
			logStyles[(int) LogStyles.Debug]			= new LogStyle(Color.Gray, defaultFontStyle);
			logStyles[(int) LogStyles.DebugData]		= new LogStyle(Color.Gray, defaultFontStyle);
			logStyles[(int) LogStyles.FileLine]		= new LogStyle(Color.Blue, defaultFontStyle);
			logStyles[(int) LogStyles.Highlight]		= new LogStyle(Color.Orange, defaultFontStyle);
			logStyles[(int) LogStyles.FilePos]		= new LogStyle(defaultColor, FontStyle.Italic);
			logStyles[(int) LogStyles.File]			= new LogStyle(Color.Purple, defaultFontStyle);
			logStyles[(int) LogStyles.Number]		= new LogStyle(Color.Blue, defaultFontStyle);
			logStyles[(int) LogStyles.Ident]			= new LogStyle(Color.Blue, FontStyle.Bold);

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
					while (idx>=0 && RawString[idx]==charSeparator) {
						idx++;
						idx2=RawString.IndexOf(charSeparator, idx);

						if (idx2>idx)
							idx=idx2+1;
					}
					idx2=RawString.IndexOf(charSeparator, idx);

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
				while (RawString[ProcessingIndex]==charSeparator) {
					ProcessingIndex++;
					idx=RawString.IndexOf(charSeparator, ProcessingIndex);

					if (idx>ProcessingIndex) {
						switch (RawString[ProcessingIndex++]) {
							case charEOS:
								if (styleStack.Count>0) {
									logStyle=(LogStyles) styleStack.Pop();
									OnStyleChanged(logStyle);
								} else {
									OnStyleChanged(LogStyles.Default);
								}
								break;
							case charTitle:
								title=RawString.Substring(ProcessingIndex, idx-ProcessingIndex);
								OnTitleChanged(title);
								break;
							case charStyle:
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
				idx=RawString.IndexOf(charSeparator, ProcessingIndex);

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
}