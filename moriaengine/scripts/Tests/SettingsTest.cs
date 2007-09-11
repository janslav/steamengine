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
	[ViewableClass("SimpleClass")]
	public class SimpleClass {
		public string foo;

		[NoShow]
		public int bar;

		[Button("Test Button")]
		public void SomeMethod() {
			LogStr.Debug("Pressed button for some method");
		}
	}

	[Remark("Class returning pages for the dialog.")]
	public class SimpleClassDataView : AbstractDataView {
		protected override IEnumerable<ButtonDataFieldView> ActionButtonsPage(int firstLineIndex, int maxButtonsOnPage) {
			SimpleClassActionButtonsPage buttonsPage = new SimpleClassActionButtonsPage(firstLineIndex, maxButtonsOnPage);
			return (IEnumerable<ButtonDataFieldView>)buttonsPage;
		}

		protected override IEnumerable<IDataFieldView> DataFieldsPage(int firstLineIndex, int maxLinesOnPage) {
			SimpleClassDataFieldsPage fieldsPage = new SimpleClassDataFieldsPage(firstLineIndex, maxLinesOnPage);
			return fieldsPage;
		}		

		public override int LineCount {
			get {
				return 2;
			}
		}

		public override string Name {
			get {
				return "SimpleClass";
			}
		}

		[Remark("This class will be automatically generated - the MoveNext method ensures the correct " +
			" iteration on all the data fields of the ViewableClass. It makes sure the upperBound " +
			"won't be reached")]
		public class SimpleClassDataFieldsPage : AbstractPage {
			public SimpleClassDataFieldsPage(int firstLineIndex, int maxFieldsOnPage) : base(firstLineIndex, maxFieldsOnPage) {
			}
			
			public override bool MoveNext() {
				if(nextIndex < upperBound) {
					switch(nextIndex) {
						case 0:
							current = ReadWriteDataFieldView_SimpleClass_Foo.instance;
							break;						
						default:
							//this happens when there are not enough lines to fill the whole page
							//or if we are beginning with the index larger then the overall LinesCount 
							//(which results in the empty page and should not happen)
							return false;
					}
					++nextIndex;//prepare the index for the next round of iteration
					return true;
				} else {
					return false;
				}
			}
		}

		[Remark("And this one ensures correct iteration over the action buttons of the ViewableClass")]
		public class SimpleClassActionButtonsPage : AbstractPage {
			public SimpleClassActionButtonsPage(int firstLineIndex, int maxButtonsOnPage) : base(firstLineIndex, maxButtonsOnPage) {
			}

			public override bool MoveNext() {
				if(nextIndex < upperBound) {
					switch(nextIndex) {						
						case 0:
							current = ButtonDataFieldView_SimpleClass_SomeMethod.instance;
							break;
						default:
							//this happens when there are not enough lines to fill the whole page
							//or if we are beginning with the index larger then the overall LinesCount 
							//(which results in the empty page and should not happen)
							return false;
					}
					++nextIndex;//prepare the index for the next round of iteration
					return true;
				} else {
					return false;
				}
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