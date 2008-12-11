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

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class Role : Poolable {
        private RoleDef def;
		private RoleKey key;

		internal List<Character> members;
		private System.Collections.ObjectModel.ReadOnlyCollection<Character> membersReadOnly;

		public Role() {
			members = new List<Character>();
			membersReadOnly = new System.Collections.ObjectModel.ReadOnlyCollection<Character>(this.members);
        }

		protected override void On_Reset() {
			this.def = null;
			this.key = null;
			this.members.Clear();
			base.On_Reset();
		}

        #region triggers
        [Summary("Trigger called when the new role is created")]
		internal virtual void On_Create() {
        }

        [Summary("Trigger called when the role is destroyed")]
		internal virtual void On_Destroy() {
        }

		[Summary("Trigger called when the new member adding is requested from this role")]
		internal virtual bool On_DenyAddMember(DenyRoleTriggerArgs args) {
			return false; //dont cancel
		}

        [Summary("Trigger called when the new member is assigned to this role")]
        internal virtual void On_MemberAdded(Character newMember) {
			//this trigger will be run after @DenyAddMember
        }

		[Summary("Trigger called when the member remove is requested from this role")]
		internal virtual bool On_DenyRemoveMember(DenyRoleTriggerArgs args) {
			return false; //dont cancel
		}   
		
		[Summary("Trigger called when the member is unassigned from this role")]
		internal virtual void On_MemberRemoved(Character exMember) {
			//this trigger will be run after @DenyRemoveMember
        }
     
        #endregion triggers

        public RoleDef RoleDef {
            get {
                return def;
            }
			internal set {
				this.def = value;
			}
        }

        public RoleKey Key {
            get {
                return key;
            }
			internal set {
				this.key = value;
			}
        }

		public override string ToString() {
			return string.Concat(this.GetType().ToString(), " °", this.key.name);
		}

		public System.Collections.ObjectModel.ReadOnlyCollection<Character> Members {
			get {
				return this.membersReadOnly;
			}
		}

		public override void Dispose() {
			while (this.members.Count > 0) {
				RolesManagement.UnAssign(this.members[0], this);
			}

			base.Dispose();
		}
	}

	public class RoleKey : AbstractKey {
		private static Dictionary<string, RoleKey> byName = new Dictionary<string, RoleKey>(StringComparer.OrdinalIgnoreCase);

		private RoleKey(string name, int uid)
			: base(name, uid) {
		}

		public static RoleKey Get(string name) {
			RoleKey key;
			if (byName.TryGetValue(name, out key)) {
				return key;
			}
			key = new RoleKey(name, uids++);
			byName[name] = key;
			return key;
		}
	}


	public sealed class PluginKeySaveImplementor : SteamEngine.Persistence.ISimpleSaveImplementor {
		public static Regex re = new Regex(@"^\°(?<value>.+)\s*$",                     
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(PluginKey);
			}
		}
		
		public Regex LineRecognizer { get {
			return re;
		} }
		
		public string Save(object objToSave) {
			return "°" + ((PluginKey) objToSave).name;
		}
		
		public object Load(Match match) {
			return PluginKey.Get(match.Groups["value"].Value);
		}
		
		public string Prefix {
			get {
				return "°";
			}
		}
	}
}		
