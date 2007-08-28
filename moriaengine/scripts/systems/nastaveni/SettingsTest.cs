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

		public override string GetValue(object target) {
			return ObjectSaver.Save(((SimpleClass)target).foo);
		}

		public override void SetValue(object target, object value) {
			((SimpleClass)target).foo = (string)value;
		}

		public override void Write(object target, GUTAComponent where, params int[] index) {
			if(index == null || index.Length == 0) {//we have a problem - we need the index !
				throw new SEException(LogStr.Error("Trying to write a " +this.GetType()+" for object " + target + " but missing the edit field dialog index"));					
			} else {
				where.AddComponent(TextFactory.CreateLabel(ImprovedDialog.ITEM_INDENT, 0, Name));
				int indent = ImprovedDialog.ITEM_INDENT + ImprovedDialog.INPUT_INDENT;
				//insert the input field - specify the x and y position, let the engine to compute the width and height of the ocmponent
				where.AddComponent(InputFactory.CreateInput(LeafComponentTypes.InputText, GetValue(target), indent, 0, index[0]));
			}
		}
	}
}