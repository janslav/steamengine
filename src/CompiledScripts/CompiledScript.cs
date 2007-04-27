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
using SteamEngine;

namespace SteamEngine.CompiledScripts { 
	/*
		Class: CompiledScript
			.NET scripts should extend this class, and make use of its features.
			This class provides automatic linking of methods intended for use as functions.
	*/
	
	public abstract class CompiledScript {
		/*
			Constructor: CompiledScript
			Finds and sets up scripted functions defined in the script (by naming them def_whatever or func_whatever).
		*/
		public CompiledScript() {
			//Use reflection to find on_ and func_ and def_
			Type t = this.GetType();			//Whatever type this really is - This has to be extended to be used.
			MemberTypes memberType = MemberTypes.Method;		//Only find methods.
			BindingFlags bindingAttr = BindingFlags.IgnoreCase|BindingFlags.Instance|BindingFlags.Public;
			MemberFilter filter = new MemberFilter(StartsWithString);		//Our StartsWithString method
			
			MemberInfo[] mi = t.FindMembers(memberType, bindingAttr, filter, "def_");
			foreach (MemberInfo m in mi) {
				if (m is MethodInfo) {	//It's a function. We make the ScriptHolder, it's auto-registered.
					ScriptHolder sh = MIScriptHolder.ChooseMIScriptHolder((MethodInfo)m, this, m.Name.Substring(4));
					sh.RegisterAsFunction();
				}
			}
			
			mi = t.FindMembers(memberType, bindingAttr, filter, "func_");
			foreach (MemberInfo m in mi) {
				if (m is MethodInfo) {	//It's a function. We make the ScriptHolder, it's auto-registered.
					ScriptHolder sh = MIScriptHolder.ChooseMIScriptHolder((MethodInfo)m, this, m.Name.Substring(5));
					sh.RegisterAsFunction();
				}
			}
		}
		
		//Simply return true or false depending on whether the method's name starts with whatever we asked for.
		//Case insensitive.
		private static bool StartsWithString(MemberInfo m, object filterCriteria) {
			string s=((string) filterCriteria).ToLower();
			return m.Name.ToLower().StartsWith(s);
		}
	}
}