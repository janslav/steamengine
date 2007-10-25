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
using SteamEngine.Timers;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(AbstractAccount), "Account",
		new string[] { "blocked", "Characters" }
		)]
	public static class AbstractAccountDescriptor {
		[Button("Block")]
		public static void Block(object target) {
			((AbstractAccount)target).Block();
		}

		[Button("Unblock")]
		public static void UnBlock(object target) {
			((AbstractAccount)target).UnBlock();
		}

		[Button("Delete")]
		public static void Delete(object target) {
			((AbstractAccount)target).Delete();
		}

		[Button("Characters")]
		public static void AccChars(object target) {
			//dialog se seznamem characteru (tech bude maximalne pet)
			Globals.SrcCharacter.Dialog(SingletonScript<D_Acc_Characters>.Instance, (AbstractAccount)target);			
		}
	}

	[ViewDescriptor(typeof(ScriptedAccount), "Account")]
	public static class ScriptedAccountDescriptor {
		[Button("Account notes")]
		public static void AccNotes(object target) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, (AbstractAccount)target, AccountNotesSorting.TimeDesc, 0, null);
		}

		[Button("New account note")]
		public static void NewAccNote(object target) {						//       trest, char, acc
			Globals.SrcCharacter.Dialog(SingletonScript<D_New_AccountNote>.Instance, false, null, (AbstractAccount)target);			
		}

		[Button("Account crimes")]
		public static void AccCrimes(object target) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, (AbstractAccount)target, AccountNotesSorting.TimeDesc, 0, null);
		}

		[Button("New account crime")]
		public static void NewAccCrime(object target) {						//       trest, char, acc
			Globals.SrcCharacter.Dialog(SingletonScript<D_New_AccountNote>.Instance, true, null, (AbstractAccount)target);
		}
	}
}