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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Summary("This class will be used fo iterating over the character's skills")]
	internal class SkillsEnumerator : IEnumerator<ISkill>, IEnumerable<ISkill> {
		private Skill current;
		private IEnumerator valuesEnum;

		internal SkillsEnumerator(Character chr) {
			valuesEnum = chr.SkillsAbilities.Values.GetEnumerator();			
		}

		#region IEnumerator<ISkill> Members
		public ISkill Current {
			get {
				return current;
			}
		}
		#endregion

		#region IDisposable Members
		public void Dispose() {
			current = null;
			valuesEnum = null;
		}
		#endregion

		#region IEnumerator Members
		object IEnumerator.Current {
			get {
				return current;
			}
		}

		[Summary("Iterate through the players skillsAbiilities dictionary but jump only on Skills")]
		public bool MoveNext() {
			while (valuesEnum.MoveNext()) {//move to the next Value (which is either Skill or Ability)
				current = valuesEnum.Current as Skill;
				if (current != null) {//if it was Skill, it is stored as an actual 'current' and we finish iterating for now
					return true;
				}
			}
			return false; //if we are here then the cycle finished
		}

		public void Reset() {
			current = null;
			valuesEnum.Reset();
		}
		#endregion

		#region IEnumerable<ISkill> Members
		public IEnumerator<ISkill> GetEnumerator() {
			return this;
		}
		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			return this;
		}

		#endregion
	}
}
