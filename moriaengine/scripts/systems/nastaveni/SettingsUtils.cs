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
//using System.Collections;
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
			//if(inst.valuesToSet[inst.DlgIndex] != null) {
			//    //the value with this index actually exists in the list -
			//    //this occures if we are redisplaying the dialog after the settings
			//    //some values may have been set incorrectly => therefore they will have different color etc.
			//    SettingsValue existingVal = (SettingsValue)inst.valuesToSet[inst.DlgIndex];
			//    dlg.LastTable[inst.RowCounter++, inst.FilledColumn] = InputFactory.CreateInput(LeafComponentTypes.InputText, indent, 0, inst.DlgIndex, D_Static_Settings.innerWidth / 4 - indent, ImprovedDialog.D_ROW_HEIGHT, existingVal.color, value.ToString());
			//} else {
				//non existing (first displaying of the dialog, or redisplaying after paging) - create a new input field
				dlg.LastTable[inst.RowCounter++, inst.FilledColumn] = InputFactory.CreateInput(LeafComponentTypes.InputText, indent, 0, inst.DlgIndex, D_Static_Settings.innerWidth / 4 - indent, ImprovedDialog.D_ROW_HEIGHT, color, value.ToString());
				inst.valuesToSet[inst.DlgIndex] = this; //store the info about the filled SettingsValue (for further purposes such as accepting setting etc)
			//}
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
		public void TrySet(string newVal) {
			//first get the string representation of the old value (including prefix - #, :, :: etc.)
			string fullOldVal = ObjectSaver.Save(value);
			//what's the Type of the field hidden in this member? (we dont care about the value, so we use just simple anonymous object)
			object membersValueUnused = null;
			Type valType = ObjectSaver.GetMemberType(member, parent.Value, out membersValueUnused); 
			//cast the stringified new value to the type of the old value
			object newValObj = null;
			if(ConvertTools.TryConvertTo(valType, newVal, out newValObj)) {
				string fullNewVal = ObjectSaver.Save(newValObj);//get the new value in the stringified (prefixes...) form
				if(!fullOldVal.Equals(fullNewVal)) {
					//the old and new values aren't the same - make a setting
					//the parent.Value will be used only in case the member is non-static field
					//otherwise it wont be used (and it is also null here)
					ObjectSaver.SetMemberValue(member, parent.Value, newValObj); //set the member itself
					oldValue = value.ToString(); //store the previous value for informational purposes
					value = newValObj.ToString(); //set the new value for displaying in the settings dialog
				}
			} else {
				//cast exception - incorrect data tried to be set (e.g. string as a number etc)
				color = Hues.Red; //set the error color 
				newValue = newVal; //set the unsuccessful setting for informational purposes
			}			
		}

		[Remark("This is the member in a category (or even in some inner category"+
				"- return its categories name concatenated with the members own name")]
		public override string FullPath() {
			if(parent != null) {
				return parent.FullPath() + "->" + name;
			} else {
				return name;
			}			
		}

	}
}