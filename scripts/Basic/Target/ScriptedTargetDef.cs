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
using Shielded;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Parsing;
using SteamEngine.Scripting.Interpretation;
using SteamEngine.Transactionality;
using SteamEngine.UoData;

namespace SteamEngine.CompiledScripts {
	public sealed class ScriptedTargetDef : AbstractTargetDef {
		private readonly FieldValue message;

		private readonly Shielded<LScriptHolder> on_start = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_ground = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_item = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_char = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_thing = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_static = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_cancel = new Shielded<LScriptHolder>();
		private readonly Shielded<LScriptHolder> targon_point = new Shielded<LScriptHolder>();

		public ScriptedTargetDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.message = this.InitTypedField("message", "Target?", typeof(string));
		}

		public override void LoadScriptLines(PropsSection ps) {
			base.LoadScriptLines(ps);

			LoadTriggers(ps, this);
		}

		public string Message {
			get {
				return (string) this.message.CurrentValue;
			}
			set {
				this.message.CurrentValue = value;
			}
		}

		protected override bool AllowGround => ((this.targon_ground.Value != null) || (this.targon_point.Value != null));

		private static void LoadTriggers(PropsSection input, ScriptedTargetDef td) {
			Transaction.AssertInTransaction();

			var trigger_start = input.PopTrigger("start");
			if (trigger_start != null) {
				td.on_start.Value = new LScriptHolder(trigger_start);
			}

			var n = input.TriggerCount;
			var trigger_point = input.GetTrigger("targon_coords");
			if (trigger_point == null) {
				trigger_point = input.GetTrigger("targon_coordinates");
			}
			if (trigger_point == null) {
				trigger_point = input.GetTrigger("targon_point");
			}
			if (trigger_point != null) {
				if (n > 1) {
					Logger.WriteWarning(input.Filename, input.HeaderLine, "ScriptedTargetDef " + LogStr.Ident(input) + " has targon_point defined. All other triggers ignored.");
				}
				td.targon_point.Value = new LScriptHolder(trigger_point);
			} else {

				for (var i = 0; i < n; i++) {
					var trigger = input.GetTrigger(i);
					switch (trigger.TriggerName.ToLowerInvariant()) {
						case "targon_ground":
							td.targon_ground.Value = new LScriptHolder(trigger);
							break;
						case "targon_item":
							td.targon_item.Value = new LScriptHolder(trigger);
							break;
						case "targon_char":
						case "targon_character":
							td.targon_char.Value = new LScriptHolder(trigger);
							break;
						case "targon_thing":
							td.targon_thing.Value = new LScriptHolder(trigger);
							break;
						case "targon_static":
							td.targon_static.Value = new LScriptHolder(trigger);
							break;
						case "targon_cancel":
							td.targon_cancel.Value = new LScriptHolder(trigger);
							break;
						default:
							Logger.WriteWarning(trigger.Filename, trigger.StartLine, LogStr.Ident(trigger.TriggerName) + " is an invalid trigger name for a ScriptedTargetDef section. Ignored.");
							break;
					}
				}

				if ((td.targon_thing.Value != null) && (td.targon_item.Value != null)) {
					Logger.WriteWarning(input.Filename, input.HeaderLine, "ScriptedTargetDef " + LogStr.Ident(input) + " has both @targon_thing and @targon_item defined. @targon_item ignored.");
					td.targon_item.Value = null;
				}
				if ((td.targon_thing.Value != null) && (td.targon_char.Value != null)) {
					Logger.WriteWarning(input.Filename, input.HeaderLine, "ScriptedTargetDef " + LogStr.Ident(input) + " has both @targon_thing and @targon_char defined. @targon_char ignored.");
					td.targon_char.Value = null;
				}
			}
		}

		protected override void On_Start(Player ch, object parameter) {
			this.ThrowIfUnloaded();
			if (this.on_start.Value != null) {
				if (this.TryRunTrigger(this.on_start.Value, ch, parameter)) {
					return;
				}
			} else {
				var msg = this.Message;
				if (!string.IsNullOrEmpty(msg)) {
					ch.SysMessage(this.Message);
				}
			}
			base.On_Start(ch, parameter);
		}

		protected override void On_Targon(GameState state, IPoint3D getback, object parameter) {
			var player = state.Character as Player;
			if (player != null) {
				if (this.targon_point.Value != null) {
					if (this.TryRunTrigger(this.targon_point.Value, player, getback, parameter)) {
						this.On_Start(player, parameter);
					}
					return;
				}
				var targettedThing = getback as Thing;
				if (targettedThing != null) {
					if (this.targon_thing.Value != null) {
						if (this.TryRunTrigger(this.targon_thing.Value, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
					var targettedChar = getback as Character;
					if ((targettedChar != null) && (this.targon_char.Value != null)) {
						if (this.TryRunTrigger(this.targon_char.Value, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
					var targettedItem = getback as Item;
					if ((targettedItem != null) && (this.targon_item.Value != null)) {
						if (this.TryRunTrigger(this.targon_item.Value, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
				} else {
					var targettedStatic = getback as AbstractInternalItem;
					if (targettedStatic != null) {
						if (this.targon_static.Value != null) {
							if (this.TryRunTrigger(this.targon_static.Value, player, getback, parameter)) {
								this.On_Start(player, parameter);
							}
							return;
						}
						if (this.targon_ground.Value != null) {
							if (this.TryRunTrigger(this.targon_ground.Value, player, getback, parameter)) {
								this.On_Start(player, parameter);
							}
							return;
						}
					}
					if (this.targon_ground.Value != null) {
						if (this.TryRunTrigger(this.targon_ground.Value, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
				}
			}
			PacketSequences.SendClilocSysMessage(state.Conn, 1046439, 0);//That is not a valid target.
			this.On_Start(player, parameter);
		}

		protected override void On_TargonCancel(GameState state, object parameter) {
			var ch = state.Character;
			if (ch != null) {
				targon_cancel.Value?.TryRun(ch, parameter);
			}
		}

		private bool TryRunTrigger(LScriptHolder script, AbstractCharacter self, params object[] parameters) {
			var retVal = script.TryRun(self, parameters);
			try {
				var retInt = Convert.ToInt32(retVal);
				if (retInt == 1) {
					return true;
				}
			} catch (Exception) {
			}
			return false;
		}

		public override void Unload() {
			Transaction.AssertInTransaction();

			this.targon_ground.Value = null;
			this.targon_item.Value = null;
			this.targon_char.Value = null;
			this.targon_thing.Value = null;
			this.targon_static.Value = null;
			this.targon_cancel.Value = null;
			this.targon_point.Value = null;

			base.Unload();
		}
	}
}