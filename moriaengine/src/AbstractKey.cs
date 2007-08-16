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

namespace SteamEngine {
	using System;
	using System.Collections;
	
	/// <summary>
	/// AbstractKey is used as an ID for tags, to make tag lookups
	/// fast and not use a lot of memory, etc. base class for all
	/// "key" classes
	/// 
	/// 
	/// </summary>                                                
	public abstract class AbstractKey {
		protected static int uids;

		public readonly string name;
		public readonly int uid;
		
		protected AbstractKey(string name, int uid) {
			this.name=name;
			this.uid=uid;
		}
		
		public override int GetHashCode() {
			return uid;
		}
		
		public override string ToString() {
			return name;	
		}
		
		public override bool Equals(Object obj) {
			if (obj.GetType()==this.GetType()) {
				AbstractKey tobj = (AbstractKey) obj;
				return (tobj.uid==this.uid);
			} else {
				return false;
			}
		}
	}
}