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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public static class SoundCalculator {
		public const ushort NoSound = (ushort) SoundNames.None;


		public static void PlayAngerSound(Character self) {
			ushort sound = self.TypeDef.AngerSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.AngerSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		public static void PlayIdleSound(Character self) {
			ushort sound = self.TypeDef.IdleSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.IdleSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		public static void PlayMissSound(Character self) {
		}

		public static ushort GetMissSound(Character self) {
			return NoSound;
		}

		public static void PlayAttackSound(Character self) {
			ushort sound = self.TypeDef.AttackSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.AttackSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		//public virtual int GetAttackSound()
		//{
		//    if (this.m_BaseSoundID != 0)
		//    {
		//        return (this.m_BaseSoundID + 2);
		//    }
		//    return -1;
		//}

		public static void PlayHurtSound(Character self) {
			ushort sound = self.TypeDef.HurtSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.HurtSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		//public virtual int GetHurtSound()
		//{
		//    if (this.m_BaseSoundID != 0)
		//    {
		//        return (this.m_BaseSoundID + 3);
		//    }
		//    return -1;
		//}

		public static void PlayDeathSound(Character self) {
			ushort sound = self.TypeDef.DeathSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.DeathSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}
	}
}

//public virtual int GetDeathSound()
//{
//    if (this.m_BaseSoundID != 0)
//    {
//        return (this.m_BaseSoundID + 4);
//    }
//    if (this.m_Body.IsHuman)
//    {
//        return Utility.Random(this.m_Female ? 788 : 1059, this.m_Female ? 4 : 5);
//    }
//    return -1;
//}









 

 

 

 

 

 

 

 

			//miss
			//swish01 = 568,
			//swish02 = 569,
			//swish03 = 570,