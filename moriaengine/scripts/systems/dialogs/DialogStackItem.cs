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
	public static class DialogStacking {
		[Remark("Tag used for stacking information about clients dialogs to be called back")]
		private static readonly TagKey _dialogStackTag_ = TagKey.Get("x_guta_dialogStackTree");

		[Remark("Gets the stored information and displays the dialog."+
				"It displays the dialog only in case it is not yet opened and visible - this can"+
				"occur e.g. when opening another dialog having one opened before and then closing the"+
				"least opened one - it will look into the stack, find the previous dialog (the first opened one)"+
				"and tries to show it which would lead to having two same dialogs opened and problems"+
				"when closing one of them (displaying other previous dialogs unwantedly etc)"+
				"neresti otevrenost ostatnich ialogu, o to se musi postarat volatel")]
		public static void Show(GumpInstance gi) {
			//send the dialog with stored values
			//only in case it is not opened 
			//if(!Cont.HasOpenedDialog(Gump)) {
			ResendAndRestackDialog(gi);
			//Cont.ReSendGump(gumpInstance);
			//} else {
				//nothing will be shown, the dialog is still opened, return this DSI to the stack
			//	DialogStackItem.EnstackDialog(Cont, this);
			//}
		}

		[Remark("LSCript used method for setting the specified argument's value")]
		public static void SetArgValue(GumpInstance gi, int index, object value) {
			gi.InputParams[index] = value;
		}

		[Remark("LSCript used method for getting the specified argument's value")]
		public static object GetArgValue(GumpInstance gi, int index) {
			return gi.InputParams[index];
		}

		[Remark("Store the info about the dialog to the dialog stack. Used for " +
			"storing info about the last used dialog whjich can be used to reopen the dialog " +
			"when exiting from the following dialog for instance." +
			"e.g. info dialog -> clicked on the 'account info' button which closes the info dialog," +
			"after finishing with the accout info dialog we would like to have the info dialog reopened!"+
			"UPDATED - using GumpInstance as a parameter only (it contains everything necessary)"+
			"UPDATED2 - oldGi - dialog being enstacked; newGi - newly opened dialog for storing stacking info")]
		public static void EnstackDialog(GumpInstance oldGi, GumpInstance newGi) {
			Dictionary<uint, Stack<GumpInstance>> dialogsMultiStack = oldGi.Cont.Conn.GetTag(_dialogStackTag_) as Dictionary<uint, Stack<GumpInstance>>;
			if(dialogsMultiStack == null) { //zatim nemame ani stack
				dialogsMultiStack = new Dictionary<uint, Stack<GumpInstance>>();
				oldGi.Cont.Conn.SetTag(_dialogStackTag_, dialogsMultiStack);
			}
			Stack<GumpInstance> actualStack;
			if (!dialogsMultiStack.TryGetValue(oldGi.uid, out actualStack)) {
				actualStack = new Stack<GumpInstance>(); //vytvortit a ulozit
				dialogsMultiStack[oldGi.uid] = actualStack;
			}
			//if(dialogsMultiStack.ContainsKey(oldGi.uid)) {
			//    actualStack = dialogsMultiStack[oldGi.uid]; //dotahnout
			//} else {
			//    actualStack = new Stack<DialogStackItem>(); //vytvortit a ulozit
			//    dialogsMultiStack[oldGi.uid] = actualStack;
			//}
			actualStack.Push(oldGi); //stackneme si starej dialog
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
		public static GumpInstance PopStackedDialog(GumpInstance actualGi) {
			Dictionary<uint, Stack<GumpInstance>> dialogsMultiStack = actualGi.Cont.Conn.GetTag(_dialogStackTag_) as Dictionary<uint, Stack<GumpInstance>>;
			GumpInstance sgi = null;
			if(dialogsMultiStack != null) { //something was stored
				//if(dialogsMultiStack.ContainsKey(actualGi.uid)) {
				//    Stack<DialogStackItem> actualsOwnStack = dialogsMultiStack[actualGi.uid];	
				Stack<GumpInstance> actualsOwnStack;
				if(dialogsMultiStack.TryGetValue(actualGi.uid, out actualsOwnStack)) {
					if(actualsOwnStack.Count != 0) {
						sgi = actualsOwnStack.Pop(); //popovat budem jen pokud ve stacku vubec neco je
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
			return sgi;
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
		private static void RenewStackedDialog(GumpInstance oldInstance, GumpInstance resentInstance) {
			Dictionary<uint, Stack<GumpInstance>> dialogsMultiStack = oldInstance.Cont.Conn.GetTag(_dialogStackTag_) as Dictionary<uint, Stack<GumpInstance>>;
			if(dialogsMultiStack != null) { //something was stored
				//if(dialogsMultiStack.ContainsKey(oldInstance.uid)) {
				//    Stack<DialogStackItem> storedStack = dialogsMultiStack[oldInstance.uid];
				Stack<GumpInstance> storedStack;
				if(dialogsMultiStack.TryGetValue(oldInstance.uid, out storedStack)) {
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
			GumpInstance sgi = DialogStacking.PopStackedDialog(actualGi);
			if (sgi != null) {
				ResendAndRestackDialog(sgi); //we also have to alter stack information about the resent dialog...
			}
		}
	}
}