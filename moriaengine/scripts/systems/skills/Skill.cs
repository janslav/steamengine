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
using SteamEngine;
using SteamEngine.Packets;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public class Skill : ISkill {
		private ushort realValue;
		private ushort cap;
		private ushort id;
		private SkillLockType lockType; //lock is C# keyword
		private Character cont;
		
		public Skill(ushort id, Character cont) {
			realValue = 0;
			cap = 1000;
			lockType = SkillLockType.Increase;
			this.id = id;
			this.cont = cont;
		}
		
		public Skill(Skill copyFrom, Character cont) {
			realValue = copyFrom.realValue;
			cap = copyFrom.cap;
			lockType = copyFrom.lockType;
			id = copyFrom.id;
			this.cont = cont;
		}
		
		public ushort RealValue {
			get {
				return realValue;
			}
			set {
				ushort oldValue = this.realValue;
				if (oldValue != value) {
					NetState.AboutToChangeSkill(cont, id);
					this.realValue = value;
					cont.Trigger_SkillChange(this, oldValue);
				}
			}
		}
		
		public ushort Cap {
			get {
				return cap;
			}
			set {
				NetState.AboutToChangeSkill(cont, id);
				this.cap = value;
			}
		}
		
		public ushort Id {
			get {
				return id;
			}
		}
		
		public SkillLockType Lock {
			get {
				return lockType;
			}
			set {
				NetState.AboutToChangeSkill(cont, id);
				this.lockType = value;
			}
		}
	}
}