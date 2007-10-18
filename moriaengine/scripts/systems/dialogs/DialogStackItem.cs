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
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.LScript;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;


namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Class encapsulating stacking an recalling dialogs")]
	public class DialogStackItem {
		[Remark("Tag used for stacking information about clients dialogs to be called back")]
		private static readonly TagKey _dialogStackTag_ = TagKey.Get("x_guta_dialogStackTree");

		[Remark("Main object contining all necessary info")]
		GumpInstance gumpInstance;
		
		//set of variables used in case we dont have the GumpInstance (LScript, e.g)
		[Remark("Instance of the dialog to be recalled")]
		Gump gump;
		[Remark("Parameters of the dialog as used")]
		object[] args;		
		[Remark("Cont as was on the dialog instance - where is the dialog set")]
		AbstractCharacter cont;
		[Remark("Focus from the original dialog")]
		Thing focus;
		[Remark("Conn holding info about who will be shown the dialog in the end")]
		GameConn connection;
		

		[Remark("Make the args of the stored dialog available for possible editing")]
		internal object[] Args {
			get {
				if (gumpInstance != null) {
					return gumpInstance.InputParams;
				} else {
					return args;
				} 
			}
			set {
				if (gumpInstance != null) {
					gumpInstance.InputParams = value;
				} else {
					args = value;
				}				
			}
		}

		[Remark("Cont - who sees the dialog")]		
		internal AbstractCharacter Cont {
			get {
				if (gumpInstance != null) {
					return gumpInstance.Cont;
				} else {
					return cont;
				}				
			}
		}

		internal Thing Focus {
			get {
				if (gumpInstance != null) {
					return gumpInstance.Focus;
				} else {
					return focus;
				}
			}
		}

		internal GameConn Conn {
			get {
				if (gumpInstance != null) {
					return gumpInstance.Cont.Conn;
				} else {
					return connection;
				}
			}
		}

		[Remark("The dialog to be recalled")]
		public Gump Gump {
			get {
				if (gumpInstance != null) {
					return gumpInstance.def;
				} else {
					return gump;
				}				
			}
		}

		[Remark("Returns a type of this dialog")]
		internal Type GumpType {
			get {
				return gumpInstance.def.GetType();
			}
		}

		internal DialogStackItem(GumpInstance gi) {
			this.gumpInstance = gi;			
		}

		[Remark("This constructor is used e.g. when calling dialog stack from the LScripted dialogs or simply in "+
				"the case we dont have the GumpInstance")]
		internal DialogStackItem(GameConn connection, AbstractCharacter cont, Thing focus, Gump gump, object[] args) {
			this.gump = gump;
			this.args = args;
			this.connection = connection;
			this.cont = cont;
			this.focus = focus;
		}

		[Remark("Gets the stored information and displays the dialog."+
				"It displays the dialog only in case it is not yet opened and visible - this can"+
				"occur e.g. when opening another dialog having one opened before and then closing the"+
				"least opened one - it will look into the stack, find the previous dialog (the first opened one)"+
				"and tries to show it which would lead to having two same dialogs opened and problems"+
				"when closing one of them (displaying other previous dialogs unwantedly etc)"+
				"neresti otevrenost ostatnich ialogu, o to se musi postarat volatel")]
		public void Show() {
			//send the dialog with stored values
			//only in case it is not opened 
			//if(!Cont.HasOpenedDialog(Gump)) {
			ResendAndRestackDialog(gumpInstance);
			//Cont.ReSendGump(gumpInstance);
			//} else {
				//nothing will be shown, the dialog is still opened, return this DSI to the stack
			//	DialogStackItem.EnstackDialog(Cont, this);
			//}
		}

		[Remark("LSCript used method for setting the specified argument's value")]
		public void SetArgValue(int index, object value) {
			Args[index] = value;
		}

		[Remark("LSCript used method for getting the specified argument's value")]
		public object GetArgValue(int index) {
			return Args[index];
		}

		[Remark("Store the info about the dialog to the dialog stack. Used for " +
			"storing info about the last used dialog whjich can be used to reopen the dialog " +
			"when exiting from the following dialog for instance." +
			"e.g. info dialog -> clicked on the 'account info' button which closes the info dialog," +
			"after finishing with the accout info dialog we would like to have the info dialog reopened!"+
			"UPDATED - using GumpInstance as a parameter only (it contains everything necessary)"+
			"UPDATED2 - oldGi - dialog being enstacked; newGi - newly opened dialog for storing stacking info")]
		public static void EnstackDialog(GumpInstance oldGi, GumpInstance newGi) {
			Dictionary<uint, Stack<DialogStackItem>> dialogsMultiStack = oldGi.Cont.Conn.GetTag(_dialogStackTag_) as Dictionary<uint, Stack<DialogStackItem>>;
			if(dialogsMultiStack == null) { //zatim nemame ani stack
				dialogsMultiStack = new Dictionary<uint, Stack<DialogStackItem>>();
				oldGi.Cont.Conn.SetTag(_dialogStackTag_, dialogsMultiStack);
			}
			Stack<DialogStackItem> actualStack;
			if(dialogsMultiStack.ContainsKey(oldGi.uid)) {
				actualStack = dialogsMultiStack[oldGi.uid]; //dotahnout
			} else {
				actualStack = new Stack<DialogStackItem>(); //vytvortit a ulozit
				dialogsMultiStack[oldGi.uid] = actualStack;
			}
			actualStack.Push(new DialogStackItem(oldGi)); //stackneme si starej dialog
			dialogsMultiStack[newGi.uid] = actualStack; //dat k dispozici novemu dialogu pro pozdejsi navraty
		}

		//[Remark("Overloaded init method - used when the gumpinstance is not fully initialized yet")]
		///TODO - zjistit jak v LScriptu fungujou dialogy a prepsat tuto metodu
		//public static void EnstackDialog(AbstractCharacter cont,  Thing focus, 
		//                                    Gump instance, params object[] args) {
		//    Dictionary<int, Stack<DialogStackItem>> dialogsMultiStack = cont.Conn.GetTag(_dialogStackTag_) as Dictionary<int, Stack<DialogStackItem>>;
		//    if(dialogsMultiStack == null) { //not yet set
		//        dialogsMultiStack = new Dictionary<int, Stack<DialogStackItem>>();
		//        cont.Conn.SetTag(_dialogStackTag_, dialogsMultiStack);
		//    }
		//    Stack<DialogStackItem> actualStack;
		//    if(dialogsMultiStack.ContainsKey(oldGi.uid)) {
		//        actualStack = dialogsMultiStack[oldGi.uid]; //dotahnout
		//    } else {
		//        actualStack = new Stack<DialogStackItem>(); //vytvortit a ulozit
		//        dialogsMultiStack[oldGi.uid] = actualStack;
		//    }
		//    actualStack.Push(new DialogStackItem(cont.Conn, cont, focus, instance, args));
		//    dialogsMultiStack[newGi.uid] = actualStack; //dat k dispozici novemu dialogu pro pozdejsi navraty		
		//}

		[Remark("Recall the last stored dialog from the dialog stack." +
				"Find it according to the given actual GumpInstance in the dialogs multistack."+
				"Handles also emptying and clearing unused (empty) stacks and the multistack too.")]
		public static DialogStackItem PopStackedDialog(GumpInstance actualGi) {
			Dictionary<uint, Stack<DialogStackItem>> dialogsMultiStack = actualGi.Cont.Conn.GetTag(_dialogStackTag_) as Dictionary<uint, Stack<DialogStackItem>>;
			DialogStackItem dsi = null;
			if(dialogsMultiStack != null) { //something was stored
				if(dialogsMultiStack.ContainsKey(actualGi.uid)) {
					Stack<DialogStackItem> actualsOwnStack = dialogsMultiStack[actualGi.uid];
					if(actualsOwnStack.Count != 0) {
						dsi = actualsOwnStack.Pop(); //popovat budem jen pokud ve stacku vubec neco je
						dialogsMultiStack.Remove(actualGi.uid); //po popnuti uz muzeme vyhodit soucasny dialog z multistacku (uz ho stejne zaviram...)					
					}

					if(actualsOwnStack.Count == 0) {//stack je uz prazdnej (nebo byl prazdnej i predtim), odstranit stack
						dialogsMultiStack.Remove(actualGi.uid);
						if(dialogsMultiStack.Count == 0) {
							actualGi.Cont.Conn.SetTag(_dialogStackTag_, null); //multistack je prazdnej, uvolnit
							actualGi.Cont.Conn.RemoveTag(_dialogStackTag_);
						}
					}
				}
			}
			return dsi;
		}

		[Remark("When we need to resend the actual dialog (e.g. only with changed parameters such as going to another page or "+
				"refrehsing some list where we need the actual data to be refreshed, we need the complete initialization phase "+
				"to be performed - we will resend the dialog in a standard way but we have to make sure the stacked information "+
				"remains valid => we will store the new (resent) gumpinstance with the same stacking info as the old one "+
				"This is necesasry since the resent dialog has a different uid than its older form")]
		public static void ResendAndRestackDialog(GumpInstance oldInstance) {
			GumpInstance newInstance = oldInstance.Cont.SendGump(
				oldInstance.Focus, oldInstance.def, oldInstance.InputParams);//resend
			RenewStackedDialog(oldInstance, newInstance);//and restack
		}

		[Remark("After resending the gump to the player, we have a new instance with a new UID - we need to"+
				"update the stacked information in the multistack - assign the stack info to the new UID")]
		public static void RenewStackedDialog(GumpInstance oldInstance, GumpInstance resentInstance) {
			Dictionary<uint, Stack<DialogStackItem>> dialogsMultiStack = oldInstance.Cont.Conn.GetTag(_dialogStackTag_) as Dictionary<uint, Stack<DialogStackItem>>;
			if(dialogsMultiStack != null) { //something was stored
				if(dialogsMultiStack.ContainsKey(oldInstance.uid)) {
					Stack<DialogStackItem> storedStack = dialogsMultiStack[oldInstance.uid];
					//store the stack back to the multistack but now under the new UID!
					dialogsMultiStack[resentInstance.uid] = storedStack;
					dialogsMultiStack.Remove(oldInstance.uid); //the old dialog has been discarded
				}
			}
		}

		[Remark("For possible clearing of the stacked dialogs (if needed)")]
		public static void ClearDialogStack(GameConn fromWho) {
			fromWho.SetTag(_dialogStackTag_, null);
		}

		[Remark("For displaying the previously stored dialog (if any)")]
		public static void ShowPreviousDialog(GumpInstance actualGi) {
			DialogStackItem dsi = DialogStackItem.PopStackedDialog(actualGi);
			if(dsi != null) {
				dsi.Show(); //we had something stored there, display it now
			}
		}
	}
}