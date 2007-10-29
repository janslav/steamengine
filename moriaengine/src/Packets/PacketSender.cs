//This software is released under GNU internal license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

using System;
using System.IO;
using System.Runtime.Serialization;
using SteamEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SteamEngine.Common;
using System.Configuration;

//See docs/packets.txt for a list of packet methods here, along with their packet ID#.
//See docs/plans/packetsender.txt for detailed information on PacketSender and its subclasses.

namespace SteamEngine.Packets {
	
	public class PacketSender : HighLevelOut {
		public static bool MovementTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Movement Trace Messages"]);
		
		static PacketSender() {
			
		}
		
		//----------------- Packet Preparation Methods ----------------
		
		public static void PrepareRemoveFromView(Thing thing) {
			PrepareRemoveFromView(thing.FlaggedUid);
		}
		//for backwards compatibility
		public static void PrepareRemoveFromView(int uid) {
			uint uuid = (uint) uid;
			PrepareRemoveFromView(uuid);
		}
		//This is used by a few things. Most notably, NetState uses it for things which have been deleted,
		//and it is used when mounts are dismounted, since mountitem UIDs are sent as the mount's UID flagged with
		//the item flag (0x40000000).
		public static void PrepareRemoveFromView(uint uid) {
			StartGenerating();
			EncodeByte(0x1d, 0);
			EncodeUInt(uid, 1);
			DoneGenerating(5);
			Compress();
		}
		
		public static void PrepareVersions(uint[] vals) {
			StartGenerating();
			EncodeByte(0x3e, 0);
			int blockSize=1;
			for (int a=0; a<9; a++) {
				if (vals.Length>a) {
					EncodeUInt(vals[a], blockSize);
				} else {
					EncodeInt(0, blockSize);
				}
				blockSize+=4;
			}
			DoneGenerating(blockSize);	//37
			Compress();
		}
		
		public static void PreparePaperdollItem(AbstractItem item) {
			Sanity.IfTrueThrow(item==null, "PreparePaperdollItem called with a null item.");
			StartGenerating();
			EncodeByte(0x2e, 0);
			EncodeUInt(item.FlaggedUid,1);
			EncodeUShort(item.Model,5);
			EncodeByte(0, 7);
			EncodeByte(item.Layer, 8);
			EncodeUInt(item.Cont.FlaggedUid,9);
			EncodeUShort(item.Color,13);
			DoneGenerating(15);
			Compress();
		}
		
		public static void PrepareMountInfo(AbstractCharacter rider) {
			AbstractCharacter mount = rider.Mount;
			
			StartGenerating();
			EncodeByte(0x2e, 0);
			EncodeUInt((uint) (mount.Uid|0x40000000), 1);
			EncodeUShort(mount.MountItem, 5);
			EncodeByte(0, 7);
			EncodeByte(25, 8);
			EncodeUInt(rider.FlaggedUid, 9);
			EncodeUShort(mount.Color, 13);
			DoneGenerating(15);
			Compress();
		}
		
		public static void PrepareUpdateStats(AbstractCharacter cre, bool showReal) {
			Sanity.IfTrueThrow(cre==null, "PrepareUpdateStats called with a null character.");
			StartGenerating();
			EncodeByte(0x2d, 0);
			EncodeUInt(cre.FlaggedUid, 1);
			short curval=cre.Hits;
			short maxval=cre.MaxHits;
			EncodeCurMaxVals(curval, maxval, showReal, 5);
			curval=cre.Mana;
			maxval=cre.MaxMana;
			EncodeCurMaxVals(curval, maxval, showReal, 9);
			curval=cre.Stam;
			maxval=cre.MaxStam;
			EncodeCurMaxVals(curval, maxval, showReal, 13);
			DoneGenerating(17);
			Compress();
		}
		
		public static void PrepareUpdateHitpoints(AbstractCharacter cre, bool showReal) {
			Sanity.IfTrueThrow(cre==null, "PrepareUpdateHitpoints called with a null character.");
			StartGenerating();
			EncodeByte(0xa1, 0);
			EncodeUInt(cre.FlaggedUid, 1);
			short curval=cre.Hits;
			short maxval=cre.MaxHits;
			EncodeCurMaxVals(curval, maxval, showReal, 5);
			DoneGenerating(9);
			Compress();
		}
		public static void PrepareUpdateMana(AbstractCharacter cre, bool showReal) {
			Sanity.IfTrueThrow(cre==null, "PrepareUpdateMana called with a null character.");
			StartGenerating();
			EncodeByte(0xa2, 0);
			EncodeUInt(cre.FlaggedUid, 1);
			short curval=cre.Mana;
			short maxval=cre.MaxMana;
			EncodeCurMaxVals(curval, maxval, showReal, 5);
			DoneGenerating(9);
			Compress();
		}
		public static void PrepareUpdateStamina(AbstractCharacter cre, bool showReal) {
			Sanity.IfTrueThrow(cre==null, "PrepareUpdateStamina called with a null character.");
			StartGenerating();
			EncodeByte(0xa3, 0);
			EncodeUInt(cre.FlaggedUid, 1);
			short curval=cre.Stam;
			short maxval=cre.MaxStam;
			EncodeCurMaxVals(curval, maxval, showReal, 5);
			DoneGenerating(9);
			Compress();
		}
		
		public static void PrepareStatusBar(AbstractCharacter cre, StatusBarType type) {
			Sanity.IfTrueThrow(cre==null, "PrepareStatusBar called with a null character.");
			Sanity.IfTrueThrow(!Enum.IsDefined(typeof(StatusBarType),type),"Invalid value "+type+" for StatusBarType in PrepareStatusBar.");
			StartGenerating();
			EncodeByte(0x11, 0);
			EncodeInt(cre.Uid, 3);
			EncodeString(cre.Name, 7, 30);
			short blockSize=0;
			short hitpoints=cre.Hits;
			short maxhitpoints=cre.MaxHits;
			int moreInfo=0;
			if (type==StatusBarType.Me) {
				EncodeByte(0, 41);	//name change valid
				moreInfo=4;
				EncodeShort(hitpoints, 37);
				EncodeShort(maxhitpoints, 39);
			} else {
				if (type==StatusBarType.Pet) {
					EncodeByte(1, 41);
				} else {
					EncodeByte(0, 41);
				}
				moreInfo=0;
				EncodeShort((short)(((int)hitpoints<<8)/maxhitpoints), 37);
				EncodeShort(256, 39);
			}
			EncodeByte((byte)moreInfo, 42);	//more information
			blockSize=43;
			if (moreInfo>=1) {
				blockSize=66;
				EncodeByte((cre.IsFemale ? (byte) 1 : (byte) 0), 43);
				short strength=cre.Str;
				short dexterity=cre.Dex;
				short intelligence=cre.Int;
				short stamina=cre.Stam;
				short maxStamina=cre.MaxStam;
				short mana=cre.Mana;
				short maxMana=cre.MaxMana;
				ulong lgold = cre.Gold;
				uint gold=(uint)(lgold>0xffffffff?0xffffffff:lgold);
				short armor=cre.StatusArmorClass;
				ushort weight=(ushort) cre.Weight;
				EncodeShort(strength, 44);
				EncodeShort(dexterity, 46);
				EncodeShort(intelligence, 48);
				EncodeShort(stamina, 50);
				EncodeShort(maxStamina, 52);
				EncodeShort(mana, 54);
				EncodeShort(maxMana, 56);
				EncodeUInt(gold, 58);
				EncodeShort(armor, 62);
				EncodeUShort(weight, 64);
			}
			if (moreInfo>=3) {
				blockSize = 69;
				EncodeUShort(300, 66); 			//(TODO): stat cap
				EncodeByte(0, 68);				//(TODO): num pets
				EncodeByte(6, 69);				//(TODO): max pets
			}
			if (moreInfo>=4) {
				blockSize = 88;
				short fireResist=cre.ExtendedStatusNum1;
				short coldResist=cre.ExtendedStatusNum2;
				short poisonResist=cre.ExtendedStatusNum3;
				short energyResist=cre.StatusMindDefense;
				short luck=cre.ExtendedStatusNum5;
				short minDamage=cre.ExtendedStatusNum6;
				short maxDamage=cre.ExtendedStatusNum7;
				long ltithingPoints=cre.TithingPoints;
				int tithingPoints=(int)(ltithingPoints>0xffffffff?0xffffffff:ltithingPoints);
				EncodeShort(fireResist, 70);
				EncodeShort(coldResist, 72);
				EncodeShort(poisonResist, 74);
				EncodeShort(energyResist, 76);
				EncodeShort(luck, 78);
				EncodeShort(minDamage, 80);
				EncodeShort(maxDamage, 82);
				EncodeInt(tithingPoints, 84);
			}
			EncodeShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();
		}

