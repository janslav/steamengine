/*
	self program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	self program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with self program; if not, write to the Free Software
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
using SteamEngine.Packets;

namespace SteamEngine.CompiledScripts {
	public static class EffectFactory {
		//More detailed effects.
		public static void LightningEffectAt(IPoint4D point) {
			PacketSender.PrepareEffect(point, point, 1, 0, 0, 0, 0, 0, 0, 0, 0);
			PacketSender.SendToClientsInRange(point);
		}

		[SteamFunction]
		public static void LightningEffect(Thing self) {
			PacketSender.PrepareEffect(self, self, 1, 0, 0, 0, 0, 0, 0, 0, 0);
			PacketSender.SendToClientsWhoCanSee(self);
		}

		[SteamFunction]
		public static void StationaryEffect(Thing self, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
			PacketSender.PrepareEffect(self, self, 3, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
			PacketSender.SendToClientsWhoCanSee(self);
		}

		public static void StationaryEffectAt(IPoint4D point, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
			PacketSender.PrepareEffect(point, point, 2, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
			PacketSender.SendToClientsInRange(point);
		}

		[SteamFunction]
		public static void EffectFromTo(IPoint4D source, IPoint4D target, ushort effect, byte speed, byte duration, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
			PacketSender.PrepareEffect(source, target, 0, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
			PacketSender.SendToClientsInRange(source);
		}
	}
}