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
		//dicitonary holding for every role the list of characters that have it assigned
		private static Dictionary<Role, List<Character>> rolesCharacters = new Dictionary<Role, List<Character>>();

		//dictionary holding a set of assigned roles to all characters that have any...
		internal static Dictionary<Character, HashSet<Role>> charactersRoles = new Dictionary<Character,HashSet<Role>>();

        [Summary("For given role return the list of charater that are cast to it (useful e.g. for obtaining list of friends of house)")]	
		public static List<Character> GetCharactersInRole(Role role) {
			return rolesCharacters[role];
		}

        [Summary("Return the list of all roles the specified char is cast to")]	
		public static HashSet<Role> GetCharactersRoles(Character chr) {
			return charactersRoles[chr];
		}

        [Summary("Find a list of characters for given role and add this character to it "+
                "then add the role to the character's roles list ")]
        public static bool AssignCharToRole(Character chr, Role role) {
			//first check - if we already have the role, we dont add it nor trigger any triggers
			if (chr.HasRole(role)) {
				return false;//information for the assigner (if necessary)
			}

            RoleDef def = role.RoleDef;
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, role);
            bool cancelAdd = def.Trigger_DenyMemberAddRequest(args); //return value means only that the trigger has been cancelled
			DenyResultRoles retVal = args.Result;//this value contains the info if we can or cannot add the member

			if (retVal == DenyResultRoles.Allow) {  //we are allowed to add the member                          
                List<Character> charsList = null;//first add to the charsList for the role
                if (!rolesCharacters.TryGetValue(role, out charsList)) {
                    charsList = new List<Character>();
                    rolesCharacters[role] = charsList;
                }
                charsList.Add(chr);
                def.Trigger_MemberAdded(chr, role);//now call the trigger on the roleDef (which will call it on the role itself)

				HashSet<Role> roles = null;
				if (!charactersRoles.TryGetValue(chr, out roles)) {
					roles = new HashSet<Role>();
					charactersRoles[chr] = roles;
				}
				roles.Add(role);//now add role to the character's role list
                chr.On_RoleAssign(role);//and call the trigger on the character...            
            }
			SendRoleMemberManipulationMessage(chr, role, retVal); //send result(message) of the "activate" call to the client

			return (retVal == DenyResultRoles.Allow); //result of the character adding
        }

        [Summary("Find a list of characters for given role and remove the specified character from it " +
                "then remove the role from the character's roles list ")]
        public static bool UnAssignCharFromRole(Character chr, Role role) {
			//first check - if we dont have the role, we dont remove anything nor trigger the triggers
			if (!chr.HasRole(role)) {
				return false;//information for the unassigner (if necessary)
			}

            RoleDef def = role.RoleDef;
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, role);
            bool cancelAdd = def.Trigger_DenyMemberRemoveRequest(args); //return value means only that the trigger has been cancelled
			DenyResultRoles retVal = args.Result;//this value contains the info if we can or cannot add the member

			if (retVal == DenyResultRoles.Allow) {//we are allowed to remove the member                          
                List<Character> charsList = null;//first remove from the charsList for the role
                if (rolesCharacters.TryGetValue(role, out charsList)) {
                    charsList.Remove(chr);//do it only if the role has any characters (do not create the list or anything else!)
                    def.Trigger_MemberRemoved(chr, role);//now call the trigger on the roleDef (which will call it on the role itself)
                }

				HashSet<Role> roles = null;
				if (charactersRoles.TryGetValue(chr, out roles)) {
					roles.Remove(role);
					chr.On_RoleUnAssign(role);//call the trigger on the character in case of success...
				}
            }
			SendRoleMemberManipulationMessage(chr, role, retVal); //send result(message) of the "activate" call to the client

			return (retVal == DenyResultRoles.Allow); //result of the character adding
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
