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
using System.Collections.Generic;

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

	[Remark("Class returning pages for the dialog. It also ensures we are not trying to reach more fields than available" +
			"and makes index corrections necessary for the page size with respects to the real number of fields "+
			"(e.g. we are on the last page)")]
	public class SimpleClassDataView : AbstractDataView {
		public override IEnumerable<IDataFieldView> GetPage(int firstLineIndex, int pageSize) {
			if(firstLineIndex > LineCount) {
				throw new SEException(LogStr.Error("Trying to access more IDataFieldViews than available - "+
									"starting from "+firstLineIndex+" but have only " + LineCount));
			}
			if(firstLineIndex + pageSize > LineCount) {
				pageSize = LineCount - firstLineIndex;
			}
			return SimpleClassPage.instance.Initialize(firstLineIndex, pageSize);
		}

		public override int LineCount {
			get {
				return 2;
			}
		}
	}

	[Remark("This class will be automatically generated - the MoveNext method ensures the correct "+
			" the correct iteration on all the IDataFieldViews of the ViewableClass")]
	public class SimpleClassPage : AbstractPage {
		public static SimpleClassPage instance = new SimpleClassPage();

		public override bool MoveNext() {
			switch(currentIndex++) {
				case 0:
					current = ReadWriteDataFieldView_SimpleClass_Foo.instance;
					break;
				case 1:
					current = ButtonDataFieldView_SimpleClass_SomeMethod.instance;
					break;
			}
		}
	}

	[Remark("Dataview implementation for the member 'foo' of the SimpleClass")]
	public class ReadWriteDataFieldView_SimpleClass_Foo : ReadWriteDataFieldView {
		public static ReadWriteDataFieldView_SimpleClass_Foo instance = new ReadWriteDataFieldView_SimpleClass_Foo();

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

		public override void SetValue(object target, object value) {
			((SimpleClass)target).foo = (string)value;			
		}

		public override void SetStringValue(object target, string value) {
			((SimpleClass)target).foo = (string)ObjectSaver.Load(value);			
		}		
	}

	[Remark("Dataview implementation for the method 'SomeMethod' of the SimpleClass")]
	public class ButtonDataFieldView_SimpleClass_SomeMethod : ButtonDataFieldView {
		public static ButtonDataFieldView_SimpleClass_SomeMethod instance = new ButtonDataFieldView_SimpleClass_SomeMethod();

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