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
	[Dialogs.ViewableClass]
	[SaveableClass]
	public partial class Role : Disposable {
		private RoleDef def;
		private RoleKey key;
		private string name;

		private Dictionary<Character, IRoleMembership> members = new Dictionary<Character, IRoleMembership>();

		//private System.Collections.ObjectModel.ReadOnlyCollection<Character> membersReadOnly;

		internal Role(RoleDef def, RoleKey key) {
			//this.members = new List<Character>();
			//this.memberships = new List<IRoleMembership>();
			//this.membersReadOnly = new System.Collections.ObjectModel.ReadOnlyCollection<IRoleMembership>(this.members);
			this.key = key;
			this.def = def;
		}

		internal void InternalAddMember(Character newMember) {
			IRoleMembership membership = this.CreateMembershipObject(newMember);
			Sanity.IfTrueThrow(newMember != membership.Member, "newMember != membership.Member");

			this.members.Add(newMember, membership);
			this.Trigger_MemberAdded(newMember, membership);
		}

		internal void InternalRemoveMember(Character removedMember, IRoleMembership membership) {
			Sanity.IfTrueThrow(removedMember != membership.Member, "removedMember != membership.Member");

			bool removed = this.members.Remove(removedMember);
			Sanity.IfTrueThrow(!removed, "!this.members.ContainsKey(removedMember)");

			this.Trigger_MemberRemoved(removedMember, membership, false);
		}

		internal void InternalLoadMembership(IRoleMembership loaded) {
			RolesManagement.InternalAddLoadedRole(this, loaded.Member);
			this.members.Add(loaded.Member, loaded);
		}

		internal void InternalClearMembers(bool beingDestroyed) {
			if (this.members.Count > 0) {
				KeyValuePair<Character, IRoleMembership>[] oldMembers = new KeyValuePair<Character, IRoleMembership>[this.members.Count];
				((ICollection<KeyValuePair<Character, IRoleMembership>>) this.members).CopyTo(oldMembers, 0);
				this.members.Clear();
				foreach (KeyValuePair<Character, IRoleMembership> pair in oldMembers) {
					this.Trigger_MemberRemoved(pair.Key, pair.Value, beingDestroyed);
				}
			}
		}

		public interface IRoleMembership : IDisposable {
			Character Member { get;}
		}

		protected virtual IRoleMembership CreateMembershipObject(Character member) {
			return new RoleMembership(member, this);
		}

		#region triggers

		internal void Trigger_Create() {
			this.def.TryTrigger(this, RoleDef.tkCreate, null);
			try {
				this.On_Create();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal void Trigger_Destroy() {
			RolesManagement.UnassignAll(this, true);

			this.def.TryTrigger(this, RoleDef.tkDestroy, null);
			try {
				this.On_Destroy();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal DenyResultRoles Trigger_DenyAddMember(Character chr) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, null, this);
			bool cancel = this.def.TryCancellableTrigger(this, RoleDef.tkDenyAddMember, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				try {
					this.On_DenyAddMember(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return args.Result;
		}

		private void Trigger_MemberAdded(Character newMember, IRoleMembership membership) {
			this.def.TryTrigger(this, RoleDef.tkMemberAdded, new ScriptArgs(newMember, membership));
			try {
				this.On_MemberAdded(newMember, membership);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal DenyResultRoles Trigger_DenyRemoveMember(Character chr, IRoleMembership membership) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, membership, this);
			bool cancel = this.def.TryCancellableTrigger(this, RoleDef.tkDenyRemoveMember, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				try {
					this.On_DenyRemoveMember(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return args.Result;
		}

		internal void Trigger_MemberRemoved(Character chr, IRoleMembership membership, bool beingDestroyed) {
			this.def.TryTrigger(this, RoleDef.tkMemberRemoved, new ScriptArgs(chr, membership, beingDestroyed));
			try {
				this.On_MemberRemoved(chr, membership, beingDestroyed);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[Summary("Trigger called when the new role is created")]
		protected virtual void On_Create() {
		}

		[Summary("Trigger called when the role is destroyed")]
		protected virtual void On_Destroy() {
		}

		[Summary("Trigger called when the new member adding is requested from this role")]
		protected virtual bool On_DenyAddMember(DenyRoleTriggerArgs args) {
			return false; //dont cancel
		}

		[Summary("Trigger called when the new member is assigned to this role")]
		protected virtual void On_MemberAdded(Character newMember, IRoleMembership membership) {
			//this trigger will be run after @DenyAddMember
		}

		[Summary("Trigger called when the member remove is requested from this role")]
		protected virtual bool On_DenyRemoveMember(DenyRoleTriggerArgs args) {
			return false; //dont cancel
		}

		[Summary("Trigger called when the member is unassigned from this role")]
		protected virtual void On_MemberRemoved(Character exMember, IRoleMembership membership, bool beingDestroyed) {
			//this trigger will be run after @DenyRemoveMember
		}

		#endregion triggers

		public RoleDef RoleDef {
			get {
				return this.def;
			}
		}

		public RoleKey Key {
			get {
				return this.key;
			}
		}

		public string Name {
			get {
				return this.name;
			}
			set {
				this.name = value;
			}
		}

		public override string ToString() {
			return string.Concat(this.GetType().ToString(), " °", this.key.name);
		}

		public ICollection<Character> Members {
			get {
				return this.members.Keys;
			}
		}
		public ICollection<IRoleMembership> Memberships {
			get {
				return this.members.Values;
			}
		}

		public IRoleMembership GetMembership(Character ch) {
			IRoleMembership retVal;
			this.members.TryGetValue(ch, out retVal);
			return retVal;
		}

		internal bool IsMember(Character target) {
			if (target != null) {
				return this.members.ContainsKey(target);
			}
			return false;
		}

		protected override void On_DisposeManagedResources() {
			this.Trigger_Destroy();
			base.On_DisposeManagedResources();
		}

		#region persistence
		[Save]
		public virtual void Save(SaveStream output) {
			output.WriteValue("def", this.def);
			output.WriteValue("key", this.key);
			if (this.name != null) {
				output.WriteValue("name", this.name);
			}
			int count = this.members.Count;
			output.WriteValue("count", count);
			//int i = 0;
			foreach (IRoleMembership membership in this.members.Values) {
				//output.WriteValue(i.ToString(), membership);				
				//i++;
				ObjectSaver.Save(membership);
			}
		}

		[LoadSection]
		public static Role LoadSection(PropsSection input) {
			int currentLineNumber = input.headerLine;
			try {
				PropsLine pl = input.PopPropsLine("def");
				currentLineNumber = pl.line;
				RoleDef def = (RoleDef) ObjectSaver.OptimizedLoad_Script(pl.value);

				pl = input.PopPropsLine("key");
				currentLineNumber = pl.line;
				RoleKey key = (RoleKey) ObjectSaver.OptimizedLoad_SimpleType(pl.value, typeof(RoleKey));

				pl = input.PopPropsLine("count");
				currentLineNumber = pl.line;
				int count = ConvertTools.ParseInt32(pl.value);

				Role role = def.CreateWhenLoading(key);

				//for (int i = 0; i < count; i++) {
				//    pl = input.PopPropsLine(i.ToString());
				//    if (pl != null) {
				//        ObjectSaver.Load(pl.value, role.Load_RoleMembership, input.filename, pl.line);
				//    }
				//}

				foreach (PropsLine p in input.GetPropsLines()) {
					try {
						role.LoadLine(input.filename, p.line, p.name.ToLower(), p.value);
					} catch (FatalException) {
						throw;
					} catch (Exception ex) {
						Logger.WriteWarning(input.filename, p.line, ex);
					}
				}

				return role;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.filename, currentLineNumber, e);
			}
		}

		protected virtual void LoadLine(string filename, int line, string valueName, string valueString) {
			if (valueName.Equals("name", StringComparison.OrdinalIgnoreCase)) {
				this.name = (string) ObjectSaver.OptimizedLoad_String(valueString);
			}
			throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
		}

		#endregion persistence
	}
}
