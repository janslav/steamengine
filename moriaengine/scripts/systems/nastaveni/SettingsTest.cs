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

using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Simple class for testing the info dialogs")]
	[ViewableClass]
	public class SimpleClass {
		public string foo;

		[Button("Test Button")]
		public void SomeMethod() {
			LogStr.Debug("Pressed button for some method");
		}
	}

	[Remark("Dataview implementation for the member 'foo' of the SimpleClass")]
	public class ReadWriteDataFieldView_SimpleClass_Foo : ReadWriteDataFieldView {
		public override string Name {
			get {
				return "foo";
			}
		}

		public override object GetValue(object target) {
			return ((SimpleClass)target).foo;
		}

		public override string GetStringValue(object target) {
			return ObjectSaver.Save(((SimpleClass)target).foo);
		}

		public override bool SetValue(object target, object value) {
			try {
				((SimpleClass)target).foo = (string)value;
			} catch {
				return false;
			}
			return true;
		}

		public override bool SetStringValue(object target, string value) {
			try {
				((SimpleClass)target).foo = (string)ObjectSaver.Load(value);
			} catch {
				return false;
			}
			return true;
		}		
	}

	[Remark("Dataview implementation for the method 'SomeMethod' of the SimpleClass")]
	public class ButtonDataFieldView_SimpleClass_SomeMethod : ButtonDataFieldView {
		public override string Name {
			get {
				return "Test Button";
			}
		}

		public override void OnButton(object target) {
			((SimpleClass)target).SomeMethod();
		}		
	}
}