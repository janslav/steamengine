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
using SteamEngine.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	
	//todo: make some members virtual?
	[SaveableClass]
	public class FlaggedRegion : Region {
		int flags;

		[LoadSection]
		public FlaggedRegion(PropsSection input)
			: base(input) {
		}

		public FlaggedRegion() 
				: base() {
		}
		
		public int Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		//public override bool On_Enter(AbstractCharacter ch) {
		//	return unloaded;//maybe we could just return false or whatever...
		//}
		//
		//public override bool On_Exit(AbstractCharacter ch) {
		//	return false;
		//}

		public override void Save(SteamEngine.Persistence.SaveStream output) {
			base.Save(output);//Region save

			if (flags != 0) {
				output.WriteValue("flags", flags);
			}
		}

		protected override void LoadLine(string filename, int line, string param, string args) {
			switch(param) {
				case "flag_announce":
					LoadSpecificFlag(filename, line, 0x00200, args);
					break;
				case "flag_antimagic_all":
					LoadSpecificFlag(filename, line, 0x00001, args);
					break;
				case "flag_antimagic_damage":
					LoadSpecificFlag(filename, line, 0x00020, args);
					break;
				case "flag_antimagic_gate":
					LoadSpecificFlag(filename, line, 0x00008, args);
					break;
				case "flag_antimagic_recallin":
					LoadSpecificFlag(filename, line, 0x00002, args);
					break;
				case "flag_antimagic_recallout":
					LoadSpecificFlag(filename, line, 0x00004, args);
					break;
				case "flag_antimagic_teleport":
					LoadSpecificFlag(filename, line, 0x00010, args);
					break;
				case "flag_arena":
					LoadSpecificFlag(filename, line, 0x10000, args);
					break;
				case "flag_guarded":
					LoadSpecificFlag(filename, line, 0x04000, args);
					break;
				case "flag_instalogout":
					LoadSpecificFlag(filename, line, 0x00400, args);
					break;
				case "flag_nobuilding":
					LoadSpecificFlag(filename, line, 0x00080, args);
					break;
				case "flag_nodecay":
					LoadSpecificFlag(filename, line, 0x01000, args);
					break;
				case "flag_nopvp":
					LoadSpecificFlag(filename, line, 0x08000, args);
					break;
				//case "flag_roof"://"this region has a roof" - wtf does that mean? -tar
				//	LoadSpecificFlag(filename, line, 0x20000, args);
				//	break;
				case "flag_safe":
					LoadSpecificFlag(filename, line, 0x02000, args);
					break;
				case "flag_ship":
					LoadSpecificFlag(filename, line, 0x00040, args);
					break;
				case "flag_underground":
					LoadSpecificFlag(filename, line, 0x00800, args); 
					break;
				//case "flag_unused":
				//	LoadSpecificFlag(0x00100, args); 
				//	break;

				case "flag":
				case "flags":
					flags = TagMath.ParseInt32(args);
					break;
				default:
					base.LoadLine(filename, line, param, args);//the Region Loadline
					break;
			}
		}

		//returns a rectangle "around" this region. I used it for UOMapRenderer rendering a view on a city :)
		public Rectangle2D GetEscribedRectangle() {
			ushort minX = ushort.MaxValue;//left
			ushort maxX = ushort.MinValue;//right
			ushort minY = ushort.MaxValue;//upper
			ushort maxY = ushort.MinValue;//lower
			foreach (Rectangle2D rect in Rectangles) {
				if (rect.StartPoint.X < minX) {
					minX = rect.StartPoint.X;
				}
				if (rect.StartPoint.Y < minY) {
					minY = rect.StartPoint.Y;
				}
				if (rect.EndPoint.X > maxX) {
					maxX = rect.EndPoint.X;
				}
				if (rect.StartPoint.Y > maxY) {
					maxY = rect.EndPoint.Y;
				}
			}
			return new Rectangle2D(minX, minY, maxX, maxY);
		}
		
		private void LoadSpecificFlag(string filename, int line, int mask, string args) {
			if (TagMath.ParseBoolean(args)) {//args is 1 or true or something like that
				flags |= mask;
			}
		}
	}
}