		public static void PrepareInitialPlayerInfo(AbstractCharacter chr) {
			Sanity.IfTrueThrow(chr==null, "PrepareInitialPlayerInfo called with a null character.");
			StartGenerating();
			EncodeByte(0x1b, 0);
			
			EncodeUInt(chr.FlaggedUid,1);
			//EncodeInt(0x25, 5);
			EncodeZeros(4, 5);
			EncodeUShort(chr.Model, 9);
			EncodeUShort(chr.X, 11);
			EncodeUShort(chr.Y, 13);
			EncodeByte(0, 15);
			EncodeSByte(chr.Z, 16);
			EncodeByte((byte)chr.Direction, 17);
			
			EncodeByte(0, 18);
			EncodeByte(0xff, 19);
			EncodeByte(0xff, 20);
			EncodeByte(0xff, 21);
			EncodeByte(0xff, 22);
			EncodeUShort(0, 23);	//UL X coordinate of the server/map
			EncodeUShort(0, 25);	//UL Y coordinate of the server/map
			
			//Map Width (According to Kair's packet guide, "The total number of tiles in the X-axis minus eight.")
			EncodeUShort((ushort)(Map.GetMapSizeX(0)-8), 27);
			//Map Height (According to Kair's packet guide, "The total number of tiles in the Y-axis.")
			EncodeUShort((ushort) Map.GetMapSizeY(0), 29);
			
			//unknown
			EncodeZeros(6, 31);
			
			DoneGenerating(37);
			Compress();
		}
		
		//For use by Server's various speech methods (Which can send to multiple clients).
		public static void PrepareSpeech(Thing from, string msg, SpeechType type, ushort font, ushort color, string lang) {
			if (Globals.supportUnicode && font==3 && !(type==SpeechType.Name && Globals.asciiForNames)) {	//if it's another font, send it as ASCII
				PrepareUnicodeMessage(from, from.Model, from.Name, msg, type, font, color, lang);
			} else {
				PrepareAsciiMessage(from, from.Model, from.Name, msg, type, font, color);
			}
		}
		
		//'from' can be null.
		public static void PrepareUnicodeMessage(Thing from, ushort model, string sourceName, string msg, SpeechType type, ushort font, ushort color, string language) {
			Sanity.IfTrueThrow(sourceName==null, "PrepareUnicodeMessage called with a null 'sourceName'.");
			Sanity.IfTrueThrow(msg==null, "PrepareUnicodeMessage called with a null 'msg'.");
			Sanity.IfTrueThrow(language==null, "PrepareUnicodeMessage called with a null 'language'.");
			StartGenerating();
			int msgByteLen = msg.Length+msg.Length;
			
			if (color==0) {	//black, which only shows in the journal.
				color=Globals.defaultUnicodeMessageColor;
			}
			EncodeByte(0xae, 0);
			ushort blockSize = (ushort) (48+msgByteLen+2);
			EncodeUShort(blockSize, 1);
			if (from==null) {
				EncodeInt(-1, 3);
			} else {
				EncodeUInt(from.FlaggedUid, 3);
			}
			EncodeUShort(model, 7);
			EncodeByte((byte) type, 9);
			EncodeUShort(color ,10); //text color
			EncodeUShort(font, 12); //font
			EncodeString(language, 14, 4);
			
			EncodeString(sourceName, 18, 40);
			int len = EncodeUnicodeString(msg, 48, 512);
			EncodeZeros(2, 48+len);
			DoneGenerating(blockSize);
			Compress();
		}
		
		//'from' can be null.
		public static void PrepareAsciiMessage(Thing from, ushort model, string sourceName, string msg, SpeechType type, ushort font, ushort color) {
			Sanity.IfTrueThrow(sourceName==null, "PrepareAsciiMessage called with a null 'sourceName'.");
			Sanity.IfTrueThrow(msg==null, "PrepareAsciiMessage called with a null 'msg'.");
			StartGenerating();
			EncodeByte(0x1c, 0);
			ushort blockSize = (ushort) (44+msg.Length+1);
			EncodeUShort(blockSize, 1);
			if (from==null) {
				EncodeInt(-1, 3);
			} else {
				EncodeUInt(from.FlaggedUid, 3);
			}
			EncodeUShort(model, 7);
			EncodeByte((byte) type, 9);
			EncodeUShort(color, 10); //text color
			EncodeUShort(font, 12); //font
			EncodeString(sourceName, 14, 40);
			int len = EncodeString(msg, 44, 1024);
			EncodeByte(0, 44+len);
			DoneGenerating(blockSize);
			Compress();
		}
		
		/**
			This method is internal because it can screw up the client if it is prepared but not sent. So only prepare
			it if you're really going to send it. (Because this method resets conn.moveSeqNum to 0, because the client
			expects it to be reset whenever a 0x20 is sent)
			
			This method is only needed when the game is started and when a client is resynced (and if the client's color or model changes).
		*/
		internal static void PrepareLocationInformation(GameConn conn) {
			AbstractCharacter chr=conn.CurCharacter;
			Sanity.IfTrueThrow(conn==null, "PrepareLocationInformation called with a null conn.");
			Sanity.IfTrueThrow(chr==null, "PrepareLocationInformation called with a conn which doesn't have a logged-in character.");
			StartGenerating();
			EncodeByte(0x20, 0);
			EncodeUInt(chr.FlaggedUid, 1);
			EncodeUShort(chr.Model, 5);
			EncodeByte(0, 7);
			EncodeUShort(chr.Color, 8);
			EncodeByte(chr.FlagsToSend, 10);
			EncodeUShort(chr.X, 11);
			EncodeUShort(chr.Y, 13);
			EncodeZeros(2, 15);
			EncodeByte((byte)chr.Direction, 17);
			EncodeSByte(chr.Z, 18);
			DoneGenerating(19);
			Compress();
			Logger.WriteInfo(MovementTracingOn, "0x20 sending: Reset moveSeqNum to 0.");
			Sanity.StackTraceIf(MovementTracingOn);
			conn.CancelMovement();
		}
		
		public static void SendBadMove(GameConn conn) {
			AbstractCharacter chr=conn.CurCharacter;
			Sanity.IfTrueThrow(conn==null, "PrepareAndSendLocationInformation called with a null conn.");
			Sanity.IfTrueThrow(chr==null, "PrepareAndSendLocationInformation called with a conn which doesn't have a logged-in character.");
			Logger.WriteInfo(MovementTracingOn, "SendBadMove("+conn+") (we have "+conn.moveSeqNum+") x("+chr.X+") y("+chr.Y+") z("+chr.Z+") dir("+chr.Direction+").");
			StartGenerating();
			EncodeByte(0x21, 0);
			EncodeByte(conn.MoveSeqNumToSend(), 1);
			EncodeUShort(chr.X, 2);
			EncodeUShort(chr.Y, 4);
			EncodeByte((byte)chr.Direction, 6);
			EncodeSByte(chr.Z, 7);
			DoneGenerating(8);
			Compress();
			conn.CancelMovement();
			Logger.WriteInfo(MovementTracingOn, "0x21 (BadMove) sending: Reset moveSeqNum to 0.");
			SendTo(conn, true);
		}
		
