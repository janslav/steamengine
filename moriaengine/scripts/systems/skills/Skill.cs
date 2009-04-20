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
using SteamEngine.Networking;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	[Dialogs.ViewableClass]
	public class Skill : ISkill {
		private ushort realValue;
		private ushort cap;
		private byte id;
		private SkillLockType lockType; //lock is C# keyword
		private Character cont;

		public Skill(int id, Character cont) {
			this.realValue = 0;
			this.cap = 1000;
			this.lockType = SkillLockType.Increase;
			this.id = (byte) id;
			this.cont = cont;
		}

		public Skill(Skill copyFrom, Character cont) {
			this.realValue = copyFrom.realValue;
			this.cap = copyFrom.cap;
			this.lockType = copyFrom.lockType;
			this.id = copyFrom.id;
			this.cont = cont;
		}

		public int RealValue {
			get {
				return this.realValue;
			}
			set {
				ushort oldValue = this.realValue;
				if (oldValue != value) {
					CharSyncQueue.AboutToChangeSkill(cont, id);
					this.realValue = (ushort) value;
					cont.Trigger_SkillChange(this, oldValue);

					this.RemoveIfDefault();
				}
			}
		}

		public int Cap {
			get {
				return cap;
			}
			set {
				if (this.cap != value) {
					CharSyncQueue.AboutToChangeSkill(cont, id);
					this.cap = (ushort) value;

					this.RemoveIfDefault();
				}
			}
		}

		public int Id {
			get {
				return id;
			}
		}

		public SkillName Name {
			get {
				return (SkillName) id;
			}
		}

		public SkillLockType Lock {
			get {
				return lockType;
			}
			set {
				if (this.lockType != value) {
					CharSyncQueue.AboutToChangeSkill(cont, id);
					this.lockType = value;

					this.RemoveIfDefault();
				}
			}
		}

		private void RemoveIfDefault() {
			if (this.realValue == 0 && this.cap == 1000 && this.lockType == SkillLockType.Increase) {
				cont.InternalRemoveSkill(id);
			}
		}

		public int ModifiedValue {
			get { return this.realValue; }
		}
	}
}