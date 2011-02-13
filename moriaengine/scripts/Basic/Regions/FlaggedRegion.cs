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
using SteamEngine;
using SteamEngine.CompiledScripts;
using SteamEngine.Persistence;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.Regions {

	[Flags]
	public enum RegionFlags {
		Zero = 0, None = 0,
		NoMagicIn = 0x0001,
		NoMagicOut = 0x0002,
		NoHarmfulMagicIn = 0x0004,
		NoHarmfulMagicOut = 0x0008,
		NoBeneficialMagicIn = 0x0010,
		NoBeneficialMagicOut = 0x0020,
		NoTeleportingIn = 0x0040,
		NoTeleportingOut = 0x0080,
		NoEnemyTeleportingIn = 0x0100,
		NoEnemyTeleportingOut = 0x0200,

		Underground = 0x0800, Dungeon = Underground,
	}

	//todo: make some members virtual?
	[SaveableClass]
	[ViewableClass]
	public class FlaggedRegion : StaticRegion {
		private RegionFlags flags;

		[LoadSection]
		public FlaggedRegion(PropsSection input)
			: base(input) {
		}

		public FlaggedRegion()
			: base() {
		}

		public FlaggedRegion(string defname, Region parent)
			: base() {
			this.Defname = defname;
			this.Parent = parent;
		}

		public RegionFlags Flags {
			get {
				return this.flags;
			}
			set {
				this.flags = value;
			}
		}

		public bool Flag_Underground {
			get {//positive value isinherited from parent - underground stays underground
				if ((this.flags & RegionFlags.Underground) == RegionFlags.Underground) {
					return true;
				} else {
					FlaggedRegion parentAsFlagged = this.Parent as FlaggedRegion;
					if (parentAsFlagged != null) {
						return parentAsFlagged.Flag_Underground;
					}
				}
				return false;
			}
			set {
				if (value) {
					this.flags |= RegionFlags.Underground;
				} else {
					this.flags &= ~RegionFlags.Underground;
				}
				//todo? refresh light and stuff of present players?
			}
		}

		public override TriggerResult On_Enter(AbstractCharacter ch, bool forced) {
			Player asPlayer = ch as Player;
			if (asPlayer != null) {
				asPlayer.SendGlobalLightLevel(LightAndWeather.GetLightIn(this));
			}

			return TriggerResult.Continue;//if forced is true, the return value is irrelevant
		}

		public override TriggerResult On_Exit(AbstractCharacter ch, bool forced) {
			Player asPlayer = ch as Player;
			if (asPlayer != null) {
				//we're exiting this one, hence "re-entering" the parent
				FlaggedRegion parentAsFlagged = this.Parent as FlaggedRegion;
				if (parentAsFlagged != null) {
					asPlayer.SendGlobalLightLevel(LightAndWeather.GetLightIn(parentAsFlagged));
				}
			}

			return TriggerResult.Continue;//if forced is true, the return value is irrelevant
		}

		#region Persistence
		public override void Save(SteamEngine.Persistence.SaveStream output) {
			ThrowIfDeleted();
			base.Save(output);//Region save

			if (this.flags != RegionFlags.Zero) {
				output.WriteValue("flags", flags);
			}
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			ThrowIfDeleted();
			switch (valueName) {
				case "flag_announce":
					LoadSpecificFlag(filename, line, 0x00200, valueString);
					break;
				case "flag_antimagic_all":
					LoadSpecificFlag(filename, line, 0x00001, valueString);
					break;
				case "flag_antimagic_damage":
					LoadSpecificFlag(filename, line, 0x00020, valueString);
					break;
				case "flag_antimagic_gate":
					LoadSpecificFlag(filename, line, 0x00008, valueString);
					break;
				case "flag_antimagic_recallin":
					LoadSpecificFlag(filename, line, 0x00002, valueString);
					break;
				case "flag_antimagic_recallout":
					LoadSpecificFlag(filename, line, 0x00004, valueString);
					break;
				case "flag_antimagic_teleport":
					LoadSpecificFlag(filename, line, 0x00010, valueString);
					break;
				case "flag_arena":
					LoadSpecificFlag(filename, line, 0x10000, valueString);
					break;
				case "flag_guarded":
					LoadSpecificFlag(filename, line, 0x04000, valueString);
					break;
				case "flag_instalogout":
					LoadSpecificFlag(filename, line, 0x00400, valueString);
					break;
				case "flag_nobuilding":
					LoadSpecificFlag(filename, line, 0x00080, valueString);
					break;
				case "flag_nodecay":
					LoadSpecificFlag(filename, line, 0x01000, valueString);
					break;
				case "flag_nopvp":
					LoadSpecificFlag(filename, line, 0x08000, valueString);
					break;
				//case "flag_roof"://"this region has a roof" - wtf does that mean? -tar
				//	LoadSpecificFlag(filename, line, 0x20000, args);
				//	break;
				case "flag_safe":
					LoadSpecificFlag(filename, line, 0x02000, valueString);
					break;
				case "flag_ship":
					LoadSpecificFlag(filename, line, 0x00040, valueString);
					break;
				case "flag_underground":
				case "flag_dungeon":
					LoadSpecificFlag(RegionFlags.Underground, valueString);
					break;
				//case "flag_unused":
				//	LoadSpecificFlag(0x00100, args); 
				//	break;

				case "flag":
				case "flags":
					this.flags = (RegionFlags) TagMath.ParseInt32(valueString);
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);//the Region Loadline
					break;
			}
		}

		private void LoadSpecificFlag(string filename, int line, int mask, string args) {
			if (TagMath.ParseBoolean(args)) {//args is 1 or true or something like that
				this.flags |= (RegionFlags) mask;
			}
		}

		private void LoadSpecificFlag(RegionFlags mask, string args) {
			if (TagMath.ParseBoolean(args)) {//args is 1 or true or something like that
				this.flags |= mask;
			}
		}
		#endregion Persistence
	}
}
