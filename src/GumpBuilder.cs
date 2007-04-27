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

		//TODO
		//textentrylimited	x,y,widthpix,widthchars,color,id,limit,startstringindex

namespace SteamEngine {
	//it could also be public... but it doesnt need to be. so it isnt :P we try to keep the API minimalistic -tar
    internal class GumpBuilder {
		internal bool movable = true;
		internal bool closable = true;
		internal bool disposable = true;
		internal uint xPos = 0;
		internal uint yPos = 0;
		private List<string[]> elements = new List<string[]>();
		private ArrayList textsList;
		private List<int> numEntryIDs;
		private Dictionary<int, int> entryTextIds;//textentryid - textid pairs
		
		//private static ArrayList voidList = ArrayList.ReadOnly(new ArrayList(0));
		
		//this is the final method where all the elements are compiled into the string
		internal string GetLayoutString() {
			StringBuilder sb = new StringBuilder();
			if (!movable) {
				sb.Append("{nomove}");
			}
			if (!closable) {
				sb.Append("{noclose}");
			}
			if (!disposable) {//what does it really mean? :)
				sb.Append("{nodispose}");
			}
			for (int elIndex = 0, elementsCount = elements.Count; elIndex<elementsCount; elIndex++) {
				string[] element = elements[elIndex];
				sb.Append("{");
				int n = (element.Length-1);//after the last one there is no space
				for (int i = 0; i<n; i++) {
					sb.Append(element[i]);
					sb.Append(" ");
				}
				sb.Append(element[n]);
				sb.Append("}");
			}
			return sb.ToString();
		}
		
		internal void SetPropertiesOn(GumpInstance instance) {
			//Logger.WriteDebug("data of "+instance);
			instance.layout = GetLayoutString();
			//Logger.WriteDebug(instance.layout);
			int textsLengthSum = 0;
			string[] texts;
			if (textsList != null) {
				int textsListCount = textsList.Count;
				texts = new string[textsListCount];
				for (int i = 0; i<textsListCount; i++) {
					string text = (string) textsList[i];
					textsLengthSum += text.Length;
					texts[i] = text;
					//Logger.WriteDebug("text "+i+": "+text);
				}
			} else {
				texts = new string[0];
			}
			instance.textsLengthsSum = textsLengthSum;
			instance.texts = texts;
			instance.x = xPos;
			instance.y = yPos;
			instance.numEntryIDs = numEntryIDs;
			instance.entryTextIds = entryTextIds;
		}
		
		private void CreateTexts() {
			if (textsList == null) {
				textsList = new ArrayList();
			}
		}
		
		internal void AddButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
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
			elements.Add(arr);
		}
		
