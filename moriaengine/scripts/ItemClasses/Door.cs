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
		private static ushort[] baseModels = new ushort[] {
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
            //0x190e    //bar door - irregular doors, only 2 models.
        };

		private ushort baseModel = 0;

        public override void On_DClick(AbstractCharacter user) {
            if (!this.IsOpen) {
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
            if (!this.IsOpen) {           //For case of opening opened doors.
				switch (this.RD) {
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
				//this.baseModel++;
            }
        }

        public void SetClose() {
            if (this.IsOpen) {            //For case of closing closed doors.
				switch (this.RD) {
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
				//this.baseModel--;
            }
        }

		[Summary("Is doors orthogonal(kolmé) on which direction?")]
		public Direction DoorDirection {
			get {
				this.SetBaseDoorModelOrThrow();
				switch (this.RD / 4) {
					case 0:
						return Direction.North;
					case 1:
						return Direction.South;
					case 2:
						return Direction.West;
					case 3:
						return Direction.East;
				}
				throw new SEException(this + " is Door with incompatible model.");
			}
		}

        public bool IsOpen {
			get {
				this.SetBaseDoorModelOrThrow();
				if ((this.RD % 2) == 0) {
					return false;
				} else {
					return true;
				}
			}
        }

		public DoorRotation DoorRotation {
			get {
				this.SetBaseDoorModelOrThrow();
				if (((this.RD / 2) % 2) == 0) {
					return DoorRotation.Left;
				} else {
					return DoorRotation.Right;
				}
			}
        }

		public ushort BaseDoorModel {
			get {
				this.SetBaseDoorModelOrThrow();
				return this.baseModel;
			}
		}

		private int RD {
			get {
				return this.Model - this.baseModel;
			}
		}

		private void SetBaseDoorModelOrThrow() {
			ushort model = this.Model;
			switch (model) {
				case 0x190e://bar door.
				case 0x190f:
					this.baseModel = 0x190e;
					return;
			}

			if ((model < this.baseModel) || (model > (this.baseModel + 15))) {
				for (int i = 0, n = baseModels.Length; i < n; i++) {      //20 doors (0-19) but the last door are irregular bar door.
					ushort bm = baseModels[i];
					if ((model >= bm) && (model <= (bm + 15))) {
						this.baseModel = bm;
						return;
					}
				}
				throw new SEException(this + " is Door with incompatible model.");
			}
		}
    }

    [Summary("Do doors rotate by left or right side?")]
	public enum DoorRotation {
        Left,
        Right
    }
}