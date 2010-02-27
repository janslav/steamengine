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
		private short modification;
		private byte id;
		private SkillLockType lockType; //lock is C# keyword
		private Character cont;

		public Skill(int id, Character cont) {
			//this.realValue = 0;
			//this.modification = 0;
			this.lockType = SkillLockType.Up;
			this.id = (byte) id;
			this.cont = cont;
		}
		
		//copying contstructor
		public Skill(Skill copyFrom, Character cont) {
			this.realValue = copyFrom.realValue;
			this.modification = copyFrom.modification;
			this.lockType = copyFrom.lockType;
			this.id = copyFrom.id;
			this.cont = cont;
		}

		[Summary("Character's skill points. This is the real value, i.e. unmodified by temporary effect, equipped magic items, etc.")]
		public int RealValue {
			get {
				return this.realValue;
			}
			set {
				int oldValue = this.realValue;
				int newValue = Math.Max(0, value);
				if (newValue != this.realValue) {
					CharSyncQueue.AboutToChangeSkill(this.cont, this.id);

					int oldModified = this.ModifiedValue;
					this.realValue = (ushort) newValue;

					if (oldModified != this.ModifiedValue) { //this might not be true, if the modifier value is negative
						SkillDef.Trigger_ValueChanged(this.cont, this, oldModified); //call changetrigger with information about previous value
					}
				}
				this.DisposeIfEmpty();
			}
		}

		[Summary("Character's skill points. This is the modified value, which can be different from RealValue when some temporary effects take place. " +
		   "When character dies, this value should become equal to RealValue.")]
		[Remark("This will never return a negative value, even if RealValue+modification is negative. That's why the modification must be changed in a separate method.")]
		public int ModifiedValue {
			get {
				return Math.Max(0, this.realValue + this.modification);
			}
		}

		[Summary("Change the value of ModifiedPoints")]
		public void ModifyValue(int difference) {
			if (difference != 0) {
				CharSyncQueue.AboutToChangeSkill(this.cont, this.id);

				int oldModified = this.ModifiedValue;
				this.modification += (short) difference;

				if (oldModified != this.ModifiedValue) { //modified value may not have changed if we're still in negative numbers
					SkillDef.Trigger_ValueChanged(this.cont, this, oldModified); //call changetrigger with information about previous value
				}
			}
			this.DisposeIfEmpty();
		}

		public int Cap {
			get {
				Player self = this.cont as Player;
				if (self != null) {
					ProfessionDef prof = self.Profession;
					if (prof != null) {
						return prof.GetSkillCap(this.id);
					}
				}
				return 0;
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

		public SkillDef Def {
			get {
				return (SkillDef) SkillDef.GetById(this.id);
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

					this.DisposeIfEmpty();
				}
			}
		}

		private void DisposeIfEmpty() {
			if (this.realValue == 0 && this.modification == 1000 && this.lockType == SkillLockType.Up) {
				cont.InternalRemoveSkill(id);
			}
		}

		#region Load / Save
		internal string GetSaveString() {
			if (this.lockType == SkillLockType.Up) {
				if (this.modification == 0) {
					return this.realValue.ToString();
				} else {
					return String.Concat(this.realValue.ToString(), ", ", this.modification.ToString());
				}
			} else {
				string lockStr;
				switch (this.lockType) {
					case SkillLockType.Down:
						lockStr = "Down";
						break;
					case SkillLockType.Locked:
						lockStr = "Locked";
						break;
					default:
						throw new SEException("this.lockType != Up | Down | Locked");
				}
				return String.Concat(this.realValue.ToString(), ", ", 
					this.modification.ToString(), ", ", 
					lockStr);
			}
		}

		internal bool LoadSavedString(string p) {
			string[] split = Utility.SplitSphereString(p);
			if (!ConvertTools.TryParseUInt16(split[0], out this.realValue)) {
				return false;
			}
			int len = split.Length;
			if (len > 1) {
				if (!ConvertTools.TryParseInt16(split[1], out this.modification)) {
					return false;
				}
			} else {
				this.modification = 0;
			}
			if (len > 2) {
				switch (split[2].Trim().ToLowerInvariant()) {
					case "down":
						this.lockType = SkillLockType.Down;
						break;
					case "locked":
						this.lockType = SkillLockType.Locked;
						break;
					case "up":
						this.lockType = SkillLockType.Up;
						break;
					default:
						return false;
				}
			} else {
				this.lockType = SkillLockType.Up;
			}
			this.DisposeIfEmpty();
			return true;
		}
		#endregion Load / Save
	}
}