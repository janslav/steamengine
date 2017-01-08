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
using System.Text;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class GumpDef : AbstractScript {
		internal GumpDef()
			: base() {
		}

		internal GumpDef(string name)
			: base(name) {
		}

		public new static GumpDef GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as GumpDef;
		}

		internal abstract Gump InternalConstruct(Thing focused, AbstractCharacter sendTo, DialogArgs args);
	}

	/// <summary>
	/// Dialog arguments holder. It can contain arguments as tags as well as an array of (e.g. hardcoded arguments)
	/// the array's length is unmodifiable so the only way to put args into it is to put them during constructor call.
	/// Arguments added in this way should be only the compulsory dialog arguments necessary in every case (for example 
	/// label and text in the Info/Error dialog-messages). Other args should be added as tags!
	/// </summary>
	public class DialogArgs : TagHolder {
		private object[] fldArgs;

		//public DialogArgs() {
		//    this.fldArgs = new object[0]; //aspon prazdny pole, ale ne null
		//}

		public DialogArgs(params object[] args) {
			this.fldArgs = args;
		}

		public object this[int i] {
			get {
				return this.fldArgs[i];
			}
			set {
				this.fldArgs[i] = value;
			}
		}

		public object[] GetArgsArray() {
			return this.fldArgs;
		}
	}

	public abstract class Gump {
		private static int uids;

		private readonly int uid;
		private readonly GumpDef def;
		private DialogArgs inputArgs;//arguments the gump is called with
		private AbstractCharacter cont;//the player who sees this instance (src)
		private Thing focus;//the thing this gump was "launched on"
		private int x;
		private int y;
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

		internal Gump(GumpDef def) {
			this.def = def;
			this.uid = uids++;
		}

		public DialogArgs InputArgs {
			get {
				return this.inputArgs;
			}

			set {
				this.inputArgs = value;
			}
		}

		public int X {
			get {
				return this.x;
			}
			set {
				this.x = value;
			}
		}

		public int Y {
			get {
				return this.y;
			}
			set {
				this.y = value;
			}
		}

		public AbstractCharacter Cont {
			get {
				return this.cont;
			}
		}

		public Thing Focus {
			get {
				return this.focus;
			}
		}

		public int Uid {
			get {
				return this.uid;
			}
		}

		public GumpDef Def {
			get {
				return this.def;
			}
		}

		public abstract void OnResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers);

		public override int GetHashCode() {
			return this.uid;
		}

		public override bool Equals(object obj) {
			return (obj == this);
		}

		public override string ToString() {
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0} {1} (uid {2})",
				Tools.TypeToString(this.GetType()), this.def.Defname, this.uid);
		}

		private void AddElement(string[] arr) {
			this.layout.Append("{").Append(String.Join(" ", arr)).Append("}");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "focus"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "cont")]
		internal void FinishCompilingPacketData(Thing focus, AbstractCharacter cont) {
			if (!this.movable) {
				this.layout.Insert(0, "{nomove}");
			}
			if (!this.closable) {
				this.layout.Insert(0, "{noclose}");
			}
			if (!this.disposable) {//what does it really mean? :)
				this.layout.Insert(0, "{nodispose}");
			}
			this.focus = focus;
			this.cont = cont;
		}

		private void CreateTexts() {
			if (this.textsList == null) {
				this.textsList = new List<string>();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
			string[] arr = new string[] {
				"button", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				downGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				upGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				(isTrigger?"1": "0"), 
				pageId.ToString(System.Globalization.CultureInfo.InvariantCulture),
				triggerId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddTiledButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId, int itemId, int hue, int width, int height) {
			string[] arr = new string[] {
				"buttontileart", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				downGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				upGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				(isTrigger? "1": "0"), 
				pageId.ToString(System.Globalization.CultureInfo.InvariantCulture),
				triggerId.ToString(System.Globalization.CultureInfo.InvariantCulture),

				itemId.ToString(System.Globalization.CultureInfo.InvariantCulture),
				hue.ToString(System.Globalization.CultureInfo.InvariantCulture),
				width.ToString(System.Globalization.CultureInfo.InvariantCulture),
				height.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddCheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = new string[] {
				"checkbox", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				uncheckedGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				checkedGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				(isChecked?"1": "0"), 
				id.ToString(System.Globalization.CultureInfo.InvariantCulture),
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddRadio(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = new string[] {
				"radio", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				uncheckedGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				checkedGumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				(isChecked?"1": "0"), 
				id.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddCheckerTrans(int x, int y, int width, int height) {
			string[] arr = new string[] {
				"checkertrans", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		//is it possible without args? sphere tables say it is...
		public void AddCheckerTrans() {
			string[] arr = new string[] { "checkertrans" };
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddText(int x, int y, int hue, int textId) {
			string[] arr = new string[] {
				"text", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				hue.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				textId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public int AddText(int x, int y, int hue, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddText(x, y, hue, textId);
			return textId;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddCroppedText(int x, int y, int width, int height, int hue, int textId) {
			string[] arr = new string[] {
				"croppedtext", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				hue.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				textId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public int AddCroppedText(int x, int y, int width, int height, int hue, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddCroppedText(x, y, width, height, hue, textId);
			return textId;
		}

		public void AddGroup(int groupId) {
			string[] arr = new string[] {
				"group", 
				groupId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddGumpPic(int x, int y, int gumpId) {
			string[] arr = new string[] {
				"gumppic", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				gumpId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddGumpPic(int x, int y, int gumpId, int hue) {
			if (hue == 0) {
				this.AddGumpPic(x, y, gumpId);
			} else {
				string[] arr = new string[] {
					"gumppic", 
					x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					y.ToString(System.Globalization.CultureInfo.InvariantCulture),
					gumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					"hue="+hue.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				};
				this.AddElement(arr);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddGumpPicTiled(int x, int y, int width, int height, int gumpId) {
			string[] arr = new string[] {
				"gumppictiled", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture),
				gumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddHtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			string[] arr = new string[] {
				"htmlgump", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				textId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				(hasBoundBox? "1": "0"), 
				(isScrollable? "1": "0")
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public int AddHtmlGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddHtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
			return textId;
		}

		public void AddPage(int pageId) {
			string[] arr = new string[] {
				"page",
				pageId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddResizePic(int x, int y, int gumpId, int width, int height) {
			string[] arr = new string[] {
				"resizepic", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				gumpId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture),
			};
			this.AddElement(arr);
		}

		//internal int AddString(string text) {
		//	Sanity.IfTrueThrow(text==null, "The text string can't be null");
		//	CreateTexts();
		//	return textsList.Add(text);
		//}

		public int AddTextLine(string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			return this.textsList.Count - 1;
		}

		public void AddTextEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			if (id < short.MinValue || id > short.MaxValue) {
				throw new SEException(LogStr.Error("Nepovolena hodnota ID textoveho vstupu - zadano :" + id + " povoleny rozsah: " + short.MinValue + "-" + short.MaxValue));
			}
			string[] arr = new string[] {
				"textentry", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				widthPix.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				hue.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				id.ToString(System.Globalization.CultureInfo.InvariantCulture),
				textId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
			if (this.entryTextIds == null) {
				this.entryTextIds = new Dictionary<int, int>();
			}
			this.entryTextIds[id] = textId;
		}

		public int AddTextEntry(int x, int y, int widthPix, int height, int hue, int id, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddTextEntry(x, y, widthPix, height, hue, id, textId);
			return textId;
		}

		public void AddNumberEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			if (id < short.MinValue || id > short.MaxValue) {
				throw new SEException(LogStr.Error("Nepovolena hodnota ID ciselneho vstupu - zadano :" + id + " povoleny rozsah: " + short.MinValue + "-" + short.MaxValue));
			}
			string[] arr = new string[] {
				"textentry", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				widthPix.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				height.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				hue.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				id.ToString(System.Globalization.CultureInfo.InvariantCulture),
				textId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
			if (this.numEntryIDs == null) {
				this.numEntryIDs = new List<int>();
			}
			this.numEntryIDs.Add(id);

			if (this.entryTextIds == null) {
				this.entryTextIds = new Dictionary<int, int>();
			}
			this.entryTextIds[id] = textId;
		}

		public int AddNumberEntry(int x, int y, int widthPix, int height, int hue, int id, decimal text) {
			this.CreateTexts();
			string textStr = text.ToString(System.Globalization.CultureInfo.InvariantCulture);
			this.textsList.Add(textStr);
			//textsLengthsSum += textStr.Length;
			int textId = this.textsList.Count - 1;
			this.AddNumberEntry(x, y, widthPix, height, hue, id, textId);
			return textId;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddTilePic(int x, int y, int model) {
			string[] arr = new string[] {
				"tilepic", 
				x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
				y.ToString(System.Globalization.CultureInfo.InvariantCulture),
				model.ToString(System.Globalization.CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y")]
		public void AddTilePicHue(int x, int y, int model, int hue) {
			if (hue == 0) {
				this.AddTilePic(x, y, model);
			} else {
				string[] arr = new string[] {
					"tilepichue", 
					x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					y.ToString(System.Globalization.CultureInfo.InvariantCulture),
					model.ToString(System.Globalization.CultureInfo.InvariantCulture),
					hue.ToString(System.Globalization.CultureInfo.InvariantCulture)
				};
				this.AddElement(arr);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddXmfhtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddXmfhtmlGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, 0);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddXmfhtmlGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			string[] arr;
			if (hue == 0) {
				arr = new string[] {
					"xmfhtmlgump", 
					x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					y.ToString(System.Globalization.CultureInfo.InvariantCulture),
					width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					height.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					textId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					(hasBoundBox? "1": "0"), 
					(isScrollable? "1": "0")
				};
			} else {
				arr = new string[] {
					"xmfhtmlgumpcolor", 
					x.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					y.ToString(System.Globalization.CultureInfo.InvariantCulture),
					width.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					height.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					textId.ToString(System.Globalization.CultureInfo.InvariantCulture), 
					(hasBoundBox? "1": "0"), 
					(isScrollable? "1": "0"),
					hue.ToString(System.Globalization.CultureInfo.InvariantCulture)
				};
			}
			this.AddElement(arr);
		}
	}

	public class ResponseText {
		private readonly int id;
		private readonly string text;

		public ResponseText(int id, string text) {
			this.id = id;
			this.text = text;
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public string Text {
			get {
				return this.text;
			}
		}
	}

	public class ResponseNumber {
		private readonly int id;
		private readonly decimal number;

		public ResponseNumber(int id, decimal number) {
			this.id = id;
			this.number = number;
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public decimal Number {
			get {
				return this.number;
			}
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
//Subcommand 4: "Close Generic GumpDef"
//
//    * BYTE[4] dialogID // which gump to destroy (second ID in 0xB0 packet)
//    * BYTE[4] buttonId // response buttonID for packet 0xB1