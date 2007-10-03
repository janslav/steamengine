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

    [Remark("Abstract class designed for implementing by scripted and compiled version of a input dialog def")]
    public abstract class AbstractInputDef : CompiledGump {
        public AbstractInputDef()
            : base() {
        }

        public AbstractInputDef(string defname)
            : base(defname) {
        }

        [Remark("Static 'factory' method for getting the instance of an existing input def.")]
        public static new AbstractInputDef Get(string defname) {
            AbstractScript script;
            byDefname.TryGetValue(defname, out script);

            return script as AbstractInputDef;
        }

        [Remark("Label of the input dialog")]
        public abstract string Label {
            get;
        }

        [Remark("Pre-inserted default input value")]
        public abstract string DefaultInput {
            get;
        }       

        [Remark("Method called when clicked on the OK button in the dialog, sending the filled text")]
        public abstract void Response(Character sentTo, TagHolder focus, string filledText);

        [Remark("Construct method creates the dialog itself")]
        public override void Construct(Thing focus, AbstractCharacter sendTo, object[] sa) {
            //store all input parameters on the dialog instance
            if(sa == null) { //no params specified, prepare the empty params field 
                //there will be at least one parameter (the inputted text)
                this.GumpInstance.SetTag(TagKey.Get("input_params"), new object[1]);
            } else {
                this.GumpInstance.SetTag(TagKey.Get("input_params"), sa);
            }

            ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

            //create the background GUTAMatrix and set its size       
            dialogHandler.CreateBackground(400);
            dialogHandler.SetLocation(350, 350);

            //first row - the label of the dialog
			dialogHandler.Add(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));			        
            dialogHandler.LastTable[0, 0] = TextFactory.CreateHeadline(this.Label);
			dialogHandler.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0); 
			dialogHandler.MakeTableTransparent();

            //second row - the basic, whole row, input field
            dialogHandler.Add(new GUTATable(1,0));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;            
            dialogHandler.LastTable[0,0] = InputFactory.CreateInput(LeafComponentTypes.InputText, 1, this.DefaultInput);
			dialogHandler.MakeTableTransparent();

            //last row with buttons
			dialogHandler.Add(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 0));
            dialogHandler.LastTable[0,0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonOK, 1);                       

            dialogHandler.WriteOut();
        }

        [Remark("Button pressed - exit the dialog or pass the calling onto the underlaying inputDef")]
		public override void OnResponse(GumpInstance gi, GumpResponse gr, object[] args) {
            switch(gr.pressedButton) {
                case 0: //exit or rightclick
					//znovuzavolat pripadny predchozi dialog
					DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
                    break;
                case 1: //OK
                    //pass the call with the input value
                    string inputVal = gr.GetTextResponse(1);
                    this.Response((Character)gi.Cont, (TagHolder)gi.Focus, inputVal);
					//a zavolat predchozi dialog
					DialogStackItem.ShowPreviousDialog(gi.Cont.Conn);
                    break;
            }
        }
    }

    [Remark("Class for displaying the input dialogs from LSCript using the [inputdef foo] section")]
    public sealed class ScriptedInputDialogDef : AbstractInputDef {
        private string label;
        private string defaultInput;
        private LScriptHolder on_response;

        public ScriptedInputDialogDef(string defname)
            : base(defname) {
        }

        internal static IUnloadable Load(PropsSection input) {
            string typeName = input.headerType.ToLower();
            string defname = input.headerName.ToLower();

            AbstractScript def;

            byDefname.TryGetValue(defname, out def);
            ScriptedInputDialogDef id = def as ScriptedInputDialogDef;
            if(id == null) {
                if(def != null) {//it isnt ScriptedInputDialogDef
                    throw new OverrideNotAllowedException("ScriptedInputDialogDef " + LogStr.Ident(defname) + " has the same name as " + LogStr.Ident(def) + ". Ignoring.");
                } else {
                    id = new ScriptedInputDialogDef(defname);
                }
            } else if(id.unloaded) {
                id.unloaded = false;
                UnRegisterInputDialogDef(id);//will be re-registered again
            } else {
                throw new OverrideNotAllowedException("ScriptedInputDialogDef " + LogStr.Ident(defname) + " defined multiple times.");
            }
            //Get the main trigger that gets perfomed when the input dialog is sent
            TriggerSection trigger_response = input.PopTrigger("response");
            if(trigger_response != null) {
                id.on_response = new LScriptHolder(trigger_response);
            } else {
                Logger.WriteWarning(input.filename, input.headerLine, "InputDialogDef " + LogStr.Ident(input) + " has not a trigger submit defined.");
            }
            RegisterInputDialogDef(id);

            //cteni dvou radku ze scriptu - nadpis dialogu a defaultni hodnota v inputu
            PropsLine pl = input.TryPopPropsLine("label");
            if(pl == null) {
                throw new SEException(input.filename, input.headerLine, "input dialog label is missing");
            }
            System.Text.RegularExpressions.Match m = ConvertTools.stringRE.Match(pl.value);
            if(m.Success) {
                id.label = m.Groups["value"].Value;
            } else {
                id.label = pl.value;
            }

            pl = input.TryPopPropsLine("default");
            if(pl == null) {
                throw new SEException(input.filename, input.headerLine, "input dialog default value is missing");
            }
            m = ConvertTools.stringRE.Match(pl.value);
            if(m.Success) {
                id.defaultInput = m.Groups["value"].Value;
            } else {
                id.defaultInput = pl.value;
            }

            return id;
        }

        [Remark("Action called when the input dialog is confirmed. The parameters of the call will be" +
                "1st - the inputted text, followed by the params the input dialog was called with")]
        public override void Response(Character sentTo, TagHolder focus, string filledText) {
            //prepend the input text to previous input parameters
            object[] pars = (object[])this.GumpInstance.GetTag(TagKey.Get("input_params"));
            object[] newPars = new object[pars.Length+1]; //create a new bigger array, we need to add a new 0th value...
            Array.Copy(pars, 0, newPars, 1, pars.Length); //copy all old values to the new field beginning with the index 1
            newPars[0] = filledText; //filled text will be 0th                        
            on_response.Run(focus, newPars); //pass the filled text value
        }


        [Remark("Unregister the input dialog def from the other defs")]
        private static void UnRegisterInputDialogDef(ScriptedInputDialogDef id) {
            byDefname.Remove(id.Defname);
        }

        [Remark("Register the input dialog def among the other defs")]
        private static void RegisterInputDialogDef(ScriptedInputDialogDef id) {
            byDefname[id.Defname] = id;
        }

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

    [Remark("Class for using input dialogs implemented in C#")]
    public abstract class CompiledInputDef : AbstractInputDef {

    }
}