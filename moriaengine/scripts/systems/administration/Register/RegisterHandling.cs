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
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine;

namespace SteamEngine.CompiledScripts {

	[Remark("Utility class for managing account information or crime notes")]
	public static class AccountRegister {
		public static readonly string ALL_CHARS = "all";

		public static List<AccountNote> GetNotes(ScriptedAccount acc, AccountNotesSorting sortBy) {
			List<AccountNote> notes = acc.AccNotes;
			NotesListSort(notes, sortBy); //sort, if necessary
			return notes;
		}

		public static List<AccountCrime> GetCrimes(ScriptedAccount acc, AccountNotesSorting sortBy) {
			List<AccountCrime> crimes = acc.AccCrimes;
			NotesListSort(crimes, sortBy); //sort, if necessary
			return crimes;
		}

		[Remark("Sorting of the account notes/crimes list")]
		private static void NotesListSort(List<AccountNote> list, AccountNotesSorting criteria) {
			if(sortBy != null) {
				switch(sortBy) {
					case AccountNotesSorting.TimeAsc:
						notes.Sort(NotesTimeComparer.instance);
						break;
					case AccountNotesSorting.IssuerDesc:
						notes.Sort(NotesTimeComparer.instance);
						notes.Reverse();
						break;
					case AccountNotesSorting.RefCharAsc:
						notes.Sort(NotesRefCharComparer.instance);
						break;
					case AccountNotesSorting.RefCharDesc:
						notes.Sort(NotesRefCharComparer.instance);
						notes.Reverse();
						break;
					case AccountNotesSorting.IssuerAsc:
						notes.Sort(NotesIssuerComparer.instance);
						break;
					case AccountNotesSorting.IssuerDesc:
						notes.Sort(NotesIssuerComparer.instance);
						notes.Reverse();
						break;					
				}
			}
		}
	}

	[Remark("Comparer for sorting account notes by time")]
	public class NotesTimeComparer : IComparer<AccountNote> {
		public readonly static NotesTimeComparer instance = new NotesTimeComparer();

		private NotesTimeComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(AccountNote x, AccountNote y) {
			return x.time.CompareTo(y.time);
		}
	}

	[Remark("Comparer for sorting account notes by referered character")]
	public class NotesRefCharComparer : IComparer<AccountNote> {
		public readonly static NotesRefCharComparer instance = new NotesRefCharComparer();

		private NotesRefCharComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(AccountNote x, AccountNote y) {
			//check if we have the reffered character, otherwise return "all" - the note or crime is related to the whole account
			string refXName = (x.referredChar != null ? x.referredChar.Name : "all");
			string refYName = (y.referredChar != null ? y.referredChar.Name : "all");
			return String.Compare(refXName, refYName, true);
		}
	}

	[Remark("Comparer for sorting account notes by note issuer")]
	public class NotesIssuerComparer : IComparer<AccountNote> {
		public readonly static NotesIssuerComparer instance = new NotesIssuerComparer();

		private NotesIssuerComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(AccountNote x, AccountNote y) {
			return String.Compare(x.issuer.Name, y.issuer.Name, true);
		}
	}



	[SaveableClass]
	public class AccountNote {
		[LoadingInitializer]
		public AccountNote() {
		}

		[SaveableData]
		public AbstractCharacter issuer; //GM who created the issue
		[SaveableData]
		public AbstractCharacter referredChar; //character on the acccount connected with the note (or crime)
		[SaveableData]
		public string text; //the notes text or the crimes description
		[SaveableData]
		public DateTime time; //time when the issue was created

		public AccountNote(AbstractCharacter issuer, AbstractCharacter referredChar, string text) {
			this.issuer = issuer;
			this.referredChar = referredChar;
			this.text = text;
			this.time = DateTime.Now;
		}
	}

	[SaveableClass]
	public class AccountCrime : AccountNote {
		[LoadingInitializer]
		public AccountCrime() {
		}
		
		[SaveableData]
		public string punishment; //the pujnishment description		

		[SaveableData]		
		public bool isAFK; //is it the AFK crime?

		public AccountCrime(AbstractCharacter issuer, AbstractCharacter referredChar, string punishment, string crime)
			: base(issuer, refferedChar, crime) {
			this.punishment = punishment;			
		}

		public bool AFK { //setter for the AFK property
			set {
				isAFK = value;
			}
		}
	}
}