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
using SteamEngine;
using SteamEngine.Common;


namespace SteamEngine.CompiledScripts {
    [Dialogs.ViewableClass]
    public partial class Door : Item {

        public bool opened = false;

        public static bool IsOpen(Door door) {
            if (door.opened == true) {
                return true;
            } else {
                return false;
            }
        }

        public override void On_DClick(AbstractCharacter user) {
            Character person = user as Character;
            if (person.CanReach(this) == DenyResult.Allow) {
                if (opened == false) {
                    Open(person);
                } else {
                    Close(person);
                }
            }
        }

        public void Open(Character person) {
            opened = true;
        }

        public void Close(Character person) {
            opened = false;
        }

        [Summary("Method returning info about doors. E.G. opened, direction, description...")]
        public DoorType Info() {
            ushort grafic = this.Model;
            DoorType dType = null;
            if (grafic >= 0x675 && grafic <= 0x684) {
                dType.baseType = 0x675;
                dType.description = "iron door";
            } else if (grafic >= 0x685 && grafic <= 0x694) {
                dType.baseType = 0x685;
                dType.description = "iron bar door";
            } else if (grafic >= 0x695 && grafic <= 0x6a4) {
                dType.baseType = 0x695;
                dType.description = "rattan door";
            } else if (grafic >= 0x6a5 && grafic <= 0x6b4) {
                dType.baseType = 0x6a5;
                dType.description = "dark wooden door";
            } else if (grafic >= 0x6b5 && grafic <= 0x6c4) {
                dType.baseType = 0x6b5;
                dType.description = "wooden dor";
            } else if (grafic >= 0x6c5 && grafic <= 0x6d4) {
                dType.baseType = 0x6c5;
                dType.description = "iron door";
            } else if (grafic >= 0x6d5 && grafic <= 0x6e4) {
                dType.baseType = 0x6d5;
                dType.description = "light wooden door";
            } else if (grafic >= 0x6e5 && grafic <= 0x6f4) {
                dType.baseType = 0x6e5;
                dType.description = "dark wooden door with iron";
            } else if (grafic >= 0x824 && grafic <= 0x833) {
                dType.baseType = 0x824;
                dType.description = "iron bar gate external";
            } else if (grafic >= 0x839 && grafic <= 0x848) {
                dType.baseType = 0x839;
                dType.description = "light wooden short door";
            } else if (grafic >= 0x84c && grafic <= 0x85b) {
                dType.baseType = 0x84c;
                dType.description = "iron bar gate external";
            } else if (grafic >= 0x866 && grafic <= 0x875) {
                dType.baseType = 0x866;
                dType.description = "dark wooden short door";
            } else {
                throw new SEException("This isn´t a door.");
            }
            return RotationAndDirection(dType, grafic);
        }

        private static DoorType RotationAndDirection(DoorType dt, ushort grafic) {
            ushort rd = (ushort)(grafic - dt.baseType);
            if (rd == 0) {
                dt.direction = Direction.North;
                dt.opened = false;
                dt.rotation = Rotation.Left;
            } else if (rd == 1) {
                dt.direction = Direction.North;
                dt.opened = true;
                dt.rotation = Rotation.Left;
            } else if (rd == 2) {
                dt.direction = Direction.North;
                dt.opened = false;
                dt.rotation = Rotation.Right;
            } else if (rd == 3) {
                dt.direction = Direction.North;
                dt.opened = true;
                dt.rotation = Rotation.Right;
            } else if (rd == 4) {
                dt.direction = Direction.South;
                dt.opened = false;
                dt.rotation = Rotation.Left;
            } else if (rd == 5) {
                dt.direction = Direction.South;
                dt.opened = true;
                dt.rotation = Rotation.Left;
            } else if (rd == 6) {
                dt.direction = Direction.South;
                dt.opened = false;
                dt.rotation = Rotation.Right;
            } else if (rd == 7) {
                dt.direction = Direction.South;
                dt.opened = true;
                dt.rotation = Rotation.Right;
            } else if (rd == 8) {
                dt.direction = Direction.West;
                dt.opened = false;
                dt.rotation = Rotation.Left;
            } else if (rd == 9) {
                dt.direction = Direction.West;
                dt.opened = true;
                dt.rotation = Rotation.Left;
            } else if (rd == 10) {
                dt.direction = Direction.West;
                dt.opened = false;
                dt.rotation = Rotation.Right;
            } else if (rd == 11) {
                dt.direction = Direction.West;
                dt.opened = true;
                dt.rotation = Rotation.Right;
            } else if (rd == 12) {
                dt.direction = Direction.East;
                dt.opened = false;
                dt.rotation = Rotation.Left;
            } else if (rd == 13) {
                dt.direction = Direction.East;
                dt.opened = true;
                dt.rotation = Rotation.Left;
            } else if (rd == 14) {
                dt.direction = Direction.East;
                dt.opened = false;
                dt.rotation = Rotation.Right;
            } else if (rd == 15) {
                dt.direction = Direction.East;
                dt.opened = true;
                dt.rotation = Rotation.Right;
            }
            return dt;
        }
    }

    [Summary("Return type, containing info about doors. E.G. opened, direction, description...")]
    public class DoorType {
        public bool opened;
        public Direction direction;
        public Rotation rotation;
        public ushort baseType;
        public string description;
    }


    [Summary("Does doors rotate by left or right side?")]
    public enum Rotation {
        Left,
        Right
    }
}