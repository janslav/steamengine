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
using System.Text.RegularExpressions;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	public class RankSystem : Role {

		private List<string> rankNames = new List<string>();

		internal RankSystem(RankSystemDef def, RoleKey key)
			: base(def, key) {

		}

		[SaveableClass]
		public class RankMembership : RoleMembership {
			private int rankIndex;
			RankSystem cont;

			internal RankMembership(Character member, RankSystem cont)
				: base(member) {
				this.cont = cont;
			}

			public int RankIndex {
				get {
					return this.rankIndex;
				}
				set {
					if (value >= 0 && value < this.cont.rankNames.Count) {
						this.rankIndex = value;
					} else {
						throw new SEException("There's no rank indexed " + value);
					}
				}
			}

			public string Name {
				get {
					return this.cont.rankNames[this.rankIndex];
				}
			}

		}

		protected override IRoleMembership CreateMembershipObject(Character member) {
			return new RankMembership(member, this);
		}

		public override void Save(SaveStream output) {
			base.Save(output);
			output.WriteValue("rankNames", this.rankNames);
		}

		protected override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "rankNames":
					ObjectSaver.Load(valueString, this.Load_RankNames, filename, line);
					return;
			}
			base.LoadLine(filename, line, valueName, valueString);
		}

		private void Load_RankNames(object resolvedObject, string filename, int line) {
			this.rankNames = (List<string>) resolvedObject;
		}
	}

	public class RankSystemDef : RoleDef {
		public RankSystemDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Role CreateImpl(RoleKey key) {
			return new RankSystem(this, key);
		}
	}
}		
