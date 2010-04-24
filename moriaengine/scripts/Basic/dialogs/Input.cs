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
using SteamEngine;
using SteamEngine.Common;
using System.Collections;
using System;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Summary("Abstract class designed for implementing by scripted and compiled version of a input dialog def")]
	public abstract class AbstractInputDef : CompiledGumpDef {
		protected static readonly TagKey inputParamsTK = TagKey.Acquire("_input_params_");

		public AbstractInputDef()
			: base() {
		}

		public AbstractInputDef(string defname)
			: base(defname) {
		}

		[Summary("Static 'factory' method for getting the instance of an existing input def.")]
		public static new AbstractInputDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbstractInputDef;
		}

		[Summary("Label of the input dialog")]
		public abstract string Label {
			get;
		}

		[Summary("Pre-inserted default input value")]
		public abstract string DefaultInput {
			get;
		}

		[Summary("Method called when clicked on the OK button in the dialog, sending the filled text")]
		public abstract void Response(Character sentTo, TagHolder focus, string filledText);

		[Summary("Construct method creates the dialog itself")]
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//there should be a input-text in the args params array

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			//create the background GUTAMatrix and set its size       
			dialogHandler.CreateBackground(400);
			dialogHandler.SetLocation(350, 350);

			//first row - the label of the dialog
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline(this.Label).Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dialogHandler.MakeLastTableTransparent();

			//second row - the basic, whole row, input field
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAInput.Builder.Id(1).Text(this.DefaultInput).Build();
			dialogHandler.MakeLastTableTransparent();

			//last row with buttons
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();
		}

		[Summary("Button pressed - exit the dialog or pass the calling onto the underlaying inputDef")]
		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			switch (gr.PressedButton) {
				case 0: //exit or rightclick
					//znovuzavolat pripadny predchozi dialog
					DialogStacking.ShowPreviousDialog(gi);
					break;
				case 1: //OK
					//pass the call with the input value
					string inputVal = gr.GetTextResponse(1);
					this.Response((Character) gi.Cont, (TagHolder) gi.Focus, inputVal);
					//a zavolat predchozi dialog
					DialogStacking.ShowPreviousDialog(gi);
					break;
			}
		}
	}

	[Summary("Class for displaying the input dialogs from LSCript using the [inputdef foo] section")]
	public sealed class ScriptedInputDialogDef : AbstractInputDef {
		private string label;
		private string defaultInput;
		private LScriptHolder on_response;

		public ScriptedInputDialogDef(string defname)
			: base(defname) {
		}

		//public override void Unload() {
		//    //we need to override this method since ScriptedInputDialogDef inherits from the CompiledGumpDef which does not unload iself... why? dunno. Let's see :)
		//    IsUnloaded = true;
		//}

		internal static IUnloadable Load(PropsSection input) {
			string typeName = input.HeaderType.ToLowerInvariant();
			string defname = input.HeaderName.ToLowerInvariant();

			AbstractScript def = AbstractScript.GetByDefname(defname);

			ScriptedInputDialogDef id = def as ScriptedInputDialogDef;
			if (id == null) {
				if (def != null) {//it isnt ScriptedInputDialogDef
					throw new OverrideNotAllowedException("ScriptedInputDialogDef " + LogStr.Ident(defname) + " has the same name as " + LogStr.Ident(def) + ". Ignoring.");
				} else {
					id = new ScriptedInputDialogDef(defname);
				}
			} else if (id.IsUnloaded) {
				id.UnUnload();
			} else {
				throw new OverrideNotAllowedException("ScriptedInputDialogDef " + LogStr.Ident(defname) + " defined multiple times.");
			}
			//Get the main trigger that gets perfomed when the input dialog is sent
			TriggerSection trigger_response = input.PopTrigger("response");
			if (trigger_response != null) {
				id.on_response = new LScriptHolder(trigger_response);
			} else {
				Logger.WriteWarning(input.Filename, input.HeaderLine, "InputDialogDef " + LogStr.Ident(input) + " has not a trigger submit defined.");
			}

			//cteni dvou radku ze scriptu - nadpis dialogu a defaultni hodnota v inputu
			PropsLine pl = input.TryPopPropsLine("label");
			if (pl == null) {
				throw new SEException(input.Filename, input.HeaderLine, "input dialog label is missing");
			}

			id.label = ConvertTools.LoadSimpleQuotedString(pl.Value);

			pl = input.TryPopPropsLine("default");
			if (pl == null) {
				throw new SEException(input.Filename, input.HeaderLine, "input dialog default value is missing");
			}

			id.defaultInput = ConvertTools.LoadSimpleQuotedString(pl.Value);

			return id;
		}

		[Summary("Action called when the input dialog is confirmed. The parameters of the call will be" +
				"1st - the inputted text, followed by the params the input dialog was called with")]
		public override void Response(Character sentTo, TagHolder focus, string filledText) {
			//prepend the input text to previous input parameters
			object[] oldParams = GumpInstance.InputArgs.GetArgsArray();
			object[] newPars = new object[oldParams.Length + 1]; //create a new bigger array, we need to add a new 0th value...
			Array.Copy(oldParams, 0, newPars, 1, oldParams.Length); //copy all old values to the new field beginning with the index 1
			newPars[0] = filledText; //filled text will be 0th                        
			on_response.Run(focus, newPars); //pass the filled text value
		}

		//[Summary("Unregister the input dialog def from the other defs")]
		//private static void UnRegisterInputDialogDef(ScriptedInputDialogDef id) {
		//    AllScriptsByDefname.Remove(id.Defname);
		//}

		//[Summary("Register the input dialog def among the other defs")]
		//private static void RegisterInputDialogDef(ScriptedInputDialogDef id) {
		//    AllScriptsByDefname[id.Defname] = id;
		//}

		public static new void Bootstrap() {
			ScriptLoader.RegisterScriptType(new string[] { "InputDef" },
				Load, false);
		}

		public override string Label {
			get {
				return label;
			}
		}

		public override string DefaultInput {
			get {
				return defaultInput;
			}
		}
	}

	[Summary("Class for using input dialogs implemented in C#")]
	public abstract class CompiledInputDef : AbstractInputDef {

	}
}