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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts {

	public abstract class AbstractTargetDef : AbstractDef {
		internal AbstractTargetDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static new AbstractTargetDef Get(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as AbstractTargetDef;
		}

		internal void Assign(Character ch) {
			On_Start(ch, null);
		}

		internal void Assign(Character ch, object parameter) {
			On_Start(ch, parameter);
		}

		virtual protected void On_Start(Character ch, object parameter) {
			GameConn conn = ch.Conn;
			if (conn != null) {
				conn.Target(this.AllowGround, On_Targon, On_TargonCancel, parameter);
			}
		}

		abstract protected bool AllowGround { get; }

		abstract protected void On_Targon(GameConn conn, IPoint3D getback, object parameter);

		abstract protected void On_TargonCancel(GameConn conn, object parameter);
	}

	public abstract class CompiledTargetDef : AbstractTargetDef {
		private bool allowGround;

		public CompiledTargetDef()
			: base(null, "Target.cs", -1) {

			Type type = this.GetType();
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo mi in methods) {
				if (mi.Name.Equals("On_TargonGround")) {
					this.allowGround = true;
					break;
				}
				if (mi.Name.Equals("On_TargonPoint")) {
					this.allowGround = true;
					break;
				}
			}
		}

		protected override sealed bool AllowGround { get {
			return this.allowGround; 
		} }

		protected override string GetName() {
			return this.GetType().Name;
		}

		protected sealed override void On_Targon(GameConn conn, IPoint3D getback, object parameter) {
			Character self = conn.CurCharacter as Character;
			if (self != null) {
				if (On_TargonPoint(self, getback, parameter)) {
					On_Start(self, parameter);
				}
			}
		}

		protected sealed override void On_TargonCancel(GameConn conn, object parameter) {
			Character self = conn.CurCharacter as Character; 
			if (self != null) {
				On_TargonCancel(self, parameter);
			}
		}

		protected virtual void On_TargonCancel(Character self, object parameter) {
		}

		protected virtual bool On_TargonPoint(Character self, IPoint3D targetted, object parameter) {
			Thing thing = targetted as Thing;
			if (thing != null) {
				return On_TargonThing(self, thing, parameter);
			}
			Static s = targetted as Static;
			if (s != null) {
				return On_TargonStatic(self, s, parameter);
			}
			return On_TargonGround(self, targetted, parameter);
		}

		protected virtual bool On_TargonThing(Character self, Thing targetted, object parameter) {
			Character ch = targetted as Character;
			if (ch != null) {
				return On_TargonChar(self, ch, parameter);
			}
			Item item = targetted as Item;
			if (item != null) {
				return On_TargonItem(self, item, parameter);
			}
			return true;//item nor char? huh?
		}

		protected virtual bool On_TargonChar(Character self, Character targetted, object parameter) {
			self.ClilocSysMessage(1046439, 0);//That is not a valid target.
			return true;
		}

		protected virtual bool On_TargonItem(Character self, Item targetted, object parameter) {
			self.ClilocSysMessage(1046439, 0);//That is not a valid target.
			return true;
		}

		protected virtual bool On_TargonStatic(Character self, Static targetted, object parameter) {
			return On_TargonGround(self, targetted, parameter);
		}

		protected virtual bool On_TargonGround(Character self, IPoint3D targetted, object parameter) {
			self.ClilocSysMessage(1046439, 0);//That is not a valid target.
			return true;
		}
	}

	public sealed class ScriptedTargetDef : AbstractTargetDef {
		private FieldValue message;

		private LScriptHolder on_start;
		private LScriptHolder targon_ground;
		private LScriptHolder targon_item;
		private LScriptHolder targon_char;
		private LScriptHolder targon_thing;
		private LScriptHolder targon_static;
		private LScriptHolder targon_cancel;
		private LScriptHolder targon_point;

		internal ScriptedTargetDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.message = this.InitField_Typed("message", "Target?", typeof(string));
		}

		public string Message {
			get {
				return (string) message.CurrentValue;
			}
			set {
				message.CurrentValue = value;
			}
		}

		protected override sealed bool AllowGround { get {
			return ((targon_ground != null) || (targon_point != null)); 
		} }

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			string typeName = input.headerType.ToLower();
			string defname = input.headerName.ToLower();

			AbstractScript def;
			byDefname.TryGetValue(defname, out def);
			ScriptedTargetDef td = def as ScriptedTargetDef;
			if (td == null) {
				if (def != null) {//it isnt ScriptedTargetDef
					throw new OverrideNotAllowedException("ScriptedTargetDef "+LogStr.Ident(defname)+" has the same name as "+LogStr.Ident(def)+". Ignoring.");
				} else {
					td = new ScriptedTargetDef(defname, input.filename, input.headerLine);
				}
			} else if (td.unloaded) {
				td.unloaded = false;
				UnRegisterScriptedTargetDef(td);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("TemplateDef "+LogStr.Ident(defname)+" defined multiple times.");
			}

			TriggerSection trigger_start = input.PopTrigger("start");
			if (trigger_start != null) {
				td.on_start = new LScriptHolder(trigger_start);
			}

			int n = input.TriggerCount;
			TriggerSection trigger_point = input.GetTrigger("targon_coords");
			if (trigger_point == null) {
				trigger_point = input.GetTrigger("targon_coordinates");
			}
			if (trigger_point == null) {
				trigger_point = input.GetTrigger("targon_point");
			}
			if (trigger_point != null) {
				if (n > 1) {
					Logger.WriteWarning(input.filename, input.headerLine, "ScriptedTargetDef "+LogStr.Ident(input)+" has targon_point defined. All other triggers ignored.");
				}
				td.targon_point = new LScriptHolder(trigger_point);
			} else {

				for (int i = 0; i<n; i++) {
					TriggerSection trigger = input.GetTrigger(i);
					switch (trigger.triggerName.ToLower()) {
						case "targon_ground":
							td.targon_ground = new LScriptHolder(trigger);
							break;
						case "targon_item":
							td.targon_item = new LScriptHolder(trigger);
							break;
						case "targon_char":
						case "targon_character":
							td.targon_char = new LScriptHolder(trigger);
							break;
						case "targon_thing":
							td.targon_thing = new LScriptHolder(trigger);
							break;
						case "targon_static":
							td.targon_static = new LScriptHolder(trigger);
							break;
						case "targon_cancel":
							td.targon_cancel = new LScriptHolder(trigger);
							break;
						default:
							Logger.WriteWarning(trigger.filename, trigger.startline, LogStr.Ident(trigger.triggerName)+" is an invalid trigger name for a ScriptedTargetDef section. Ignored.");
							break;
					}
				}

				if ((td.targon_thing != null) && (td.targon_item != null)) {
					Logger.WriteWarning(input.filename, input.headerLine, "ScriptedTargetDef "+LogStr.Ident(input)+" has both @targon_thing and @targon_item defined. @targon_item ignored.");
					td.targon_item = null;
				}
				if ((td.targon_thing != null) && (td.targon_char != null)) {
					Logger.WriteWarning(input.filename, input.headerLine, "ScriptedTargetDef "+LogStr.Ident(input)+" has both @targon_thing and @targon_char defined. @targon_char ignored.");
					td.targon_char = null;
				}
			}

			RegisterScriptedTargetDef(td);

			td.LoadScriptLines(input);
			return td;
		}

		private static void UnRegisterScriptedTargetDef(ScriptedTargetDef td) {
			byDefname.Remove(td.Defname);
			if (td.altdefname != null) {
				byDefname.Remove(td.altdefname);
			}
		}

		private static void RegisterScriptedTargetDef(ScriptedTargetDef td) {
			byDefname[td.Defname] = td;
			if (td.altdefname != null) {
				byDefname[td.altdefname] = td;
			}
		}

		public static void Bootstrap() {
			ScriptLoader.RegisterScriptType(new string[] { "ScriptedTargetDef", "TargetDef" },
				LoadFromScripts, false);
		}

		protected override sealed void On_Start(Character ch, object parameter) {
			ThrowIfUnloaded();
			if (on_start != null) {
				if (TryRunTrigger(on_start, ch, parameter)) {
					return;
				}
			} else {
				string msg = this.Message;
				if (!string.IsNullOrEmpty(msg)) {
					ch.SysMessage(this.Message);
				}
			}
			base.On_Start(ch, parameter);
		}

		protected override sealed void On_Targon(GameConn conn, IPoint3D getback, object parameter) {
			Player player = conn.CurCharacter as Player;
			if (player != null) {
				if (targon_point != null) {
					if (TryRunTrigger(targon_point, player, getback, parameter)) {
						On_Start(player, parameter);
					}
					return;
				} else {
					Thing targettedThing = getback as Thing;
					if (targettedThing != null) {
						if (targon_thing != null) {
							if (TryRunTrigger(targon_thing, player, getback, parameter)) {
								On_Start(player, parameter);
							}
							return;
						} else {
							Character targettedChar = getback as Character;
							if ((targettedChar != null) && (targon_char != null)) {
								if (TryRunTrigger(targon_char, player, getback, parameter)) {
									On_Start(player, parameter);
								}
								return;
							}
							Item targettedItem = getback as Item;
							if ((targettedItem != null) && (targon_item != null)) {
								if (TryRunTrigger(targon_item, player, getback, parameter)) {
									On_Start(player, parameter);
								}
								return;
							}
						}
					} else {
						Static targettedStatic = getback as Static;
						if (targettedStatic != null) {
							if (targon_static != null) {
								if (TryRunTrigger(targon_static, player, getback, parameter)) {
									On_Start(player, parameter);
								}
								return;
							} else if (targon_ground != null) {
								if (TryRunTrigger(targon_ground, player, getback, parameter)) {
									On_Start(player, parameter);
								}
								return;
							}
						} 
						if (targon_ground != null) {
							if (TryRunTrigger(targon_ground, player, getback, parameter)) {
								On_Start(player, parameter);
							}
							return;
						}
					}
				}
			}
			Server.SendClilocSysMessage(conn, 1046439, 0);//That is not a valid target.
			On_Start(player, parameter);
		}

		protected override sealed void On_TargonCancel(GameConn conn, object parameter) {
			AbstractCharacter ch = conn.CurCharacter;
			if ((ch != null) && (targon_cancel != null)) {
				targon_cancel.TryRun(ch, parameter);
			}
		}

		private bool TryRunTrigger(LScriptHolder script, AbstractCharacter self, params object[] parameters) {
			object retVal = script.Run(self, parameters);
			try {
				int retInt = Convert.ToInt32(retVal);
				if (retInt == 1) {
					return true;
				}
			} catch (Exception) {
			}
			return false;
		}

		public override void Unload() {
			targon_ground = null;
			targon_item = null;
			targon_char = null;
			targon_thing = null;
			targon_static = null;
			targon_cancel = null;
			targon_point = null;

			base.Unload();
		}
	}
}