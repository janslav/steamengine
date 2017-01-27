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
using System.Diagnostics.CodeAnalysis;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting.Objects;

namespace SteamEngine
{
	public sealed class CompiledGump : Gump {
		public CompiledGump(CompiledGumpDef def)
			: base(def) {
		}
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public override void OnResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers) {
			CompiledGumpDef gdef = (CompiledGumpDef) this.Def;
			try {
				gdef.OnResponse(this, this.Focus, new GumpResponse(pressedButton, selectedSwitches, responseTexts, responseNumbers), this.InputArgs);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		#region drawing interface
		//RunUO interface
		public void AddAlphaRegion(int x, int y, int width, int height) {
			this.AddCheckerTrans(x, y, width, height);
		}

		//sphere-like interface
		public void CheckerTrans(int x, int y, int width, int height) {
			this.AddCheckerTrans(x, y, width, height);
		}

		public void CheckerTrans() {
			this.AddCheckerTrans();
		}

		public void AddBackground(int x, int y, int width, int height, int gumpId) {
			this.AddResizePic(x, y, gumpId, width, height);
		}

		public void ResizePic(int x, int y, int gumpId, int width, int height) {
			this.AddResizePic(x, y, gumpId, width, height);
		}

		public void AddButton(int x, int y, int normalId, int pressedId, int buttonId, GumpButtonType type, int param) {
			if (type == GumpButtonType.Reply) {
				base.AddButton(x, y, normalId, pressedId, true, param, buttonId);
			} else {
				base.AddButton(x, y, normalId, pressedId, false, param, buttonId);
			}
		}

		public void Button(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
			base.AddButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId);
		}

		public void AddImageTiledButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId, int itemId, int hue, int width, int height) {
			this.AddTiledButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId, itemId, hue, width, height);
		}

		public void ImageTiledButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId, int itemId, int hue, int width, int height) {
			this.AddTiledButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId, itemId, hue, width, height);
		}

		public void Group(int groupId) {
			this.AddGroup(groupId);
		}

		public void AddHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar) {
			this.AddHtmlGump(x, y, width, height, text, background, scrollbar);
		}

		public void HtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddHtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}
		//99z+ interface
		public void HtmlGumpA(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.AddHtmlGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}
		//55ir interface
		public void DhtmlGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.AddHtmlGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}

		public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background, bool scrollbar) {
			this.AddXmfhtmlGump(x, y, width, height, number, background, scrollbar);
		}

		public void AddHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background, bool scrollbar) {
			this.AddXmfhtmlGumpColor(x, y, width, height, number, background, scrollbar, color);
		}

		public void XmfhtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddXmfhtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}

		public void XmfhtmlGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			this.AddXmfhtmlGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, hue);
		}

		public void AddCheck(int x, int y, int inactiveId, int activeId, bool initialState, int switchId) {
			this.AddCheckBox(x, y, inactiveId, activeId, initialState, switchId);
		}

		public void CheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			this.AddCheckBox(x, y, uncheckedGumpId, checkedGumpId, isChecked, id);
		}

		public void AddItem(int x, int y, int itemId) {
			this.AddTilePic(x, y, itemId);
		}

		public void AddItem(int x, int y, int itemId, int hue) {
			this.AddTilePicHue(x, y, itemId, hue);
		}

		public void TilePic(int x, int y, int tileId) {
			this.AddTilePic(x, y, tileId);
		}

		public void TilePicHue(int x, int y, int tileId, int hue) {
			this.AddTilePicHue(x, y, tileId, hue);
		}

		public void AddImage(int x, int y, int gumpId) {
			this.AddGumpPic(x, y, gumpId);
		}

		public void AddImage(int x, int y, int gumpId, int hue) {
			this.AddGumpPic(x, y, gumpId, hue);
		}

		public void AddImageTiled(int x, int y, int width, int height, int gumpId) {
			this.AddGumpPicTiled(x, y, width, height, gumpId);
		}

		public void GumpPicTiled(int x, int y, int width, int height, int gumpId) {
			this.AddGumpPicTiled(x, y, width, height, gumpId);
		}

		public void AddLabel(int x, int y, int hue, string text) {
			this.AddText(x, y, hue, text);
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

		public void AddLabelCropped(int x, int y, int width, int height, int hue, string text) {
			this.AddCroppedText(x, y, width, height, hue, text);
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

		public void TextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, int textId) {
			this.AddTextEntry(x, y, widthPix, widthChars, hue, id, textId);
		}
		//99z+ interface
		public void TextEntryA(int x, int y, int widthPix, int widthChars, int hue, int id, string text) {
			this.AddTextEntry(x, y, widthPix, widthChars, hue, id, text);
		}
		//55ir interface
		public void DTextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, string text) {
			this.AddTextEntry(x, y, widthPix, widthChars, hue, id, text);
		}

		public void NumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, int textId) {
			this.AddNumberEntry(x, y, widthPix, widthChars, hue, id, textId);
		}
		//hypothetical 55ir interface
		public void DNumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, decimal text) {
			this.AddNumberEntry(x, y, widthPix, widthChars, hue, id, text);
		}
		//hypothetical 99z+ interface
		public void NumberEntryA(int x, int y, int widthPix, int widthChars, int hue, int id, decimal text) {
			this.AddNumberEntry(x, y, widthPix, widthChars, hue, id, text);
		}

		//sphere interface
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

		public bool Closable {
			get {
				return this.closable;
			}
			set {
				this.closable = value;
			}
		}

		public bool Disposable {
			get {
				return this.disposable;
			}
			set {
				this.disposable = value;
			}
		}

		public bool Dragable {
			get {
				return this.movable;
			}
			set {
				this.movable = value;
			}
		}
		#endregion drawing interface
	}
}