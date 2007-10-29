//This software is released under GNU internal license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

using System;
using System.IO;
using System.Runtime.Serialization;
using SteamEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using SteamEngine.Common;

namespace SteamEngine.Packets {
	
	/**
		This class holds instances of already prepared packets.
	*/
	public class Prepared {
		
		/*
			TODO:
				PrepareEnableFeatures
				PrepareStartGame
				SendUpdateRange
				SendGoodMove, SendBadMove
		*/
		
		private static FreedPacketGroup[] pickUpFailed;
		private static FreedPacketGroup[] godMode;
		private static FreedPacketGroup[] hackMove;
		private static FreedPacketGroup[] targettingCursor;
		private static FreedPacketGroup cancelTargettingCursor;
		private static FreedPacketGroup[] warMode;
		private static FreedPacketGroup[] rejectDeleteRequest;
		private static FreedPacketGroup[] failedLogin;
		private static FreedPacketGroup requestClientVersion;
		private static FreedPacketGroup[,] seasonAndCursor;
		private static FreedPacketGroup[] facetChange;
		private static FreedPacketGroup[] deathMessages;
		
		internal static void Init() {
		}
		
		static Prepared() {
			BoundPacketGroup pg = null;
			
			pickUpFailed = new FreedPacketGroup[(int)TryReachResult.FailedCount];
			for (int index=0; index<(int)TryReachResult.FailedCount; index++) {
				pg = PacketSender.NewBoundGroup();
				PacketSender.PreparePickupFailed((TryReachResult)index);
				pickUpFailed[index] = pg.Free();
			}
			
			godMode = new FreedPacketGroup[2];
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareGodMode(false); godMode[0] = pg.Free();
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareGodMode(true); godMode[1] = pg.Free();
			
			hackMove = new FreedPacketGroup[2];
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareHackMove(false); hackMove[0] = pg.Free();
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareHackMove(true); hackMove[1] = pg.Free();
			
			targettingCursor = new FreedPacketGroup[2];
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareTargettingCursor(false); targettingCursor[0] = pg.Free();
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareTargettingCursor(true); targettingCursor[1] = pg.Free();

			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareCancelTargettingCursor(); cancelTargettingCursor = pg.Free();
			
			warMode = new FreedPacketGroup[2];
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareWarMode(false); warMode[0] = pg.Free();
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareWarMode(true); warMode[1] = pg.Free();

			deathMessages = new FreedPacketGroup[3];
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareDeathMessage(0); deathMessages[0] = pg.Free();
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareDeathMessage(1); deathMessages[1] = pg.Free();
			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareDeathMessage(2); deathMessages[2] = pg.Free();
			
			seasonAndCursor = new FreedPacketGroup[5,2];
			for (int index=0; index<5; index++) {
				ConstructSeasonAndCursor((Season) index, CursorType.Normal);
				ConstructSeasonAndCursor((Season) index, CursorType.Gold);
			}
			
			rejectDeleteRequest = new FreedPacketGroup[8];
			for (int index=0; index<6; index++) {
				pg = PacketSender.NewBoundGroup();
				PacketSender.PrepareRejectDeleteRequest((DeleteRequestReturnValue) index);
				rejectDeleteRequest[index] = pg.Free();
			}
			pg = PacketSender.NewBoundGroup();
			PacketSender.PrepareRejectDeleteRequest(DeleteRequestReturnValue.RejectWithoutSendingAMessage);
			rejectDeleteRequest[6] = pg.Free();
			pg = PacketSender.NewBoundGroup();
			PacketSender.PrepareRejectDeleteRequest(DeleteRequestReturnValue.AcceptedRequest);
			rejectDeleteRequest[7] = pg.Free();
			
			failedLogin = new FreedPacketGroup[(int)FailedLoginReason.Count];
			for (int index=0; index<(int)FailedLoginReason.Count; index++) {
				pg = PacketSender.NewBoundGroup();
				PacketSender.PrepareFailedLogin((FailedLoginReason)index);
				failedLogin[index] = pg.Free();
			}

			facetChange = new FreedPacketGroup[10];//why 10? well, why not? :)
			for (int index=0; index<10; index++) {
				pg = PacketSender.NewBoundGroup();
				PacketSender.PrepareFacetChange((byte)index);
				facetChange[index] = pg.Free();
			}

			pg = PacketSender.NewBoundGroup(); PacketSender.PrepareRequestCliVer(); requestClientVersion = pg.Free();
		}
		
