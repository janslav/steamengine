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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class Gump : AbstractScript {
		internal Gump()
			: base() {
		}

		internal Gump(string name)
			: base(name) {
		}

		public static new Gump Get(string name) {
			AbstractScript script;
			byDefname.TryGetValue(name, out script);
			return script as Gump;
		}

		internal abstract GumpInstance InternalConstruct(Thing focused, AbstractCharacter sendTo, DialogArgs args);
	}

	[Remark("Dialog arguments holder. It can contain arguments as tags as well as an array of (e.g. hardcoded arguments)"+
			"the array's length is unmodifiable so the only way to put args into it is to put them during constructor call."+
			"Arguments added in this way should be only the compulsory dialog arguments necessary in every case (for example "+
			"label and text in the Info/Error dialog-messages). Other args should be added as normal tags!")]
	public class DialogArgs : TagHolder {
		private object[] fldArgs;

		public DialogArgs() {
			this.fldArgs = new object[0] { }; //aspon prazdny pole, ale ne null
		}

		public DialogArgs(params object[] args) {
			this.fldArgs = args;
		}

		public object[] ArgsArray {
			get {
				return fldArgs;
			}
		}
	}

	public abstract class GumpInstance : PluginHolder {
		private static uint uids = 0;

		public readonly uint uid;
		public readonly Gump def;
		internal DialogArgs inputArgs;//arguments the gump is called with
		internal AbstractCharacter cont;//the player who sees this instance (src)
		internal Thing focus;//the thing this gump was "launched on"
		internal uint x;
		internal uint y;
		internal readonly StringBuilder layout = new StringBuilder();
		internal List<int> numEntryIDs;
		internal Dictionary<int, int> entryTextIds;

		//fields from former GumpBuilder
		internal bool movable = true;
		internal bool closable = true;
		internal bool disposable = true;
		//private List<string[]> elements = new List<string[]>();
		internal List<string> textsList;
		//internal int textsLengthsSum;

		internal GumpInstance(Gump def) {
			this.def = def;
			uid = uids++;
		}

		public DialogArgs InputArgs {
			get {
				return inputArgs;
			}

			set {
				inputArgs = value;
			}
		}

		public uint X {
			get {
				return x;
			}
			set {
				x = value;
			}
		}

		public uint Y {
			get {
				return y;
			}
			set {
				y = value;
			}
		}

		public AbstractCharacter Cont {
			get {
				return cont;
			}
		}

		public Thing Focus {
			get {
				return focus;
			}
		}

		public abstract void OnResponse(uint pressedButton, uint[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers);

		public override int GetHashCode() {
			return (int) uid;
		}

		public override bool Equals(object o) {
			return (o == this);
		}

		public override string ToString() {
			return String.Format("{0} {1} (uid {2})", GetType().Name, def.Defname, uid);
		}

		private void AddElement(string[] arr) {
			layout.Append("{").Append(String.Join(" ", arr)).Append("}");
		}

		//this is the final method where all the elements are compiled into the string
		public void CompilePacketData() {
			if (!movable) {
				layout.Insert(0, "{nomove}");
			}
			if (!closable) {
				layout.Insert(0, "{noclose}");
			}
			if (!disposable) {//what does it really mean? :)
				layout.Insert(0, "{nodispose}");
			}
		}

		private void CreateTexts() {
			if (textsList == null) {
				textsList = new List<string>();
			}
		}

		public void AddButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
			string[] arr = new string[] {
				"button", 
				x.ToString(), 
				y.ToString(),
				downGumpId.ToString(), 
				upGumpId.ToString(), 
				(isTrigger?"1": "0"), 
				pageId.ToString(),
				triggerId.ToString()
			};
			AddElement(arr);
		}

		public void AddCheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = new string[] {
				"checkbox", 
				x.ToString(), 
				y.ToString(),
				uncheckedGumpId.ToString(), 
				checkedGumpId.ToString(), 
				(isChecked?"1": "0"), 
				id.ToString(),
			};
			AddElement(arr);
		}

		public void AddRadio(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = new string[] {
				"radio", 
				x.ToString(), 
				y.ToString(),
				uncheckedGumpId.ToString(), 
				checkedGumpId.ToString(), 
				(isChecked?"1": "0"), 
				id.ToString()
			};
			AddElement(arr);
		}

		public void AddCheckerTrans(int x, int y, int width, int height) {
			string[] arr = new string[] {
				"checkertrans", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString()
			};
			AddElement(arr);
		}

		//is it possible without args? sphere tables say it is...
		public void AddCheckerTrans() {
			string[] arr = new string[] { "checkertrans" };
			AddElement(arr);
		}

		public void AddText(int x, int y, int hue, int textId) {
			string[] arr = new string[] {
				"text", 
				x.ToString(), 
				y.ToString(),
				hue.ToString(), 
				textId.ToString()
			};
			AddElement(arr);
		}

		public int AddText(int x, int y, int hue, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			CreateTexts();
			textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = textsList.Count - 1;
			AddText(x, y, hue, textId);
			return textId;
		}

		public void AddCroppedText(int x, int y, int width, int height, int hue, int textId) {
			string[] arr = new string[] {
				"croppedtext", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString(), 
				hue.ToString(), 
				textId.ToString()
			};
			AddElement(arr);
		}

		public int AddCroppedText(int x, int y, int width, int height, int hue, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			CreateTexts();
			textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = textsList.Count - 1;
			AddCroppedText(x, y, width, height, hue, textId);
			return textId;
		}

		public void AddGroup(int groupId) {
			string[] arr = new string[] {
				"group", 
				groupId.ToString(), 
			};
			AddElement(arr);
		}

		public void AddGumpPic(int x, int y, int gumpId) {
			string[] arr = new string[] {
				"gumppic", 
				x.ToString(), 
				y.ToString(),
				gumpId.ToString()
			};
			AddElement(arr);
		}

		public void AddGumpPic(int x, int y, int gumpId, int hue) {
			if (hue == 0) {
				AddGumpPic(x, y, gumpId);
			} else {
				string[] arr = new string[] {
					"gumppic", 
					x.ToString(), 
					y.ToString(),
					gumpId.ToString(), 
					"hue="+hue.ToString(), 
				};
				AddElement(arr);
			}
		}

		public void AddGumpPicTiled(int x, int y, int width, int height, int gumpId) {
			string[] arr = new string[] {
				"gumppictiled", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString(),
				gumpId.ToString(), 
			};
			AddElement(arr);
		}

		public void AddHTMLGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			string[] arr = new string[] {
				"htmlgump", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString(), 
				textId.ToString(), 
				(hasBoundBox? "1": "0"), 
				(isScrollable? "1": "0")
			};
			AddElement(arr);
		}

		public int AddHTMLGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			CreateTexts();
			textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = textsList.Count - 1;
			AddHTMLGump(x, y, width, height, textId, hasBoundBox, isScrollable);
			return textId;
		}

		public void AddPage(int pageId) {
			string[] arr = new string[] {
				"page",
				pageId.ToString(), 
			};
			AddElement(arr);
		}

		public void AddResizePic(int x, int y, int gumpId, int width, int height) {
			string[] arr = new string[] {
				"resizepic", 
				x.ToString(), 
				y.ToString(),
				gumpId.ToString(), 
				width.ToString(), 
				height.ToString(),
			};
			AddElement(arr);
		}

		//internal int AddString(string text) {
		//	Sanity.IfTrueThrow(text==null, "The text string can't be null");
		//	CreateTexts();
		//	return textsList.Add(text);
		//}

		public int AddTextLine(string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			CreateTexts();
			textsList.Add(text);
			//textsLengthsSum += text.Length;
			return textsList.Count - 1;
		}

		public void AddTextEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			if(id < short.MinValue || id > short.MaxValue) {
				throw new SEException(LogStr.Error("Nepovolena hodnota ID textoveho vstupu - zadano :"+id+" povoleny rozsah: "+short.MinValue+"-"+short.MaxValue));
			}
			string[] arr = new string[] {
				"textentry", 
				x.ToString(), 
				y.ToString(),
				widthPix.ToString(), 
				height.ToString(), 
				hue.ToString(), 
				id.ToString(),
				textId.ToString()
			};
			AddElement(arr);
			if (entryTextIds == null) {
				entryTextIds = new Dictionary<int, int>();
			}
			entryTextIds[id] = textId;
		}

		public int AddTextEntry(int x, int y, int widthPix, int height, int hue, int id, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			CreateTexts();
			textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = textsList.Count - 1;
			AddTextEntry(x, y, widthPix, height, hue, id, textId);
			return textId;
		}

		public void AddNumberEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			if(id < short.MinValue || id > short.MaxValue) {
				throw new SEException(LogStr.Error("Nepovolena hodnota ID ciselneho vstupu - zadano :" + id + " povoleny rozsah: " + short.MinValue + "-" + short.MaxValue));
			}
			string[] arr = new string[] {
				"textentry", 
				x.ToString(), 
				y.ToString(),
				widthPix.ToString(), 
				height.ToString(), 
				hue.ToString(), 
				id.ToString(),
				textId.ToString()
			};
			AddElement(arr);
			if (numEntryIDs == null) {
				numEntryIDs = new List<int>();
			}
			numEntryIDs.Add(id);

			if (entryTextIds == null) {
				entryTextIds = new Dictionary<int, int>();
			}
			entryTextIds[id] = textId;			
		}

		public int AddNumberEntry(int x, int y, int widthPix, int height, int hue, int id, double text) {
			CreateTexts();
			string textStr = text.ToString();
			textsList.Add(textStr);
			//textsLengthsSum += textStr.Length;
			int textId = textsList.Count - 1;
			AddNumberEntry(x, y, widthPix, height, hue, id, textId);
			return textId;
		}

		public void AddTilePic(int x, int y, int model) {
			string[] arr = new string[] {
				"tilepic", 
				x.ToString(), 
				y.ToString(),
				model.ToString()
			};
			AddElement(arr);
		}

		public void AddTilePicHue(int x, int y, int model, int hue) {
			if (hue == 0) {
				AddTilePic(x, y, model);
			} else {
				string[] arr = new string[] {
					"tilepichue", 
					x.ToString(), 
					y.ToString(),
					model.ToString(),
					hue.ToString()
				};
				AddElement(arr);
			}
		}

		public void AddXMFHTMLGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			AddXMFHTMLGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, 0);
		}

		public void AddXMFHTMLGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			string[] arr;
			if (hue == 0) {
				arr = new string[] {
					"xmfhtmlgump", 
					x.ToString(), 
					y.ToString(),
					width.ToString(), 
					height.ToString(), 
					textId.ToString(), 
					(hasBoundBox? "1": "0"), 
					(isScrollable? "1": "0")
				};
			} else {
				arr = new string[] {
					"xmfhtmlgumpcolor", 
					x.ToString(), 
					y.ToString(),
					width.ToString(), 
					height.ToString(), 
					textId.ToString(), 
					(hasBoundBox? "1": "0"), 
					(isScrollable? "1": "0"),
					hue.ToString()
				};
			}
			AddElement(arr);
		}
	}

	public class ResponseText {
		public readonly uint id;
		public readonly string text;
		public ResponseText(uint id, string text) {
			this.id = id;
			this.text = text;
		}
	}

	public class ResponseNumber {
		public readonly uint id;
		public readonly double number;
		public ResponseNumber(uint id, double number) {
			this.id = id;
			this.number = number;
		}
	}
}

//The 0xBF packet starts off with a cmd byte, followed by two bytes for the length.  After that is a two byte value which is a subcmd, and the message varies after that.
//General Info (5 bytes, plus specific message)
//� BYTE cmd
//� BYTE[2] len
//� BYTE[2] subcmd
//� BYTE[len-5] submessage
//
//Subcommand 4: "Close Generic Gump"
//
//    * BYTE[4] dialogID // which gump to destroy (second ID in 0xB0 packet)
//    * BYTE[4] buttonId // response buttonID for packet 0xB1