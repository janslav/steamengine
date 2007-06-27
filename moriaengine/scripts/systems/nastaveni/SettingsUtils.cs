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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	[Remark("Common ancestor for settings groups, subgroups and values. Has defined name, which is displayed"+
			"in the dialog. Has also the writeOut method for writing itself to the dialog")]
	public abstract class AbstractSetting {
		[Remark("Name of the Settings (group or value). This name is obtained from one of the SavedMember attribute's "+
				"field.")]
		protected string name;
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		[Remark("Level in the dialog - the category is 1st, its members are 2nd. If one of the 2nd members is not "+
				"primitive saveable and contains other saved members or saveable data then these are 3rd etc.")]
		protected int level;
		public int Level {
			get {
				return level;
			}
			set {
				level = value;
			}
		}

		[Remark("Value of the inner laying member. If we are dealing with s SettingsValue then this is the"+
				"stored value of the inner laying member/field. If we are dealing with SettingsCategory and the "+
				"category is 'inner', then this is the value of the category field. If this is the root virtual category, "+
				"then this will be simple null..."+
				"The value simplifies displaying the value of the settings item in the dialog and in case of the categories "+
				"it is necessary for loading or setting the underlaying non-static children member's values.")]
		protected object value;
		public object Value {
			get {
				return value;
			}
			set {
				this.value = value;
			}
		}

		[Remark("Write the setting to the dialog. The change in rowCounter will be propagated back")]
		public abstract void WriteSetting(ImprovedDialog dlg);

		[Remark("Write the settings label (used especially for writing the categories name).")]
		public abstract void WriteLabel(ImprovedDialog dlg); 

		[Remark("Voluntary method for writing the input field for setting - not used for categories")]
		protected virtual void WriteInput() {
		}

		[Remark("Get the full path of the member - it is either the category name or the "+
				"full path from the most parental category over the inner categories to the"+
				"members name itself (depends on the objects type)."+
				"This is used for displaying in the settings results dialog")]
		public abstract string FullPath();
	}

	[Remark("The class for holding the information about the settings category."+
		    "The categories are created from the Name and other AbstractSettings belonging to the category" +
			"These settings can be primitive saveables such as strings, dates, numbers; or they can be"+
			"a composed setting 'subcategory' SavedMember, holding inside its own SavedMembers"+
			"See the DBManager and its DBConfig field for example."+
			"All of this information is obtained via the 'SavedMember' or 'SaveableData' attributes")]
	public class SettingsCategory : AbstractSetting {
		private AbstractSetting[] members;
		public AbstractSetting[] Members {
			get {
				return members;
			}
			set {
				members = value;
			}
		}

		public SettingsCategory(string name) {
			this.name = name;
			this.level = 0;
		}

		public SettingsCategory(string name, AbstractSetting[] members)	: this(name) {
			this.members = members;
		}

		[Remark("The overloaded []. Enables us to directly insert the underlaying subgroups or saveable primitives"+
				"as well as to access these on the given position."+
				"This way of inserting allows us to set the proper level in the dialog (indentation)")]
		public AbstractSetting this[int pos] {
			get {
				return members[pos];
			}
			set {
				//just add the given item to the list, neverminding the position.
				//the position is used just for getting !
				members[pos]=value;				
			}
		}

		public override void WriteSetting(ImprovedDialog dlg) {
			WriteLabel(dlg);
			WriteMembers(dlg);
		}

		[Remark("Write the categories label, using appropriate level indentation")]
		public override void WriteLabel(ImprovedDialog dlg) {
			dlg.LastTable[D_Static_Settings.Instance.RowCounter++, D_Static_Settings.Instance.FilledColumn] = TextFactory.CreateText(D_Static_Settings.ITEM_INDENT * level, 0, Hues.SettingsTitleColor, name);			
		}

		[Remark("Iterate through the array of inner members, set their level and write them also out")]
		internal void WriteMembers(ImprovedDialog dlg) {
			foreach(AbstractSetting abss in members) {
				abss.Level = this.level + 1; //for all cases, set it now
				abss.WriteSetting(dlg);
			}
		}

		[Remark("This is the category - return simply its name")]
		public override string FullPath() {
			return name;
		}

		[Remark("Iterates through category members and if member is a SettingValue then it clears"+
				"its color, oldvalue and newvalue fields. If the member is another category then it"+
				"clears it recursively")]
		public void ClearSettingValues() {
			foreach(AbstractSetting aSet in members) {
				if(aSet is SettingsValue) {					
					SettingsValue sval = (SettingsValue)aSet;
					sval.NewValue = "";
					sval.OldValue = "";
					sval.Color = Hues.WriteColor; //default color of writing
				} else {
					//recursive call
					((SettingsCategory)aSet).ClearSettingValues();
				}
			}
		}
	}

	[Remark("The class for holding the information about one single value (primitive saveable) in the "+
			"settings dialog. It stores its description (label in the dialog) and the MemberInfo of "+
			"this field.")]
	public class SettingsValue : AbstractSetting {
		private MemberInfo member;
		public MemberInfo Member {
			get {
				return member;
			}
			set {
				member = value;
			}
		}

		[Remark("A parent category of this settings value. In case of inner categories this link will enable us to"+
				"access the parents category's instance for loading this member's own value.")]
		private SettingsCategory parent;
		public SettingsCategory Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		[Remark("Color of this dialog item, if everything is OK then this field is null which causes"+
				"the member to be displayed in normal color. If there was some problem during"+
				"applying new settings (such as incorrect data type) then the color is set and"+
				"the member is displayed in this color")]
		private Hues color;
		public Hues Color {
			get {
				return color;
			}
			set {
				color = value;
			}
		}

		[Remark("Here will be the new value tried to be set as this member's value. If setting is"+
				"unsuccessful then this tried value will be displayed in the after-setting overview.")]
		private string newValue;
		public string NewValue {
			get {
				return newValue == null ? "" : newValue;				
			}
			set {
				newValue = value;
			}
		}

		[Remark("Here will be the old member's value after succesfull setting. We will use it to display"+
				"the after-setting overview.")]		
		private string oldValue;
		public string OldValue {
			get {
				return oldValue == null ? "" : oldValue;				
			}
			set {
				oldValue = value;
			}
		}

		[Remark("The prefix from the objects value obtained by ObjectSaver.Save method.")]
		private string valuesPrefix;
		public string ValuesPrefix {
			get {
				return valuesPrefix;
			}
			set {
				valuesPrefix = value;
			}
		}

		public SettingsValue(string name, MemberInfo member) {
			this.level = 0;
			this.name = name;
			this.member = member;
			this.color = Hues.WriteColor; //default color of writing
		}

		[Remark("Create the label and an input field to the dialog. Use appropriate indentation according to the level")]
		public override void WriteSetting(ImprovedDialog dlg) {
			D_Static_Settings inst = D_Static_Settings.Instance;
			WriteLabel(dlg);
			//check the size of the underlaying table - if we are writing the category containing more values
			//it is possible to override the normal rowcount in the table (we dont want the category to split)
			//therefore it would be necessary to enlarge the rowcount in the table to avoid exceptions
			if(inst.RowCounter >= dlg.LastTable.RowCount) {
				dlg.LastTable.RowCount++; //add one more row
			}
						//write out the input field, indent it according to the level plus add indentation from the label
			int indent = D_Static_Settings.ITEM_INDENT * level + D_Static_Settings.INPUT_INDENT; //start of the input field
			
			//increase the input ID counter
			inst.DlgIndex++;
			//display the value of the settings member in its "saved" form - including prefixes e.t.c
			dlg.LastTable[inst.RowCounter++, inst.FilledColumn] = InputFactory.CreateInput(LeafComponentTypes.InputText, indent, 0, inst.DlgIndex, D_Static_Settings.innerWidth / 4 - indent, ImprovedDialog.D_ROW_HEIGHT, color, SettingsUtilities.WriteValue(value,this));
			inst.valuesToSet[inst.DlgIndex] = this; //store the info about the filled SettingsValue (for further purposes such as accepting setting etc)
		}

		[Remark("Write the value's label, using appropriate level indentation")]
		public override void WriteLabel(ImprovedDialog dlg) {
			//check the color, if null then simple write out, no colors, nothing...
			//the color will be not null only in case of unsuccessfull setting of this member
			dlg.LastTable[D_Static_Settings.Instance.RowCounter, D_Static_Settings.Instance.FilledColumn] = TextFactory.CreateText(D_Static_Settings.ITEM_INDENT * level, 0, color, name);			
		}

		[Remark("Get the newVal and try to set it as the new value to the undelraying member."+
				"If successful then OK, if no, mark this value as unsuccessfully set for further"+
				"displaying.")]
		public void TrySet(string newValInserted) {
			try {
				//first get the string representation of the old value (including prefix - #, :, :: etc.)
				string fullOldVal = ObjectSaver.Save(value);

				//get the inserted string, and transform it to its object representation
				object newValLoaded = SettingsUtilities.ReadValue(newValInserted,this); 
				//what's the Type of the field hidden in this member? (we dont care about the value, so we use just simple anonymous object)
				object membersValueUnused = null;
				Type valType = SettingsUtilities.GetMemberType(member, parent.Value, out membersValueUnused);
				//cast the objectified inserted new value to the type of the old member's value
				//the newValLoaded is object of certain type, we will just find out whether the object
				//is of the correct, member's, type
				object newValObj = null;
				if (ConvertTools.TryConvertTo(valType, newValLoaded, out newValObj) && newValLoaded != null) {
					string fullNewVal = ObjectSaver.Save(newValObj);//get the new value in the stringified (prefixes...) form
					if(!fullOldVal.Equals(fullNewVal)) {
						//the old and new values aren't the same - make a setting
						//the parent.Value will be used only in case the member is non-static field
						//otherwise it wont be used (and it is also null here)
						SettingsUtilities.SetMemberValue(member, parent.Value, newValObj); //set the member itself
						oldValue = fullOldVal; //store the previous value for informational purposes
						value = newValObj; //set the new value for displaying in the settings dialog
					}
				} else {
					//cast exception - incorrect data tried to be set (e.g. string as a number etc)
					color = Hues.Red; //set the error color 
					newValue = newValInserted; //set the unsuccessful setting for informational purposes
				}
			} catch(SEException sex) {
				//there are lots of saves and loads - many potentional exceptions
				//in ou case any exception means unsuccessfull settings - we will display 
				//the values
				color = Hues.Red; //set the error color 
				newValue = newValInserted; //set the unsuccessful setting for informational purposes
			}
		}

		[Remark("This is the member in a category (or even in some inner category"+
				"- return its categories name concatenated with the members own name")]
		public override string FullPath() {
			if(parent != null) {
				return parent.FullPath() + " -> " + name;
			} else {
				return name;
			}			
		}
	}

	[Remark("Utility class containing methods for working with Members (getting values, setting them "+
			"recognizing data types etc")]
	internal static class SettingsUtilities {
		[Remark("This hashtable will contain object types as keys with its corresponding prefixes as values")]
		private static Hashtable prefixTypes; 

		[Remark("Return the type of the type referenced by given MemberInfo. " +
				"It will also set the provided object with the member's value using the" +
				"parent's value to get it.")]
		public static Type GetMemberType(MemberInfo mi, object parentValue, out object value) {
			if(mi.MemberType == MemberTypes.Property) {
				PropertyInfo pi = (PropertyInfo)mi;
				try {
					value = pi.GetValue(parentValue, null);
				} catch(TargetException tae) { //the value still could not be found
					value = null;
				}
				return pi.PropertyType;
			} else {
				FieldInfo fi = (FieldInfo)mi;
				if(fi.IsStatic) {
					//do not bother with any parent values, static field is static field and has only one value...
					//we dont need any info about the parent's instance (if any)
					value = fi.GetValue(null);
				} else {
					try {
						value = fi.GetValue(parentValue); //we expect the parent's instance here
					} catch(TargetException mae) {
						//something wrong
						value = null;
					}
				}
				return fi.FieldType;
			}
		}

		[Remark("Set the member with given value. If the member is statical, then it is easy to proceed" +
				"but non-statical members must have the parents object reference to successfully set " +
				"their new value, returns true or false if the setting is successful or not.")]
		public static bool SetMemberValue(MemberInfo mi, object parentValue, object value) {
			if(mi.MemberType == MemberTypes.Property) {
				PropertyInfo pi = (PropertyInfo)mi;
				try {
					pi.SetValue(parentValue, value, null);
				} catch(TargetException tae) { //the value still could not be found
					return false;
				}
			} else {
				FieldInfo fi = (FieldInfo)mi;
				if(fi.IsStatic) {
					//do not bother with any parent values, static field is static field and has only one value...
					//we dont need any info about the parent's instance (if any)
					fi.SetValue(null, value);
				} else {
					try {
						fi.SetValue(parentValue, value); //we expect the parent's instance here
					} catch(TargetException tae) {
						//something wrong
						return false;
					}
				}
			}
			return true; //success
		}

		[Remark("Return the value of the given MemberInfo. Use the static way to obtain it so it is not " +
				"applicable for the non static fields or members.")]
		public static object GetMemberValue(MemberInfo mi) {
			if(mi.MemberType == MemberTypes.Property) {
				return ((PropertyInfo)mi).GetValue(null, null);
			} else {
				return ((FieldInfo)mi).GetValue(null);
			}
		}

		[Remark("Find the value type and decide what is its prefix when used ObjectSaver.Save, then"+
				"remove this prefix to obtain pure stringified value. The found prefix will be stored"+
				"in its parent SettingsValue")]
		public static string WriteValue(object value, SettingsValue sval) {
			string valuePrefix = "";
			if (prefixTypes == null) {
				prefixTypes = new Hashtable();
			}
			if (prefixTypes.ContainsKey(value.GetType())) {
				//prefix is already stored
				valuePrefix = (String)prefixTypes[value.GetType()];
			} else {
				//prefix is to be found first
				Type t = value.GetType();

				//types like Enum, Numbers, String, Regions  or Globals doesn't have any prefixes, they will be displayed as is
				if (t.IsEnum || TagMath.IsNumberType(t) || t.Equals(typeof(String))
					|| typeof(Region).IsAssignableFrom(t) || value == Globals.Instance) {
				} else if (typeof(Thing).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else if (t.Equals(typeof(TimeSpan))) {
					valuePrefix = ":";
				} else if (t.Equals(typeof(DateTime))) {
					valuePrefix = "::";
				} else if (typeof(GameAccount).IsAssignableFrom(t)) {
					valuePrefix = "$";
				} else if (typeof(AbstractScript).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else {
					//it must be this type of class, nothing else should occur in settings !!!
					ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
					if (iss != null) {
						valuePrefix = iss.Prefix;
					}
				}
				//time to store the prefix
				prefixTypes[t] = valuePrefix;
			}
			//now save the value, and strip the prefix
			string savedValue = ObjectSaver.Save(value);
				//just take the rest of the string aftrer the prefix
			savedValue = savedValue.Substring(savedValue.IndexOf(valuePrefix) + valuePrefix.Length);
			//store the prefix for 
			sval.ValuesPrefix = valuePrefix;

			return savedValue;
		}

		[Remark("This is another metod for loading data - it takes the selected string, "+
				"finds out its real type and adds to it the prefix we stripped off before so its "+
				"again available for ObjectSaver.Load method to be stored.")]
		public static object ReadValue(string value, SettingsValue sval) {
			//add the stored prefix to the obtained string from the dialog and transform it to the
			//object instance which will be then stored to the member
			try {
				return ObjectSaver.Load(sval.ValuesPrefix + value);
			} catch (SEException sex) {
				//loading fails - probably wrong data inserted...
				return null;
			} catch (FormatException fex) {
				//this happens at least when invalid IP address was entered
				return null;
			}
		}
	}
}