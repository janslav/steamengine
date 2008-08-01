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
using System.IO;
using System.Runtime.Serialization;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SteamEngine;
using SteamEngine.Common;


namespace SteamEngine.Packets {
	internal class InPackets : PacketBase {
		public static bool MovementTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Movement Trace Messages"]);
		
		internal byte[] packet = null;
		internal byte[] myPacket = new byte [Server.maxPacketLen];
		internal int packetLen = 0;
		internal int packetLenUsed = 0;
		//internal bool FirstMoveRequest = true;
		
		internal InPackets() {
		}
		
		internal void OutputPacketLog() {
			OutputPacketLog(packet, packetLen);
		}
		internal void OutputPacketLog(int len) {
			OutputPacketLog(packet, len);
		}
		
		/**
			Decodes a unicode string, truncating it if it contains endlines (and replacing tabs with spaces).
			If the string contains a \0 (the 'end-of-string' character), it will be truncated.	
			
			@param startpos The position in the packet buffer at which the string starts.
			@param len The number of bytes to decode (two per character).
			@return The decoded string.
		*/
		internal string DecodeUnicodeString(int startpos, int len) {
			return DecodeUnicodeString(startpos, len, true);
		}
		
		/**
			Decodes a unicode string, truncating it if it contains endlines (and replacing tabs with spaces).
			If the string contains a \0 (the 'end-of-string' character), it will be truncated.

			@param startpos The position in the packet buffer at which the string starts.
			@param len The number of bytes to decode (two per character).
			@return The decoded string.
		*/
		internal string DecodeUnicodeString(int startpos, int len, bool truncateEndlines) {
			string str = Encoding.BigEndianUnicode.GetString(packet,startpos,len);
			return truncateEndlines?TagMath.RemoveIllegalChars(str):str;
		}

		/**
			Decodes an ascii string, truncating it if it contains endlines (and replacing tabs with spaces).
			If the string contains a \0 (the 'end-of-string' character), it will be truncated.	
			
			@param startpos The position in the packet buffer at which the string starts.
			@param len The length of the string.
			@return The decoded string.
		*/
		internal string DecodeAsciiString(int startpos, int len) {
			return DecodeAsciiString(startpos, len, true);
		}
		
		/**
			Decodes an ascii string.
			If the string contains a \0 (the 'end-of-string' character), it will be truncated.	
			
			@param startpos The position in the packet buffer at which the string starts.
			@param len The length of the string.
			@param truncateEndlines If true, truncates the string if it contains endlines (and replacing tabs with spaces).
			@return The decoded string.
		*/
		internal string DecodeAsciiString(int startpos, int len, bool truncateEndlines) {
			string str="";
			try {
				str = Encoding.UTF8.GetString(packet,startpos,len);
			} catch (ArgumentOutOfRangeException) {
				//return null;
				throw;
			}
			if (str.IndexOf((char)0)>-1) {
				str = str.Substring(0,str.IndexOf((char)0));
			}
			return truncateEndlines?TagMath.RemoveIllegalChars(str):str;
		}
		
		internal int DecodeInt(int startpos) {
			return ((packet[startpos]<<24)+(packet[startpos+1]<<16)+(packet[startpos+2]<<8)+packet[startpos+3]);
		}
		
		internal uint DecodeUInt(int startpos) {
			return (uint) ((packet[startpos]<<24)+(packet[startpos+1]<<16)+(packet[startpos+2]<<8)+packet[startpos+3]);
		}
		
		internal short DecodeShort(int startpos) {
			return (short) ((packet[startpos]<<8)+packet[startpos+1]);
		}
		
		internal ushort DecodeUShort(int startpos) {
			return (ushort) ((packet[startpos]<<8)+packet[startpos+1]);
		}
		
		internal sbyte DecodeSByte(int startpos) {
			return (sbyte) packet[startpos];
		}
		
		internal byte DecodeByte(int startpos) {
			return packet[startpos];
		}
		
		internal void HandleGodModeRequest(GameConn c) {
			packetLenUsed=2;
			byte req=packet[1];
			if (req==0 || req==1) {
				if (c.MaxPlevel>=Globals.plevelOfGM) {
					c.GodMode=(req==1);	//1:true or 0:false
				} else {
					Server.SendSystemMessage(c, "Your account is not priviliged enough to use god-mode.", 0);
					c.SuspiciousError("Not-priviliged-enough account attempting to use god-mode.");
					return;
				}
			} else {
				c.SuspiciousError("Unknown god mode requested state "+req+". We expected either 0 or 1.");
			}
		}
		
		//untested since we apparently can't see (or add?) NPCs at present (Aug 24 2004) -SL
		internal void HandleAttack(GameConn c) {
			packetLenUsed=5;
			int uid = DecodeInt(1);
			uid = Thing.UidClearFlags(uid);


			AbstractCharacter cre = Thing.UidGetCharacter(uid);
			AbstractCharacter self = c.CurCharacter;
			if ((cre == null) || (cre.IsDeleted)) {
				PacketSender.PrepareRemoveFromView(uid);
				PacketSender.SendTo(c, true);
			} else {
				self.AttackRequest(cre);
			}
		}
	
		
		internal void HandleGodClientDeleteStaticRequest(GameConn c) {
			//OutputPacketLog();
			packetLenUsed=9;
			Logger.WriteWarning("Recieved unimplemented god mode Delete Static command.");
			OutputPacketLog(packetLenUsed);
		}

		//The god client has the user enter upper left and lower right coordinates, but sends width and height instead
		//of lower right coordinates. If you put in, say, UL(10,10) and LR(20,30), the GC will send UL(10,10)
		//and Width(10) and Height(20).
		
		//Z range is 0-255. If the user enters a value below 0 in the GC, the GC changes it to their current Z level.
		//Normal Z levels can be negative, so don't ask me what these Z values mean...
		internal void HandleGodClientNewAreaRequest(GameConn c) {
			packetLenUsed=106;
			Logger.WriteWarning("Recieved unimplemented god mode add Area command.");
			OutputPacketLog(packetLenUsed);
		}
		
