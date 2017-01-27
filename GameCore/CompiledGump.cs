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
using SteamEngine.Scripting.Objects;

namespace SteamEngine
{
	public sealed class CompiledGump : Gump {
		public CompiledGump(CompiledGumpDef def)
			: base(def) {
		}
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public override void OnResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers) {
			CompiledGumpDef gdef = (CompiledGumpDef) this.Def;
			gdef.gumpInstance = this;
			try {
				gdef.OnResponse(this, new GumpResponse(pressedButton, selectedSwitches, responseTexts, responseNumbers), this.InputArgs);
			} catch (FatalException) {
				throw;
			} catch (TransException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			gdef.gumpInstance = null;
		}
	}
}