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

using System.Collections.Generic;
using System.Linq;
using Shielded;
using SteamEngine.Common;
using SteamEngine.Scripting;
using SteamEngine.Scripting.Objects;


namespace SteamEngine {
	public abstract class AbstractDefTriggerGroupHolder : AbstractDef, ITriggerGroupHolder {

		//attention! this class does not (yet?) use the prevNode field on TGListNode, cos we don't need it here.
		private readonly ShieldedSeqNc<TriggerGroup> triggerGroups = new ShieldedSeqNc<TriggerGroup>(); //linked list of triggergroup references

		private static readonly ShieldedSeqNc<DelayedResolver> delayedLoaders = new ShieldedSeqNc<DelayedResolver>();

		protected AbstractDefTriggerGroupHolder(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			SeShield.AssertInTransaction();

			switch (param) {
				case "event":
				case "tevent":
				case "events":
				case "tevents":
				case "speech":
				case "tspeech":
				case "triggergroup":
				case "triggergroups":
					//case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE? NO!
					delayedLoaders.Add(new DelayedResolver(this, args, filename, line));
					break;
				default:
					base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
					break;
			}
		}

		// This is necessary, since when our ThingDef are being made, not all scripts may have been loaded yet. -SL
		private class DelayedResolver {
			private readonly AbstractDefTriggerGroupHolder cont;
			private readonly string filename;
			private readonly string tgName;
			private readonly int line;

			internal DelayedResolver(AbstractDefTriggerGroupHolder cont, string tgName, string filename, int line) {
				this.cont = cont;
				this.tgName = tgName;
				this.filename = filename;
				this.line = line;
			}

			internal void Resolve() {
				if (this.tgName != "0") {   //"0" means nothing
					var tg = TriggerGroup.GetByDefname(this.tgName);
					if (tg == null) {
						Logger.WriteWarning(LogStr.FileLine(this.filename, this.line) + "'" + LogStr.Ident(this.tgName) + "' is not a valid TriggerGroup (Event/Type).");
					} else {
						this.cont.AddTriggerGroup(tg);
					}
				}
			}
		}

		internal new static void LoadingFinished() {
			foreach (var loader in SeShield.InTransaction(delayedLoaders.ToList)) {
				SeShield.InTransaction(loader.Resolve);
			}

			SeShield.InTransaction(delayedLoaders.Clear);
		}

		public void AddTriggerGroup(TriggerGroup tg) {
			SeShield.AssertInTransaction();
			if (tg == null) return;

			if (!this.triggerGroups.Contains(tg)) {
				this.triggerGroups.Add(tg);
			}
		}

		public IEnumerable<TriggerGroup> GetAllTriggerGroups() {
			SeShield.AssertInTransaction();
			return this.triggerGroups;
		}

		public void RemoveTriggerGroup(TriggerGroup tg) {
			SeShield.AssertInTransaction();
			if (tg == null) return;
			this.triggerGroups.Remove(tg);
		}

		public bool HasTriggerGroup(TriggerGroup tg) {
			if (tg == null) return false;
			return this.triggerGroups.Contains(tg);
		}

		public void ClearTriggerGroups() {
			SeShield.AssertInTransaction();
			this.triggerGroups.Clear();
		}


		public override void Unload() {
			this.ClearTriggerGroups();
			base.Unload();
		}

		/// <summary>
		/// Triggers a trigger on this object, using the specified ScriptArgs
		/// </summary>
		/// <param name="tk">The TriggerKey for the trigger to call.</param>
		/// <param name="sa">The arguments (other than argv) for sphere scripts</param>
		public virtual void Trigger(TriggerKey tk, ScriptArgs sa) {
			SeShield.AssertInTransaction();
			foreach (var tg in this.triggerGroups) {
				tg.Run(this, tk, sa);
			}
		}

		public virtual void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			SeShield.AssertInTransaction();
			foreach (var tg in this.triggerGroups) {
				tg.TryRun(this, tk, sa);
			}
		}

		/// <summary>
		/// Executes the trigger, reads return values, and returns true if anything returned 1 (returning false otherwise).
		/// </summary>
		/// <param name="tk">The trigger to execute</param>
		/// <param name="sa">Arguments for scripts (argn, args, argo, argn1, argn2, etc). Can be null.</param>
		/// <returns>TriggerResult.Cancel if any called trigger scripts returned 1, TriggerResult.Continue otherwise.</returns>
		public virtual TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			SeShield.AssertInTransaction();
			foreach (var tg in this.triggerGroups) {
				if (TagMath.Is1(tg.Run(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		public virtual TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			SeShield.AssertInTransaction();
			foreach (var tg in this.triggerGroups) {
				if (TagMath.Is1(tg.TryRun(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		public void Trigger(TriggerKey tk, params object[] scriptArguments) {
			SeShield.AssertInTransaction();
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				this.Trigger(tk, new ScriptArgs(scriptArguments));
			} else {
				this.Trigger(tk, (ScriptArgs) null);
			}
		}

		public TriggerResult CancellableTrigger(TriggerKey tk, params object[] scriptArguments) {
			SeShield.AssertInTransaction();
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				return this.CancellableTrigger(tk, new ScriptArgs(scriptArguments));
			}
			return this.CancellableTrigger(tk, (ScriptArgs) null);
		}
	}
}
