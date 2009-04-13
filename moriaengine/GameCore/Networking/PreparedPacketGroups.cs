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
using System.Collections.Generic;
using System.Text;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using System.IO;
using System.Net;

namespace SteamEngine.Networking {

	public static class PreparedPacketGroups {

		private static PacketGroup[] loginDeniedPGs = new PacketGroup[Tools.GetEnumLength<LoginDeniedReason>()];

		private static PacketGroup targetGround;
		private static PacketGroup targetXYZ;
		private static PacketGroup targetCancelled;

		private static PacketGroup[] facetChange = new PacketGroup[5];

		private static PacketGroup[] warMode = new PacketGroup[2];

		private static PacketGroup[] pickUpFailed = new PacketGroup[7];

		private static PacketGroup[] rejectDeleteCharacter = new PacketGroup[8];

		private static PacketGroup[] deathMessages = new PacketGroup[3];

		private static PacketGroup enableMapDiffFiles;

		private static PacketGroup clientVersion;

		private static PacketGroup clientFeatures;

		static PreparedPacketGroups() {
			for (int i = 0, n = loginDeniedPGs.Length; i < n; i++) {
				loginDeniedPGs[i] = PacketGroup.CreateFreePG();
				loginDeniedPGs[i].AcquirePacket<LoginDeniedOutPacket>().Prepare((LoginDeniedReason) i);
			}

			targetGround = PacketGroup.CreateFreePG();
			targetGround.AcquirePacket<TargetCursorCommandsOutPacket>().Prepare(true);
			targetXYZ = PacketGroup.CreateFreePG();
			targetXYZ.AcquirePacket<TargetCursorCommandsOutPacket>().Prepare(false);
			targetCancelled = PacketGroup.CreateFreePG();
			targetCancelled.AcquirePacket<TargetCursorCommandsOutPacket>().PrepareAsCancel();

			warMode[0] = PacketGroup.CreateFreePG();
			warMode[0].AcquirePacket<SetWarModeOutPacket>().Prepare(false);
			warMode[1] = PacketGroup.CreateFreePG();
			warMode[1].AcquirePacket<SetWarModeOutPacket>().Prepare(true);

			for (int i = 0, n = pickUpFailed.Length; i < n; i++) {
				pickUpFailed[i] = PacketGroup.CreateFreePG();
				pickUpFailed[i].AcquirePacket<RejectMoveItemRequestOutPacket>().Prepare((DenyResult) i);
			}

			for (int i = 0; i < 6; i++) {
				rejectDeleteCharacter[i] = PacketGroup.CreateFreePG();
				rejectDeleteCharacter[i].AcquirePacket<RejectDeleteCharacterOutPacket>().Prepare((DeleteCharacterResult) i);
			}
			rejectDeleteCharacter[6] = PacketGroup.CreateFreePG();
			rejectDeleteCharacter[6].AcquirePacket<RejectDeleteCharacterOutPacket>().Prepare(DeleteCharacterResult.Deny_NoMessage);
			rejectDeleteCharacter[7] = PacketGroup.CreateFreePG();
			rejectDeleteCharacter[7].AcquirePacket<RejectDeleteCharacterOutPacket>().Prepare(DeleteCharacterResult.Allow);

			for (byte i = 0, n = (byte) deathMessages.Length; i < n; i++) {
				deathMessages[i] = PacketGroup.CreateFreePG();
				deathMessages[i].AcquirePacket<ResurrectionMenuOutPacket>().Prepare(i);
			}

			enableMapDiffFiles = PacketGroup.CreateFreePG();
			enableMapDiffFiles.AcquirePacket<EnableMapDiffFilesOutPacket>().Prepare();

			clientVersion = PacketGroup.CreateFreePG();
			clientVersion.AcquirePacket<ClientVersionOutPacket>();

			clientFeatures = PacketGroup.CreateFreePG();
			clientFeatures.AcquirePacket<EnableLockedClientFeaturesOutPacket>().Prepare(Globals.FeaturesFlags);
		}

		public static void SendLoginDenied(TcpConnection<GameState> conn, LoginDeniedReason why) {
			PacketGroup pg = loginDeniedPGs[(int) why];
			conn.SendPacketGroup(pg);
		}

		public static void SendTargettingCursor(TcpConnection<GameState> conn, bool ground) {
			if (ground) {
				conn.SendPacketGroup(targetGround);
			} else {
				conn.SendPacketGroup(targetXYZ);
			}
		}

		public static void SendCancelTargettingCursor(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(targetCancelled);
		}

		public static void SendFacetChange(TcpConnection<GameState> conn, byte facet) {
			PacketGroup pg = facetChange[facet];
			if (pg == null) {
				pg = PacketGroup.CreateFreePG();
				pg.AcquirePacket<SetFacetOutPacket>().Prepare(facet);
				facetChange[facet] = pg;
			}
			conn.SendPacketGroup(pg);
		}

		public static void SendWarMode(TcpConnection<GameState> conn, bool enabled) {
			conn.SendPacketGroup(warMode[enabled ? 1 : 0]);
		}

		public static void SendRejectMoveItemRequest(TcpConnection<GameState> conn, DenyResult msg) {
			conn.SendPacketGroup(pickUpFailed[(int) msg]);
		}

		public static void SendRejectDeleteCharacter(TcpConnection<GameState> conn, DeleteCharacterResult msg) {
			int imsg = (int) msg;
			if (imsg >= 254) {
				imsg -= 248;
			}
			Sanity.IfTrueThrow(imsg < 0 || imsg > 7, "Invalid DeleteCharacterResult '" + msg + "'.");
			conn.SendPacketGroup(rejectDeleteCharacter[(int) msg]);
		}

		public static void SendYouAreDeathMessage(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(deathMessages[2]);
		}

		public static void SendResurrectMessage(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(deathMessages[1]);
		}

		public static void SendEnableMapDiffFiles(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(enableMapDiffFiles);
		}

		public static void SendClientVersionQuery(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(clientVersion);
		}

		public static void SendClientFeatures(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(clientFeatures);
		}

	}
}