		public static void SendGoodMove(GameConn conn) {
			AbstractCharacter chr=conn.CurCharacter;
			Sanity.IfTrueThrow(conn==null, "PrepareAndSendLocationInformation called with a null conn.");
			Sanity.IfTrueThrow(chr==null, "PrepareAndSendLocationInformation called with a conn which doesn't have a logged-in character.");
			Logger.WriteInfo(MovementTracingOn, "SendGoodMove("+conn+") (we have "+conn.moveSeqNum+") x("+chr.X+") y("+chr.Y+") z("+chr.Z+") dir("+chr.Direction+").");
			StartGenerating();
			EncodeByte(0x22, 0);
			EncodeByte(conn.MoveSeqNumToSend(), 1);
			EncodeByte(0, 2);
			DoneGenerating(3);
			Compress();
			SendTo(conn, true);
		}			
		
		public static void PrepareOpenContainer(AbstractItem cont) {
			Sanity.IfTrueThrow(cont==null, "PrepareContainerInfo was passed a null container.");
			StartGenerating();
			EncodeByte(0x24, 0);
			EncodeUInt(cont.FlaggedUid, 1);
			EncodeUShort(cont.Gump, 5);
			DoneGenerating(7);
			Compress();
		}
		
		public static bool PrepareContainerContents(AbstractItem cont, GameConn viewerConn, AbstractCharacter viewer) {
			StartGenerating();
			EncodeByte(0x3c, 0);
			ushort blockSize = 5;
			ushort numSegments = 0;
			foreach (AbstractItem i in cont) {
				if (viewer.CanSeeVisibility(i)) {
					numSegments++;
					EncodeUInt(i.FlaggedUid, blockSize);
					EncodeUShort(i.Model, blockSize+4);
					EncodeByte(0, blockSize+6);
					EncodeUShort(i.ShortAmount, blockSize+7);
					EncodeUShort(i.X, blockSize+9);
					EncodeUShort(i.Y, blockSize+11);
					EncodeUInt(cont.FlaggedUid, blockSize+13);
					EncodeUShort(i.Color, blockSize+17);
					blockSize+=19;
					i.On_BeingSentTo(viewerConn);
				}
			}
			if (blockSize == 0) {
				DiscardUncompressed();
				return false;
			}
			EncodeUShort(blockSize, 1);
			EncodeUShort(numSegments, 3);
			DoneGenerating(blockSize);
			Compress();
			return true;
		}
		
		public static void PrepareItemInContainer(AbstractItem i) {
			Sanity.IfTrueThrow(i==null, "PrepareItemInContainer called with a null item.");
			StartGenerating();
			EncodeByte(0x25, 0);
			EncodeUInt(i.FlaggedUid, 1);
			EncodeUShort(i.Model, 5);
			EncodeByte(0, 7);
			EncodeUShort(i.ShortAmount, 8);
			EncodeUShort(i.X, 10);
			EncodeUShort(i.Y, 12);
			EncodeUInt(i.Cont.FlaggedUid, 14);
			EncodeUShort(i.Color, 18);
			DoneGenerating(20);
			Compress();
		}
		
		/*		Doesn't work, just crashes the client. Oh well, it was worth a shot!
		
		public static void PrepareNewArea(ushort upperLeftX, ushort upperLeftY, ushort width,
								ushort height, ushort lowZ, ushort highZ, string name, string description, ushort sfx,
								ushort music, ushort nightsfx, byte dungeon, ushort light) {
			if (name==null) name="";
			if (description==null) description="";
			StartGenerating();
			EncodeByte(0x58, 0);
			EncodeString(name, 1, 40);
			EncodeUInt(0, 41);
			EncodeUShort(upperLeftX, 45);
			EncodeUShort(upperLeftY, 47);
			EncodeUShort(width, 49);	//in x tiles
			EncodeUShort(height, 51);	//in y tiles
			EncodeUShort(lowZ, 53);
			EncodeUShort(highZ, 55);
			EncodeString(description, 57, 40);
			
			EncodeUShort(sfx, 97);
			EncodeUShort(music, 99);
			EncodeUShort(nightsfx, 101);
			EncodeByte(dungeon, 103);
			EncodeUShort(light, 104);
			DoneGenerating(106);
			Compress();
		}
		*/
		
		internal static void PreparePickupFailed(TryReachResult msg) {
			StartGenerating();
			EncodeByte(0x27, 0);
			EncodeByte((byte)msg, 1);
			DoneGenerating(2);
			Compress();
		}
		
		//One of these is probably what the UO client expects, we don't know yet, the various packet guides call this
		//packet different names, etc.
		//public static void PrepareDropFailed(Thing th) {
		//    StartGenerating();
		//    EncodeByte(0x28, 0);
		//    EncodeUInt(th.FlaggedUid, 1);
		//    DoneGenerating(5);
		//    Compress();
		//}
				
		public static void SendGodMode(GameConn conn) {
			StartGenerating();
			EncodeByte(0x2b, 0);
			EncodeByte((byte)(conn.GodMode?1:0), 1);
			DoneGenerating(2);
			Compress();
			SendTo(conn, true);
		}
		
		public static void PrepareGodMode(bool godMode) {
			StartGenerating();
			EncodeByte(0x2b, 0);
			EncodeByte((byte)(godMode?1:0), 1);
			DoneGenerating(2);
			Compress();
		}
		
		public static void SendHackMove(GameConn conn) {
			StartGenerating();
			EncodeByte(0x32, 0);
			EncodeByte((byte)(conn.HackMove?1:0), 1);
			DoneGenerating(2);
			Compress();
			SendTo(conn, true);
		}
		
		public static void PrepareHackMove(bool hackMove) {
			StartGenerating();
			EncodeByte(0x32, 0);
			EncodeByte((byte)(hackMove?1:0), 1);
			DoneGenerating(2);
			Compress();
		}
		
		public static void PrepareTargettingCursor(bool ground) {
			StartGenerating();
			EncodeByte(0x6c, 0);
			EncodeBool(ground, 1);
			EncodeZeros(17, 2);
			DoneGenerating(19);
			Compress();
		}

		public static void PrepareTargettingCursorForMultis(int model) {
			StartGenerating();
			EncodeByte(0x99, 0);
			EncodeByte(1, 1);
			//EncodeImt(??, 2);ID of deed... why? let's try to ignore that
			EncodeZeros(23, 3);
			EncodeShort((short) (model&0x8fff), 18);
			DoneGenerating(26);
			Compress();
		}
		
		public static void PrepareCancelTargettingCursor() {
			StartGenerating();
			EncodeByte(0x6c, 0);
			EncodeByte(0, 1);
			EncodeZeros(17, 2);
			EncodeByte(3, 6);
			DoneGenerating(19);
			Compress();
		}
		
		public static void PrepareWarMode(AbstractCharacter cre) {
			StartGenerating();
			EncodeByte(0x72, 0);
			EncodeBool(cre.Flag_WarMode, 1);
			EncodeByte(0, 2);
			EncodeByte(32, 3);
			EncodeByte(0, 4);
			DoneGenerating(5);
			Compress();
		}
		public static void PrepareWarMode(bool warMode) {
			StartGenerating();
			EncodeByte(0x72, 0);
			EncodeBool(warMode, 1);
			EncodeByte(0, 2);
			EncodeByte(32, 3);
			EncodeByte(0, 4);
			DoneGenerating(5);
			Compress();
		}
		
		//Experimental.
		public static void PrepareForceMove(AbstractCharacter cre, bool running) {
			StartGenerating();
			EncodeByte(0x97, 0);
			byte dir = cre.Dir;
			if (running) {
				dir|=0x80;
			}
			EncodeByte(dir, 1);
			DoneGenerating(2);
			Compress();
		}
		
		public static void SendRejectDeleteRequest(GameConn conn, DeleteRequestReturnValue reason) {
			StartGenerating();
			EncodeByte(0x85, 0);
			EncodeByte((byte)reason, 1);
			DoneGenerating(2);
			Compress();
			SendTo(conn, true);
		}
		public static void PrepareRejectDeleteRequest(DeleteRequestReturnValue reason) {
			StartGenerating();
			EncodeByte(0x85, 0);
			EncodeByte((byte)reason, 1);
			DoneGenerating(2);
			Compress();
		}
		
		public static void PrepareItemInformation(AbstractItem item) {
			PrepareItemInformation(item, MoveRestriction.Normal);
		}
		
