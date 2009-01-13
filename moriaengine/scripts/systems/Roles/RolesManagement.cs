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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[Summary("Utility class managing everything about roles including assigning and storing information about casts")]
	public static class RolesManagement {
		//dictionary holding a set of assigned roles to all characters that have any...
		internal static Dictionary<AbstractCharacter, Dictionary<RoleKey, Role>> charactersRoles = new Dictionary<AbstractCharacter, Dictionary<RoleKey, Role>>();

		[Summary("Try assign chr to role. Runs and obeys @deny triggers.")]
		[Return("true = chr is now member of role, false = it's not")]
		public static bool TryAssign(Character chr, Role role) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						return true; // we're already in place, all is ok
					} else {
						if (role.Trigger_DenyAddMember(chr) == DenyResultRoles.Allow) {
							if (!TryUnAssign(chr, prevRole)) {//the previous role occupies our spot. Let's try to kick it
								return false; //removing was denied, can't proceed
							}
						} else { //else message?
							return false;
						}
					}
				}
			} else {
				if (role.Trigger_DenyAddMember(chr) == DenyResultRoles.Allow) {
					rolesByKey = new Dictionary<RoleKey, Role>();
					charactersRoles[chr] = rolesByKey;
				} else { //else message?
					return false;
				}
			}

			rolesByKey[key] = role;
			role.InternalAddMember(chr);
			return true;
		}

		[Summary("Assign chr to role. Ignores @deny triggers.")]
		public static void Assign(Character chr, Role role) {
			InternalAddLoadedRole(role, chr);
			role.InternalAddMember(chr);
		}

		internal static void InternalAddLoadedRole(Role role, Character chr) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						return; // we're already in place, all is ok
					} else {
						UnAssign(chr, role);
					}
				}
			} else {
				rolesByKey = new Dictionary<RoleKey, Role>();
				charactersRoles[chr] = rolesByKey;
			}

			rolesByKey[key] = role;
			//role.InternalAddMember(chr); this is done when calling this method
		}

        [Summary("Find a list of characters for given role and remove the specified character from it " +
                "then remove the role from the character's roles list ")]
		[Return("true = chr is not member of role, false = it is")]
        public static bool TryUnAssign(Character chr, Role role) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						if (role.Trigger_DenyRemoveMember(chr) == DenyResultRoles.Allow) {							
							if (rolesByKey.Count == 1) { //last role of that char
								charactersRoles.Remove(chr);
							} else {
								rolesByKey.Remove(key);
							}
							role.InternalRemoveMember(chr);
							return true; //unassign succesful
						} else {
							return false; //unassign denied
						}
					}
				}
			}
			return true; //wasn't member in the first place
		}

		public static void UnAssign(AbstractCharacter chr, Role role) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						if (rolesByKey.Count == 1) { //last role of that char
							charactersRoles.Remove(chr);
						} else {
							rolesByKey.Remove(key);
						}
						role.InternalRemoveMember(chr);
					}
				}
			}
		}

		public static void UnassignAll(Role role, bool beingDestroyed) {
			RoleKey key = role.Key;
			foreach (AbstractCharacter chr in role.Members) {
				Dictionary<RoleKey, Role> rolesByKey;
				if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
					rolesByKey.Remove(key);
					if (rolesByKey.Count == 0) { //last role of that char
						charactersRoles.Remove(chr);
					}
				}
			}
			role.InternalClearMembers(beingDestroyed);
		}

		public static Role GetRole(Character chr, RoleKey roleKey) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role retVal;
				if (rolesByKey.TryGetValue(roleKey, out retVal)) {
					return retVal;
				}
			}
			return null;
		}

		public static bool HasRole(Character chr, Role role) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role byKey;
				if (rolesByKey.TryGetValue(role.Key, out byKey)) {
					return role == byKey;
				}
			}
			return false;
		}

		public static bool HasRole(Character chr, RoleKey roleKey) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				return rolesByKey.ContainsKey(roleKey);
			}
			return false;
		}

		public static IList<AbstractCharacter> GetCharactersInRole(Role role) {
			if (role != null) {
				return role.Members;
			}
			return EmptyReadOnlyGenericCollection<AbstractCharacter>.instance;
		}

		public static ICollection<Role> GetCharactersRoles(Character chr) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				return rolesByKey.Values;
			}
			return EmptyReadOnlyGenericCollection<Role>.instance;
		}

        [Summary("Method for sending clients messages about the role adding/removing result")]
		private static void SendRoleMemberManipulationMessage(Character whom, Role role, DenyResultRoles res) {
            //first send the common message
            switch (res) {
				//...any possibilities here :-)
				case DenyResultRoles.Deny_NoMessage:
                    //no message here
                    break;
            }
        }
	}
}
