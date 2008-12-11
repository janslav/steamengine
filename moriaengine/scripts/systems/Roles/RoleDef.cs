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
		internal static readonly TriggerKey tkDenyRemoveMember = TriggerKey.Get("DenyRemoveMember");
		internal static readonly TriggerKey tkDenyAddMember = TriggerKey.Get("DenyAddMember");

		//private static Dictionary<string, RoleDef> byName = new Dictionary<string, RoleDef>(StringComparer.OrdinalIgnoreCase);

		private static Dictionary<string, ConstructorInfo> roleDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		private TriggerGroup scriptedTriggers;

		[Summary("Method for instatiating Roles. Basic implementation is easy but the CreateImpl method should be overriden " +
				"in every RoleDef's descendant!")]
		public Role Create(RoleDef def, RoleKey key) {
			Role newRole = CreateImpl();
			newRole.RoleDef = def;
			newRole.Key = key;
			this.Trigger_Create(newRole);
			return newRole;
		}

		protected virtual Role CreateImpl() {
			return Pool<Role>.Acquire();
		}

		#region triggerMethods

		protected void Trigger_Create(Role role) {
			this.TryTrigger(role, RoleDef.tkCreate, null);
			try {
				role.On_Create();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		protected void Trigger_Destroy(Role role) {
			this.TryTrigger(role, RoleDef.tkDestroy, null);
			try {
				role.On_Destroy();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal DenyResultRoles Trigger_DenyAddMember(Character chr, Role role) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, role);
			bool cancel = this.TryCancellableTrigger(role, RoleDef.tkDenyAddMember, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				role.On_DenyAddMember(args);
			}
			return args.Result;
		}

		internal void Trigger_MemberAdded(Character chr, Role role) {
			TryTrigger(role, RoleDef.tkMemberAdded, new ScriptArgs(chr));
			role.On_MemberAdded(chr);
		}

		internal DenyResultRoles Trigger_DenyRemoveMember(Character chr, Role role) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, role);
			bool cancel = this.TryCancellableTrigger(role, RoleDef.tkDenyRemoveMember, args);
			if (!cancel) {//not cancelled (no return 1 in LScript), lets continue
				role.On_DenyRemoveMember(args);
			}
			return args.Result;
		}

		internal void Trigger_MemberRemoved(Character chr, Role role) {
			this.TryTrigger(role, RoleDef.tkMemberRemoved, new ScriptArgs(chr));
			role.On_MemberRemoved(chr);
		}

		#endregion triggerMethods

		public static RoleDef ByDefname(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as RoleDef;
		}

		//public static RoleDef ByName(string key) {
		//    RoleDef retVal;
		//    byName.TryGetValue(key, out retVal);
		//    return retVal;
		//}

		//public static int RolesCount {
		//    get {
		//        return byName.Count;
		//    }
		//}

		#region Loading from scripts

		private static void RegisterRoleDef(RoleDef rd) {
			byDefname[rd.Defname] = rd;
			//byName[rd.Name] = rd;
		}

		private static void UnRegisterRoleDef(RoleDef rd) {
			byDefname.Remove(rd.Defname);
			//byName.Remove(rd.Name);
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

		internal static IUnloadable LoadFromScripts(PropsSection input) {
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
				input.headerName = "t__" + input.headerName + "__"; //naming of the trigger group for @assign, unassign etc. triggers
				roleDef.scriptedTriggers = ScriptedTriggerGroup.Load(input);
			} else {
				roleDef.scriptedTriggers = null;
			}

			roleDef.LoadScriptLines(input);

			RegisterRoleDef(roleDef);

			if (roleDef.scriptedTriggers == null) {
				return roleDef;
			} else {
				return new UnloadableGroup(roleDef, roleDef.scriptedTriggers);
			}
		}

		internal static void LoadingFinished() {

		}
		#endregion Loading from scripts

		//private FieldValue name; //logical name of the ability

		public RoleDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//name = InitField_Typed("name", "", typeof(string));
		}

		//public string Name {
		//    get {
		//        return (string) name.CurrentValue;
		//    }
		//}

		public bool TryCancellableTrigger(Role role, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				object retVal = this.scriptedTriggers.TryRun(role, td, sa);
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

		public void TryTrigger(Role role, TriggerKey td, ScriptArgs sa) {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(role, td, sa);
			}
		}

		public override string ToString() {
			return this.GetType().Name + " " + this.Defname;
		}

		#region utilities
		[Summary("Return enumerable containing all roles (copying the values from the main dictionary)")]
		public static IEnumerable<RoleDef> AllRoles {
			get {
				foreach (AbstractScript script in AllScripts) {
					RoleDef roleDef = script as RoleDef;
					if (roleDef != null) {
						yield return roleDef;
					}
				}
			}
		}
		#endregion utilities
	}

	[Summary("Argument wrapper used in DenyMemberAddRequest trigger")]
	public class DenyRoleTriggerArgs : ScriptArgs {
		public readonly Character assignee;
		public readonly Role role;

		public DenyRoleTriggerArgs(params object[] argv)
			: base(argv) {
			Sanity.IfTrueThrow(!(argv[0] is DenyResultRoles), "argv[0] is not DenyResultRoles");
		}

		public DenyRoleTriggerArgs(Character assignee, Role role)
			: this(DenyResultRoles.Allow, assignee, role) {
			this.assignee = assignee;
			this.role = role;
		}

		public DenyResultRoles Result {
			get {
				return (DenyResultRoles) Convert.ToInt32(argv[0]);
			}
			set {
				argv[0] = value;
			}
		}
	}
}
