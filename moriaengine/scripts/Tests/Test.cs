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
using SteamEngine;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	//If something looks difficult, make suggestions for ways to make it easier!

	public class TestScript : CompiledScript {	//Our script must extend 'Script'
		//base("something") means this is a triggerGroup named "something", and can have triggers in  it.
		//Without that, you can't write on_ methods, they're illegal if they aren't in a triggerGroup.
		//Note that if you need a reference to the triggerGroup, it is 'this.triggerGroup'.
		//public TestScript() : base("e_test") {
		//	//Specify a section for the INI, and specify name, default values, and comments for keys in that section
		//	Globals.Instance.AddTriggerGroup(this.triggerGroup);	
		//	//Add our trigger group (e_test) to 
		//	//Server.globals. This is OK, it won't be added again
		//	//if it's already there.
		//}
		
		public void Moo(string asdf) {
			char o;
			for (int a=0; a<asdf.Length; a++) {
				o=asdf[a];
				Console.Write(o);
			}
		}
		
		[Summary("Documentation in scripts")]
		[Remark("Documentation attributes are allowed in scripts.")]
		public TagHolder def_testInvoking(TagHolder self, string accname) {
			Console.WriteLine("I am called on "+self+" with '"+accname+"'");
			return new TagHolder();
		}
		
		public void def_TestFuncEmpty(TagHolder self) {
			Moo(self.Name);
		}
		public void def_TestFunc2Args(TagHolder self, string a, string b) {
			Moo(self.Name);
		}
		public void def_TestFunc3Args(TagHolder self, string a, string b, string c) {
			Moo(self.Name);
		}
		public void def_TestFunc4Args(TagHolder self, string a, string b, string c, string d) {
			Moo(self.Name);
		}
		public void def_TestFunc5Args(TagHolder self, string a, string b, string c, string d, string e) {
			Moo(self.Name);
		}
		public void def_TestFuncPArray(TagHolder self, params object[] args) {
			Moo(self.Name);
		}
		
		public void def_TestException(TagHolder self) {
			throw new Exception("I threw an Test exception");
		}
		
		public void def_funkce(TagHolder self, ScriptArgs sa) {
			Console.WriteLine("function has been run: "+sa.Args);
		}
		
		//public void on_startup(TagHolder globals) {
		//	TagKey testTag=Tag("testTag");
		//	
		//	ArrayList list=new ArrayList();
		//	list.Add("first item");
		//	list.Add("second item");
		//	list.Add(Server.dice.Next(5, 100));
		//	ArrayList list2=new ArrayList();
		//	list2.Add(Server.dice.Next(5, 100));
		//	list2.Add(Server.dice.Next(5, 100));
		//	list2.Add(Server.dice.Next(5, 100));
		//	list2.Add("third item");
		//	list2.Add(Thing.UidGetThing(0));
		//	list2.Add(GameAccount.Get(0));
		//	list.Add(list2);
		//	globals.SetTag(testTag,list);
		//	
		//	TagHolder copied = new TagHolder(globals);
      //
		//	Console.WriteLine("original:"+Globals.ObjToString(globals.tags));
		//	Console.WriteLine("copied:"+Globals.ObjToString(copied.tags));
		//	ArrayList copiedlist=(ArrayList)copied.GetTag(testTag);
		//	copiedlist[1]="changeditem";
		//	list.Add("newitem");
		//	list2.Add("newitem in list 2");
		//	Console.WriteLine("original:"+Globals.ObjToString(globals.tags));			
		//	Console.WriteLine("copied:"+Globals.ObjToString(copied.tags));
		//	
		//	Console.WriteLine("list:"+list.Count+"   "+list.Capacity);
		//}
		//
		//public void on_testtrigger(TagHolder globals, object[] args) {
		//	Console.WriteLine("triggered with args: "+Globals.ObjToString(args));
		//	Console.WriteLine("timers is now: "+Globals.ObjToString(Server.globals.timers));
		//}
	}
}