		/**
			For flags, 0x20 makes the item always movable, and 0x80 shades it hidden-grey.
			Nothing else seems to have any effect.
		*/
		internal static void PrepareItemInformation(AbstractItem item, MoveRestriction restrict) {
			StartGenerating();
			EncodeByte(0x1a, 0);
			ushort blockSize = 1;
			uint uid=item.FlaggedUid;
			ushort amt = item.ShortAmount;
			if (amt>1) {
				uid=uid|0x80000000;
				EncodeUInt(uid, 3);
				//Logger.WriteDebug("Preparin' stackable "+item.ToString());
				EncodeUShort(amt, 9);
				blockSize=11;
			} else {
				EncodeUInt(uid, 3);
				blockSize=9;
			}
			EncodeUShort(item.Model, 7);
			byte direction = (byte) item.Direction;
			int ypos=blockSize+2;
			if (direction > 0) {
				EncodeUShort((ushort) (item.X|0x8000), blockSize);
				EncodeByte(direction, blockSize+4);
				EncodeSByte(item.Z, blockSize+5);
				blockSize+=6;
			} else {
				EncodeUShort(item.X, blockSize);
				EncodeSByte(item.Z, blockSize+4);
				blockSize+=5;
			} 
			
			ushort y=item.Y;
			byte flags=item.FlagsToSend;
			if (restrict==MoveRestriction.Movable) {
				flags=(byte) (flags|0x20);
			//} else if (restrict==MoveRestriction.Immovable) {
			//	flags=(byte) (flags|0x10);	//Doesn't seem to work. None of the other flags seem to either. Hmmm...
			}
			ushort color = item.Color;
			if (color>0) {
				y=(ushort) (y|0x8000);
				EncodeUShort(color, blockSize);
				blockSize+=2;
			}
			if (flags>0) {
				y=(ushort) (y|0x4000);
				EncodeByte(flags, blockSize);
				blockSize++;
			}
			EncodeUShort(y, ypos);
			EncodeUShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();
		}
		
		public static void PrepareSpecialHighlight(AbstractCharacter cre, byte amount) {
			StartGenerating();
			EncodeByte(0xc4, 0);
			EncodeUInt(cre.FlaggedUid,1);
			EncodeByte(amount, 5);
			DoneGenerating(6);
			Compress();
		}
		
		public static void PrepareMovingCharacter(AbstractCharacter chr, bool running, HighlightColor highlight) {
			Sanity.IfTrueThrow(chr==null, "PrepareMovingCharacter called with a null chr.");
			StartGenerating();
			EncodeByte(0x77, 0);
			EncodeUInt(chr.FlaggedUid, 1);
			EncodeUShort(chr.Model, 5);
			MutablePoint4D point = chr.point4d;
			EncodeUShort(point.x, 7);
			EncodeUShort(point.y, 9);
			EncodeSByte(point.z, 11);
			byte dir = (byte)chr.Direction;
			if (running) {
				
				dir|=0x80;
			}
			EncodeByte(dir, 12);
			EncodeUShort(chr.Color, 13);
			EncodeByte(chr.FlagsToSend, 15);
			//Logger.WriteDebug("Flags Sent=0x"+opacket[15].ToString("x"));
			EncodeByte((byte) highlight, 16);
			DoneGenerating(17);
			Compress();
		}

		public static void PrepareCharacterInformation(AbstractCharacter cre, HighlightColor highlight) {
			Sanity.IfTrueThrow(cre==null, "PrepareCharacterInformation called with null cre.");
			Logger.WriteDebug("Prepare to send character information for Character "+cre+".");
			StartGenerating();
			EncodeByte(0x78, 0);
			ushort blockSize = 1;
			EncodeUInt(cre.FlaggedUid, 3);
			EncodeUShort(cre.Model, 7);
			MutablePoint4D point = cre.point4d;
			EncodeUShort(point.x, 9);
			EncodeUShort(point.y, 11);
			EncodeSByte(point.z, 13);
			EncodeByte(cre.Dir, 14);
			EncodeUShort(cre.Color, 15);
			EncodeByte(cre.FlagsToSend, 17);
			EncodeByte((byte) highlight, 18);
			//equipped
			blockSize=19;
			if (cre.visibleLayers != null) {
				foreach (AbstractItem it in cre.visibleLayers) {
					Sanity.IfTrueThrow(it.Cont!=cre, LogStr.Ident(cre)+" thinks that "+LogStr.Ident(it)+" is equipped, but its cont is "+LogStr.Ident(it.Cont)+".");
					//Logger.WriteDebug("Layer "+it.Layer+" is "+it+" IsEq="+it.IsEquippable+" IsCont="+it.IsContainer+" type="+it.GetType()+" FlaggedUid="+it.FlaggedUid+" Model="+it.Model+" Color="+it.Color);
					if (it.IsEquippable) {
						EncodeUInt(it.FlaggedUid, blockSize);
						if (it.Color==0) {
							EncodeUShort(it.Model, blockSize+4);
							EncodeByte(it.Layer, blockSize+6);
							blockSize+=7;
						} else {
							EncodeUShort((ushort) (it.Model|0x8000), blockSize+4);
							EncodeByte(it.Layer, blockSize+6);
							EncodeUShort(it.Color, blockSize+7);
							blockSize+=9;
						}
					}
				}
			}
			AbstractCharacter mount = cre.Mount;
			if (mount!=null) {
				EncodeInt(mount.Uid|0x40000000, blockSize);
				if (mount.Color==0) {
					EncodeUShort(mount.MountItem, blockSize+4);
					EncodeByte(25, blockSize+6);
					blockSize+=7;
				} else {
					EncodeUShort((ushort) (mount.MountItem|0x8000), blockSize+4);
					EncodeByte(25, blockSize+6);
					EncodeUShort(mount.Color, blockSize+7);
					blockSize+=9;
				}
			}

			EncodeUInt(0, blockSize);
			blockSize+=4;
			EncodeUShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();
		}
		
		public static void SendUncompressedFailedLogin(GameConn c, FailedLoginReason reason) {
			StartGenerating();
			EncodeByte(0x82, 0);
			EncodeByte((byte)reason, 1);
			DoneGenerating(2);
			SendUncompressed(c, true);
		}
		public static void SendFailedLogin(GameConn c, FailedLoginReason reason) {
			StartGenerating();
			EncodeByte(0x82, 0);
			EncodeByte((byte)reason, 1);
			DoneGenerating(2);
			Compress();
			SendTo(c, true);
		}
		public static void PrepareFailedLogin(FailedLoginReason reason) {
			StartGenerating();
			EncodeByte(0x82, 0);
			EncodeByte((byte)reason, 1);
			DoneGenerating(2);
			Compress();
		}
		
		public static void SendCharacterListAfterDelete(GameConn c) {
			ushort blockSize = 4+60*5;
			StartGenerating();
			EncodeByte(0x86, 0);
			AbstractAccount acc = c.curAccount;
			EncodeUShort(blockSize, 1);
			EncodeByte((byte) AbstractAccount.maxCharactersPerGameAccount, 3);
			for (int charNum=0; charNum<AbstractAccount.maxCharactersPerGameAccount; charNum++) {
				AbstractCharacter cre = acc.GetCharacterInSlot(charNum);
				string charName="";
				if (cre!=null) {
					charName = cre.Name;
				}
				int len = charName.Length;
				if (len>30) {
					len=30;
				}
			
				EncodeString(charName, 4+charNum*60, 30);
				EncodeZeros(30, 4+charNum*60+30);
				
			}
			DoneGenerating(blockSize);
			Compress();
			SendTo(c, true);
		}
		
