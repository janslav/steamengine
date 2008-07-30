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

        private ushort rd = 0;
        ushort[] bType = new ushort[] {
            0xE8,     //secret stone door		
            0x314,    //secret stone door
            0x324,    //secret stone door
            0x334,    //secret wooden door - chestnut
            0x344,    //secret wooden door - spruce
            0x354,    //secret stone door
            0x675,    //iron door
            0x685,    //iron bar door
            0x695,    //rattan door
            0x6a5,    //dark wooden door
            0x6b5,    //wooden door
            0x6c5,    //iron door
            0x6d5,    //light wooden door
            0x6e5,    //dark wooden door with iron
            0x824,    //iron bar gate external
            0x839,    //light wooden short door
            0x84c,    //iron bar gate external
            0x866,    //dark wooden short door
            0x1fed,   //barred metal door
            0x190e    //bar door - retarded doors, only 2 grafics. needs to stay last
        };

        private void Validity() {
            if (this.GetBaseType() == 0x190e) {
                if (!((rd == 1) || (rd == 0))) {
                    throw new SEException("rd of retarded BAR doors is out of range.");
                } else {
                    rd = (ushort)(this.Model - this.GetBaseType());
                }
            }
            if (rd != (this.Model - this.GetBaseType())) {
                if ((rd > 15) || (rd < 0)) {
                    throw new SEException("rd of doors is out of range.");
                } else {
                    rd = (ushort)(this.Model - this.GetBaseType());
                }
            }
        }

        public override void On_DClick(AbstractCharacter user) {
            this.Validity();
            if (!this.IsOpen()) {
                Trigger_Open();
            } else {
                Trigger_Close();
            }
        }

        public void Trigger_Open() {
            On_DenyOpen();
            On_Open();
            SetOpen();
        }

        public void Trigger_Close() {
            On_DenyClose();
            On_Close();
            SetClose();
        }

        public void On_DenyOpen() { }

        public void On_Open() { }

        public void On_DenyClose() { }

        public void On_Close() { }

        public void SetOpen() {
            if (!this.IsOpen()) {           //For case of opening opened doors.
                switch (rd) {
                    case 0:
                        this.X--;
                        this.Y++;
                        break;
                    case 2:
                        this.X++;
                        this.Y++;
                        break;
                    case 4:
                        this.X--;
                        break;
                    case 6:
                        this.X++;
                        this.Y--;
                        break;
                    case 8:
                        this.X++;
                        this.Y++;
                        break;
                    case 10:
                        this.X++;
                        this.Y--;
                        break;
                    case 12:
                        break;
                    case 14:
                        this.Y--;
                        break;
                }
                this.Model++;
                rd++;
            }
        }

        public void SetClose() {
            if (this.IsOpen()) {            //For case of closing closed doors.
                switch (rd) {
                    case 1:
                        this.X++;
                        this.Y--;
                        break;
                    case 3:
                        this.X--;
                        this.Y--;
                        break;
                    case 5:
                        this.X++;
                        break;
                    case 7:
                        this.X--;
                        this.Y++;
                        break;
                    case 9:
                        this.X--;
                        this.Y--;
                        break;
                    case 11:
                        this.X--;
                        this.Y++;
                        break;
                    case 13:
                        break;
                    case 15:
                        this.Y++;
                        break;
                }
                this.Model--;
                rd--;
            }
        }

        [Summary("Is doors orthogonal(kolmé) on which direction?")]
        public Direction GetDirection() {
            this.Validity();
            switch (rd / 4) {
                case 0:
                    return Direction.North;
                case 1:
                    return Direction.South;
                case 2:
                    return Direction.West;
                case 3:
                    return Direction.East;
            }
            throw new SEException("Something is wrong, maybe it isnt a door?");
        }

        public bool IsOpen() {
            this.Validity();
            if ((rd % 2) == 0) {
                return false;
            } else {
                return true;
            }
        }

        public Rotation GetRotation() {
            this.Validity();
            if (((rd / 2) % 2) == 0) {
                return Rotation.Left;
            } else {
                return Rotation.Right;
            }
        }

        public ushort GetBaseType() {       //This metod doesnt need (no use of rd) and CANNOT call Validity method (infinite loop)
            ushort grafic = this.Model;
            if ((grafic == 0x190e) || (grafic == 0x190f)) {     //bar door.
                return 0x190e;
                for (int i = 0; i < 19; i++) {      //20 doors (0-19) but the last door are retarded bar door.
                    if ((grafic >= bType[i]) && (grafic <= (bType[i] + 15))) {
                        return bType[i];
                    }
                }
                throw new SEException("This isnt a door perhaps.");
            }
        }
    }
    [Summary("Does doors rotate by left or right side?")]
    public enum Rotation {
        Left,
        Right
    }
}