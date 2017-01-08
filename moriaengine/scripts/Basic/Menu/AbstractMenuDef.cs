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
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	public abstract class AbstractMenuDef : AbstractDef {

		MenuRespose responseCallback;
		MenuCancel cancelCallback;

		internal AbstractMenuDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.responseCallback = this.On_Response;
			this.cancelCallback = this.On_Cancel;
		}

		public new static AbstractMenuDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbstractMenuDef;
		}

		internal void Assign(Player self, object parameter) {
			GameState state = self.GameState;
			if (state != null) {
				Language lang = state.Language;
				state.Menu(this.GetAllTexts(lang),
					this.responseCallback, this.cancelCallback, parameter);
			}
		}

		protected abstract IEnumerable<string> GetAllTexts(Language language);

		protected abstract void On_Response(GameState state, int index, object parameter);

		protected abstract void On_Cancel(GameState state, object parameter);
	}
}