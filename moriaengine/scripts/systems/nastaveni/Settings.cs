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
using SteamEngine.Common;


namespace SteamEngine.CompiledScripts.Dialogs {
	[Remark("Interface for displaying the labels and values of single members of the target"+
			"(infoized) object in the dialog")]
	interface IDataFieldView {
		[Remark("The name of this field / the label of the button")]
		string Name { get; }
		
		[Remark("Is the data read only? - i.e. displaying the settings results?")]
		bool ReadOnly { get; }

		[Remark("Shall this value be displayed with a button?")]
		bool IsButtonEnabled { get; }

		[Remark("Take the target object and retreive its member's (for which this interface instance is) value")]
		string GetValue(object target);

		[Remark("Take the target object and set its member's value")]
		void SetValue(object target, object value);

		[Remark("What will happen when the button is pressed?")]
		void OnButton(object target);

		[Remark("Provides the creation of a dialog field for a particular member of 'target', "+
				"it will be placed to 'where', the button's or editable field's index is the first value in "+
				"'index' params array"+
				"If the index is used for the non-editable or non-button enabled dataview field, an exception"+
				"will be thrown")]
		void Write(object target, GUTAComponent where, params int[] index);		
	}

	[Remark("Abstract class providing basic implementation of IDataFieldView")]
	public abstract class AbstractDataFieldView : IDataFieldView {
		//private MemberInfo member; //can be a field or method

		//[Remark("Setting the MemberInfo field - we will examine the exact type of the member"+
		//		"and make further settings")]
		//public MemberInfo Member {
		//	set {
		//		member = value;
		//		if(member is MethodBase) {
		//			//the member is a method - we will need a button here !
		//			buttonized = true;
		//		} else if(member is FieldInfo) {
		//			//it will be the label-value field...
		//			buttonized = false;
		//		}
		//	}
		//}

		//The properties will be set properly in the child classes
		public abstract bool ReadOnly { get; }
		public abstract bool IsButtonEnabled { get; }
		public abstract string Name { get; }

		//We will leave the implementation of these methods to children of this class
		public abstract string GetValue(object target);
		public abstract void SetValue(object target, object value);
		public abstract void OnButton(object target);
		public abstract void Write(object target, GUTAComponent where, params int[] index);
	}

	[Remark("Abstract class providing basics to display a non editable 'label-value' in the dialog")]
	public abstract class ReadOnlyDataFieldView : AbstractDataFieldView {
		[Remark("There is no button for this dataview field")]
		public override bool IsButtonEnabled {
			get {
				return false;
			}			
		}

		[Remark("Yes, this dataview field is read only")]
		public override bool ReadOnly {
			get {
				return true;
			}
		}

		[Remark("This method is forbidden in this class")]
		public override void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set a value to the non-editable field"));
		}

		[Remark("This field does not have any buttons too - buttons has another type of data view")]
		public override void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}
	}

	[Remark("Abstract class providing basics to display an editable 'label-value' in the dialog")]
	public abstract class ReadWriteDataFieldView : AbstractDataFieldView {
		[Remark("There is no button for this dataview field")]
		public override bool IsButtonEnabled {
			get {
				return false;
			}
		}

		[Remark("No, this dataview field can be edited")]
		public override bool ReadOnly {
			get {
				return false;
			}
		}

		[Remark("This field does not have any buttons - buttons are present in another type of data view")]
		public override void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}
	}

	[Remark("Abstract class providing basics to display a '[button]-label' in the dialog")]
	public abstract class ButtonDataFieldView : AbstractDataFieldView {
		[Remark("There is actually a button present for this dataview field")]
		public override bool IsButtonEnabled {
			get {
				return true;
			}
		}

		[Remark("Nothing to write or set here...")]
		public override bool ReadOnly {
			get {
				return true;
			}
		}

		[Remark("This method is forbidden in this class, there is nothing to set")]
		public override void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set any value to a buttonized dataview field"));
		}

		[Remark("This method is forbidden in this class, there is nothing to get")]
		public override string GetValue(object target) {
			throw new SEException(LogStr.Error("Cannot get any value from the buttonized dataview field"));
		}
	}

	[Summary("Decorate your class by this attribute if you want it to be viewable by info dialogs.")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]	
	public class ViewableClassAttribute : Attribute {
		//no params constructor
		public ViewableClassAttribute() {
		}		
	}

	[Summary("Used in ViewableClasses for methods we want to be available as buttons in the info dialogs.")]
	[AttributeUsage(AttributeTargets.Method)]
	public class ButtonAttribute : Attribute {
		[Remark("The name of the button which will be connected with the method decorated by this attribute")]
		private string name;

		public string Name {
			get {
				return name;
			}
		}

		//no params constructor
		public ButtonAttribute() {			
		}

		public ButtonAttribute(string name) {
			this.name = name;
		}
	}

	/* pro priklad takto:
	 * 
	 *	[ViewableClass]
		public static SimpleClass {
			public string foo;
	 *		[Button]
			public void SomeMethod() {
			}
		}
	 * 
	 * napis ipageablecolelction kterej vrati tenhle jeden datafieldview foo
	 * button pridej do ty pageablecollection*/ 

}