		internal void AddCheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = new string[] {
				"checkbox", 
				x.ToString(), 
				y.ToString(),
				uncheckedGumpId.ToString(), 
				checkedGumpId.ToString(), 
				(isChecked?"1": "0"), 
				id.ToString(),
			};
			elements.Add(arr);
		}
		
		internal void AddRadio(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = new string[] {
				"radio", 
				x.ToString(), 
				y.ToString(),
				uncheckedGumpId.ToString(), 
				checkedGumpId.ToString(), 
				(isChecked?"1": "0"), 
				id.ToString()
			};
			elements.Add(arr);
		}
		
		internal void AddCheckerTrans(int x, int y, int width, int height) {
			string[] arr = new string[] {
				"checkertrans", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString()
			};
			elements.Add(arr);
		}

		//is it possible without args? sphere tables say it is...
		internal void AddCheckerTrans() {
			string[] arr = new string[] {"checkertrans"};
			elements.Add(arr);
		}
		
		internal void AddText(int x, int y, int hue, int textId) {
			string[] arr = new string[] {
				"text", 
				x.ToString(), 
				y.ToString(),
				hue.ToString(), 
				textId.ToString()
			};
			elements.Add(arr);
		}
		
		internal int AddText(int x, int y, int hue, string text) {
			Sanity.IfTrueThrow(text==null, "The text string can't be null");
			CreateTexts();
			int textId = textsList.Add(text);
			AddText(x, y, hue, textId);
			return textId;
		}
		
		internal void AddCroppedText(int x, int y, int width, int height, int hue, int textId) {
			string[] arr = new string[] {
				"croppedtext", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString(), 
				hue.ToString(), 
				textId.ToString()
			};
			elements.Add(arr);
		}
		
		internal int AddCroppedText(int x, int y, int width, int height, int hue, string text) {
			Sanity.IfTrueThrow(text==null, "The text string can't be null");
			CreateTexts();
			int textId = textsList.Add(text);
			AddCroppedText(x, y, width, height, hue, textId);
			return textId;
		}
		
		internal void AddGroup(int groupId) {
			string[] arr = new string[] {
				"group", 
				groupId.ToString(), 
			};
			elements.Add(arr);
		}
		
		internal void AddGumpPic(int x, int y, int gumpId) {
			string[] arr = new string[] {
				"gumppic", 
				x.ToString(), 
				y.ToString(),
				gumpId.ToString()
			};
			elements.Add(arr);
		}

		internal void AddGumpPic(int x, int y, int gumpId, int hue) {
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
				elements.Add(arr);
			}
		}
		
		internal void AddGumpPicTiled(int x, int y, int width, int height, int gumpId) {
			string[] arr = new string[] {
				"gumppictiled", 
				x.ToString(), 
				y.ToString(),
				width.ToString(), 
				height.ToString(),
				gumpId.ToString(), 
			};
			elements.Add(arr);
		}

		internal void AddHTMLGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
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
			elements.Add(arr);
		}
		
		internal int AddHTMLGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			Sanity.IfTrueThrow(text==null, "The text string can't be null");
			CreateTexts();
			int textId = textsList.Add(text);
			AddHTMLGump(x, y, width, height, textId, hasBoundBox, isScrollable);
			return textId;
		}
		
		internal void AddPage(int pageId) {
			string[] arr = new string[] {
				"page",
				pageId.ToString(), 
			};
			elements.Add(arr);
		}

		internal void AddResizePic(int x, int y, int gumpId, int width, int height) {
			string[] arr = new string[] {
				"resizepic", 
				x.ToString(), 
				y.ToString(),
				gumpId.ToString(), 
				width.ToString(), 
				height.ToString(),
			};
			elements.Add(arr);
		}
		
		//internal int AddString(string text) {
		//	Sanity.IfTrueThrow(text==null, "The text string can't be null");
		//	CreateTexts();
		//	return textsList.Add(text);
		//}
		
		internal int AddTextLine(string text) {
			Sanity.IfTrueThrow(text==null, "The text string can't be null");
			CreateTexts();
			return textsList.Add(text);
		}
		
		internal void AddTextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, int textId) {
			string[] arr = new string[] {
				"textentry", 
				x.ToString(), 
				y.ToString(),
				widthPix.ToString(), 
				widthChars.ToString(), 
				hue.ToString(), 
				id.ToString(),
				textId.ToString()
			};
			elements.Add(arr);
			if (entryTextIds == null) {
				entryTextIds = new Dictionary<int, int>();
			}
			entryTextIds[id] = textId;
		}
		
		internal int AddTextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, string text) {
			Sanity.IfTrueThrow(text==null, "The text string can't be null");
			CreateTexts();
			int textId = textsList.Add(text);
			AddTextEntry(x, y, widthPix, widthChars, hue, id, textId);
			return textId;
		}

		internal void AddNumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, int textId) {
			string[] arr = new string[] {
				"textentry", 
				x.ToString(), 
				y.ToString(),
				widthPix.ToString(), 
				widthChars.ToString(), 
				hue.ToString(), 
				id.ToString(),
				textId.ToString()
			};
			elements.Add(arr);
			if (numEntryIDs == null) {
				numEntryIDs = new List<int>();
			}
			numEntryIDs.Add(id);

			if (entryTextIds == null) {
				entryTextIds = new Dictionary<int, int>();
			}
			entryTextIds[id] = textId;
		}

		internal int AddNumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, double text) {
			CreateTexts();
			int textId = textsList.Add(text.ToString());
			AddNumberEntry(x, y, widthPix, widthChars, hue, id, textId);
			return textId;
		}

		internal void AddTilePic(int x, int y, int model) {
			string[] arr = new string[] {
				"tilepic", 
				x.ToString(), 
				y.ToString(),
				model.ToString()
			};
			elements.Add(arr);
		}
		
		internal void AddTilePicHue(int x, int y, int model, int hue) {
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
				elements.Add(arr);
			}
		}
		
		internal void AddXMFHTMLGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			AddXMFHTMLGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, 0);
		}
		
		internal void AddXMFHTMLGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
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
			elements.Add(arr);
		}
	}
}
