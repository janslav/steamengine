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
using System.Collections.Generic;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>
	/// Interface for displaying the labels and values of single members of the target
	/// (infoized) object in the dialog
	/// </summary>
	public interface IDataFieldView {
		/// <summary>The name of this field / the label of the button</summary>
		string GetName(object target);

		/// <summary>Is the data read only? - i.e. displaying the settings results?</summary>
		bool ReadOnly { get; }

		/// <summary>Shall this value be displayed with a button?</summary>
		bool IsButtonEnabled { get; }

		/// <summary>The real type of the data field (it needn't necessary be the type of the value...)</summary>
		Type FieldType { get; }

		/// <summary>Take the target object and retreive its member's (for which this interface instance is) value</summary>
		object GetValue(object target);

		/// <summary>Take the target object and set its member's value</summary>
		void SetValue(object target, object value);

		/// <summary>Take the target object and retreive its member's value in the stringified form</summary>
		string GetStringValue(object target);

		/// <summary>Take the stringified value, convert it and set it to the respective member of the target</summary>
		void SetStringValue(object target, string value);

		/// <summary>What will happen when the button is pressed?</summary>
		void OnButton(object target);
	}

	/// <summary>Class for managing all generated dataviews and providing them according to wanted type</summary>
	public static class DataViewProvider {
		public static Hashtable dataViewsForTypes = new Hashtable();

		public static SortedList<Type, IDataView> dataViewsForbaseClasses = new SortedList<Type, IDataView>(TypeHierarchyComparer.instance);

		/// <summary>Will find dataview for given type.</summary>
		public static IDataView FindDataViewByType(Type handledType) {
			IDataView view = (IDataView) dataViewsForTypes[handledType];
			if (view != null) {
				return view;
			} else {
				foreach (KeyValuePair<Type, IDataView> pair in dataViewsForbaseClasses) {
					if (pair.Key.IsAssignableFrom(handledType)) {
						dataViewsForTypes[handledType] = pair.Value;
						return pair.Value;
					}
				}
			}
			return null;
		}

		/// <summary>Register a new hook to ClassManager - it will send the examined Types here and we will care for next.</summary>
		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<IDataView>(CheckGeneratedDataViewClass);
			//ClassManager.RegisterHook(CheckGeneratedDataViewClass);
		}

		/// <summary>
		/// Method for checking if the given Type is a descendant of IDataView. If so, store it in the map
		/// with the HandledType as Key...
		/// </summary>
		public static bool CheckGeneratedDataViewClass(Type type) {
			if (!type.IsAbstract) {
				//if (typeof(IDataView).IsAssignableFrom(type)) { //this should be managed by the ClassManager :)
				ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
				if (ci != null) {
					IDataView idv = (IDataView) ci.Invoke(new object[0] { });

					if (idv.HandleSubclasses) {
						dataViewsForbaseClasses.Add(idv.HandledType, idv);
					} else {
						dataViewsForTypes.Add(idv.HandledType, idv);
					}

				} else {
					throw new SEException("Non-parametric-constructor of " + type + " cannot be created. IDataView cannot be registered.");
				}
				//}
			}
			return false;
		}
	}

	/// <summary>Interface used for all generated DataView classes</summary>
	public interface IDataView {
		/// <summary>This getter will provide us the Type this AbstractDataView is made for</summary>
		Type HandledType { get; }

		/// <summary>If true, subclasses of HandledType will also be handled.</summary>
		bool HandleSubclasses { get; }

		/// <summary>Name that will be displayed in the Info dialog headline - description of the infoized class</summary>
		string GetName(object instance);

		/// <summary>Number of buttons</summary>
		int GetActionButtonsCount(object instance);

		/// <summary>Number of fields</summary>
		int GetFieldsCount(object instance);

		/// <summary>GetPage for data fields</summary>
		IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target);

		/// <summary>GetPage for action buttons</summary>
		IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target);
	}

	/// <summary>
	/// The ancestor of all generated classes that manage and return the paged data to display.
	/// It implements two interfaces - first for paging the data fields, second for paging the action buttons
	/// both types will be available by two similar GetPage methods.
	/// </summary>
	public abstract class AbstractDataView : IDataView {
		/// <summary>
		/// Implement the method to return an initialized instance of AbstractPage. This 
		/// should be then used in foreach block or somehow (as an IEnumerable) for iterating through the data fields
		/// </summary>
		public abstract IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target);

		/// <summary>Similar as the previous method but for iterating over the action buttons pages</summary>
		public abstract IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target);

		//these three interface properties will be implemented in children
		public abstract Type HandledType { get; }

		public bool HandleSubclasses {
			get {
				return false;
			}
		}

		public abstract string GetName(object instance);

		public abstract int GetActionButtonsCount(object instance);

		public abstract int GetFieldsCount(object instance);


		/// <summary>
		/// This will be used to return the desired page of objects to be displayed
		/// it will also hold all the IDataFieldViews belonging to the ViewableClass
		/// </summary>
		public abstract class AbstractPage<T> : IEnumerable<T>, IEnumerator<T> {
			//increased everytime the MoveNext method will be invoked
			protected int nextIndex;
			protected object target; //the parent object we are making info/settings on
			//this is the current field we are displaying - it will be used in Enumerators methods
			protected T current;

			/// <summary>
			/// This method will be used by IPageableCollection to prepare the Enumerator
			/// - set the starting index and the reference object from which we possibly can obtain some
			/// necessary inforamtion such as upper bound of iteration... if needed
			/// </summary>
			public AbstractPage(int startIndex, object target) {
				//initialize indices and prepare for usage
				this.nextIndex = startIndex;
				this.target = target;
			}

			#region IEnumerable<T> Members
			/// <summary>Interface method used for iterating - it will return itself, but prepared for iterating</summary>
			public IEnumerator<T> GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerable Members
			/// <summary>Interface method used for iterating - it will return itself, but prepared for iterating</summary>
			IEnumerator IEnumerable.GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerator<T> Members
			/// <summary>Yet another interface property - returns the prepared field for displaying</summary>
			public T Current {
				get {
					return current;
				}
			}
			#endregion

			#region IDisposable Members
			/// <summary>Do nothing, we don't need to dispose anything in some special way</summary>
			public void Dispose() {
				//we dont care
			}
			#endregion

			#region IEnumerator Members

			/// <summary>
			/// This is the most important method - it will ensure the iterating on the fields 
			/// belonging to the desired page
			/// </summary>
			public abstract bool MoveNext();

			public void Reset() {
				throw new SEException("The method or operation is not implemented.");
			}

			object IEnumerator.Current {
				get {
					return current;
				}
			}
			#endregion
		}
	}

	/// <summary>Abstract class providing basics to display a non editable 'label-value' in the dialog</summary>
	public abstract class ReadOnlyDataFieldView : IDataFieldView {
		/// <summary>There is no button for this dataview field</summary>
		public bool IsButtonEnabled {
			get {
				return false;
			}
		}

		/// <summary>Yes, this dataview field is read only</summary>
		public bool ReadOnly {
			get {
				return true;
			}
		}

		/// <summary>This method is forbidden in this class</summary>
		public void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set a value to the non-editable field"));
		}

		/// <summary>This method is forbidden in this class</summary>
		public void SetStringValue(object target, string value) {
			throw new SEException(LogStr.Error("Cannot convert and set a stringified value to the non-editable field"));
		}

		/// <summary>This field does not have any buttons too - buttons have another type of data view</summary>
		public void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string GetName(object target);
		public abstract Type FieldType { get; }
		public abstract object GetValue(object target);
		public abstract string GetStringValue(object target);
	}

	/// <summary>Abstract class providing basics to display an editable 'label-value' in the dialog</summary>
	public abstract class ReadWriteDataFieldView : IDataFieldView {
		/// <summary>There is no button for this dataview field</summary>
		public bool IsButtonEnabled {
			get {
				return false;
			}
		}

		/// <summary>No, this dataview field can be edited</summary>
		public bool ReadOnly {
			get {
				return false;
			}
		}

		/// <summary>This field does not have any buttons - buttons are present in another type of data view</summary>
		public void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string GetName(object target);
		public abstract Type FieldType { get; }
		public abstract object GetValue(object target);
		public abstract void SetValue(object target, object value);
		public abstract string GetStringValue(object target);
		public abstract void SetStringValue(object target, string value);

	}

	/// <summary>Abstract class providing basics to display a '[button]-label' in the dialog</summary>
	public abstract class ButtonDataFieldView : IDataFieldView {
		/// <summary>There is actually a button present for this dataview field</summary>
		public bool IsButtonEnabled {
			get {
				return true;
			}
		}

		/// <summary>Nothing to write or set here...</summary>
		public bool ReadOnly {
			get {
				return true;
			}
		}

		public Type FieldType {
			get {
				throw new SEException(LogStr.Error("This property is not provided for button fields"));
			}
		}

		/// <summary>This method is forbidden in this class, there is nothing to set</summary>
		public void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set any value to a buttonized dataview field"));
		}

		/// <summary>This method is forbidden in this class, there is nothing to get</summary>
		public object GetValue(object target) {
			throw new SEException(LogStr.Error("Cannot get any value from the buttonized dataview field"));
		}

		/// <summary>This method is forbidden in this class, there is nothing to set</summary>
		public void SetStringValue(object target, string value) {
			throw new SEException(LogStr.Error("Cannot convert and set any stringified value to a buttonized dataview field"));
		}

		/// <summary>This method is forbidden in this class, there is nothing to get</summary>
		public string GetStringValue(object target) {
			throw new SEException(LogStr.Error("Cannot get and convert any value from the buttonized dataview field"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string GetName(object target);
		public abstract void OnButton(object target);

	}

	/// <summary>Decorate your class by this attribute if you want it to be viewable by info dialogs.</summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ViewableClassAttribute : Attribute {
		/// <summary>The name that will be displayed in the headline of the infodialog</summary>
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

	/// <summary>
	/// Decorate a member of the ViewableClass by this attribute if you want to prevent them to be displayed in info dialogs.
	/// all other attributes will be displayed
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class NoShowAttribute : Attribute {
		//no params constructor
		public NoShowAttribute() {
		}
	}

	/// <summary>
	/// Decorate a member of the ViewableClass by this attribute if you want it to be infoized and you want
	/// to specify its name explicitely
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class InfoFieldAttribute : Attribute {
		/// <summary>The name of the field which will be displayed in the dialog rather than the fields name itself</summary>
		private string name;

		public string Name {
			get {
				return name;
			}
		}

		public InfoFieldAttribute(string name) {
			this.name = name;
		}
	}

	/// <summary>Used in ViewableClasses for methods we want to be available as buttons in the info dialogs.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ButtonAttribute : Attribute {
		/// <summary>The name of the button which will be connected with the method decorated by this attribute</summary>
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

	/// <summary>
	/// Used for marking classes used as descriptors - see SimpleClassDescriptor for example.
	/// Obligatory constructor parameter is handled type, voluntary is the name of the described class
	/// </summary>
	public class ViewDescriptorAttribute : Attribute {
		private Type handledType;

		/// <summary>The name that will be displayed in the headline of the infodialog</summary>
		private string name;

		/// <summary>Array of field names that wont be generated to the DataView</summary>
		private string[] nonDisplayedFields;

		public Type HandledType {
			get {
				return handledType;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public string[] NonDisplayedFields {
			get {
				return nonDisplayedFields;
			}
		}

		public ViewDescriptorAttribute(Type handledType) {
			this.handledType = handledType;
		}

		public ViewDescriptorAttribute(Type handledType, string name) {
			this.handledType = handledType;
			this.name = name;
		}

		public ViewDescriptorAttribute(Type handledType, string name, string[] nonDisplayedFields) {
			this.handledType = handledType;
			this.name = name;
			this.nonDisplayedFields = nonDisplayedFields;
		}
	}

	/// <summary>Used for marking field get method in descriptors</summary>
	public class GetMethodAttribute : Attribute {
		/// <summary>
		/// The name of the field that appears in the info dialog. Obligatory, it will be used for matching get and set 
		///  descriptor method of the same field
		///  </summary>
		private string name;
		private Type fieldType;

		public string Name {
			get {
				return name;
			}
		}

		public Type FieldType {
			get {
				return fieldType;
			}
		}

		public GetMethodAttribute(string name, Type fieldType) {
			this.name = name;
			this.fieldType = fieldType;
		}
	}

	/// <summary>Used for marking field set method in descriptors</summary>
	public class SetMethodAttribute : Attribute {
		/// <summary>
		/// The name of the field that appears in the info dialog. Obligatory, it will be used for matching get and set 
		///  descriptor method of the same field
		///  </summary>
		private string name;
		private Type fieldType;

		public string Name {
			get {
				return name;
			}
		}

		public Type FieldType {
			get {
				return fieldType;
			}
		}

		public SetMethodAttribute(string name, Type fieldType) {
			this.name = name;
			this.fieldType = fieldType;
		}
	}

	/// <summary>For comparing collections containing Types</summary>
	public class TypeHierarchyComparer : IComparer<Type> {
		public static TypeHierarchyComparer instance = new TypeHierarchyComparer();

		private TypeHierarchyComparer() {
		}

		public int Compare(Type x, Type y) {
			if (x.IsSubclassOf(y)) {
				return 1;
			} else if (x == y) {
				return 0;
			} else {
				return -1;
			}
		}
	}
}