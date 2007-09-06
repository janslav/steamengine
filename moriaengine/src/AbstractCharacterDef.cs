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
using System.Reflection;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine {	
	
	public abstract class AbstractCharacterDef : ThingDef {
		private FieldValue sound;
		private FieldValue mountItem;
		private FieldValue animsAvailable;

		public AbstractCharacterDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
			sound = InitField_Typed("sound", 0, typeof(ushort));
			mountItem = InitField_Model("mountItem", 0);
			animsAvailable = InitField_Typed("animsAvailable", 0, typeof(uint));//not sure if it's supposed to be 0 really :)
		}
		
		public SoundFX Sound {
			get {
				return (SoundFX)(ushort) sound.CurrentValue;
			} set {
				sound.CurrentValue=(ushort)value;
			}
		}
		
		
		public ushort MountItem { 
			get {
				return (ushort) mountItem.CurrentValue; 
			} 
			set {
				mountItem.CurrentValue = value; 
			}
		}
		
		
		public uint AnimsAvailable { 
			get {
				return (uint) animsAvailable.CurrentValue; 
			} 
			set {
				animsAvailable.CurrentValue = value; 
			}
		}

		public override sealed bool IsItemDef { get {
			return false;
		} }
		
		public override sealed bool IsCharDef { get {
			return true;
		} }
		
		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if ("anim".Equals(param)) {
				param = "animsavailable";
			}
			base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
		}
	}
}
