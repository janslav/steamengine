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
	[Summary("Class encapsulating stacking an recalling dialogs")]
	public static class DialogStacking {
		[Summary("Tag used for stacking information about clients dialogs to be called back")]
		private static readonly TagKey dialogStackTK = TagKey.Get("_guta_dialogStackTree_");

		[Summary("LSCript used method for setting the specified argument's value")]
		public static void SetArgValue(Gump gi, int index, object value) {
			gi.InputArgs.ArgsArray[index] = value;
		}

		[Summary("LSCript used method for getting the specified argument's value")]
		public static object GetArgValue(Gump gi, int index) {
			return gi.InputArgs.ArgsArray[index];
		}

		[Summary("Store the info about the dialog to the dialog stack. Used for " +
			"storing info about the last used dialog whjich can be used to reopen the dialog " +
			"when exiting from the following dialog for instance." +
			"e.g. info dialog -> clicked on the 'account info' button which closes the info dialog," +
			"after finishing with the accout info dialog we would like to have the info dialog reopened!" +
			"UPDATED - using Gump as a parameter only (it contains everything necessary)" +
			"UPDATED2 - oldGi - dialog being enstacked; newGi - newly opened dialog for storing stacking info")]
		public static void EnstackDialog(Gump oldGi, Gump newGi) {
			Dictionary<uint, Stack<Gump>> dialogsMultiStack = oldGi.Cont.GetTag(dialogStackTK) as Dictionary<uint, Stack<Gump>>;
			if (dialogsMultiStack == null) { //zatim nemame ani stack
				dialogsMultiStack = new Dictionary<uint, Stack<Gump>>();
				oldGi.Cont.SetTag(dialogStackTK, dialogsMultiStack);
			}
			Stack<Gump> actualStack;
			if (!dialogsMultiStack.TryGetValue(oldGi.uid, out actualStack)) {
				actualStack = new Stack<Gump>(); //vytvortit a ulozit
				dialogsMultiStack[oldGi.uid] = actualStack;
			}
			actualStack.Push(oldGi); //stackneme si starej dialog
			dialogsMultiStack[newGi.uid] = actualStack; //dat k dispozici novemu dialogu pro pozdejsi navraty
		}

		[Summary("Recall the last stored dialog from the dialog stack." +
				"Find it according to the given actual Gump in the dialogs multistack." +
				"Handles also emptying and clearing unused (empty) stacks and the multistack too.")]
		public static Gump PopStackedDialog(Gump actualGi) {
			Dictionary<uint, Stack<Gump>> dialogsMultiStack = actualGi.Cont.GetTag(dialogStackTK) as Dictionary<uint, Stack<Gump>>;
			Gump sgi = null;
			if (dialogsMultiStack != null) { //something was stored
				Stack<Gump> actualsOwnStack;
				if (dialogsMultiStack.TryGetValue(actualGi.uid, out actualsOwnStack)) {
					if (actualsOwnStack.Count != 0) {
						sgi = actualsOwnStack.Pop(); //popovat budem jen pokud ve stacku vubec neco je
						dialogsMultiStack.Remove(actualGi.uid); //po popnuti uz muzeme vyhodit soucasny dialog z multistacku (uz ho stejne zaviram...)					
					}

					if (actualsOwnStack.Count == 0) {//stack je uz prazdnej (nebo byl prazdnej i predtim), odstranit stack
						dialogsMultiStack.Remove(actualGi.uid);
						if (dialogsMultiStack.Count == 0) {
							actualGi.Cont.RemoveTag(dialogStackTK);//multistack je prazdnej, uvolnit
						}
					}
				}
			}
			return sgi;
		}

		[Summary("When we need to resend the actual dialog (e.g. only with changed parameters such as going to another page or " +
				"refrehsing some list where we need the actual data to be refreshed, we need the complete initialization phase " +
				"to be performed - we will resend the dialog in a standard way but we have to make sure the stacked information " +
				"remains valid => we will store the new (resent) gumpinstance with the same stacking info as the old one " +
				"This is necesasry since the resent dialog has a different uid than its older form")]
		public static void ResendAndRestackDialog(Gump oldInstance) {
			Gump newInstance = oldInstance.Cont.SendGump(
				oldInstance.Focus, oldInstance.def, oldInstance.InputArgs);//resend
			RenewStackedDialog(oldInstance, newInstance);//and restack
		}

		[Summary("After resending the gump to the player, we have a new instance with a new UID - we need to" +
				"update the stacked information in the multistack - assign the stack info to the new UID")]
		private static void RenewStackedDialog(Gump oldInstance, Gump resentInstance) {
			Dictionary<uint, Stack<Gump>> dialogsMultiStack = oldInstance.Cont.GetTag(dialogStackTK) as Dictionary<uint, Stack<Gump>>;
			if (dialogsMultiStack != null) { //something was stored
				Stack<Gump> storedStack;
				if (dialogsMultiStack.TryGetValue(oldInstance.uid, out storedStack)) {
					//store the stack back to the multistack but now under the new UID!
					dialogsMultiStack[resentInstance.uid] = storedStack;
					dialogsMultiStack.Remove(oldInstance.uid); //the old dialog has been discarded
				}
			}
		}

		[Summary("For possible clearing of the stacked dialogs (if needed)")]
		public static void ClearDialogStack(AbstractCharacter fromWho) {
			fromWho.RemoveTag(dialogStackTK);
		}

		[Summary("For displaying the previously stored dialog (if any)")]
		public static void ShowPreviousDialog(Gump actualGi) {
			Gump sgi = DialogStacking.PopStackedDialog(actualGi);
			if (sgi != null) {
				ResendAndRestackDialog(sgi); //we also have to alter stack information about the resent dialog...
			}
		}
	}
}