		/**
		Note: If you are sending this packet to character's conn, DON'T include 0x02 in the flags, or
		the paperdoll will show the [War] button even if the character isn't in war mode. Go figure.
		[AOS 2d client 4.0.0l] -SL
		*/
		public static void PreparePaperdoll(AbstractCharacter character, bool canEquip) {
			StartGenerating();
			EncodeByte(0x88, 0);
			EncodeUInt(character.FlaggedUid, 1);
			//EncodeZeros(60, 5);
			EncodeString(character.PaperdollName, 5, 60);
			//For the paperdoll, canEquip is apparently the only one which matters.
			byte flagsToSend = character.FlagsToSend;
			
			Sanity.IfTrueThrow((flagsToSend&0x02)>0, ""+character+"'s FlagsToSend included 0x02, which is the 'can equip items on' flag for paperdoll packets - FlagsToSend should never include it.");
			if (canEquip) {	//include 0x02 if we can equip stuff on them.
				flagsToSend|=0x02;	//does this not work or something? Arrrgh.
			}
			EncodeByte(flagsToSend, 65);
			DoneGenerating(66);
			Compress();
		}
		
		public static void SendLoginToServer(GameConn c, ushort server) {
			Logger.WriteDebug("Sending login information for server "+server+".");
			IPAddress ep=c.IP;
			byte[] ipbytes=ep.GetAddressBytes();
			StartGenerating();	
			EncodeByte(0x8c, 0);
			if (Server.routerIPstr==null) {
				if (server<0 || server>=Server.numIPs) {
					DoneGenerating(1);
					DiscardUncompressed();
					c.Close("Requested nonexistant server number "+server+".");
					return;
				}
				Logger.WriteDebug(string.Format("Sending IP #{0}:{1}.{2}.{3}.{4}.",server,Server.localIPs[server,0],Server.localIPs[server,1],Server.localIPs[server,2],Server.localIPs[server,3]));
				EncodeBytes(Server.localIPs, server, 1);			//4 bytes
			} else {
				int localIPnum=Server.IsLocalIP(ipbytes);
				if (localIPnum==-1) {
					Logger.WriteDebug(string.Format("Sending router IP:{0}.",Server.routerIPstr));
					EncodeBytes(Server.routerIP, 1);				//4 bytes
				} else {
					Logger.WriteDebug(string.Format("Sending Local IP #{0}:{1}.{2}.{3}.{4}.",localIPnum,Server.localIPs[localIPnum,0],Server.localIPs[localIPnum,1],Server.localIPs[localIPnum,2],Server.localIPs[localIPnum,3]));
					EncodeBytes(Server.localIPs, localIPnum, 1);	//4 bytes
				}
			}	
			EncodeUShort(Globals.port, 5);
			/*
				UOX sends 7f 00 00 01,	(Looking at UOX code from August 2004)
				RunUO 1.0 RC0 sends a random number, but RunUO used to send ff ff ff ff (I don't know in what version this was changed).
				Wolfpack 12.9.8 sends ff ff ff ff.
				SE sends 00 00 00 00 now, but used to send ff ff ff ff.
			*/
			EncodeZeros(4, 7);
			DoneGenerating(11);
			SendUncompressed(c, true);
		}
		
		//This still isn't as nice as I would like, but it doesn't want to be simplified any more.
		//It's much nicer than the one in OutPackets, at least.
		// -SL
		public static void SendServersList(GameConn c) {
			StartGenerating();
			EncodeByte(0xa8, 0);
			EncodeByte(0x5d, 3);	//0x13; //unknown //0x5d on RunUo
			//EncodeUShort((ushort)Server.numIPs, 4);
			ushort blockSize=6;
			
			if (Server.routerIPstr==null) {
				EncodeUShort((ushort)Server.numIPs, 4);
				for (int server=0; server<Server.numIPs; server++) {
					EncodeUShort((ushort)server, blockSize);
					string serverName = Globals.serverName;
					serverName += "("+Server.ipStrings[server]+")";
					EncodeString(serverName, blockSize+2, 30);
					EncodeZeros(2, blockSize+32);
					EncodeByte(Server.PercentFull(), blockSize+34); //percent full
					EncodeSByte(Globals.timeZone, blockSize+35);
					
					EncodeBytesReversed(Server.localIPs, server, blockSize+36);	//4 bytes
					blockSize+=40;
				}
				
			} else {
				Logger.WriteDebug("routerIP="+Server.routerIPstr);
				EncodeUShort(1, 4);
				//get their IP
				byte[] ipbytes=c.IP.GetAddressBytes();
				
				EncodeZeros(2, blockSize);
				string serverName = Globals.serverName;
				EncodeString(serverName, blockSize+2, 30);
				EncodeZeros(2, blockSize+32);
				EncodeByte(Server.PercentFull(), blockSize+34); //percent full
				EncodeSByte(Globals.timeZone, blockSize+35);
				
				int localIPnum=Server.IsLocalIP(ipbytes);
				if (localIPnum==-1) {
					Logger.WriteDebug("Sending router IP: "+Server.routerIPstr);
					EncodeBytesReversed(Server.routerIP, blockSize+36);	//4 bytes
				} else {
					Logger.WriteDebug(String.Format("Sending Local IP #{0}:{1}.{2}.{3}.{4}.",localIPnum,Server.localIPs[localIPnum,0],Server.localIPs[localIPnum,1],Server.localIPs[localIPnum,2],Server.localIPs[localIPnum,3]));
					EncodeBytesReversed(Server.localIPs, localIPnum, blockSize+36);	//4 bytes
				}
				blockSize+=40;
			}
			EncodeUShort(blockSize, 1);
			DoneGenerating(blockSize);
			SendUncompressed(c, true);
		}
		
		public static void SendCharList(GameConn c) {
			StartGenerating();
			//EncodeByte(0xb9, 0);
			//EncodeUShort(Globals.featuresFlags, 1);
			EncodeByte(0xa9, 0);
			byte numChars = 0;
			
			ushort blockSize=4;
			
			//characters
			for (int charNum=0; charNum<AbstractAccount.maxCharactersPerGameAccount; charNum++) {
				AbstractAccount acc = c.curAccount;
				AbstractCharacter cre=acc.GetCharacterInSlot(charNum);
				//int charId = -1;
				//string charName = "";
				if (cre!=null) {
					//charId=cre.uid;
					//charName = cre.Name;
					EncodeString(cre.Name, blockSize, 30);
					EncodeZeros(30, blockSize+30);
					numChars++;
				} else {
					EncodeZeros(60, blockSize);
				}
				blockSize+=60;
				
			}
			EncodeByte(numChars, 3);
			
			//cities
			EncodeByte(1, blockSize);
			EncodeByte(0, blockSize+1);
			string area = "London";
			EncodeString(area, blockSize+2, 30);
			EncodeByte(0, blockSize+32);
			EncodeString(area, blockSize+33, 30);
			EncodeByte(0, blockSize+63);
			
			//TODO: Login Flags as bools in globals.
			//Login Flags:
			//0x14 = One character only
			//0x08 = Right-click menus
			//0x20 = AOS features
			//0x40 = Six characters instead of five
			
			EncodeUInt(Globals.loginFlags, blockSize+64);
			
			blockSize+=68;
			EncodeUShort((ushort)(blockSize), 1);
			DoneGenerating(blockSize);
			Compress();
			SendTo(c, true);
		}
		
		public static void PrepareEnableFeatures(ushort features) {
			StartGenerating();
			EncodeByte(0xb9, 0);
			EncodeUShort(features, 1);
			DoneGenerating(3);
			Compress();
		}
		
		//use the Prepared version
		internal static void PrepareSeasonAndCursor(Season season, CursorType cursor) {
			StartGenerating();
			EncodeByte(0xbc, 0);
			EncodeByte((byte)season, 1);
			EncodeByte((byte)cursor, 2);
			DoneGenerating(3);
			Compress();
		}
		
		public static void PrepareSound(IPoint4D source, ushort sound) {
			Sanity.IfTrueThrow(source==null, "PrepareSound called with a null source.");
			StartGenerating();
			source = source.TopPoint;
			EncodeByte(0x54, 0);
			EncodeByte(0x01, 1);
			EncodeUShort(sound, 2);
			EncodeUShort(0, 4);
			EncodeUShort(source.X,6);
			EncodeUShort(source.Y,8);
			EncodeByte(0, 10);
			EncodeSByte(source.Z,11);
			DoneGenerating(12);
			Compress();
		}
		
		public static void PrepareStartGame() {
			StartGenerating();
			EncodeByte(0x55, 0);
			DoneGenerating(1);
			Compress();
		}
		
