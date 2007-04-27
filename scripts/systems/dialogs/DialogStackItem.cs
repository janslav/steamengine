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
		private static readonly TagKey dialogStack = TagKey.Get("x_guta_dialogStack");

		[Remark("Instance of the dialog to be recalled")]
		Gump instance;
		[Remark("Parameters of the dialog as used")]
		object[] args;
		[Remark("Cont as was on the dialog instance - where is the dialog set")]
		AbstractCharacter cont;
		[Remark("Focus from the original dialog")]
		Thing focus;
		[Remark("Conn holding info about who will be shown the dialog in the end")]
		GameConn connection;

		[Remark("Make the args of the stored dialog available for possible editing")]
		public object[] Args {
			get {
				return args;
			}
			set {
				args = value;
			}
		}
	

		internal DialogStackItem(GameConn connection, AbstractCharacter cont, Thing focus,
									Gump instance, object[] args) {
			this.instance = instance;
			this.args = args;
			this.connection = connection;
			this.cont = cont;
			this.focus = focus;
		}

		[Remark("Gets the stored information and displays the dialog")]
		public void Show() {
			//send the dialog with stored values
			cont.SendGump(focus, instance, args);
		}

		[Remark("Store the info about the dialog to the dialog stack. Used for " +
			"storing info about the last used dialog whjich can be used to reopen the dialog " +
			"when exiting from the following dialog for instance." +
			"e.g. info dialog -> clicked on the 'account info' button which closes the info dialog," +
			"after finishing with the accout info dialog we would like to have the info dialog reopened!")]
		public static void EnstackDialog(GumpInstance gi,
											Gump instance, params object[] args) {
			Stack<DialogStackItem> dialogsStack = gi.Cont.Conn.GetTag(dialogStack) as Stack<DialogStackItem>;
			if(dialogsStack == null) { //not yet set
				dialogsStack = new Stack<DialogStackItem>();
				gi.Cont.Conn.SetTag(dialogStack, dialogsStack);
			}
			dialogsStack.Push(new DialogStackItem(gi.Cont.Conn, gi.Cont, gi.Focus, instance, args));
		}

		[Remark("Overloaded init method - used when the gumpinstance is not fully initialized yet")]
		public static void EnstackDialog(AbstractCharacter cont,  Thing focus, 
											Gump instance, params object[] args) {
			Stack<DialogStackItem> dialogsStack = cont.Conn.GetTag(dialogStack) as Stack<DialogStackItem>;
			if(dialogsStack == null) { //not yet set
				dialogsStack = new Stack<DialogStackItem>();
				cont.Conn.SetTag(dialogStack, dialogsStack);
			}
			dialogsStack.Push(new DialogStackItem(cont.Conn, cont, focus, instance, args));
		}

		[Remark("Recall the last stored dialog from the dialog stack." +
				"Method must be called from the dialog we want to return from in the OnResponse method")]
		public static DialogStackItem PopStackedDialog(GameConn showTo) {
			Stack<DialogStackItem> dialogsStack = showTo.GetTag(dialogStack) as Stack<DialogStackItem>;
			DialogStackItem dsi = null;
			if(dialogsStack != null) { //something was stored
				dsi = dialogsStack.Pop();
				if(dialogsStack.Count == 0) {//the stack is empty now
					showTo.SetTag(dialogStack, null); //free the tag
				}
			}
			return dsi;
		}

		[Remark("For possible clearing of the stacked dialogs (if needed)")]
		public static void ClearDialogStack(GameConn fromWho) {
			fromWho.SetTag(dialogStack, null);
		}

		[Remark("For displaying the previously stored dialog (if any)")]
		public static void ShowPreviousDialog(GameConn forWho) {
			DialogStackItem dsi = DialogStackItem.PopStackedDialog(forWho);
			if(dsi != null) {
				dsi.Show(); //we had something stored there, display it now
			}
		}
	}
}