		internal void HandleGodClientEditRequest(GameConn c) {
			packetLenUsed=11;
			if (!c.GodMode) {
				c.SuspiciousError("Attempt to use god client features without being in god mode.");
				return;
			}
			byte command = packet[1];
			ushort x = DecodeUShort(2);
			ushort y = DecodeUShort(4);
			ushort id = DecodeUShort(6);
			sbyte z = DecodeSByte(8);
			ushort color = DecodeUShort(9);
			/*
			12:42: (D)  0A
			12:42: (D)  0A 14 71 00 23 00 02 0F 00 00
			*/
			switch (command) {
				case 0x04: {	//add new dynamic item
					AbstractItemDef idef = ThingDef.FindItemDef(id);
					AbstractItem itm = (AbstractItem) idef.Create(x, y, z, c.CurCharacter.M);
					itm.Color=color;
					break;
				} case 0x06: {	//Hackmove request
					c.HackMove=(x==1);	//1:true or 0:false
					break;
				} case 0x07: {	//Add new NPC
					//AbstractCharacter chr = ThingDef.CreateCharacter(id.ToString(), x, y, z, c.curCharacter.M);
					Logger.WriteWarning("Recieved unimplemented god mode add NPC command.");
					OutputPacketLog(packetLenUsed);
					break;
				} case 0x0A: {	//Add new static item
					Logger.WriteWarning("Recieved unimplemented god mode add Static command.");
					OutputPacketLog(packetLenUsed);
					break;
				} default: {
					Logger.WriteWarning("Unknown god mode edit command "+command+".");
					OutputPacketLog(packetLenUsed);
					break;
				}
			}
		}
		
		internal void HandleWarModeChange(GameConn c) {
			AbstractCharacter cre=c.CurCharacter;
			packetLenUsed=5;
			int status=packet[1];
			if (packet[1] == 0) {
				cre.Flag_WarMode=false;
			} else {
				cre.Flag_WarMode=true;
			}
		}
		
		internal void HandleCheckVersion(GameConn c) {
			packetLenUsed=9;
			ushort a=DecodeUShort(1);
			ushort b=DecodeUShort(3);
			uint version=DecodeUInt(5);
			Logger.WriteWarning("Recieved Check Ver a["+a+"] b["+b+"] v["+version+"] : hex a["+a.ToString("x")+"] b["+b.ToString("x")+"] v["+version.ToString("x")+"]");			
		}
		
		internal void HandleUse(GameConn c) {
			ushort blockSize=DecodeUShort(1);
			packetLenUsed=blockSize;
			byte command=packet[3];
			switch (command) {
				case 0x00: {	//god mode teleport
					Logger.WriteWarning("God-mode teleport unimplemented.");
					OutputPacketLog(packetLenUsed);
					break;
				} case 0x24: {	//skill macro
					//expect either one or two number characters followed by 0x20, then 0x30, then 0x00.
					int skill=0;
					int num=0;
					if (packet[5]==0x20) {
						skill=packet[4]-48;
						num=packet[6]-48;
					} else if (packet[6]==0x20) {
						//This should still be faster than (packet[4]-48)*10+(packet[5]-48).
						int x=(packet[4]-48);
						skill=(x<<3)+(x<<1)+(packet[5]-48);	//not checked for invalid values here
						num=packet[7]-48;
					} else {
						c.SuspiciousError("Missing a space in use skill packet?");
						#if DEBUG
						OutputPacketLog(packetLenUsed);
						#endif
					}
					if (num!=0) {
						c.EchoMessage("Unexpected behavior from use skill packet: 'Num' (normally 0) was sent as "+num+".");
					}
					//if (skill==0) {
					//	Temporary.UseLastSkillRequest(c.curCharacter);
					//} else {
					//	Temporary.UseSkillNumberRequest(c.curCharacter, skill);
					//}
					c.CurCharacter.SelectSkill(skill);
					break;
				} case 0x56: {	//spell macro
					//expect either one or two number characters followed by 0x00.
					int spell=0;
					if (packet[5]==0x00) {
						spell=packet[4]-48;
					} else if (packet[6]==0x00) {
						//This should still be faster than (packet[4]-48)*10+(packet[5]-48).
						int x=(packet[4]-48);
						spell=(x<<3)+(x<<1)+(packet[5]-48);	//not checked for invalid values here
					} else {
						c.SuspiciousError("Missing a space in cast spell packet?");
						#if DEBUG
						OutputPacketLog(packetLenUsed);
						#endif
					}
					if (spell==0) {
						Temporary.UseLastSpellRequest(c.CurCharacter);
					} else {
						Temporary.UseSpellNumberRequest(c.CurCharacter, spell);
					}
					break;
				} case 0x58: {	//open door macro
					if (blockSize!=5) {
						c.SuspiciousError("Open door macro with a blockSize not equal to 5! It's "+blockSize+".");
						#if DEBUG
						OutputPacketLog(packetLenUsed);
						#endif
					}
					Temporary.OpenDoorMacroRequest(c.CurCharacter);
					OutputPacketLog(packetLenUsed);
					break;
				} case 0x6b: {	//GC command, can happen before entering god mode
					string gccommand = "";
					if (c.GodMode) {	//If we trust them enough to let them enter god mode, don't check string length:
						gccommand = DecodeAsciiString(4, blockSize-4);
					} else {
						if (blockSize>36) {
							gccommand = DecodeAsciiString(4, 32)+"...";
						} else {
							gccommand = DecodeAsciiString(4, blockSize-4);
						}
					}
					Temporary.GodClientCommandRequest(c, gccommand);
					break;
				} case 0xc7: {	//anim
					byte animId=packet[4];
					if (animId==98) {			//b
						Temporary.AnimRequest(c.CurCharacter, RequestableAnim.Bow);
					} else if (animId==115) {	//s
						Temporary.AnimRequest(c.CurCharacter, RequestableAnim.Salute);
					#if DEBUG
					} else {
						if (packetLenUsed>32) {
							c.SuspiciousError("0x12 Anim packet of over 32 bytes ('"+DecodeAsciiString(4, 32)+"'...)");
							break;
						}
						string anim = DecodeAsciiString(4, blockSize-4);
						c.SuspiciousError("Unknown Anim name in 0x12 Anim packet ('"+anim+"')");
					#endif
					}
					break;
				} default: {
					Logger.WriteWarning("Unknown 0x12 packet command "+command.ToString("x")+".");
					OutputPacketLog(packetLenUsed);
					break;
				}
			}
		}
		