		/**
			Shows the character doing a particular animation.
			
			[ I did quite a lot of testing of this packet, since none of the packet guides seemed to have correct information about it.
				All testing on this packet was done with the AOS 2D client.
				Things that Jerrith's has wrong about this packet:
					Anim is a byte, not a ushort, and it starts at pos 6. If anim is greater than the # of valid anims for that character's model (It's out-of-range), then anim 0 is drawn. The byte at pos 5 doesn't seem to do anything (If it were a ushort, the client would display out-of-range-anim# behavior if you set byte 5 to something; It doesn't, it only looks at byte 6 for the anim#.). However, there are also anims which may not exist - for human (and equippables) models, these draw as some other anim instead. There are anims in the 3d client which don't exist in the 2d, too.
					Byte 5 is ignored by the client (But I said that already).
					Jerrith's "direction" variable isn't direction at all; The client knows the direction the character is facing already. It isn't a byte either. Read the next line:
					Bytes 7 and 8, which Jerrith's calls "unknown" and "direction," are actually a ushort which I call numBackwardsFrames. What it does is really weird - it's the number of frames to draw when the anim is drawn backwards, IF 'backwards' is true. If you send 0, it draws a blank frame (i.e. the character vanishes, but reappears after the frame is over). If you send something greater than the number of frames in the anim, then it draws a blank frame for both forwards AND backwards! It's beyond me how that behavior could have been useful for anything, but that's what it does...
					What Jerrith's calls "repeat" isn't the number of times to repeat the anim, it is the number of anims to draw, starting with 'anim'. If you specify 0 for this, though, it will continue drawing anims until the cows come home (and it apparently goes back to 0 after it draws the last one!).
					Jerrith's has "forward/backward" correct, although it doesn't mention what happens if you have both this and 'repeat' (undo) set to true (In that case it runs in reverse, and then runs forward).
					What Jerrith's calls "repeat flag" doesn't make the anim repeat. What it REALLY does is make the anim run once, and then run in reverse immediately after (so I call it "undo" :P). If 'backwards' is true, then it's going to run backwards and then forwards, but it still looks like it's undo'ing the anim it just drew, so :).
					Jerrith's has 'frame delay' correct.
			]
			
			
			
			@param anim The animation the character should perform.
			@param numBackwardsFrames If backwards is true, this is the number of frames to do when going backwards. 0 makes the character blink invisible for the first frame of the backwards anim. Anything above the number of frames in that particular anim makes the character blink invisible for both forwards and backwards, but ONLY if backwards is true. This is rather bizarre, considering how numAnims works, and how various anims have different frame amounts.
			@param numAnims The number of anims to do, starting with 'anim' and counting up from there. 0 means 'keep going forever'. If this exceeds the number of valid anims, it wraps back around to anim 0 and keeps going. So if you specify 0, it really WILL run forever, or at least until another anim is drawn for that character, including if it isn't through an anim packet (like if it moves or turns or something).
			@param backwards If true, perform the animation in reverse.
			@param undo If true, the animation is run, and then the opposite of the animation is run, and this is done for each anim which would be drawn (according to numAnims). If backwards is also true, then you will see the animation performed backwards and then forwards. Conversely, if backwards is false, then with undo true you will see the animation performed forwards and then backwards.
			@param frameDelay The delay time between animations: 0 makes the animation fastest, higher values make it proportionally slower (0xff is like watching glaciers drift).
				I timed some different values with a normal anim 14, which has 7 frames. (Using only my eyes and the windows clock, mind you, so it isn't super-accurate or anything.
					(~ means approximately)
					0: ~2s
					1: ~3s
					5: ~5s (4.5s?)
					10: ~8s
					50: ~36s
					
					What I gathered from those results:
					.25-.3 second delay between frames by default.
					.65-.7 seconds * frameDelay extra delay (for all 7 frames, so if it were .7 then it would be .1*frameDelay per frame)
					
					I did some more math and decided .25 and .7->.1 were probably pretty accurate estimates, and so:
					
					(.25*7)+(.7*frameDelay)=how many seconds UO should spend showing the anim
					(.25*7)+(.7*50)=36.75
					(.25*7)+(.7*1)=2.45
					(.25*7)+(.7*0)=1.75
					
					Or for anims without 7 frames:
					(.25*numFrames)+(.1*numFrames*frameDelay)=how many seconds UO should spend showing the anim
		*/
		public static void PrepareAnimation(AbstractCharacter cre, byte anim, ushort numAnims, bool backwards, bool undo, byte frameDelay) {
			Logger.WriteDebug("PrepareAnimation("+cre+",anim:"+anim+",numAnims:"+numAnims+",backwards:"+backwards+",undo:"+undo+",frameDelay:"+frameDelay+")");
			StartGenerating();
			EncodeByte(0x6e, 0);
			EncodeUInt(cre.FlaggedUid, 1);
			EncodeShort(anim, 5);
			EncodeByte(1, 7);//used to be numBackwardsFrames...?
			EncodeByte((byte) ((cre.Dir - 4)&0x7), 8);
			EncodeUShort(numAnims, 9);
			EncodeBool(backwards, 11);
			EncodeBool(undo, 12);
			EncodeByte(frameDelay, 13);
			DoneGenerating(14);
			Compress();
		}
		
		public static void PrepareEffect(IPoint4D source, IPoint4D target, byte type, ushort effect, byte speed, byte duration, ushort unk, byte fixedDirection, byte explodes, uint hue, uint renderMode) {
			StartGenerating();
			EncodeByte(0xc0, 0);
			EncodeByte(type, 1);
			source = source.TopPoint;
			target = target.TopPoint;
			Thing sourceAsThing = source as Thing;
			if (sourceAsThing != null) {
				EncodeUInt(sourceAsThing.FlaggedUid, 2);
			} else {
				EncodeUInt(0xffffffff, 2);
			}
			Thing targetAsThing = target as Thing;
			if (targetAsThing != null) {
				EncodeUInt(targetAsThing.FlaggedUid, 6);
			} else {
				EncodeUInt(0xffffffff, 6);
			}
			EncodeUShort(effect, 10);
			EncodeUShort(source.X, 12);
			EncodeUShort(source.Y, 14);
			EncodeSByte(source.Z, 16);
			EncodeUShort(target.X, 17);
			EncodeUShort(target.Y, 19);
			EncodeSByte(target.Z, 21);
			EncodeByte(speed, 22);
			EncodeByte(duration, 23);
			EncodeUShort(unk, 25);
			EncodeByte(fixedDirection, 26);
			EncodeByte(explodes, 27);
			EncodeUInt(hue, 28);
			EncodeUInt(renderMode, 32);
			DoneGenerating(36);
			Compress();
		}
		
		/*
		public static void SendInitFastwalk(GameConn c) {
			int r;
			StartGenerating();
			EncodeByte(0xbf, 0);
			EncodeShort(29, 1);
			EncodeShort(1, 3);
			
			EncodeInt(r = Server.dice.Next(1, Int32.MaxValue), 25);     // Encoding backwards because of the client top of stack
			c.FastWalk.Push(r);
			EncodeInt(r = Server.dice.Next(1, Int32.MaxValue), 21);
			c.FastWalk.Push(r);
			EncodeInt(r = Server.dice.Next(1, Int32.MaxValue), 17);
			c.FastWalk.Push(r);
			EncodeInt(r = Server.dice.Next(1, Int32.MaxValue), 13);
			c.FastWalk.Push(r);
			EncodeInt(r = Server.dice.Next(1, Int32.MaxValue), 9);
			c.FastWalk.Push(r);
			EncodeInt(r = Server.dice.Next(1, Int32.MaxValue), 5);
			c.FastWalk.Push(r);
			
			DoneGenerating(29);
			Compress();
			SendTo(c, true);
		}
		
		public static void SendAddFastwalkKey(GameConn c) {
			StartGenerating();
			EncodeByte(0xbf, 0);
			EncodeShort(9, 1);                    //packet length
			EncodeShort(2, 3);                    //subcommand
			EncodeInt((int)c.FastWalk.Peek(), 5);  //new key
			DoneGenerating(3);
			Compress();
			SendTo(c, true);
		}
		*/
		
