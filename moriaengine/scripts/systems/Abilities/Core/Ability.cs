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
	[Summary("This class holds information about one ability the user has - the number of ability points "+
			 "and any additional info (such as timers connected with the ability running etc.)")]
	public class Ability {
		private int points;
		
		private bool running;

		private Character cont;

		private AbilityDef def;

		private double lastUsage;
		
		public Ability(AbilityDef def, Character cont) {
			this.points = 0;
			this.running = false;
			this.cont = cont;
			this.def = def;
		}

		[Summary("Get or set actual ability points for this ability. Considers using of triggers if necessary")]
		public int Points {
			get {
				return points;
			}
			set {
				int oldValue = this.points;
				if((oldValue != value) && (value <= this.MaxPoints)) {//value has changed and is not at its maximum yet
					this.points = Math.Max(0, Math.Min(value, this.MaxPoints)); //dont go under zero and dont allow to go over MaxPoints!
				} 
				//run triggers if necessary
				if(oldValue > value) {
					if(this.points == 0) { //removed last point(s)
						//remove the ability from cont
						cont.RemoveAbility(def);
					}
				}
			}
		}

		[Summary("Obtain max points this ability can be assigned")]
		public ushort MaxPoints {
			get {
				//call the method on the def, the max value can be dependant on the container's profession e.g.
				return def.MaxPoints;
			}
		}

		[Summary("Is the ability actually running?")]
		public bool Running {
			get {
				return running;
			}
			internal set {
				running = value;
			}
		}

		public string Name {
			get {
				return def.Name;
			}
		}

		[Summary("Character who possesses this ability")]		
		public Character Cont {
			get {
				return cont;
			}
		}

		public string DefName {
			get {
				return def.Defname;
			}
		}

		[Summary("Server time of the last usage")]
		
		public double LastUsage {
			get {
				return lastUsage;
			}
			internal set {
				lastUsage = value;
			}
		}
	}
}