		//internal void HandleCloseGenericGumpRequest(GameConn c, ushort blocksize) {
		//	Logger.WriteWarning("Recieved unimplemented close generic gump request (0xbf sub 0x04) from "+c+".");
		//	OutputPacketLog(blocksize);
		//	//packetLenUsed+=8;
		//	
		//}
		
		internal void HandleSetUpdateRangeRequest(GameConn c) {
			packetLenUsed=2;
			byte updateRange = packet[1];
			//min 5, max 18. Default 18.
			Logger.WriteDebug("Update Range requested="+updateRange);
			if (updateRange<Globals.MinUpdateRange) {
				c.RequestedUpdateRange=Globals.MinUpdateRange;
			} else if (updateRange>Globals.MaxUpdateRange) {
				c.RequestedUpdateRange=Globals.MaxUpdateRange;
				//Logger.SuspiciousError(c, "Invalid update range requested: "+updateRange+". Valid values are "+Globals.MinUpdateRange+"-"+Globals.MaxUpdateRange);
			} else {
				c.RequestedUpdateRange=updateRange;
			}
			Logger.WriteDebug("Requested Update Range set="+c.RequestedUpdateRange);
		}
		
		internal void HandleSkillLockChange(GameConn c) {
			//TODO? should we call some triggers or something? :)
			packetLenUsed = DecodeShort(1);
			short skillId = DecodeShort(3);
			//ISkill[] skills = c.CurCharacter.Skills;
			//if ((skillId >= 0) && (skillId < skills.Length)) {
			//	ISkill skill = skills[skillId];
			if((skillId >= 0) && (skillId < AbstractSkillDef.SkillsCount)) {
				c.CurCharacter.SetSkillLockType(skillId, (SkillLockType) packet[5]);					
				//ISkill skill = c.CurCharacter.GetSkillObject(skillId);
				//if (skill != null) {	
				//    skill.Lock = (SkillLockType) packet[5];
				//    Logger.WriteDebug("SkillLock of skill " + skill + " changed to " + skill.Lock);
				//} else {
				//    Logger.WriteDebug("SkillLock of skill change not performed, skill with id " + skillId + " is missing");
				//}
			} else {
				Logger.WriteDebug("SkillLock of skillid "+skillId+" requested - out of range");
			}
			
		}
		
		internal void HandleRequestStatus(GameConn c) {
			//OutputPacketLog(10);
			packetLenUsed=10;
			byte getType = packet[5];
			int decodedUid = DecodeInt(6);
			if (getType!=0x04 && getType!=0x05) {
				if (getType==0) {
					Logger.WriteDebug("Verdata request for "+decodedUid);
				} else {
					Logger.WriteWarning("Unknown getType="+getType.ToString("x")+" in HandleRequestStatus.");
					OutputPacketLog(packetLenUsed);
				}
				return;
			}
			AbstractCharacter cre = Thing.UidGetCharacter(Thing.UidClearFlags(decodedUid));
			if (cre != null) {
				switch (getType) {
					case 0x04: {
						Logger.WriteDebug("Basic stats.");
						cre.ShowStatusBarTo(c);
						return;
					} case 0x05: {
						Logger.WriteDebug("Request skills.");
						//(TODO): Send skills information.
						//Skills.SendSkills(c, Thing.UidGetCharacter(uid));
						cre.ShowSkillsTo(c);
						return;
					} default: {
						Server.SendSystemMessage(c, "Unknown getType="+getType.ToString("x")+" in HandleRequestStatus.", 0);
						return;
					}
				}
			} else {
				PacketSender.PrepareRemoveFromView(decodedUid);
				PacketSender.SendTo(c, true);
			}
			
		}

		internal void HandleEquipItem(GameConn c) {
			packetLenUsed=10;
			int itemuid = DecodeInt(1);
			AbstractItem i = Thing.UidGetItem(Thing.UidClearFlags(itemuid));
			AbstractCharacter cre = c.CurCharacter;
			if (i != null && cre.HasPickedUp(i)) {
				int contuid = DecodeInt(6);
				AbstractCharacter contChar = Thing.UidGetCharacter(contuid);
				if (contChar == null) {
					contChar = cre;
				}
				DenyResult result = cre.TryEquipItemOnChar(contChar);

				if (result != DenyResult.Allow && cre.HasPickedUp(i)) {
					Server.SendDenyResultMessage(c, contChar, result);
					cre.TryGetRidOfDraggedItem();
				}
			} else {
				PacketSender.PrepareRemoveFromView(itemuid);
				PacketSender.SendTo(c, true);
			}
		}
		