		public static void SendUpdateRange(GameConn c, byte updateRange) {
			Logger.WriteDebug("Sending update range "+updateRange+" to "+c);
			StartGenerating();
			EncodeByte(0xc8, 0);
			EncodeByte(updateRange, 1);
			DoneGenerating(2);
			Compress();
			SendTo(c, true);
		}
		
		//public static void PrepareMovingItemAnimation(IItemState former, IItemState current) {
		//	PrepareMovingItemAnimation(former, current, former.ShortAmount);
		//}
		//public static void PrepareMovingItemAnimation(IItemState former, IItemState current, ushort amount) {
		//	PrepareMovingItemAnimation(former, current, former, current, amount);
		//}
		//public static void PrepareMovingItemAnimation(IItemState former, IItemState current, IPoint6D formerP, IPoint6D currentP) {
		//	PrepareMovingItemAnimation(former, current, formerP, currentP, former.ShortAmount);
		//}
		public static void PrepareMovingItemAnimation(IPoint4D from, IPoint4D to, AbstractItem i) {
			//PrepareSound(former.TopObj(), SoundFX.AmbientOwl);
			StartGenerating();
			EncodeByte(0x23, 0);
			EncodeUShort(i.Model, 1);
			EncodeZeros(3, 3);
			EncodeUShort(i.ShortAmount, 6);
			from = from.TopPoint;
			Thing asThing = from as Thing;
			if (asThing != null) {
				EncodeUInt(asThing.FlaggedUid, 8);
			} else {
				EncodeUInt(0xffffffff, 8);
			}
			EncodeUShort(from.X, 12);
			EncodeUShort(from.Y, 14);
			EncodeSByte(from.Z, 16);

			to = to.TopPoint;
			asThing = to as Thing;
			if (to != null) {
				EncodeUInt(asThing.FlaggedUid, 17);
			} else {
				EncodeUInt(0xffffffff, 17);
			}
			EncodeUShort(to.X, 21);
			EncodeUShort(to.Y, 23);
			EncodeSByte(to.Z, 25);
			DoneGenerating(26);
			Compress();
		}
		
		//First test results: Crashes the client. Heh! Hopefully that's because I didn't enable AOS stuff and
		//it wasn't expecting this. -SL
		public static void PrepareCharacterName(AbstractCharacter character) {
			StartGenerating();
			EncodeByte(0x98, 0);
			ushort blockSize=37;
			EncodeUInt(character.FlaggedUid, 3);
			EncodeString(character.Name, 7, 29);
			EncodeByte(0, 36);
			EncodeUShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();
		}
		
		public static void PrepareGump(GumpInstance instance) {
			//1+2+4+4+4+4+2+instance.layout.Length+1+2+(instance.textsLength+(instance.texts.Length*2)) -tar
			//lol -SL
			StartGenerating();
			EncodeByte(0xb0, 0);
			EncodeUInt(instance.focus.FlaggedUid, 3);
			EncodeUInt(instance.uid, 7);
			EncodeUInt(instance.x, 11);
			EncodeUInt(instance.y, 15);
			string layout = instance.layout.ToString();
			int layoutLength = layout.Length;
			if (layoutLength == 0) {
				//throw new SEException("The gump is empty!");
				Logger.WriteWarning("The GumpInstance "+instance+" represents an empty gump?!");
			}
			Sanity.IfTrueThrow(layoutLength>32768,"Gump layout for '"+instance.def.Defname+"' is too long ("+layout.Length+"). That would take at least several seconds to send, and you'd probably crash the client by sending something that big, if it was sendable at all (Also, the packet's size can never be > 65535 bytes (It has to be stored in a ushort and sent to the client, and that's the highest value a ushort can hold)).");
			EncodeUShort((ushort)(layoutLength+1), 19);
			//no idea why does it have to be +1, but it wont work without it... it is not written in any packet guide, I sniffed it from sphere, tried it, and it worked :) -tar
			//It probably includes the null terminator. Just another little inconsistancy in the packets, nothing unusual... -SL
			
			ushort blockSize = (ushort) (21+EncodeString(layout, 21));
			EncodeByte(0, blockSize);	//null terminator for the layout string
			int numTextLines;
			if (instance.textsList != null) {
				numTextLines = instance.textsList.Count;
			} else {
				numTextLines = 0;
			}
			EncodeUShort((ushort)numTextLines, blockSize+1);
			blockSize+=3;
			for (int i = 0; i<numTextLines; i++) {
				string line = instance.textsList[i];
#if DEBUG
				if (line.Length>4096) {
					Sanity.IfTrueThrow(true, "You're trying to send a text line in '"+instance.def.Defname+"' which is "+line.Length+" bytes long. Are you trying to crash the client? This line begins with '"+line.Substring(0, 30)+"'.");
				}
#endif
				ushort bytelen = (ushort) EncodeUnicodeString(line, blockSize+2);
				EncodeUShort((ushort)line.Length, blockSize);
				blockSize+=(ushort)(bytelen+2);
			}
			EncodeUShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();
		}
		
		//0xbf:
		//BYTE cmd
		//BYTE[2] len
		//BYTE[2] subcmd
		//BYTE[len-5] submessage 
		
		//Subcommand: 0x19: Extended stats
		//
		//    * BYTE type // always 2 ? never seen other value
		//    * BYTE[4] serial
		//    * BYTE unknown // always 0 ?
		//    * BYTE lockBits // Bits: XXSS DDII (s=strength, d=dex, i=int), 0 = up, 1 = down, 2 = locked
		
		//from the above i ould say that this is wrong, there are Bytes but they should be Shorts -tar
		public static void PrepareStatLocks(AbstractCharacter chr) {
			StartGenerating();
			EncodeByte(0xbf, 0);
			EncodeByte(12, 1);
			EncodeByte(0x19, 2);
			EncodeByte(0x02, 3);
			EncodeUInt(chr.FlaggedUid, 4);
			EncodeByte(0, 10);
			EncodeByte(chr.StatLockByte, 11);
			DoneGenerating(12);
			Compress();
		}
		
		public static void PrepareCloseGump(uint gumpUid, int buttonId) {
			StartGenerating();
			EncodeByte(0xbf, 0);
			EncodeShort(13, 1);
			EncodeShort(0x04, 3);
			EncodeUInt(gumpUid, 5);
			EncodeInt(buttonId, 9);
			DoneGenerating(13);
			Compress();
		}
		
		//use the prepared version
		internal static void PrepareFacetChange(byte facet) {
			StartGenerating();
			EncodeByte(0xbf, 0);
			EncodeShort(6, 1);
			EncodeShort(0x08, 3);
			EncodeByte(facet, 5);
			DoneGenerating(13);
			Compress();
		}
		
		public static void PreparePing(byte data) {
			StartGenerating();
			EncodeByte(0x73, 0);
			EncodeByte(data, 1);
			DoneGenerating(2);
			Compress();
		}
		
		public static void PrepareClilocMessage(Thing from, uint msg, SpeechType type, ushort font, ushort color, string args) {
			string sourceName = from==null?"":from.Name;
			ushort model = (from==null?(ushort)0xffff:from.Model);
			if (color==0) {	//black, which only shows in the journal.
				color=Globals.defaultUnicodeMessageColor;
			}
			Sanity.IfTrueThrow(sourceName==null, "PrepareClilocMessage called with a null 'sourceName'.");
			StartGenerating();
			EncodeByte(0xc1, 0);
			ushort blockSize = 0;
			if (from==null) {
				EncodeInt(-1, 3);
			} else {
				EncodeUInt(from.FlaggedUid, 3);
			}
			EncodeUShort(model, 7);
			EncodeByte((byte) type, 9);
			EncodeUShort(color, 10); //text color
			EncodeUShort(font, 12); //font
			EncodeUInt(msg, 14);
			EncodeString(sourceName, 18, 30);
			blockSize=18+30;
			if (args == null) {
				args = "";
			}
			int len = EncodeLittleEndianUnicodeString(args, blockSize, 1024);
			blockSize+=(ushort) len;
			EncodeShort(0, blockSize);
			blockSize += 2;
			EncodeUShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();			
		}
		
