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
using System.Collections.Generic;
using SteamEngine.CompiledScripts;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace Steamengine.CompiledScripts {

	[HasSavedMembers]
	[Remark("Utility class for storing and managing account information notes")]
	public static class InformationRegister {
		[SavedMember]
		public static Dictionary<string, List<AccountNote>> accNotes = new Dictionary<string, List<AccountNote>>();

		[Remark("Returns a copy of the AccNotes Dictionary (usable for sorting etc.)")]
		public static Dictionary<string, List<AccountNote>> AccNotes {
			get {
				return new Dictionary<string, List<AccountNote>>(accNotes);
			}
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
		public string text; //the notes text
		[SaveableData]
		public DateTime time; //time when the issue was created

		public AccountNote(AbstractCharacter issuer, string text) {
			this.issuer = issuer;
			this.text = text;
			this.time = DateTime.Now;
		}
	}
}