		internal void HandleDropItem(GameConn c) {
			packetLenUsed=14;
			int itemuid = DecodeInt(1);
			//always an item, may or may not have the item-flag.
			AbstractItem i = Thing.UidGetItem(Thing.UidClearFlags(itemuid));
			ushort x = DecodeUShort(5);
			ushort y = DecodeUShort(7);
			sbyte z = DecodeSByte(9);
			AbstractCharacter cre = c.CurCharacter;
			if (i != null && cre.HasPickedUp(i)) {
				int contuid = DecodeInt(10);

				DenyResult result;
				if (contuid == -1) {//dropping on ground
				    result = cre.TryPutItemOnGround(x, y, z);
				} else {
				    Thing co = Thing.UidGetThing(contuid);
				    if (co != null) {
						//Console.WriteLine("HandleDropItem: x = "+x+", y = "+y+", z = "+z+", cont = "+co);

						AbstractItem coAsItem = co as AbstractItem;
						if (coAsItem != null) {
							if (coAsItem.IsContainer && (x != 0xFFFF) && (y != 0xFFFF)) {
								//client put it to some coords inside container
								result = cre.TryPutItemInItem(coAsItem, x, y, false);
							} else {
								//client put it on some other item. The client probably thinks the other item is either a container or that they can be stacked. We'll see ;)
								result = cre.TryPutItemOnItem(coAsItem);//we ignore the x y
							}
						} else {
							result = cre.TryPutItemOnChar((AbstractCharacter) co);
						}					
				    } else {
						PacketSender.PrepareRemoveFromView(contuid);
						PacketSender.SendTo(c, true);
						result = DenyResult.Deny_ThatIsOutOfSight;
					}
				}

				if (result != DenyResult.Allow) {
					Server.SendDenyResultMessage(c, i, result);
					cre.TryGetRidOfDraggedItem();
				}
			} else {
				PacketSender.PrepareRemoveFromView(itemuid);
				PacketSender.SendTo(c, true);
			}
			
		}
		
		
		internal void HandlePickupItem(GameConn c) {
			int uid = DecodeInt(1);
			ushort amt = DecodeUShort(5);
			packetLenUsed=7;
			//always an item, may or may not have the item-flag.
			uid = Thing.UidClearFlags(uid);
			AbstractItem item = Thing.UidGetItem(uid);
			if (item != null) {
				AbstractCharacter cre = c.CurCharacter;
				DenyResult result = cre.TryPickupItem(item, amt);
				if (result != DenyResult.Allow) {
					if (result < DenyResult.Allow) {
						Prepared.SendPickupFailed(c, result);
					} else {
						Server.SendDenyResultMessage(c, item, result);
						Prepared.SendPickupFailed(c, DenyResult.Deny_NoMessage);
					}
				}
			} else {
				PacketSender.PrepareRemoveFromView(uid);
				PacketSender.SendTo(c, true);
			}
		}
		
		internal void HandleSingleClick(GameConn c) {
			int uid = DecodeInt(1);
			uid=Thing.UidClearFlags(uid);
			Thing thing = Thing.UidGetThing(uid);
			if (thing!=null) {
				if (Globals.aos && c.Version.aosToolTips) {
					thing.Trigger_AosClick(c.CurCharacter);
				} else {
					thing.Trigger_Click(c.CurCharacter);
				}
			}
			packetLenUsed=5;
		}

		internal void HandleDoubleClick(GameConn c) {
			uint flaggedUid = DecodeUInt(1);
			int uid = (int) flaggedUid;
			bool paperdollFlag = ((flaggedUid&0x80000000)==0x80000000);
			Logger.WriteDebug("uid is "+uid.ToString("x"));
			Thing t;
			if (uid==0x3cfff) {
				t = c.CurCharacter;
			} else {
				t = Thing.UidGetThing(Thing.UidClearFlags(uid));
			}
			AbstractCharacter curChar = c.CurCharacter;
			if ((t == null) || (!curChar.CanSeeForUpdate(t)))  {
				PacketSender.PrepareRemoveFromView(uid);
				PacketSender.SendTo(c, true);
			} else if (t.IsItem) {
				t.Trigger_DClick(curChar);
			} else {
				if (paperdollFlag) {
					((AbstractCharacter) t).ShowPaperdollTo(c);
				} else {
					t.Trigger_DClick(c.CurCharacter);
				}
			}
			//else invalid uid
			packetLenUsed=5;
		}
		
		internal void HandleResyncMoveRequest(GameConn c) {
			Logger.WriteInfo(MovementTracingOn, "Resync move request data("+DecodeUShort(1)+") (we have "+c.moveSeqNum+") TODO: Re-enable kick code.");
			//TODO: Possibly uncomment this later, after figuring out how the response to move-resync is supposed to be done.
//			if (c.RequestedResync) {
//				Server.DisconnectAndLog(c, "Requesting move resync (By sending 0x22) more than once.");
//			} else {
//				Server.ResyncPlayer(c);
//				c.RequestedResync=true;
//			}
			packetLenUsed=3;
		}
		
		
		internal void HandleGumpResponse(GameConn c) {
			packetLenUsed = DecodeShort(1);
			//uint player = DecodeInt(3); //we dont need this, the gumpinstance does remember it
			uint gumpUid = DecodeUInt(7);
			Gump gi = c.PopGump(gumpUid);
			if (gi != null) {
				uint buttonId = DecodeUInt(11);
				uint switchesCount = DecodeUInt(15);
				int position = 19;
				uint[] selectedSwitches = new uint[switchesCount];
				for (int i = 0; i<switchesCount; i++) {
					selectedSwitches[i] = DecodeUInt(position);
					position += 4;
				}
				uint entriesCount = DecodeUInt(position);
				position += 4;
				ResponseText[] responseTexts = new ResponseText[entriesCount];
				for (int i = 0; i<entriesCount; i++) {
					ushort id = DecodeUShort(position);
					position += 2;
					int len = DecodeUShort(position)*2;
					position += 2;
					string text = DecodeUnicodeString(position, len);
					position += len;
					responseTexts[i] = new ResponseText(id, text);
				}
				int n = (gi.numEntryIDs != null) ? gi.numEntryIDs.Count : 0;
				ResponseNumber[] responseNumbers = new ResponseNumber[n];
				for (int i = 0; i<n; i++) {
					foreach (ResponseText rt in responseTexts) {
						if (gi.numEntryIDs[i] == rt.id) {
							double number;
							if (ConvertTools.TryParseDouble(rt.text, out number)) {
								responseNumbers[i] = new ResponseNumber(rt.id, number);
							} else {
								c.WriteLine("'"+rt.text+" is not a number!");
								SendGumpBack(c, gi, responseTexts);
								return;
							}
							break;
						}
					}
					//we could fill the possible gap here... or not. The clients should expect nulls in the array.
				}
                gi.OnResponse(buttonId, selectedSwitches, responseTexts, responseNumbers);                
			} else {
				c.WriteLine("Unknown gump");
				Logger.WriteWarning(c+": unresolved gump (uid "+LogStr.Number(gumpUid)+")");
			}
		}

