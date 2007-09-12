//This software is released under GNU internal license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

using System;
using System.IO;
using System.Runtime.Serialization;
using SteamEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using SteamEngine.Common;

namespace SteamEngine.Packets {

	internal class PacketHandler : InPackets {
		
		internal PacketHandler() {
		}
		
		internal void Cycle(GameConn conn) {
			Globals.SetSrc(conn.CurCharacter);
			packet = myPacket;	//in case an async read returns a different array, myPacket always points to our big one.
			try {
    			packetLen = conn.Read(ref packet, 0, Server.maxPacketLen);
    			//poll the Conn for the packet
    			//Console.WriteLine("Packet len "+packetLen);
    		} catch (FatalException) {
				throw;
			} catch (Exception e) {
    			packetLen=-1;
    			Logger.WriteError("PacketHandler.Cycle(GameConn) caught exception thrown by Read: ",e);
    		}
			try {
				if (packetLen<=0) {
					conn.Close("Connection lost");
				} else {
					//handle packet

					//Darkstorms encryption
					//Console.WriteLine("Before decoding: packet 0x"+packet[0].ToString("x")+", len "+packetLen);
					//OutputPacketLog();
					conn.encryption.DecodeIncomingPacket(conn, ref packet, ref packetLen);
					//Console.WriteLine("After decoding: packet 0x"+packet[0].ToString("x")+", len "+packetLen);
					//OutputPacketLog();

					if (conn.justConnected) {
						HandleInitConnection(conn);
					} else {
						packetLenUsed=0;
						bool cont=true;
						while ((cont)&&(conn.IsConnected)) {
							HandlePacket(conn);
							if (packetLenUsed!=0 && packetLenUsed<packetLen) {
								packetLen-=packetLenUsed;
								Buffer.BlockCopy(packet, packetLenUsed, packet, 0, packetLen);
								packetLenUsed=0;
							} else {
								cont=false;
							}
						}
					}
				}
			} catch (FatalException) {
				throw;
			} catch (IOException) {
				conn.Close("Lost connection");
			} catch (SocketException) {
				conn.Close("Lost connection");
			} catch (Exception e) {
				Logger.WriteCritical(e);
				conn.Close("Exception caught while handling incoming packet. Avoid this at all costs. As a punishment, client closed.");
			}
		}
		
		internal void HandleInitConnection(GameConn c) {
			try {
				Logger.WriteDebug("InitConnection "+packetLen);
				if (packetLen<4) {
					c.Close("Wrong initializing packet.");
				} else if (packetLen>=4) {
					c.justConnected=false;
					//Buffer.BlockCopy(packet,0,c.initCode,0,4);
					packetLenUsed=4;
					if (packetLen>4) {
						bool cont=true;
						while (cont) {
							if (packetLenUsed!=0 && packetLenUsed<packetLen) {
								packetLen-=packetLenUsed;
								Buffer.BlockCopy(packet,packetLenUsed,packet,0,packetLen);
								packetLenUsed=0;
								HandlePacket(c);
							} else {
								cont=false;
							}
						}
					}
				}
			} catch (IOException) {
				c.Close("Lost connection");
			} catch (SocketException) {
				c.Close("Lost connection");
			}
		}
		
