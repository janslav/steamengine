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
using Shielded;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Parsing;
using SteamEngine.Scripting.Interpretation;

namespace SteamEngine.CompiledScripts {
	public sealed class ScriptedMenuDef : AbstractMenuDef {
		private readonly FieldValue message;

		private readonly Shielded<LScriptHolder[]> triggers = new Shielded<LScriptHolder[]>();

		private readonly Shielded<NumberedLocStringCollection[]> choiceLists = new Shielded<NumberedLocStringCollection[]>();

		public ScriptedMenuDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.message = this.InitTypedField("message", "Menu?", typeof(string));
		}

		public override void LoadScriptLines(PropsSection ps) {
			SeShield.AssertInTransaction();

			base.LoadScriptLines(ps);

			var n = ps.TriggerCount;
			var locEntries = new string[n + 1];
			locEntries[0] = (string) this.message.CurrentValue;


			var lScriptHolders = new LScriptHolder[n];
			this.triggers.Value = lScriptHolders;

			for (var i = 0; i < n; i++) {
				var ts = ps.GetTrigger(i);
				lScriptHolders[i] = new LScriptHolder(ts);
				locEntries[i + 1] = ts.TriggerName;
			}

			var langCount = Tools.GetEnumLength<Language>();
			var collections = new NumberedLocStringCollection[langCount];
			this.choiceLists.Value = collections;
			var locdefname = "loc_" + ps.HeaderName;
			for (var i = 0; i < langCount; i++) {
				collections[i] = new NumberedLocStringCollection(locdefname, "LScript", (Language) i, locEntries);
			}
		}

		protected override IEnumerable<string> GetAllTexts(Language language) {
			return this.choiceLists.Value[(int) language].Entries;
		}

		public override void Unload() {
			SeShield.AssertInTransaction();

			if (this.choiceLists.Value != null) {
				foreach (var loc in this.choiceLists.Value) {
					LocManager.UnregisterLoc(loc);
				}
				this.choiceLists.Value = null;

				foreach (var trg in this.triggers.Value) {
					trg.Unload();
				}
				this.triggers.Value = null;
			}
			base.Unload();
		}

		protected override void On_Response(GameState state, int index, object parameter) {
			this.ThrowIfUnloaded();

			var scp = this.triggers.Value[index];
			if (scp != null) {
				var self = state.Character;
				if (self != null) {
					scp.TryRun(self, parameter);
				}
			} else {
				Logger.WriteWarning("Unknown trigger number '" + index + "' in MenuDef " + this.PrettyDefname);
			}
		}

		protected override void On_Cancel(GameState state, object parameter) {
			//TODO?			
		}
	}
}