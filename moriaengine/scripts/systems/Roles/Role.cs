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
	public class Role : Disposable {
        private RoleDef def;
		private RoleKey key;
		private string name;

		private List<AbstractCharacter> members;
		private System.Collections.ObjectModel.ReadOnlyCollection<AbstractCharacter> membersReadOnly;

		internal Role(RoleDef def, RoleKey key) {
			members = new List<AbstractCharacter>();
			membersReadOnly = new System.Collections.ObjectModel.ReadOnlyCollection<AbstractCharacter>(this.members);
			this.key = key;
			this.def = def;
        }

		internal void InternalAddMember(AbstractCharacter newMember) {
			this.members.Add(newMember);
			this.Trigger_MemberAdded((Character) newMember);
		}

		internal void InternalRemoveMember(AbstractCharacter newMember) {
			this.members.Remove(newMember);
			this.Trigger_MemberRemoved((Character) newMember, false);
		}

		internal void InternalClearMembers(bool beingDestroyed) {
			if (this.members.Count > 0) {
				AbstractCharacter[] oldMembers = this.members.ToArray();
				this.members.Clear();
				foreach (Character ch in oldMembers) {
					this.Trigger_MemberRemoved(ch, beingDestroyed);
				}
			}
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
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, this);
			bool cancel = this.def.TryCancellableTrigger(this, RoleDef.tkDenyAddMember, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				try {
					this.On_DenyAddMember(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return args.Result;
		}

		internal void Trigger_MemberAdded(Character chr) {
			this.def.TryTrigger(this, RoleDef.tkMemberAdded, new ScriptArgs(chr));
			try {
				this.On_MemberAdded(chr);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal DenyResultRoles Trigger_DenyRemoveMember(Character chr) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, this);
			bool cancel = this.def.TryCancellableTrigger(this, RoleDef.tkDenyRemoveMember, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				try {
					this.On_DenyRemoveMember(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return args.Result;
		}

		internal void Trigger_MemberRemoved(Character chr, bool beingDestroyed) {
			this.def.TryTrigger(this, RoleDef.tkMemberRemoved, new ScriptArgs(chr, beingDestroyed));
			try {
				this.On_MemberRemoved(chr, beingDestroyed);
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
		protected virtual void On_MemberAdded(AbstractCharacter newMember) {
			//this trigger will be run after @DenyAddMember
        }

		[Summary("Trigger called when the member remove is requested from this role")]
		protected virtual bool On_DenyRemoveMember(DenyRoleTriggerArgs args) {
			return false; //dont cancel
		}   
		
		[Summary("Trigger called when the member is unassigned from this role")]
		protected virtual void On_MemberRemoved(AbstractCharacter exMember, bool beingDestroyed) {
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

		public System.Collections.ObjectModel.ReadOnlyCollection<AbstractCharacter> Members {
			get {
				return this.membersReadOnly;
			}
		}

		internal bool IsMember(AbstractCharacter target) {
			if (target != null) {
				return this.members.Contains(target);
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
			for (int i = 0; i < count; i++) {
				output.WriteValue(i.ToString(), this.members[i]);
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

				for (int i = 0; i < count; i++) {
					pl = input.PopPropsLine(i.ToString());
					if (pl != null) {
						ObjectSaver.Load(pl.value, role.Load_RoleMember, input.filename, pl.line);
					}
				}

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

		public virtual void LoadLine(string filename, int line, string valueName, string valueString) {
			if (valueName.Equals("name", StringComparison.OrdinalIgnoreCase)) {
				this.name = (string) ObjectSaver.OptimizedLoad_String(valueString);
			}
		}

		private void Load_RoleMember(object resolvedObject, string filename, int line) {
			Character loaded = (Character) resolvedObject;
			RolesManagement.InternalAddLoadedRole(this, loaded);
			this.members.Add(loaded);
		}
		#endregion persistence
	}
}		