		public static void SendPickupFailed(GameConn c, TryReachResult msg) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			Sanity.IfTrueThrow((int)msg<0 || (int)msg>(int)TryReachResult.FailedCount, "Invalid pickUpFailedMessage '"+msg+"'.");
			pickUpFailed[(int)msg].SendTo(c);
		}
		
		public static void SendGodMode(GameConn c) {
			SendGodMode(c, c.GodMode);
		}
		
		public static void SendGodMode(GameConn c, bool enabled) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			godMode[enabled?1:0].SendTo(c);
		}
		
		private static void ConstructSeasonAndCursor(Season season, CursorType cursor) {
			BoundPacketGroup pg = PacketSender.NewBoundGroup();
			PacketSender.PrepareSeasonAndCursor(season, cursor);
			seasonAndCursor[(int) season, (int) cursor] = pg.Free();
		}
		
		public static void SendSeasonAndCursor(GameConn c, Season season, CursorType cursor) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			Sanity.IfTrueThrow((((byte) season) < 0 || ((byte) season) > 4), "Illegal season value.");
			Sanity.IfTrueThrow((cursor != CursorType.Normal && cursor != CursorType.Gold), "Illegal cursor value.");
		
			seasonAndCursor[(int) season, (int) cursor].SendTo(c);
		}

		public static void SendFacetChange(GameConn c, byte facet) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			Sanity.IfTrueThrow(facet>facetChange.Length, "Invalid facet "+facet+".");
			facetChange[facet].SendTo(c);
		}
		
		public static void SendHackMove(GameConn c) {
			SendHackMove(c, c.HackMove);
		}
		
		public static void SendHackMove(GameConn c, bool enabled) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			hackMove[enabled?1:0].SendTo(c);
		}
		
		public static void SendTargettingCursor(GameConn c, bool ground) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			targettingCursor[ground?1:0].SendTo(c);
		}
		
		public static void SendCancelTargettingCursor(GameConn c) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			cancelTargettingCursor.SendTo(c);
		}
		
		public static void SendWarMode(GameConn c, AbstractCharacter character) {
			SendWarMode(c, character.Flag_WarMode);
		}
		
		public static void SendWarMode(GameConn c, bool enabled) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			warMode[enabled?1:0].SendTo(c);
		}
		
		public static void SendRejectDeleteRequest(GameConn c, DeleteRequestReturnValue msg) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			int imsg = (int) msg;
			if (imsg>=254) {
				imsg-=248;
			}
			Sanity.IfTrueThrow(imsg<0 || imsg>7, "Invalid DeleteRequestReturnValue '"+msg+"'.");
			rejectDeleteRequest[imsg].SendTo(c);
		}
		
		public static void SendFailedLogin(GameConn c, FailedLoginReason msg) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			Sanity.IfTrueThrow((int)msg<0 || (int)msg>(int)FailedLoginReason.Count, "Invalid FailedLoginReason '"+msg+"'.");
			failedLogin[(int)msg].SendTo(c);
		}
		
		public static void SendRequestClientVersion(GameConn c) {
			Sanity.IfTrueThrow(c==null, "You can't send a packet to a null connection.");
			requestClientVersion.SendTo(c);
		}

		public static void SendYouAreDeathMessage(GameConn c) {
			deathMessages[0].SendTo(c);
		}

		public static void SendResurrectMessage(GameConn c) {
			deathMessages[1].SendTo(c);
		}
	}
}