		public static void PrepareAllSkillsUpdate(ISkill[] skills, bool displaySkillCaps) {
			Sanity.IfTrueThrow(skills==null, "PrepareAllSkillsUpdate called with a null 'skills'.");
			StartGenerating();
			EncodeByte(0x3A, 0);
			if (displaySkillCaps) {
				EncodeByte(0x02, 3);//full list with skillcaps
			} else {
				EncodeByte(0x00, 3);//full list without skillcaps
			}
			short blockSize = 4;
			for (ushort i = 0; i<46 ; i++) {
				ISkill s = skills[i];
				EncodeUShort((ushort) (i+1), blockSize);
				EncodeUShort(s.RealValue, blockSize+2);
				EncodeUShort(s.RealValue, blockSize+4);
				EncodeByte((byte) s.Lock, blockSize+6);
				if (displaySkillCaps) {
					EncodeUShort(s.Cap, blockSize+7);
					blockSize+=9;
				} else {
					blockSize+=7;
				}
			}
			EncodeUShort(0, blockSize);//terminator
			blockSize+=2;
			EncodeShort(blockSize, 1);
			DoneGenerating(blockSize);
			Compress();	
		}
		
		public static void PrepareSingleSkillUpdate(ISkill skill, bool displaySkillCap) {
			Sanity.IfTrueThrow(skill==null, "PrepareSingleSkillUpdate called with a null 'skill'.");
			StartGenerating();
			EncodeByte(0x3A, 0);
			
			if (displaySkillCap) {
				EncodeShort(13, 1);//blocksize
				EncodeByte(0xDF, 3);//single skill update with cap
				EncodeUShort(skill.Cap, 11);
				DoneGenerating(13);
			} else {
				EncodeShort(11, 1);//blocksize
				EncodeByte(0xFF, 3);//single skill update without skillcap
				DoneGenerating(11);
			}
			
			EncodeUShort((ushort) (skill.Id), 4);
			EncodeUShort(skill.RealValue, 6);
			EncodeUShort(skill.RealValue, 8);
			EncodeByte((byte) skill.Lock, 10);
			
			Compress();	
		}
		
		public static void PrepareRequestCliVer() {
			StartGenerating();
			EncodeByte(0xbd, 0);
			EncodeShort(0x3, 1);
			DoneGenerating(3);
			Compress();	
		}

		public static void PreparePropertiesRefresh(Thing t, int propertiesUid) {
			StartGenerating();
			EncodeByte(0xdc, 0);
			EncodeUInt(t.FlaggedUid, 1);
			EncodeInt(propertiesUid, 5);
			DoneGenerating(9);
			Compress();	
		}

		public static void PrepareOldPropertiesRefresh(Thing t, int propertiesUid) {
			StartGenerating();
			EncodeByte(0xbf, 0);
			EncodeShort(13, 1);
			EncodeShort(0x10, 3);
			EncodeUInt(t.FlaggedUid, 5);
			EncodeInt(propertiesUid, 9);
			DoneGenerating(13);
			Compress();
		}

		public static void PrepareMegaCliloc(Thing t, int propertiesUid, IList<uint> ids, IList<string> strings) {
			StartGenerating();
			EncodeByte(0xd6, 0);
			EncodeShort(1, 3);
			EncodeUInt(t.FlaggedUid, 5);
			EncodeShort(0, 9);
			EncodeInt(propertiesUid, 11);

			int len = 15;
			for (int i = 0, n = ids.Count; i<n; i++) {
				EncodeUInt(ids[i], len);
				string msg = strings[i];
				if (msg == null) {
					msg = "";
				}
				int strLen = EncodeLittleEndianUnicodeString(msg, len+6);
				EncodeShort((short) strLen, len+4);
				len += strLen + 6;
			}
			EncodeInt(0, len);
			len += 4;
			EncodeShort((short) len, 1);
			DoneGenerating(len);
			Compress();
		}

		public static void PrepareDeathAnim(AbstractCharacter dieingChar, AbstractItem corpse) {
			StartGenerating();
			EncodeByte(0xaf, 0);
			EncodeUInt(dieingChar.FlaggedUid, 1);
			if (corpse == null) {
				EncodeUInt(0xffffffff, 5);
			} else {
				EncodeUInt(corpse.FlaggedUid, 5);
			}
			EncodeInt(0, 9);
			DoneGenerating(13);
			Compress();
		}

		internal static void PrepareDeathMessage(byte type) {
			StartGenerating();
			EncodeByte(0x2c, 0);
			EncodeByte(type, 1);
			DoneGenerating(2);
			Compress();
		}

		public interface ICorpseEquipInfo {
			uint FlaggedUid { get; }
			byte Layer { get; }
			ushort Color { get; }
			ushort Model { get; }
		}

		public static void PrepareCorpseEquip(AbstractItem corpse, IEnumerable<ICorpseEquipInfo> items) {
			StartGenerating();
			EncodeByte(0x89, 0);
			int len = 7;
			EncodeUInt(corpse.FlaggedUid, 3);
			foreach (ICorpseEquipInfo iulp in items) {
				EncodeByte(iulp.Layer, len);
				EncodeUInt(iulp.FlaggedUid, len+1);
				len += 5;
			}

			EncodeByte(0, len);//terminator
			len++;
			EncodeShort((short) len, 1);
			DoneGenerating(len);
			Compress();
		}

		public static bool PrepareCorpseContents(AbstractItem corpse, IEnumerable<AbstractItem> items, ICorpseEquipInfo hair, ICorpseEquipInfo beard) {
			StartGenerating();
			EncodeByte(0x3c, 0);
			ushort blockSize = 5;
			ushort numSegments = 0;
			foreach (AbstractItem i in items) {
				numSegments++;
				EncodeUInt(i.FlaggedUid, blockSize);
				EncodeUShort(i.Model, blockSize+4);
				EncodeByte(0, blockSize+6);
				EncodeUShort(i.ShortAmount, blockSize+7);
				EncodeUShort(i.X, blockSize+9);
				EncodeUShort(i.Y, blockSize+11);
				EncodeUInt(corpse.FlaggedUid, blockSize+13);
				EncodeUShort(i.Color, blockSize+17);
				blockSize+=19;
			}
			if (hair != null) {
				numSegments++;
				EncodeUInt(hair.FlaggedUid, blockSize);
				EncodeUShort(hair.Model, blockSize+4);
				EncodeByte(0, blockSize+6);
				EncodeUShort(1, blockSize+7);
				EncodeUShort(0, blockSize+9);
				EncodeUShort(0, blockSize+11);
				EncodeUInt(corpse.FlaggedUid, blockSize+13);
				EncodeUShort(hair.Color, blockSize+17);
				blockSize+=19;
			}
			if (beard != null) {
				numSegments++;
				EncodeUInt(beard.FlaggedUid, blockSize);
				EncodeUShort(beard.Model, blockSize+4);
				EncodeByte(0, blockSize+6);
				EncodeUShort(1, blockSize+7);
				EncodeUShort(0, blockSize+9);
				EncodeUShort(0, blockSize+11);
				EncodeUInt(corpse.FlaggedUid, blockSize+13);
				EncodeUShort(beard.Color, blockSize+17);
				blockSize+=19;
			}

			if (blockSize == 0) {
				DiscardUncompressed();
				return false;
			}
			EncodeUShort(blockSize, 1);
			EncodeUShort(numSegments, 3);
			DoneGenerating(blockSize);
			Compress();
			return true;
		}

		public static void PrepareItemInCorpse(AbstractItem corpse, ICorpseEquipInfo i) {
			Sanity.IfTrueThrow(i==null, "PrepareItemInContainer called with a null item.");
			StartGenerating();
			EncodeByte(0x25, 0);
			EncodeUInt(i.FlaggedUid, 1);
			EncodeUShort(i.Model, 5);
			EncodeByte(0, 7);
			EncodeUShort(1, 8);
			EncodeUShort(0, 10);
			EncodeUShort(0, 12);
			EncodeUInt(corpse.FlaggedUid, 14);
			EncodeUShort(i.Color, 18);
			DoneGenerating(20);
			Compress();
		}
	}
}