		private void SendGumpBack(GameConn c, Gump gi, ResponseText[] responseTexts) {
			//first we copy the responsetext into the default texts for the textentries so that they don't change.
			foreach (ResponseText rt in responseTexts) {
				int defaultTextId;
				if (gi.entryTextIds.TryGetValue((int) rt.id, out defaultTextId)) {
					if (defaultTextId < gi.textsList.Count) {//one can never be too sure
						gi.textsList[defaultTextId] = rt.text;
					}
				}
			}
			//and then we send the gump back again
			c.SentGump(gi);
			PacketSender.PrepareGump(gi);
			PacketSender.SendTo(c, true);
		}
		
		internal void HandleMoveRequest(GameConn c) {
			byte dir = packet[1];
			byte odir = dir;
			byte seqNum = packet[2];
			if (packetLen==3) {
				packetLenUsed=3;
			} else {
				packetLenUsed=7;
			}
			Logger.WriteInfo(MovementTracingOn, "container move("+seqNum+") dir("+dir+") (we have "+c.moveSeqNum+")");
			if (c.reqMoveSeqNum==seqNum) {
				if ((dir&0x80)==0x80) {
					//running
					dir-=0x80;
				}
				if (dir>=0 && dir<8) {
					//valid dir
					//AbstractCharacter cre = c.curCharacter;
					//if (Globals.fastWalkPackets) {
					//	bool badFastwalk = c.FastWalk.Check(DecodeInt(3));
					//	if (badFastwalk) {
					//		bool allowMove=(bool) cre.TryTrigger(TriggerKey.fastWalk, new object[1] {c});
					//		if (!allowMove) {
					//			Server._out.SendBadMovePacket(c);
					//			return;
					//		}
					//	}
					//	Server._out.SendAddFastwalkKeyPacket(c);
					//}
					if (c.reqMoveSeqNum==255) {
						Logger.WriteInfo(MovementTracingOn, "reqMoveSeqNum wraps around to 1.");
						c.reqMoveSeqNum=1;
					} else {
						Logger.WriteInfo(MovementTracingOn, "reqMoveSeqNum gets increased.");
						c.reqMoveSeqNum++;
					}
					c.MovementRequest(odir);

				} else {
					//invalid dir
					c.Close("Invalid direction byte in MoveRequest packet");
				}
			} else {
				Logger.WriteError("Invalid seqNum "+LogStr.Number(seqNum)+", expecting "+LogStr.Ident(c.reqMoveSeqNum));
				PacketSender.SendBadMove(c);
			}
		}
		
		internal void HelpRequest(GameConn c) {
			packetLenUsed=258;
			c.WriteLine("Sorry, there isn't a help menu yet.");
			//(TODO): Do something with this, like calling a scripted function or trigger on the GameConn requesting help.
		}
		
		/*
		internal void HandleMoveRequest(GameConn c) {
			if(FirstMoveRequest) {
				FirstMoveRequest = false;
				Server.MoveRequestTimer.Enabled = true;
			}

			byte dir = packet[1];
			byte seqNum = packet[2];
			int key = DecodeInt(3);

			if (packetLen==3) {
				packetLenUsed=3;
			} else {
				packetLenUsed=7;
			}

			c.FastWalk.PassedOnTimer = false;
			Server.MoveRequests.Add(new MoveRequestInfo(c, dir, seqNum, key));
		}*/
	
