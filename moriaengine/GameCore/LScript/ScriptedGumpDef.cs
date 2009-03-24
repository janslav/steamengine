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
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.LScript {

	//this is the class that gets instantiated for every LScript DIALOG/GUMP script
	public sealed class ScriptedGumpDef : GumpDef {
		private LScriptHolder layoutScript;
		private LScriptHolder textsScript;
		private ResponseTrigger[] responseTriggers;

		private ScriptedGumpDef(string name)
			: base(name) {
		}

		internal static ScriptedGumpDef Load(PropsSection input) {
			string[] headers = input.headerName.Split(new char[] { ' ', '\t' }, 2);
			string name = headers[0];//d_something
			GumpDef gump = GumpDef.Get(name);
			ScriptedGumpDef sgd;
			if (gump != null) {
				sgd = gump as ScriptedGumpDef;
				if (sgd == null) {//is not scripted, so can not be overriden
					throw new SEException(LogStr.FileLine(input.filename, input.headerLine) + "GumpDef/Dialog " + LogStr.Ident(name) + " already exists!");
				}
			} else {
				sgd = new ScriptedGumpDef(name);
				DelayedResolver.DelayResolve(new DelayedMethod(sgd.CheckValidity));
			}
			if (headers.Length == 1) {//layout section
				if ((sgd.layoutScript != null) && (!sgd.unloaded)) {//already loaded
					throw new SEException("GumpDef/Dialog " + LogStr.Ident(name) + " already exists!");
				}
				LScriptHolder sc = new LScriptHolder(input.GetTrigger(0));
				if (sc.unloaded) {//in case the compilation failed (syntax error)
					sgd.unloaded = true;
					return null;
				}
				sgd.layoutScript = sc;
				sgd.unloaded = false;
				return sgd;
			} else if (headers.Length == 2) {//buttons or texts section
				string type = headers[1].ToLower();
				switch (type) {
					case "text":
					case "texts":
						if ((sgd.textsScript != null) && (!sgd.unloaded)) {//already loaded
							throw new SEException("TEXT section for GumpDef/Dialog called " + LogStr.Ident(name) + " already exists!");
						}
						TriggerSection trigger = input.GetTrigger(0);
						StringReader stream = new StringReader(trigger.code.ToString());
						StringBuilder modifiedCode = new StringBuilder();
						while (true) {
							string curLine = stream.ReadLine();
							if (curLine != null) {
								curLine = curLine.Trim();
								if ((curLine.Length == 0) || (curLine.StartsWith("//"))) {
									continue;
								}
								curLine = Utility.UnComment(curLine);
								modifiedCode.Append("AddString(\"");
								modifiedCode.Append(curLine);
								modifiedCode.Append("\")");
								modifiedCode.Append(Environment.NewLine);
							} else {
								break;
							}
						}
						trigger.code = modifiedCode;
						LScriptHolder sc = new LScriptHolder(trigger);
						if (sc.unloaded) {//in case the compilation failed (syntax error)
							sgd.unloaded = true;
							return null;
						}
						sgd.textsScript = sc;
						return sgd;
					case "button":
					case "buttons":
					case "triggers":
					case "trigger":
						if ((sgd.responseTriggers != null) && (!sgd.unloaded)) {//already loaded
							throw new SEException("BUTTON section for GumpDef/Dialog called " + LogStr.Ident(name) + " already exists!");
						}
						ArrayList responsesList = new ArrayList();
						for (int i = 1, n = input.TriggerCount; i < n; i++) {//starts from 1 because 0 is the "default" script, which is igored in this section
							trigger = input.GetTrigger(i);
							string triggerName = trigger.triggerName;
							if (String.Compare(triggerName, "anybutton", true) == 0) {
								responsesList.Add(new ResponseTrigger(0, int.MaxValue, trigger));
								continue;
							} else {
								try {
									uint index = TagMath.ParseUInt32(triggerName);
									responsesList.Add(new ResponseTrigger(index, index, trigger));
									continue;
								} catch (Exception) {
									string[] boundStrings = triggerName.Split(' ', '\t', ',');
									if (boundStrings.Length == 2) {
										try {
											uint lowerBound = TagMath.ParseUInt32(boundStrings[0].Trim());
											uint upperBound = TagMath.ParseUInt32(boundStrings[1].Trim());
											responsesList.Add(new ResponseTrigger(lowerBound, upperBound, trigger));
											continue;
										} catch (Exception) { }
									}
								}
							}
							Logger.WriteError("String '" + LogStr.Ident(triggerName) + "' is not valid as gump/dialog response trigger header");
						}
						sgd.responseTriggers = (ResponseTrigger[]) responsesList.ToArray(typeof(ResponseTrigger));
						return sgd;
				}
			}
			throw new SEException("Invalid GumpDef/Dialog header");
		}

		private void CheckValidity(object[] args) {//check method, used as delayed
			if (layoutScript == null) {
				Logger.WriteWarning("Dialog " + LogStr.Ident(Defname) + " missing the main (layout) section?");
				unloaded = true;
				return;
			}
			if (unloaded && (layoutScript != null)) {
				Logger.WriteWarning("Dialog " + LogStr.Ident(Defname) + " resynced incompletely?");
				return;
			}
		}

		public sealed override void Unload() {
			layoutScript = null;
			responseTriggers = null;
			textsScript = null;
		}

		internal sealed override Gump InternalConstruct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ThrowIfUnloaded();
			ScriptedGump instance = new ScriptedGump(this);
			ScriptArgs sa = new ScriptArgs(instance, sendTo); //instance and recipient are stored everytime
			if (args != null) {
				instance.InputArgs = args; //store the Dialog Args to the instance				
			} else {
				instance.InputArgs = new DialogArgs(); //prepare the empty DialogArgs object (no params)
			}
			if (textsScript != null) {
				textsScript.TryRun(focus, sa);
				if (!textsScript.lastRunSuccesful) {
					return null;
				}
			}

			layoutScript.TryRun(focus, sa);

			ScriptedGump returnedInstance = sa.argv[0] as ScriptedGump;
			if (returnedInstance == null) {
				returnedInstance = instance;
			}

			if (layoutScript.lastRunSuccesful) {
				returnedInstance.FinishCompilingPacketData(focus, sendTo);
				return returnedInstance;
			}
			return null;
		}

		internal void OnResponse(ScriptedGump instance, uint pressedButton, uint[] selectedSwitches, ResponseText[] returnedTexts, ResponseNumber[] responseNumbers) {
			if (responseTriggers != null) {
				for (int i = 0, n = responseTriggers.Length; i < n; i++) {
					ResponseTrigger rt = responseTriggers[i];
					if (rt.IsInBounds(pressedButton)) {
						ScriptArgs sa = new ScriptArgs(
							instance,							//0
							instance.Cont,						//1
							pressedButton,						//2
							new ArgTxtHolder(returnedTexts),	//3
							new ArgChkHolder(selectedSwitches),	//4
							new ArgNumHolder(responseNumbers)	//5
						);
						rt.script.TryRun(instance.Focus, sa);
						return;
					}
				}
			}
		}

		private class ResponseTrigger {
			private readonly uint lowerBound;
			private readonly uint upperBound;
			internal readonly LScriptHolder script;
			internal ResponseTrigger(uint lowerBound, uint upperBound, TriggerSection trigger) {
				this.lowerBound = lowerBound;
				this.upperBound = upperBound;
				this.script = new LScriptHolder(trigger);
			}

			internal bool IsInBounds(uint index) {
				return ((lowerBound <= index) && (index <= upperBound));
			}
		}

		public sealed override string ToString() {
			return "ScriptedGumpDef " + Defname;
		}
	}

	public class ArgChkHolder {
		private uint[] selectedSwitches;
		internal ArgChkHolder(uint[] selectedSwitches) {
			this.selectedSwitches = selectedSwitches;
		}

		public int this[int id] {
			get {
				for (int i = 0, n = selectedSwitches.Length; i < n; i++) {
					if (selectedSwitches[i] == id) {
						return 1;
					}
				}
				return 0;
			}
		}
	}

	public class ArgTxtHolder {
		private ResponseText[] responseTexts;
		internal ArgTxtHolder(ResponseText[] responseTexts) {
			this.responseTexts = responseTexts;
		}

		public string this[int id] {
			get {
				for (int i = 0, n = responseTexts.Length; i < n; i++) {
					ResponseText rt = responseTexts[i];
					if (rt.id == id) {
						return rt.text;
					}
				}
				return "";
			}
		}
	}

	public class ArgNumHolder {
		private ResponseNumber[] responseNumbers;
		internal ArgNumHolder(ResponseNumber[] responseNumbers) {
			this.responseNumbers = responseNumbers;
		}

		public double this[int id] {
			get {
				for (int i = 0, n = responseNumbers.Length; i < n; i++) {
					ResponseNumber rn = responseNumbers[i];
					if ((rn != null) && (rn.id == id)) {
						return rn.number;
					}
				}
				return 0;
			}
		}
	}

	public class ScriptedGump : Gump {
		internal protected ScriptedGump(ScriptedGumpDef def)
			: base(def) {
		}

		public override void OnResponse(uint pressedButton, uint[] selectedSwitches, ResponseText[] returnedTexts, ResponseNumber[] responseNumbers) {
			ScriptedGumpDef sdef = (ScriptedGumpDef) def;
			sdef.OnResponse(this, pressedButton, selectedSwitches, returnedTexts, responseNumbers);
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

		public void HTMLGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddHTMLGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}

		//public void HTMLGump(int x, int y, int width, int height, int textId, int hasBoundBox, int isScrollable) {
		//    builder.AddHTMLGump(x, y, width, height, textId, hasBoundBox!=0, isScrollable!=0);
		//}
		//99z+ interface
		public void HTMLGumpA(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.AddHTMLGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}
		//public void HTMLGumpA(int x, int y, int width, int height, string text, int hasBoundBox, int isScrollable) {
		//    builder.AddHTMLGump(x, y, width, height, text, hasBoundBox!=0, isScrollable!=0);
		//}
		//55ir interface
		public void DHTMLGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.AddHTMLGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}
		//public void DHTMLGump(int x, int y, int width, int height, string text, int hasBoundBox, int isScrollable) {
		//    builder.AddHTMLGump(x, y, width, height, text, hasBoundBox!=0, isScrollable!=0);
		//}

		public void XMFHTMLGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddXMFHTMLGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}
		//public void XMFHTMLGump(int x, int y, int width, int height, int textId, int hasBoundBox, int isScrollable) {
		//    builder.AddXMFHTMLGump(x, y, width, height, textId, hasBoundBox!=0, isScrollable!=0);
		//}

		public void XMFHTMLGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			this.AddXMFHTMLGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, hue);
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

		public void Page(int page) {
			this.AddPage(page);
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
		public void NumberEntryA(int x, int y, int widthPix, int height, int hue, int id, double text) {
			this.AddNumberEntry(x, y, widthPix, height, hue, id, text);
		}
		//hypothetical 55ir interface
		public void DNumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, double text) {
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

		internal static bool IsMethodName(string name) {//used in OpNode_Lazy_Expresion for a little hack
			switch (name.ToLower()) {
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