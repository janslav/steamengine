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
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class RoleDef : AbstractDef {
		internal static readonly TriggerKey tkMemberAdded = TriggerKey.Get("MemberAdded");
		internal static readonly TriggerKey tkMemberRemoved = TriggerKey.Get("MemberRemoved");
		internal static readonly TriggerKey tkCreate = TriggerKey.Get("Create");
        internal static readonly TriggerKey tkDestroy = TriggerKey.Get("Destroy");
        internal static readonly TriggerKey tkDenyMemberRemoveRequest = TriggerKey.Get("DenyMemberRemoveRequest");
        internal static readonly TriggerKey tkDenyMemberAddRequest = TriggerKey.Get("DenyMemberAddRequest");

		private static Dictionary<string, RoleDef> byName = new Dictionary<string, RoleDef>(StringComparer.OrdinalIgnoreCase);
		
		private static Dictionary<string, ConstructorInfo> roleDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

        //asi nebude potreba...
        private TriggerGroup scriptedTriggers;

        [Summary("Method for instatiating Roles. Basic implementation is easy but the CreateImpl method should be overriden "+
                "in every RoleDef's descendant!")]
        public virtual Role Create(string name) {
            Role newRole = CreateImpl(name);
            Trigger_Create(newRole);
            return newRole;
        }

        protected virtual Role CreateImpl(string name) {
            return new Role(this, name);
        }

        #region triggerMethods
        protected virtual void On_Create(Character chr, Role role) {
            chr.SysMessage("Role " + Name + " nemá implementaci trigger metody On_Create");            
        }

        protected void Trigger_Create(Role role) {
            TryTrigger(Globals.SrcCharacter, RoleDef.tkCreate, null);
            role.On_RoleCreate(this);
            On_Create((Character)Globals.SrcCharacter, role);            
        }

        protected virtual bool On_Destroy(Character chr, Role role) {
            chr.SysMessage("Role " + Name + " nemá implementaci trigger metody On_Destroy");
            return false; //no cancelling
        }

        protected bool Trigger_Destroy(Character chr, Role role) {
            bool cancel = false;
            cancel = TryCancellableTrigger(chr, RoleDef.tkDestroy, null);
            if (!cancel) {
                cancel = role.On_RoleDestroy(this);
                if (!cancel) {//still not cancelled
                    cancel = On_Destroy(chr, role);
                }
            }
            return cancel;
        }        

        protected virtual void On_MemberAdded(Character chr, Role role) {
            chr.SysMessage("Role " + Name + " nemá implementaci trigger metody On_MemberAdded");            
        }

        internal void Trigger_MemberAdded(Character chr, Role role) {
            TryTrigger(chr, RoleDef.tkMemberAdded, null);
            role.On_RoleMemberAdded(this, chr);
            On_MemberAdded(chr, role);            
        }

        protected virtual bool On_DenyMemberAddRequest(DenyRoleMemberAddArgs args) {
            Role rle = args.assgdRole;
            //common check - what if the member already has the role?
            if (args.assignee.HasRole(rle)) {
                args.Result = DenyResultRolesMemberAdd.Deny_AlreadyHasRole;
                return true;//same as "return 1" from LScript - cancel trigger sequence
            }
            return false; //continue
        }

        internal bool Trigger_DenyMemberAddRequest(DenyRoleMemberAddArgs args) {
            bool cancel = false;
            cancel = this.TryCancellableTrigger(args.assignee, RoleDef.tkDenyMemberAddRequest, args);
            if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
                cancel = args.assgdRole.On_RoleDenyMemberAddRequest(args);
                if (!cancel) {//still not cancelled
                    cancel = On_DenyMemberAddRequest(args);
                }
            }
            return cancel;
        }

        protected virtual void On_MemberRemoved(Character chr, Role role) {
            chr.SysMessage("Role " + Name + " nemá implementaci trigger metody On_MemberRemoved");            
        }

        internal void Trigger_MemberRemoved(Character chr, Role role) {
            //not cancellable trigger - it is run after the DenyMemberRemoveRequest
            if(chr != null && role != null) {
                TryTrigger(chr, RoleDef.tkMemberRemoved, null);
                role.On_RoleMemberRemoved(this, chr);
                On_MemberRemoved(chr, role);
            }
        }

        protected virtual bool On_DenyMemberRemoveRequest(DenyRoleMemberRemoveArgs args) {
            Role rle = args.assgdRole;
            //common check - does the member have the role?
            if (!args.assignee.HasRole(rle)){
                args.Result = DenyResultRolesMemberRemove.Deny_DoesntHaveRole;
                return true;//same as "return 1" from LScript - cancel trigger sequence
            }           
            return false; //continue
        }

        internal bool Trigger_DenyMemberRemoveRequest(DenyRoleMemberRemoveArgs args) {
            bool cancel = false;
            cancel = this.TryCancellableTrigger(args.assignee, RoleDef.tkDenyMemberRemoveRequest, args);
            if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
                cancel = args.assgdRole.On_RoleDenyMemberRemoveRequest(args);
                if (!cancel) {//still not cancelled
                    cancel = On_DenyMemberRemoveRequest(args);
                }
            }
            return cancel;
        }
        #endregion triggerMethods

		public static RoleDef ByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as RoleDef;
		}

		public static RoleDef ByName(string key) {
			RoleDef retVal;
			byName.TryGetValue(key, out retVal);
			return retVal;
		}

		public static int RolesCount {
			get {
				return byName.Count;
			}
		}

		public static void RegisterRoleDef(RoleDef rd) {
			byDefname[rd.Defname] = rd;
			byName[rd.Name] = rd;
		}

		public static void UnRegisterRoleDef(RoleDef rd) {
			byDefname.Remove(rd.Defname);
			byName.Remove(rd.Name);
		}

		internal static void UnloadScripts() {
			byName.Clear();
			roleDefCtorsByName.Clear();
		}

		public static new void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<RoleDef>(RegisterRoleDefType);
		}

		//for loading of roledefs from .scp scripts
		public static new bool ExistsDefType(string name) {
			return roleDefCtorsByName.ContainsKey(name);
		}

		private static Type[] roleDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//called by ClassManager
		internal static bool RegisterRoleDefType(Type roleDefType) {
			ConstructorInfo ci;
			if (roleDefCtorsByName.TryGetValue(roleDefType.Name, out ci)) { //we have already a RoleDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) + " in the register of RoleDef classes.");
			}
			ci = roleDefType.GetConstructor(roleDefConstructorParamTypes);
			if (ci == null) {
				throw new Exception("Proper constructor not found.");
			}
			roleDefCtorsByName[roleDefType.Name] = MemberWrapper.GetWrapperFor(ci);

			ScriptLoader.RegisterScriptType(roleDefType.Name, LoadFromScripts, false);

			return false;
		}


		internal static void StartingLoading() {
		}

		internal static RoleDef LoadFromScripts(PropsSection input) {
			//it is something like this in the .scp file: [headerType headerName] = [RoleDef ro_starosta] etc.
			string typeName = input.headerType.ToLower();
			string roleDefName = input.headerName.ToLower();

			AbstractScript def;
			byDefname.TryGetValue(roleDefName, out def);
			RoleDef roleDef = def as RoleDef;

			ConstructorInfo constructor = roleDefCtorsByName[typeName];

			if (roleDef == null) {
				if (def != null) {//it isnt roleDef
					throw new ScriptException("RoleDef " + LogStr.Ident(roleDefName) + " has the same name as " + LogStr.Ident(def));
				} else {
					object[] cargs = new object[] { roleDefName, input.filename, input.headerLine };
					roleDef = (RoleDef) constructor.Invoke(cargs);
				}
			} else if (roleDef.unloaded) {
				if (roleDef.GetType() != constructor.DeclaringType) {
					throw new OverrideNotAllowedException("You can not change the class of a RoleDef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				roleDef.unloaded = false;
				//we have to load the name first, so that it may be unloaded by it...

				PropsLine p = input.PopPropsLine("name");
				roleDef.LoadScriptLine(input.filename, p.line, p.name.ToLower(), p.value);

				UnRegisterRoleDef(roleDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("RoleDef " + LogStr.Ident(roleDefName) + " defined multiple times.");
			}

			//now do load the trigger code. 
			//possibly will not be used until we decide to widen the roledef's functionality
            if (input.TriggerCount > 0) {
                input.headerName = "t__" + input.headerName + "__"; //naming of the trigger group for @assign, unassign etd. triggers
                roleDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
            } else {
                roleDef.scriptedTriggers = null;
            }

			roleDef.LoadScriptLines(input);

			RegisterRoleDef(roleDef);

            return roleDef;
		}

		internal static void LoadingFinished() {

		}

		private FieldValue name; //logical name of the ability
		
		public RoleDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			name = InitField_Typed("name", "", typeof(string));			
		}

		public string Name {
			get {
				return (string) name.CurrentValue;
			}
		}

		public bool TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				object retVal = this.scriptedTriggers.TryRun(self, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return false;
		}

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);
		}

		public override string ToString() {
			return GetType().Name + " " + Name;
		}

		#region utilities
		[Summary("Return enumerable containing all roles (copying the values from the main dictionary)")]
		public static IEnumerable<RoleDef> AllRoles {
			get {
				if (byName != null) {
					return byName.Values;
				} else {
					return null;
				}
			}
		}		
		#endregion utilities
	}

    [Summary("Argument wrapper used in DenyMemberRemoveRequest trigger")]
	public class DenyRoleMemberRemoveArgs : ScriptArgs {
	    public readonly Character assignee;
	    public readonly RoleDef runRoleDef;
	    public readonly Role assgdRole;

	    public DenyRoleMemberRemoveArgs(params object[] argv)
	        : base(argv) {
            Sanity.IfTrueThrow(!(argv[0] is DenyResultRolesMemberRemove), "argv[0] is not DenyResultRolesMemberRemove");
	    }

        public DenyRoleMemberRemoveArgs(Character assignee, Role assgdRole)
            : this(DenyResultRolesMemberRemove.Allow, assignee, assgdRole.RoleDef, assgdRole) {
            this.assignee = assignee;
            this.runRoleDef = assgdRole.RoleDef;
            this.assgdRole = assgdRole;
	    }

        public DenyResultRolesMemberRemove Result {
	        get {
                return (DenyResultRolesMemberRemove)Convert.ToInt32(argv[0]);
	        }
	        set {
	            argv[0] = value;
	        }
	    }
	}

    [Summary("Argument wrapper used in DenyMemberAddRequest trigger")]	
    public class DenyRoleMemberAddArgs : ScriptArgs {
        public readonly Character assignee;
        public readonly RoleDef runRoleDef;
        public readonly Role assgdRole;

        public DenyRoleMemberAddArgs(params object[] argv)
            : base(argv) {
            Sanity.IfTrueThrow(!(argv[0] is DenyResultRolesMemberAdd), "argv[0] is not DenyResultRolesMemberAdd");
        }

        public DenyRoleMemberAddArgs(Character assignee, Role assgdRole)
                    : this(DenyResultRolesMemberAdd.Allow, assignee, assgdRole.RoleDef, assgdRole) {
            this.assignee = assignee;
            this.runRoleDef = assgdRole.RoleDef;
            this.assgdRole = assgdRole;
        }

        public DenyResultRolesMemberAdd Result {
            get {
                return (DenyResultRolesMemberAdd)Convert.ToInt32(argv[0]);
            }
            set {
                argv[0] = value;
            }
        }
    }
}
