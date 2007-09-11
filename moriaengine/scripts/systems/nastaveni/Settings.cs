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
	public interface IDataFieldView {
		[Remark("The name of this field / the label of the button")]
		string Name { get; }
		
		[Remark("Is the data read only? - i.e. displaying the settings results?")]
		bool ReadOnly { get; }

		[Remark("Shall this value be displayed with a button?")]
		bool IsButtonEnabled { get; }

		[Remark("Take the target object and retreive its member's (for which this interface instance is) value")]
		object GetValue(object target);

		[Remark("Take the target object and set its member's value")]
		void SetValue(object target, object value);

		[Remark("Take the target object and retreive its member's value in the stringified form")]
		string GetStringValue(object target);

		[Remark("Take the stringified value, convert it and set it to the respective member of the target")]
		void SetStringValue(object target, string value);

		[Remark("What will happen when the button is pressed?")]
		void OnButton(object target);				
	}

	[Remark("Abstract class providing basics to display a non editable 'label-value' in the dialog")]
	public abstract class ReadOnlyDataFieldView : IDataFieldView {
		[Remark("There is no button for this dataview field")]
		public bool IsButtonEnabled {
			get {
				return false;
			}			
		}

		[Remark("Yes, this dataview field is read only")]
		public bool ReadOnly {
			get {
				return true;
			}
		}

		[Remark("This method is forbidden in this class")]
		public void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set a value to the non-editable field"));
		}

		[Remark("This method is forbidden in this class")]
		public void SetStringValue(object target, string value) {
			throw new SEException(LogStr.Error("Cannot convert and set a stringified value to the non-editable field"));
		}

		[Remark("This field does not have any buttons too - buttons have another type of data view")]
		public void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string Name { get; }
		public abstract object GetValue(object target);
		public abstract string GetStringValue(object target);
	}

	[Remark("Abstract class providing basics to display an editable 'label-value' in the dialog")]
	public abstract class ReadWriteDataFieldView : IDataFieldView {
		[Remark("There is no button for this dataview field")]
		public bool IsButtonEnabled {
			get {
				return false;
			}
		}

		[Remark("No, this dataview field can be edited")]
		public bool ReadOnly {
			get {
				return false;
			}
		}

		[Remark("This field does not have any buttons - buttons are present in another type of data view")]
		public void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string Name { get; }
		public abstract object GetValue(object target);
		public abstract void SetValue(object target, object value);
		public abstract string GetStringValue(object target);
		public abstract void SetStringValue(object target, string value);

	}

	[Remark("Abstract class providing basics to display a '[button]-label' in the dialog")]
	public abstract class ButtonDataFieldView : IDataFieldView {
		[Remark("There is actually a button present for this dataview field")]
		public bool IsButtonEnabled {
			get {
				return true;
			}
		}

		[Remark("Nothing to write or set here...")]
		public bool ReadOnly {
			get {
				return true;
			}
		}

		[Remark("This method is forbidden in this class, there is nothing to set")]
		public void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set any value to a buttonized dataview field"));
		}

		[Remark("This method is forbidden in this class, there is nothing to get")]
		public object GetValue(object target) {
			throw new SEException(LogStr.Error("Cannot get any value from the buttonized dataview field"));
		}

		[Remark("This method is forbidden in this class, there is nothing to set")]
		public void SetStringValue(object target, string value) {
			throw new SEException(LogStr.Error("Cannot convert and set any stringified value to a buttonized dataview field"));
		}

		[Remark("This method is forbidden in this class, there is nothing to get")]
		public string GetStringValue(object target) {
			throw new SEException(LogStr.Error("Cannot get and convert any value from the buttonized dataview field"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string Name { get; }
		public abstract void OnButton(object target);

	}

	[Summary("Decorate your class by this attribute if you want it to be viewable by info dialogs.")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]	
	public class ViewableClassAttribute : Attribute {
		[Remark("The name that will be displayed in the headline of the infodialog")]
		private string name;
		
		public string Name {
			get {
				return name;
			}
		}

		//no params constructor
		public ViewableClassAttribute() {
		}

		public ViewableClassAttribute(string name) {
			this.name = name;
		}
	}

	[Summary("Decorate amember of the ViewableClass by this attribute if you want to prevent them to be displayed in info dialogs."+
			 "all other attributes will be displayed")]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class NoShowAttribute : Attribute {
		//no params constructor
		public NoShowAttribute() {
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
}