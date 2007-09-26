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
		private static readonly TagKey _dialogStackTag_ = TagKey.Get("x_guta_dialogStack");

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
			/*this.instance = instance;
			this.args = args;
			this.connection = connection;
			this.cont = cont;
			this.focus = focus;
			 * */
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
				"when closing one of them (displaying other previous dialogs unwantedly etc)")]
		public void Show() {
			//send the dialog with stored values
			//only in case it is not opened 
			if(!Cont.HasOpenedDialog(Gump)) {
				Cont.SendGump(gumpInstance);
			} else {
				//nothing will be shown, the dialog is still opened, return this DSI to the stack
				DialogStackItem.EnstackDialog(Cont, this);
			}
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
			"UPDATED - using GumpInstance as a parameter only (it contains everything necessary)")]
		public static void EnstackDialog(GumpInstance gi) {
			Stack<DialogStackItem> dialogsStack = gi.Cont.Conn.GetTag(_dialogStackTag_) as Stack<DialogStackItem>;
			if(dialogsStack == null) { //not yet set
				dialogsStack = new Stack<DialogStackItem>();
				gi.Cont.Conn.SetTag(_dialogStackTag_, dialogsStack);
			}
			dialogsStack.Push(new DialogStackItem(gi));
		}

		[Remark("Overloaded init method - used when the gumpinstance is not fully initialized yet")]
		public static void EnstackDialog(AbstractCharacter cont,  Thing focus, 
											Gump instance, params object[] args) {
			Stack<DialogStackItem> dialogsStack = cont.Conn.GetTag(_dialogStackTag_) as Stack<DialogStackItem>;
			if(dialogsStack == null) { //not yet set
				dialogsStack = new Stack<DialogStackItem>();
				cont.Conn.SetTag(_dialogStackTag_, dialogsStack);
			}
			dialogsStack.Push(new DialogStackItem(cont.Conn, cont, focus, instance, args));
		}

		[Remark("Allows us to 'push back' a previously popped dialog stack item.")]
		public static void EnstackDialog(AbstractCharacter cont, DialogStackItem dsi) {
			Stack<DialogStackItem> dialogsStack = cont.Conn.GetTag(_dialogStackTag_) as Stack<DialogStackItem>;
			if(dialogsStack == null) { //not yet set
				dialogsStack = new Stack<DialogStackItem>();
				cont.Conn.SetTag(_dialogStackTag_, dialogsStack);
			}
			dialogsStack.Push(dsi);
		}

		[Remark("Recall the last stored dialog from the dialog stack." +
				"Method must be called from the dialog we want to return from in the OnResponse method")]
		public static DialogStackItem PopStackedDialog(GameConn showTo) {
			Stack<DialogStackItem> dialogsStack = showTo.GetTag(_dialogStackTag_) as Stack<DialogStackItem>;
			DialogStackItem dsi = null;
			if(dialogsStack != null) { //something was stored
				dsi = dialogsStack.Pop();
				if(dialogsStack.Count == 0) {//the stack is empty now
					showTo.SetTag(_dialogStackTag_, null); //free the tag
				}
			}
			return dsi;
		}

		[Remark("For possible clearing of the stacked dialogs (if needed)")]
		public static void ClearDialogStack(GameConn fromWho) {
			fromWho.SetTag(_dialogStackTag_, null);
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