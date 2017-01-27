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
using System.Diagnostics.CodeAnalysis;
using Shielded;
using SteamEngine.Common;

namespace SteamEngine.Scripting.Objects {

	//this is the class to be overriden in C# scripts
	//its subclasses (in scripts) are instantiated by ClassManager class

	public enum GumpButtonType {
		Page = 0,
		Reply = 1
	}

	public abstract class CompiledGumpDef : GumpDef {
		protected CompiledGumpDef() {
		}

		protected CompiledGumpDef(string defName)
			: base(defName) {
		}

		protected override string InternalFirstGetDefname() {
			return this.GetType().Name;
		}

		//not sure why this was here... let's find out
		//public override void Unload() {
		//}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal override Gump InternalConstruct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			var gi = new CompiledGump(this) {
				InputArgs = args ?? new DialogArgs()
			};

			try {
				this.Construct(gi, focus, sendTo, args);
				gi.FinishCompilingPacketData(focus, sendTo);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
			return null;
		}

		public abstract void Construct(CompiledGump gi, Thing focus, AbstractCharacter sendTo, DialogArgs args);
		public abstract void OnResponse(CompiledGump gi, Thing focus, GumpResponse gr, DialogArgs args);

		public override string ToString() {
			return "CompiledGumpDef " + this.Defname;
		}
	}

	public class GumpResponse {
		private readonly int[] selectedSwitches;
		private readonly ResponseText[] responseTexts;
		private readonly ResponseNumber[] responseNumbers;

		public GumpResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers) {
			this.PressedButton = pressedButton;
			this.selectedSwitches = selectedSwitches;
			this.responseTexts = responseTexts;
			this.responseNumbers = responseNumbers;
		}

		public int PressedButton { get; }

		public bool IsSwitched(int id) {
			for (int i = 0, n = this.selectedSwitches.Length; i < n; i++) {
				if (this.selectedSwitches[i] == id) {
					return true;
				}
			}
			return false;
		}

		public string GetTextResponse(int id) {
			for (int i = 0, n = this.responseTexts.Length; i < n; i++) {
				ResponseText rt = this.responseTexts[i];
				if (rt.Id == id) {
					return rt.Text;
				}
			}
			return "";
		}

		public decimal GetNumberResponse(int id) {
			for (int i = 0, n = this.responseNumbers.Length; i < n; i++) {
				ResponseNumber rn = this.responseNumbers[i];
				if ((rn != null) && (rn.Id == id)) {
					return rn.Number;
				}
			}
			return 0;
		}
	}
}