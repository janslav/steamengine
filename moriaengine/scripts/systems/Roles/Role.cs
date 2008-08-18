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
using SteamEngine.Packets;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class Role {
        internal static int uids;

        protected int uid;

        protected RoleDef roledef;
        protected string name;

        internal Role(RoleDef roledef, string name) {
            this.uid = uids++;
            this.roledef = roledef;
            this.name = name;
        }

        [Summary("Special message informing the target character that there is some problem removing him "+
                "from this particular role")]
        internal virtual void SendSpecialMemeberRemoveFailureMessage(Character toWho) {
            //toWho.RedMessage("Something");
        }

        [Summary("Special message informing the target character that there is some problem adding him " +
                "to this particular role")]
        internal virtual void SendSpecialMemeberAddFailureMessage(Character toWho) {
            //toWho.RedMessage("Something");
        }

        #region triggers
        [Summary("Trigger called when the new role is created")]
        internal virtual void On_RoleCreate(RoleDef roledef) {            
        }

        [Summary("Trigger called when the role is destroyed")]
        internal virtual bool On_RoleDestroy(RoleDef roledef) {
            return false; //dont cancel
        }

        [Summary("Trigger called when the new member is assigned to this role")]
        internal virtual void On_RoleMemberAdded(RoleDef roledef, Character chr) {
            //no cancelling, this trigger will be run after the checkings in the 
            //"denymemberaddrequest"
        }

        [Summary("Trigger called when the new member adding is requested from this role")]
		internal virtual bool On_RoleDenyMemberAddRequest(DenyRoleTriggerArgs args) {
            return false; //dont cancel
        }

        [Summary("Trigger called when the member is unassigned from this role")]
        internal virtual void On_RoleMemberRemoved(RoleDef roledef, Character chr) {
            //no cancelling, this trigger will be run after the checkings in the 
            //"denymemberremoverequest"
        }

        [Summary("Trigger called when the member remove is requested from this role")]
		internal virtual bool On_RoleDenyMemberRemoveRequest(DenyRoleTriggerArgs args) {
            return false; //dont cancel
        }        
        #endregion triggers

        public RoleDef RoleDef {
            get {
                return roledef;
            }
        }

        public string Name {
            get {
                return name;
            }
            set {
                name = value;
            }
        }

        public override int GetHashCode() {
            return uid;
        }

        public override string ToString() {
            return name;
        }

        public override bool Equals(Object obj) {
            return this == obj;
        }
	}
}