		internal void HandlePacket(GameConn c) {
			c.noResponse = false;
			if (packetLen==0) {
				return;
			}
#if DEBUG
			if ((packet[0] != 0xbf) || (packet[4] != 0x24)) {//this packet is spammed by the client 4.0.something+, we dont want even the debug console full of these
				Logger.WriteDebug("Handling packet 0x"+packet[0].ToString("x")+" from "+c);
			}
#endif
			if (c.curAccount==null && (packet[0]!=0x80 && packet[0]!=0x91 && packet[0]!=0xa0 && packet[0]!=0xa4 && packet[0]!=0xd9 && packet[0]!=0x73)) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Phase 1: Unexpected packet 0x"+packet[0].ToString("x"));
#if DEBUG
				OutputPacketLog();
#endif
				return;
			}
			if (c.CurCharacter==null && (packet[0]!=0x80 && packet[0]!=0x91 && packet[0]!=0xa0) && (packet[0]!=0x00 && packet[0]!=0x5d && packet[0]!=0x83) && packet[0]!=0xd9 && packet[0]!=0x73 && packet[0]!=0xa4) { 
				//added 0xa4 - old hardware info
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Phase 2: Unexpected packet 0x"+packet[0].ToString("x"));
#if DEBUG
				OutputPacketLog();
#endif
				return;
			}
			if (c!=null && c.CurCharacter!=null) {
				if (packet[0]==0x91 || packet[0]==0xa0 || packet[0]==0x5d || packet[0]==0x83) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
					c.Close("Phase 3: Unexpected packet 0x"+packet[0].ToString("x"));
#if DEBUG
					OutputPacketLog();
#endif
					return;
				}
				if (packet[0]==0x80) {
					packetLenUsed=62;
					return;
				}
				if (packet[0]==0x00) {
					packetLen=packetLen;//ignoring
					return;
				}
				
			}
			//OutputPacketLog();
			switch (packet[0]) {	
				case 0x00:		{	//create character
					CreateCharPacket(c);
					return;
				} case 0x01:	{	//returning to main menu
					packetLenUsed=5;
					c.Close("Client logoff.");
					return;
				} case 0x02:	{	//client wants to move
					HandleMoveRequest(c);
					//c.requestedResync = false;//well... when should this be reset? :)
					//It's not used at all, right now. -SL
					return;
				} case 0x03:	{	//client wants to speak
					HandleSpeech(c);
					return;
				} case 0x04:	{
					HandleGodModeRequest(c);
					return;
				} case 0x05:	{	//client wants to attack
					HandleAttack(c);
					return;
				} case 0x06:	{	//client double clicks something
					HandleDoubleClick(c);
					return;
				} case 0x07:	{	//client wants to pick up an item
					HandlePickupItem(c);
					return;
				} case 0x08:	{	//client wants to drop an item (onto item or into container)
					HandleDropItem(c);
					return;
				} case 0x09:	{	//client single clicks something
					HandleSingleClick(c);
					return;
				} case 0x0a:	{	//god client 'edit' packet
					HandleGodClientEditRequest(c);
					return;
				} case 0x0b:	{	//god client 'edit area' packet
					break;
				} case 0x12:	{	//client wants to use a skill/action/magic/open door
					HandleUse(c);
					return;
				} case 0x13:	{	//client wants to wear a held item or place it on a target (in other words, drop item on char)
					HandleWearItem(c);
					return;
				} case 0x1a:	{	//statlock change
					HandleStatLockChange(c);
					break;
				} case 0x22:	{	//client wants to resync movement
					HandleResyncMoveRequest(c);
					return;
				} case 0x2c: {		//sends client after death...
				    packetLenUsed = 2;
				    return;
				} case 0x3A:	{	//client wants to change skill lock on a particular skill
					HandleSkillLockChange(c);
					return;
				} case 0x34:	{	//requesting player status
					HandleRequestStatus(c);
					return;
				} case 0x3b:	{	//request to buy items
					//HandleRequestBuyItems(c);
					break;
				} case 0x4b:	{	//"Check Version" (obsolete according to Kair's packet guide. Sent by the 2.0.8n God Client, at least)
					HandleCheckVersion(c);
					return;
				} case 0x58:	{	//new area
					HandleGodClientNewAreaRequest(c);
					return;
				} case 0x5d:	{	//login char
					//OutputPacketLog();
					LoginChar(c);
					return;
				} case 0x61:	{	//delete static
					HandleGodClientDeleteStaticRequest(c);
					return;
				} case 0x66:	{	//requesting book page or writing to it
					//HandleBookRequest(c);
					break;
				} case 0x6c:	{	//object targetted, or ground
					HandleTarget(c);
					return;
				} case 0x72:	{	//request war mode change
					HandleWarModeChange(c);
					return;
				} case 0x73:	{	//ping
					HandlePing(c);
					return;
				} case 0x7d:	{	//response to menu
					break;
				} case 0x80:	{	//login request
					packetLenUsed=62;
					PacketSender.SendServersList(c);
					return;
				} case 0x83:	{	//delete character
					DeleteCharPacket(c);
					return;
				} case 0x91:	{	//game server login
					GameServerLogin(c);
					return;
				} case 0x93:	{	//sent on book close
					break;
				} case 0x95:	{	//dye something
					break;
				} case 0x98:	{	//left ctrl+shift in newer clients. shows all names.
					HandleNameRequest(c);
					return;
				} case 0x9b:	{	//help button
					HelpRequest(c);
					return;
				} case 0x9f:	{	//sell request
					break;
				} case 0xa0:	{	//select server
					packetLenUsed=3;
					PacketSender.SendLoginToServer(c, DecodeUShort(1));
					c.encryption.Relayed();
					return;
				} case 0xa4:	{	//old packet for client hardware info, new one is 0xd9 - ignore
					//OutputPacketLog();
					packetLenUsed=149;
					return;
				} case 0xa7:	{	//request tips/notice
					//ignore this, for now
					packetLenUsed=4;
					return;
				} case 0xad:	{	//unicode speech request
					HandleUnicodeSpeech(c);
					return;
				} case 0xb1:	{	//gump response
					HandleGumpResponse(c);
					return;
				} case 0xb6:	{	//help/tip request
					packetLenUsed=9;
					break;
				} case 0xb8:	{	//request 'char profile' - NOT the PD
					break;
				} case 0xbb:	{	//ultima messenger, 9 bytes, ignorable
					packetLenUsed=9;
					return;
				} case 0xbd:	{	//client version
					HandleClientVersion(c);
					return;
				} case 0xbf:	{	//subcommand thingy
					if (!HandleSubCommandPacket(c)) {
						packetLenUsed = DecodeUShort(1);//That's (blockSize)
						OutputPacketLog(packetLenUsed);	
					}
					return;
				} case 0xc8:	{	//update range
					HandleSetUpdateRangeRequest(c);
					return;
				} case 0xd6: {	//request multiple megaclilocs
					HandleRequestMultipleProperties(c);
					return;
				} case 0xd9:	{	//client hardware info - ignore
					Logger.WriteDebug("0xd9 detected: Length="+packetLen);
					packetLenUsed=packetLen;
					return;
				} default: 	{	//unhandled client packet
					Logger.WriteError("Unknown packet 0x"+packet[0].ToString("x"));
					Server.SendSystemMessage(c, "Unknown packet 0x"+packet[0].ToString("x"), 0);
					OutputPacketLog();
					packetLenUsed=packetLen;
					return;
				}
					
			}
			string s = "Packet 0x"+packet[0].ToString("x")+" recognized, but not yet supported.";
			Logger.WriteWarning(s);
			Server.SendSystemMessage(c, s, 0);
			OutputPacketLog();
			packetLenUsed=packetLen;
		}

		internal bool HandleSubCommandPacket(GameConn c) {
			//return true if we handle something
			ushort blocksize = DecodeUShort(1);
			ushort subcmd = DecodeUShort(3);
			if (subcmd != 0x24) {
				Logger.WriteDebug("Handling 0xbf subcmd "+subcmd.ToString("x")+".");
			}
			int len=((int)blocksize)-5;
			packetLenUsed=5;
			if (len<0 || blocksize>packetLen) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal 0xBF packet.");
			} else {
				switch (subcmd) {
					//it's a server packet, dummy ;) -tar
					//case 0x04: {
					//	//close generic gump
					//	HandleCloseGenericGumpRequest(c, blocksize);
					//	return true;
					//} 
					case 0x05: {
						//screen size
						HandleScreenSizePacket(c);
						return true;
					} case 0x06: {
						//party system
						int subcmd2 = packet[5];
						packetLenUsed++;
						switch (subcmd2) {
							case 0x01: {
								//add a party member
								packetLenUsed+=4;
								break;
							} case 0x02:  {
								//remove a party member
								packetLenUsed+=4;
								break;
							} case 0x03: {
								//tell party member something
								packetLenUsed=blocksize;
								break;
							} case 0x04: {
								//tell full party something
								packetLenUsed=blocksize;
								break;
							} case 0x06: {
								//set whether party can loot me
								packetLenUsed++;
								break;
							} case 0x08: {
								//accept join party invitation
								packetLenUsed+=4;
								break;
							} case 0x09: {
								//decline join party invitation
								packetLenUsed+=4;
								break;
							} default: {
								Server.SendSystemMessage(c, "Unknown party command "+subcmd2.ToString("x")+" in HandleSubCommandPacket.", 0);
								return false;
							}
						}
						Logger.WriteWarning("0xBF party command "+subcmd2.ToString("x")+" recognized, but not yet supported.");
						return false;
					} case 0x09: {	//primary ability, no extra data in the packet
						Temporary.UsePrimaryAbilityRequest(c.CurCharacter);
						return true;
					} case 0x0a: {	//secondary ability, no extra data in the packet
						Temporary.UseSecondaryAbilityRequest(c.CurCharacter);
						return true;
					} case 0x0b: {
						//client language
						HandleLanguagePacket(c, blocksize);
						return true;
					} case 0x0c: {
						//closed status gump
						HandleCloseStatusGump(c);
						return true;
					} case 0x0e: {
						//c.ClientIs(Client.OSI3D);
						if (!Globals.blockOSI3DClient) {	//if we do block it, then they're about to get kicked, otherwise we'll handle the action.
							//UO3D action..
							HandleUO3DAction(c);
						}
						return true;
					} case 0x0f: {
						//unknown	
						//10:44 AM:  BF
						//10:44 AM:  00 0A 00 0F 0A 00 00 00 0F
						packetLenUsed+=5;
						return false;
					} case 0x10: {
						HandleRequestProperties(c);
						return true;
					} case 0x13: {
						//request popup menu
						packetLenUsed+=4;
						break;
					} case 0x15: {
						//popup entry selection
						packetLenUsed+=6;
						break;
					} case 0x24: {
						//unknown, sent by 4.0.5a.
						packetLenUsed+=6;
						//Logger.WriteDebug("Disregarding 0xbf sub 0x24 packet (value=0x"+DecodeByte(5).ToString("x")+").");
						return true;
					} default: {
						Server.SendSystemMessage(c, "Unknown subcommand "+subcmd.ToString("x")+" in HandleSubCommandPacket.", 0);
						return false;
					}
				}
				Logger.WriteWarning("0xBF subcommand "+subcmd.ToString("x")+" recognized, but not yet supported.");
			}
			return false;
		}
		
		
	}
}
