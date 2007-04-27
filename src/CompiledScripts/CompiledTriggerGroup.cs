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

namespace SteamEngine.CompiledScripts { 

	/*
		Class: CompiledTriggerGroup
			.NET scripts should extend this class, and make use of its features.
			This class provides automatic linking of methods intended for use as triggers and
			creates a TriggerGroup that your triggers are in.
	*/
	public abstract class CompiledTriggerGroup {
		TriggerGroup triggerGroup;
		/*
			Constructor: CompiledTriggerGroup
			Creates a triggerGroup named after the class, and then finds and sets up triggers 
			defined in the script (by naming them on_whatever).
		*/
		public CompiledTriggerGroup() {
			Type t=this.GetType();			//Whatever type this really is - This has to be extended to be used.
			
			triggerGroup = TriggerGroup.GetNewOrCleared(t.Name);	//Get/create our TriggerGroup
			
			MemberTypes memberType=MemberTypes.Method;		//Only find methods.
			BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public;
			MemberFilter filter = new MemberFilter(StartsWithString);		//Our StartsWithString method
			
			MemberInfo[] mi = t.FindMembers(memberType, bindingAttr, filter, "on_");	//Does it's name start with "on_"?
			foreach (MemberInfo m in mi) {
				if (m is MethodInfo) {	//It's a trigger. We make the ScriptHolder and add it to our TG's triggers list.
					triggerGroup.AddTrigger(MIScriptHolder.ChooseMIScriptHolder((MethodInfo)m, this, m.Name.Substring(3)));
				}
			}
		}   
		
		public TriggerGroup ThisTG { get {
			return triggerGroup;
		} }
		
		    
		//Simply return true or false depending on whether the method's name starts with whatever we asked for.
		//Case insensitive.
		public bool StartsWithString(MemberInfo m, object filterCriteria) {
			string s=((string) filterCriteria).ToLower();
			return m.Name.ToLower().StartsWith(s);
		}
	}
	
	
	//Implemented by the types which can represent map tiles
	//like t_water and such
	//more in the Map class
	//if someone has a better idea about how to do this ...
	public abstract class GroundTileType : CompiledTriggerGroup {
		private static Hashtable byName = new Hashtable(StringComparer.OrdinalIgnoreCase);
		
		public GroundTileType() : base() {
			Type t = this.GetType();
			byName[t.Name] = this;
		}
		
		public static GroundTileType Get(string name) {
			return (GroundTileType) byName[name];
		}

		public static bool IsMapTileInRange(int tileId, int aboveOrEqualTo, int below) {
			return (tileId>=aboveOrEqualTo && tileId<=below);
		}


		public abstract bool IsTypeOfMapTile(int mapTileId);
		
		internal static void UnLoadScripts() {
			byName.Clear();
		}
	}
}