		internal void HandleSpeech(GameConn c) {
			short blockSize = DecodeShort(1);
			byte type = packet[3];
			ushort color = DecodeUShort(4);
			ushort font = DecodeUShort(6);
			string speech = "";
			int speechlen=blockSize-8;
			if (blockSize>packetLen || speechlen>255) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Invalid speech packet.");
				return;
			} else {
				packetLenUsed=blockSize;
			}
			try {
				speech = Encoding.UTF8.GetString(packet,8,speechlen);
			} catch (ArgumentOutOfRangeException) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Invalid speech packet.");
				return;
			}
			if (speech.IndexOf((char)0)>-1) {
				speech = speech.Substring(0,speech.IndexOf((char)0));
			}
			speech.Replace('\n',' ');
			speech.Replace('\r',' ');
			speech.Replace('\t',' ');
			if (speech.IndexOf(Globals.commandPrefix)==0) {
				Commands.PlayerCommand(c, speech.Substring(Globals.commandPrefix.Length));
			} else if (speech.IndexOf(Globals.alternateCommandPrefix)==0) {
				Commands.PlayerCommand(c, speech.Substring(Globals.alternateCommandPrefix.Length));
			} else {
				//actually speak it
				font=3;	//We don't want them speaking in runic or anything.
				AbstractCharacter cre = c.CurCharacter;
				cre.Speech(speech, 0, (SpeechType) type, color, font, null, null);
				
			}
			
		}
		
		internal void HandleUO3DAction(GameConn c) {
			//todo

			//AbstractCharacter cre=c.curCharacter;
			//int action=DecodeInt(5);
			//Server._out.SendUO3DAction(cre, action);
			packetLenUsed+=4;
		}
		
		internal void HandleCloseStatusGump(GameConn c) {

			//todo: use for parties
			packetLenUsed+=4;
		}

		//0xbf 0x10
		internal void HandleRequestProperties(GameConn c) {
			//Console.WriteLine("HandleRequestProperties packet recieved.");
			//OutputPacketLog(packetLen);
			if (Globals.aos) {
				AbstractCharacter curChar = c.CurCharacter;
				if (curChar != null) {
					uint flaggedUid = DecodeUInt(5);
					Thing t = Thing.UidGetThing(Thing.UidClearFlags((int) flaggedUid));
					if ((t != null) && (!t.IsDeleted)) {
						ObjectPropertiesContainer iopc = t.GetProperties();
						if (iopc != null) {
							if (curChar.CanSeeForUpdate(t)) {
								iopc.SendDataPacket(c);
							}
						}
					}
				}
			}
			packetLenUsed=9;
		}

		//0xd6
		internal void HandleRequestMultipleProperties(GameConn c) {
			//Console.WriteLine("HandleRequestMultipleProperties packet recieved.");
			short blockSize = DecodeShort(1);
			if (blockSize>packetLen) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Invalid blockSize="+blockSize+" is greater than packetLen="+packetLen);
				return;
			}
			if (Globals.aos) {
				AbstractCharacter curChar = c.CurCharacter;
				if (curChar != null) {
					int dataBlockSize = blockSize - 3;
					if ((dataBlockSize >= 0) && ((dataBlockSize % 4) == 0)) {
						int count = dataBlockSize / 4;
						for (int i = 0; i < count; i++) {
							uint flaggedUid = DecodeUInt(i*4+3);
							Thing t = Thing.UidGetThing(Thing.UidClearFlags((int) flaggedUid));
							if ((t != null) && (!t.IsDeleted)) {
								ObjectPropertiesContainer iopc = t.GetProperties();
								if (iopc != null) {
									if (curChar.CanSeeForUpdate(t)) {
										iopc.SendDataPacket(c);
									}
								}
							}
						}
					}
				}
			}
			packetLenUsed=blockSize;
		}
		
		internal void HandleClientVersion(GameConn c) {
			short blockSize = DecodeShort(1);
			if (blockSize>packetLen || blockSize>0x1f) {	//27 characters. "4.0.0p" is 6 characters.
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Invalid blockSize="+blockSize+" is greater than packetLen="+packetLen);
				return;
			}
			string ver = new string(Encoding.UTF8.GetChars(packet,3,blockSize-3));
			if (ver.IndexOf((char)0)>-1) {
				ver=ver.Substring(0,ver.IndexOf((char)0));
			}
			c.clientVersion = ClientVersion.Get(ver);
			AbstractCharacter curChar = c.CurCharacter;
			//if (curChar != null) {
			//    curChar.Resync();
			//}
			packetLenUsed=blockSize;
		}
		
		internal void HandleLanguagePacket(GameConn c, ushort blocksize) {
			//This should work OK with both old and new clients now, and the God Client too, etc.
			blocksize-=5;
			if (blocksize>6) blocksize=6;	//4 is the highest I've actually seen. But let's do 6 for good measure.
			if (blocksize<3) blocksize=3;	//3 is the lowest I've actually seen.
			packetLenUsed+=blocksize;
			string lang = new string(Encoding.UTF8.GetChars(packet,5,blocksize));
			if (lang.IndexOf((char)0)>-1) {
				lang=lang.Substring(0,lang.IndexOf((char)0));
			}
			c.lang=lang;
		}
		
		internal void HandleScreenSizePacket(GameConn c) {
			packetLenUsed+=8;
			short one = DecodeShort(5);
			short two = DecodeShort(7);
			short three = DecodeShort(9);
			short four = DecodeShort(11);
			Logger.WriteDebug("one="+one+" two="+two+" three="+three+" four="+four);
		}

		private static Dictionary<int, bool> keywordsDict = new Dictionary<int, bool>();
		private static int[] emptyInts = new int[0];
		
		internal void HandleUnicodeSpeech(GameConn c) {
			short blockSize = DecodeShort(1);
			byte type = packet[3];
			ushort color = DecodeUShort(4);
			ushort font = DecodeUShort(6);
			string language = new string(Encoding.UTF8.GetChars(packet,8,4));
			if (language.IndexOf((char)0)>-1) {
				language=language.Substring(0,language.IndexOf((char)0));
			}
			string uncheckedSpeech = "";
			int speechlen=blockSize-12;
			if (blockSize>packetLen || speechlen>511) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Invalid speech packet : Illegal blocksize.");
				packetLenUsed=packetLen;
				return;
			} else {
				packetLenUsed=blockSize;
			}
			int start=12;
			int[] keywords = emptyInts;
			if ((type&0xc0)==0xc0) {
				int value = DecodeShort(12);
				int numKeywords = (value & 0xFFF0) >> 4;
				int hold = value & 0xF;

				start += 2;
				for (int i = 0; i < numKeywords; ++i) {
					int speechID;
					if ((i & 1) == 0) {
						hold <<= 8;
						hold |= DecodeByte(start);
						start++;
						speechID = hold;
						hold = 0;
					} else {
						value = DecodeShort(start);
						start += 2;
						speechID = (value & 0xFFF0) >> 4;
						hold = value & 0xF;
					}

					if (!keywordsDict.ContainsKey(speechID)) {
						keywordsDict[speechID] = true;
					}
				}

				int n = keywordsDict.Count;
				if (n > 0) {
					keywords = new int[n];
					keywordsDict.Keys.CopyTo(keywords, 0);
					keywordsDict.Clear();
				}

				speechlen = blockSize-start;
				type=(byte) (type&~0xc0);
				try {
					uncheckedSpeech = Encoding.UTF8.GetString(packet,start,speechlen);
				} catch (ArgumentOutOfRangeException) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
					c.Close("Invalid speech packet : Illegal speechlen.");
					return;
				}
				
			} else {
				start=12;
				try {
					uncheckedSpeech = DecodeUnicodeString(start, speechlen);
				} catch (ArgumentOutOfRangeException) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
					c.Close("Invalid speech packet : Illegal speechlen.");
					return;
				}
			}
			if (uncheckedSpeech.IndexOf((char)0)>-1) {
				uncheckedSpeech = uncheckedSpeech.Substring(0,uncheckedSpeech.IndexOf((char)0));
			}
			string speech="";
			if (uncheckedSpeech.Length>0) {
				for (int i=0; i<uncheckedSpeech.Length; i++) {
					char lc=uncheckedSpeech[i];
					if (lc<' ' || (!Globals.supportUnicode && lc>'~')) {	//don't need special checks for \r, \n, \t, they're all < 32 (space).
						speech+=' ';
					} else {
						speech+=uncheckedSpeech[i];
					}
				}
			}
			
			if (speech.IndexOf(Globals.commandPrefix)==0) {
				Commands.PlayerCommand(c, speech.Substring(Globals.commandPrefix.Length));
			} else if (speech.IndexOf(Globals.alternateCommandPrefix)==0) {
				Commands.PlayerCommand(c, speech.Substring(Globals.alternateCommandPrefix.Length));
			} else {
				//actually speak it
				font=3;	//We don't want them speaking in runic or anything.
				AbstractCharacter cre = c.CurCharacter;
				cre.Speech(speech, 0, (SpeechType) type, color, font, null, null);
			}
		}
		
		internal void DeleteCharRequest(GameConn c) {
			packetLenUsed=39;
				
			int charIndex = DecodeInt(31);
			//Console.WriteLine("CharIndex="+charIndex);
			if (charIndex<0 || charIndex>=AbstractAccount.maxCharactersPerGameAccount) {
				//Server.DisconnectAndLog(c, "Illegal char index "+charIndex);
				Packets.Prepared.SendRejectDeleteRequest(c, DeleteRequestReturnValue.Reject_NonexistantCharacter);
				return;
			}
			AbstractAccount acc = c.curAccount;
			DeleteRequestReturnValue drr = acc.RequestDeleteCharacter(charIndex);
			if (drr==DeleteRequestReturnValue.AcceptedRequest) {
				PacketSender.SendCharacterListAfterDelete(c);
			} else if (drr!=DeleteRequestReturnValue.RejectWithoutSendingAMessage) {
				Packets.Prepared.SendRejectDeleteRequest(c, drr);
			}
			
		}
		
		internal void HandleNameRequest(GameConn conn) {
			packetLenUsed=DecodeUShort(1);

			AbstractCharacter c = Thing.UidGetCharacter(DecodeInt(3));
			if (c != null) {
				PacketSender.PrepareCharacterName(c);
				PacketSender.SendTo(conn, true);
			}
		}
		
		internal void HandleStatLockChange(GameConn c) {
			byte stat = DecodeByte(1);
			byte lockValue = DecodeByte(2);

			if (lockValue > 2) {
				lockValue = 0;
			}
			AbstractCharacter ch = c.CurCharacter;
			switch (stat) {
				case 0: 
					ch.StrLock = (StatLockType)lockValue; 
					break;
				case 1: 
					ch.DexLock = (StatLockType)lockValue; 
					break;
				case 2: 
					ch.IntLock = (StatLockType)lockValue;
					break;
			}
		}

		internal void LoginChar(GameConn c) {
			packetLenUsed=73;
			//OutputPacketLog(packetLenUsed);
			int charIndex = DecodeInt(65);
			//Console.WriteLine("CharIndex="+charIndex);
			if (charIndex<0 || charIndex>AbstractAccount.maxCharactersPerGameAccount) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal char index "+charIndex);
				return;
			}
			AbstractAccount acc = c.curAccount;
			AbstractCharacter cre = c.LoginCharacter(charIndex);
			if (cre==null) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Login denied by scripts or no character in that slot.");
				return;
			}
			if (cre.Account==null || cre.Account!=acc) {
				Logger.WriteWarning("Invalid account - Fixing.");
				cre.MakeBePlayer(acc);
			}
			//Logger.WriteDebug("account = "+c.curAccount);
			//Logger.WriteDebug("curCharacter = "+c.curCharacter);
			
			Server.StartGame(c);
		}
		
		internal void GameServerLogin(GameConn c) {
			packetLenUsed=65;
			//OutputPacketLog(packetLenUsed);
			PacketSender.PrepareEnableFeatures(Globals.featuresFlags);
			PacketSender.SendTo(c, true);
			string username = new string(Encoding.UTF8.GetChars(packet, 5, 30));
			string password = new string(Encoding.UTF8.GetChars(packet, 35, 30));
			if (username.IndexOf((char)0)>-1) {
				username = username.Substring(0,username.IndexOf((char)0));
			}
			if (password.IndexOf((char)0)>-1) {
				password = password.Substring(0,password.IndexOf((char)0));
			}
			Logger.WriteDebug("Game Server Login.");
			AbstractAccount.HandleLoginAttempt(username, password, c);
		}
		
		/**
			LOS checks should be done by whatever requested the target.
		*/
		internal void HandleTarget(GameConn c) {
			packetLenUsed=19;
			if (c.curAccount!=null) {
				//We always send 0 now for targNum, since they can't have more than 1 targetting cursor anyways.
				
				byte targGround = packet[1];
				int uid = DecodeInt(7);
				ushort x = DecodeUShort(11);
				ushort y = DecodeUShort(13);
				//byte unk = packet[15];	//supposedly z is a short, but we just ignore the other byte.
				sbyte z = DecodeSByte(16);
				ushort dispId = DecodeUShort(17);
				c.HandleTarget(targGround, uid, x, y, z, dispId);
			}
		}
		
		internal void HandlePing(GameConn c) {
			c.restoringUpdateRange=false;
			packetLenUsed=2;
			//PacketSender.PreparePing(DecodeByte(1));
			//PacketSender.SendTo(c, true);
		}
		
		//at least part of this should be in scripts
		internal void CreateCharPacket(GameConn c) {
			AbstractAccount acc = c.curAccount;
			if (acc == null) {
				//IIRC, this shouldn't ever happen because of the code which only allows certain packets before login is complete. But we'll leave this check here anyways. -SL
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("CreateChar Packet without being logged in");
				return;
			}
			
			int charSlot = acc.GetBlankChar();
			if (charSlot==-1) {
				c.WriteLine("You already have the maximum number of characters in your account.");
				c.Close("Tried to create more chars than allowed");
				return;
			}
			
			//ignore bytes 0-9
			//byte 10 starts charname
			string charname = new string(Encoding.UTF8.GetChars(packet, 10, 30));
			//Logger.WriteDebug("charname initially ["+charname+"]");
			int indexOfNull = charname.IndexOf((char)0);
			//Logger.WriteDebug("charname indexOfNull ["+indexOfNull+"]");
			if (indexOfNull > (-1)) {
				charname=charname.Substring(0, indexOfNull);
			}
			Logger.WriteDebug("charname without nulls ["+charname+"]");
			for (int a=0; a<charname.Length; a++) {
				if (!(charname[a]==' ' || (charname[a]>='a' && charname[a]<='z') || (charname[a]>='A' && charname[a]<='Z'))) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
					c.Close("Illegal name - Contains something other than letters and spaces.");
					return;
				}
			}
			if (charname[0]==' ' || charname[charname.Length-1]==' ') {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal name - Starts or ends with spaces.");
				return;
			}
			if (charname.IndexOf("  ")>-1) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal name - More than one space in a row.");
				return;
			}
			Logger.WriteDebug("charname now ["+charname+"]");
			CreateCharArguments ccArgs = new CreateCharArguments();
			ccArgs.charname = charname;
			//string password = new string(utf.GetChars(packet, 39, 30));
			byte gender = packet[70];
			if (gender<0 || gender>1) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal gender="+gender);
				return;
			}
			ccArgs.gender = gender;
			
			ccArgs.startStr = packet[71];
			ccArgs.startDex = packet[72];
			ccArgs.startInt = packet[73];
			
			ushort skinColor = DecodeUShort(80);
			skinColor=(ushort) (skinColor&~0x8000);
			if (skinColor<0x3ea || skinColor>0x422) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal skinColor="+skinColor);
				return;
			}
			ccArgs.skinColor = skinColor;
			
			ushort hairStyle = DecodeUShort(82);
			if (hairStyle==0) {
			} else if (hairStyle<0x203B || hairStyle>0x204A || 
					   (hairStyle>0x203D && hairStyle<0x2044)) {
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal hairStyle="+hairStyle);
				return;
			}
			ccArgs.hairStyle = hairStyle;
			
			ushort hairColor = DecodeUShort(84);
			if (hairColor<0x44e || hairColor>0x4ad){
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal hairColor="+hairColor);
				return;
			}
			ccArgs.hairColor = hairColor;
			
			ushort facialHair = DecodeUShort(86);
			if (facialHair==0) {
			} else if (facialHair<0x203E || facialHair>0x204D || (facialHair>0x2041 && facialHair<0x204B)){
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal facialHair="+facialHair);
				return;
			}
			ccArgs.facialHair = facialHair;
			
			ushort facialHairColor = DecodeUShort(88);
			if (facialHairColor<0x44e || facialHairColor>0x4ad){
				Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
				c.Close("Illegal facialHairColor="+facialHairColor);
				return;
			}
			ccArgs.facialHairColor = facialHairColor;
			
			ccArgs.locationNum = (short) (packet[0x60]<<8+packet[0x61]);
			//short unknown = packet[0x62]<<8+packet[0x63];
			//short slot = packet[0x64]<<8+packet[0x65];

			ushort shirtColor = (ushort) Globals.dice.Next(0x2,0x3e9);
			ushort pantsColor = (ushort) Globals.dice.Next(0x2,0x3e9);
			if (packetLen>=104) {
				shirtColor = DecodeUShort(100);
				pantsColor = DecodeUShort(102);
				if (shirtColor<0x02 || shirtColor>0x3e9) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
					c.Close("Illegal shirtColor="+shirtColor);
					return;
				}
				if (pantsColor<0x02 || pantsColor>0x3e9) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.CommunicationsProblem);
					c.Close("Illegal pantsColor="+pantsColor);
					return;
				}
				packetLenUsed=104;
			} else {
				packetLenUsed=packetLen;
			}
			ccArgs.shirtColor = shirtColor;
			ccArgs.pantsColor = pantsColor;
			
			ScriptArgs sa = new ScriptArgs(ccArgs);
			c.TryTrigger(TriggerKey.newPC, sa);//creates the character
			
			AbstractCharacter cre = acc.GetCharacterInSlot(charSlot);
			if (cre == null) {
				c.Close("The @newPC trigger on "+LogStr.Ident(c)+" failed to create a new character.");
				return;
			}
			Globals.SetSrc(cre);
			
			Logger.WriteDebug("Logging in character "+cre);
			AbstractCharacter cre2 = c.LoginCharacter(charSlot);
			Sanity.IfTrueThrow(cre2==null || cre2!=cre, "LoginCharacter failed, but we created the character right before calling it. wtf??");
			//Logger.WriteDebug("account = "+c.curAccount);
			//Logger.WriteDebug("curCharacter = "+c.curCharacter);
			
			Logger.WriteDebug("Starting game for character "+cre);
			Server.StartGame(c);
		}
	}
	
	public class CreateCharArguments {
		public string charname;
		
		public byte gender;
		
		public byte startStr;
		public byte startDex;
		public byte startInt;
			
		public ushort skinColor;
		public ushort hairStyle;
		public ushort hairColor;
		public ushort facialHair;
		public ushort facialHairColor;
			
		public ushort shirtColor;
		public ushort pantsColor;
		
		public short locationNum;
		
		//todo: skills
		
	}

	internal class MoveRequestInfo {
		internal MoveRequestInfo(GameConn _c, byte _dir, byte _seqNum, int _FastWalkKey) {
			c = _c;
			dir = _dir;
			seqNum = _seqNum;
			FastWalkKey = _FastWalkKey;
		}

		internal GameConn c;
		internal byte dir, seqNum;
		internal int FastWalkKey;
	}

}