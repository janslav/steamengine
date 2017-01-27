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

using System.Diagnostics.CodeAnalysis;
using SteamEngine.Scripting.Objects;

namespace SteamEngine
{
	public class InterpretedGump : Gump {
		protected internal InterpretedGump(InterpretedGumpDef def)
			: base(def) {
		}

		public override void OnResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers) {
			InterpretedGumpDef sdef = (InterpretedGumpDef) this.Def;
			sdef.OnResponse(this, pressedButton, selectedSwitches, responseTexts, responseNumbers);
		}

		public void CheckerTrans(int x, int y, int width, int height) {
			this.AddCheckerTrans(x, y, width, height);
		}

		public void CheckerTrans() {
			this.AddCheckerTrans();
		}

		public void ResizePic(int x, int y, int gumpId, int width, int height) {
			this.AddResizePic(x, y, gumpId, width, height);
		}

		public void Button(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
			this.AddButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId);
		}

		public void Group(int groupId) {
			this.AddGroup(groupId);
		}

		public void HtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddHtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}

		//public void HTMLGump(int x, int y, int width, int height, int textId, int hasBoundBox, int isScrollable) {
		//    builder.AddHTMLGump(x, y, width, height, textId, hasBoundBox!=0, isScrollable!=0);
		//}
		//99z+ interface
		public void HtmlGumpA(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.AddHtmlGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}
		//public void HTMLGumpA(int x, int y, int width, int height, string text, int hasBoundBox, int isScrollable) {
		//    builder.AddHTMLGump(x, y, width, height, text, hasBoundBox!=0, isScrollable!=0);
		//}
		//55ir interface
		public void DhtmlGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.AddHtmlGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}
		//public void DHTMLGump(int x, int y, int width, int height, string text, int hasBoundBox, int isScrollable) {
		//    builder.AddHTMLGump(x, y, width, height, text, hasBoundBox!=0, isScrollable!=0);
		//}

		public void XmfhtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddXmfhtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}
		//public void XMFHTMLGump(int x, int y, int width, int height, int textId, int hasBoundBox, int isScrollable) {
		//    builder.AddXMFHTMLGump(x, y, width, height, textId, hasBoundBox!=0, isScrollable!=0);
		//}

		public void XmfhtmlGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			this.AddXmfhtmlGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, hue);
		}
		//public void XMFHTMLGumpColor(int x, int y, int width, int height, int textId, int hasBoundBox, int isScrollable, int hue) {
		//    builder.AddXMFHTMLGumpColor(x, y, width, height, textId, hasBoundBox!=0, isScrollable!=0, hue);
		//}

		public void CheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			this.AddCheckBox(x, y, uncheckedGumpId, checkedGumpId, isChecked, id);
		}
		//public void CheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, int isChecked, int id) {
		//    builder.AddCheckBox(x, y, uncheckedGumpId, checkedGumpId, isChecked!=0, id);
		//}

		public void TilePic(int x, int y, int tileId) {
			this.AddTilePic(x, y, tileId);
		}

		public void TilePicHue(int x, int y, int tileId, int hue) {
			this.AddTilePicHue(x, y, tileId, hue);
		}

		public void GumpPicTiled(int x, int y, int width, int height, int gumpId) {
			this.AddGumpPicTiled(x, y, width, height, gumpId);
		}

		public void Text(int x, int y, int hue, int textId) {
			this.AddText(x, y, hue, textId);
		}
		//99z+ interface
		public void TextA(int x, int y, int hue, string text) {
			this.AddText(x, y, hue, text);
		}
		//55ir+ interface. why can the idiots not unite? :P
		public void DText(int x, int y, int hue, string text) {
			this.AddText(x, y, hue, text);
		}

		public void CroppedText(int x, int y, int width, int height, int hue, int textId) {
			this.AddCroppedText(x, y, width, height, hue, textId);
		}

		public void CroppedTextA(int x, int y, int width, int height, int hue, string textId) {
			this.AddCroppedText(x, y, width, height, hue, textId);
		}

		public void DCroppedText(int x, int y, int width, int height, int hue, string textId) {
			this.AddCroppedText(x, y, width, height, hue, textId);
		}

		public void Page(int pageId) {
			this.AddPage(pageId);
		}

		public void Radio(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			this.AddRadio(x, y, uncheckedGumpId, checkedGumpId, isChecked, id);
		}
		public void Radio(int x, int y, int uncheckedGumpId, int checkedGumpId, int isChecked, int id) {
			this.AddRadio(x, y, uncheckedGumpId, checkedGumpId, isChecked != 0, id);
		}

		public void TextEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			this.AddTextEntry(x, y, widthPix, height, hue, id, textId);
		}
		//99z+ interface
		public void TextEntryA(int x, int y, int widthPix, int height, int hue, int id, string text) {
			this.AddTextEntry(x, y, widthPix, height, hue, id, text);
		}
		//55ir interface
		public void DTextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, string text) {
			this.AddTextEntry(x, y, widthPix, widthChars, hue, id, text);
		}

		public void NumberEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			this.AddNumberEntry(x, y, widthPix, height, hue, id, textId);
		}
		//hypothetical 99z+ interface
		public void NumberEntryA(int x, int y, int widthPix, int height, int hue, int id, decimal text) {
			this.AddNumberEntry(x, y, widthPix, height, hue, id, text);
		}
		//hypothetical 55ir interface
		public void DNumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, decimal text) {
			this.AddNumberEntry(x, y, widthPix, widthChars, hue, id, text);
		}

		public int AddString(string text) {
			return this.AddTextLine(text);
		}

		public void SetLocation(int x, int y) {
			this.X = x;
			this.Y = y;
		}

		public void NoClose() {
			this.closable = false;
		}

		public void NoMove() {
			this.movable = false;
		}

		public void NoDispose() {
			this.disposable = false;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal static bool IsMethodName(string name) {//used in OpNode_Lazy_Expresion for a little hack
			switch (name.ToLowerInvariant()) {
				case "checkertrans":
				case "resizepic":
				case "button":
				case "group":
				case "htmlgump":
				case "htmlgumpa":
				case "dhtmlgump":
				case "xmfhtmlgump":
				case "xmfhtmlgumpcolor":
				case "checkbox":
				case "tilepic":
				case "tilepichue":
				case "gumppictiled":
				case "text":
				case "texta":
				case "dtext":
				case "croppedtext":
				case "croppedtexta":
				case "dcroppedtext":
				case "page":
				case "radio":
				case "textentry":
				case "textentrya":
				case "dtextentry":
				case "numberentry":
				case "numberentrya":
				case "dnumberentry":
				case "setlocation":
				case "noclose":
				case "nomove":
				case "nodispose":
				case "addstring":
					return true;
			}
			return false;
		}
	}
}