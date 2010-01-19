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
using System.Timers;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	[Summary("This class holds information about one ability the user has - the number of ability points " +
			 "and any additional info (such as timers connected with the ability running etc.)")]
	public sealed class Ability {
		private int realPoints;
		private int modification;
		private bool running;
		private Character cont;
		private AbilityDef def;
		private TimeSpan lastUsage;

		internal Ability(AbilityDef def, Character cont) {
			this.modification = 0;
			this.realPoints = 0;
			this.running = false;
			this.cont = cont;
			this.def = def;
		}

		[Summary("Character's ability points. This is the real value, i.e. unmodified my temporary effect, equipped magic items, etc.")]
		public int RealPoints {
			get {
				return this.realPoints;
			}
			set {
				int oldValue = this.realPoints;
				int newValue = Math.Min(0, value);
				int diff = newValue - oldValue;
				if (diff != 0) {
					int oldModified = this.ModifiedPoints;
					this.realPoints = newValue;
					if (oldModified != this.ModifiedPoints) {
						this.def.Trigger_ValueChanged(this.cont, this, oldModified); //call changetrigger with information about previous value
					}

					if ((newValue == 0) && (this.modification == 0)) { //removed last point(s)						
						this.cont.InternalRemoveAbility(def);
					}
				}
			}
		}


		[Summary("Character's ability points. This is the modified value, which can be different from RealPoints when some temporary effects take place. " +
			"When character dies, this value should become equal to RealPoints.")]
		[Remark("This will never return a negative value, even if RealPoints+modification is negative. That's why the modification must be changed in a separate method.")]
		public int ModifiedPoints {
			get {
				return Math.Max(0, this.realPoints + this.modification);
			}
		}

		[Summary("Change the value of ModifiedPoints")]
		public void ModifyPoints(int difference) {
			if (difference != 0) {
				int oldValue = this.ModifiedPoints;
				this.modification += difference;
				int newValue = this.ModifiedPoints;
				if (oldValue != newValue) { //modified value may not have changed if we're still in negative numbers
					this.def.Trigger_ValueChanged(this.cont, this, oldValue); //call changetrigger with information about previous value
				}
			}
		}

		[Summary("Obtain max points this ability can be assigned")]
		public int MaxPoints {
			get {
				return 100; //TODO
			}
		}

		[Summary("Is the ability actually running?")]
		public bool Running {
			get {
				return this.running;
			}
		}

		internal void InternalSetRunning(bool running) {
			this.running = running;
		}

		public string Name {
			get {
				return this.def.Name;
			}
		}

		[Summary("Character who possesses this ability")]
		public Character Cont {
			get {
				return this.cont;
			}
		}

		public AbilityDef AbilityDef {
			get {
				return this.def;
			}
		}

		[Summary("Server time of the last usage")]
		public TimeSpan LastUsage {
			get {
				return this.lastUsage;
			}
			internal set {
				this.lastUsage = value;
			}
		}

		internal string GetSaveString() {
			if ((this.modification == 0) && (!this.running)) {
				return this.realPoints.ToString();
			} else {
				if (this.running) {
					return String.Concat(this.RealPoints.ToString(), ", ",
						this.modification.ToString(), ", true");
				}
				return String.Concat(this.RealPoints.ToString(), ", ", this.modification.ToString());
			}
		}

		internal bool LoadSavedString(string p) {
			string[] split = Utility.SplitSphereString(p);
			if (!ConvertTools.TryParseInt32(split[0], out this.realPoints)) {
				return false;
			}
			int len = split.Length;
			if (len > 0) {
				if (!ConvertTools.TryParseInt32(split[1], out this.modification)) {
					return false;
				}
			}
			if (len > 1) {
				if (!ConvertTools.TryParseBoolean(split[2], out this.running)) {
					return false;
				}
			}
			return true;
		}
	}
}