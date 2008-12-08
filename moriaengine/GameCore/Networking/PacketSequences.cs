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
	public static class PacketSequences {

		internal static void SendStartGameSequence(TCPConnection<GameState> conn, GameState state, AbstractAccount acc, AbstractCharacter ch) {
			Logger.WriteDebug("Starting game for character " + ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();

			pg.AcquirePacket<LoginConfirmationOutPacket>().Prepare(ch, Regions.Map.GetMapSizeX(0), Regions.Map.GetMapSizeY(0)); //0x1B

			byte facet = ch.GetMap().Facet;
			if (facet != 0) {
				pg.AcquirePacket<SetFacetOutPacket>().Prepare(facet);//0xBF 0x08
			}
			//TODO? 0xBF 0x04

			pg.AcquirePacket<SeasonalInformationOutPacket>().Prepare(ch.Season, ch.Cursor); //0xBC
			pg.AcquirePacket<EnableLockedClientFeaturesOutPacket>().Prepare(Globals.featuresFlags); //0xB9
			pg.AcquirePacket<StatusBarInfoOutPacket>().Prepare(ch, StatusBarType.Me); //0x11
			pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20

			pg.AcquirePacket<LoginCompleteOutPacket>();
			//TODO? 0x5B - current time

			pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20

			conn.SendPacketGroup(pg);

			PreparedPacketGroups.SendWarMode(conn, ch.Flag_WarMode);

			//state.WriteLine("Welcome to " + Globals.serverName);
			new DelayedResyncTimer(ch).DueInSeconds = 2;
		}

		internal class DelayedResyncTimer : SteamEngine.Timers.Timer {
			AbstractCharacter ch;

			internal DelayedResyncTimer(AbstractCharacter ch) {
				this.ch = ch;
			}

			protected sealed override void OnTimeout() {
				ch.Resync();
			}
		}

		public static void SendCharInfoWithPropertiesTo(AbstractCharacter viewer, GameState viewerState, 
			TCPConnection<GameState> viewerConn, AbstractCharacter target) {

			DrawObjectOutPacket packet = Pool<DrawObjectOutPacket>.Acquire();
			packet.Prepare(target, target.GetHighlightColorFor(viewer));
			viewerConn.SendSinglePacket(packet);

			TrySendPropertiesTo(viewerState, viewerConn, target);
		}

		public static void SendContainerContentsWithPropertiesTo(AbstractCharacter viewer, GameState viewerState,
			TCPConnection<GameState> viewerConn, AbstractItem container) {

			if (container.Count > 0) {
				ItemsInContainerOutPacket iicp = Pool<ItemsInContainerOutPacket>.Acquire();

				using (ListBuffer<AbstractItem> listBuffer = Pool<ListBuffer<AbstractItem>>.Acquire()) {
					if (iicp.PrepareContainer(container, viewer, listBuffer.list)) {
						viewerConn.SendSinglePacket(iicp);
						if (Globals.aos && viewerState.Version.aosToolTips) {
							foreach (AbstractItem contained in listBuffer.list) {
								ObjectPropertiesContainer containedOpc = contained.GetProperties();
								if (containedOpc != null) {
									containedOpc.SendIdPacket(viewerState, viewerConn);
								}
							}
						}
					} else {
						iicp.Dispose();
					}
				}
			}
		}

		public static void TrySendPropertiesTo(GameState viewerState, TCPConnection<GameState> viewerConn, Thing target) {
			if (Globals.aos && viewerState.Version.aosToolTips) {
				ObjectPropertiesContainer iopc = target.GetProperties();
				if (iopc != null) {
					iopc.SendIdPacket(viewerState, viewerConn);
				}
			}
		}

		public static void SendRemoveFromView(TCPConnection<GameState> conn, uint flaggedUid) {
			DeleteObjectOutPacket packet = Pool<DeleteObjectOutPacket>.Acquire();
			packet.Prepare(flaggedUid);
			conn.SendSinglePacket(packet);
		}

		public static void SendRemoveFromView(TCPConnection<GameState> conn, int flaggedUid) {
			DeleteObjectOutPacket packet = Pool<DeleteObjectOutPacket>.Acquire();
			packet.Prepare(flaggedUid);
			conn.SendSinglePacket(packet);
		}

		#region messages
		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendSystemMessage(TCPConnection<GameState> c, string msg, int color) {
			if (c != null) {
				InternalSendMessage(c, null, msg, "System", SpeechType.Speech, 3, color);
			}
		}

		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - The cliloc # to send.
				color - The color of the message.
				args - Additional arguments needed for the cliloc message, if any.
		*/
		public static void SendClilocSysMessage(TCPConnection<GameState> c, uint msg, int color, string args) {
			if (c != null) {
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(null, msg, "System", SpeechType.Speech, 3, color, args);
				c.SendSinglePacket(packet);
			}
		}

		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - The cliloc # to send.
				color - The color of the message.
				args - Additional arguments needed for the cliloc message, if any.
		*/
		public static void SendClilocSysMessage(TCPConnection<GameState> c, uint msg, int color, params string[] args) {
			if (c != null) {
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(null, msg, "System", SpeechType.Speech, 3, color, string.Join("\t", args));
				c.SendSinglePacket(packet);
			}
		}

		/* 
			Method: SendOverheadMessage
				Sends an overhead message to a particular game-connection. The message
				will appear above the head of that connection's currently logged in character.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessage(TCPConnection<GameState> c, string msg, int color) {
			if (c != null) {
				AbstractCharacter cre = c.State.CharacterNotNull;
				InternalSendMessage(c, cre, msg, "System", SpeechType.Speech, 3, color);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send.
				color - The color of the message.
		*/

		public static void SendNameFrom(TCPConnection<GameState> c, Thing from, string msg, int color) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				InternalSendMessage(c, from, msg, "", SpeechType.Name, 3, color);
			}
		}


		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/

		public static void SendClilocNameFrom(TCPConnection<GameState> c, Thing from, uint msg, int color, string args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "", SpeechType.Name, 3, color, args);
				c.SendSinglePacket(packet);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/

		public static void SendClilocNameFrom(TCPConnection<GameState> c, Thing from, uint msg, int color, string arg1, string arg2) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "", SpeechType.Name, 3, color, string.Concat(arg1, "\t", arg2));
				c.SendSinglePacket(packet);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/

		public static void SendClilocNameFrom(TCPConnection<GameState> c, Thing from, uint msg, int color, params string[] args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "", SpeechType.Name, 3, color, string.Join("\t", args));
				c.SendSinglePacket(packet);
			}
		}

		public static void SendClilocMessageFrom(TCPConnection<GameState> c, Thing from, uint msg, int color, string args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "System", SpeechType.Speech, 3, color, args);
				c.SendSinglePacket(packet);
			}
		}
		public static void SendClilocMessageFrom(TCPConnection<GameState> c, Thing from, uint msg, int color, string arg1, string arg2) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "System", SpeechType.Speech, 3, color, string.Concat(arg1, "\t", arg2));
				c.SendSinglePacket(packet);
			}
		}

		public static void SendClilocMessageFrom(TCPConnection<GameState> c, Thing from, uint msg, int color, params string[] args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "System", SpeechType.Speech, 3, color, string.Join("\t", args));
				c.SendSinglePacket(packet);
			}
		}

		/*
			Method: SendOverheadMessageFrom
				Displays a message above a particular thing, but only to one game-connection. This works
				like speech, but is only sent to the chosen connection, so only that player will see it.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing the message will appear above.
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessageFrom(TCPConnection<GameState> c, Thing from, string msg, int color) {
			if (c != null) {
				//if (from == null) 
				//if (from is AbstractCharacter) {
				//    SendMessage(c, from, from.Model, from.Name, msg, SpeechType.Speech, 3, color);
				//} else if (from is AbstractItem) {
				//    SendMessage(c, from, 0, from.Name, msg, SpeechType.Speech, 3, color);
				//}

				InternalSendMessage(c, from, msg, from.Name, SpeechType.Speech, 3, color);
			}
		}
		/*
			Method: SendOverheadMessageFrom
				Displays a message above a particular static, but only to one game-connection. This works
				like speech, but is only sent to the chosen connection, so only that player will see it.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The static the message will appear above.
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessageFrom(TCPConnection<GameState> c, Static from, string msg, int color) {
			if (c != null) {
				//if (from == null) throw new ArgumentNullException("from cannot be null in SendOverheadMessageFrom.");

				InternalSendMessage(c, null, msg, from.Name, SpeechType.Speech, 3, color);
			}
		}


		/*
			Method: SendOverheadServerMessage
				Displays a server message above the client's head, but only to that client.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadServerMessage(TCPConnection<GameState> c, string msg, int color) {
			if (c != null) {
				AbstractCharacter cre = c.State.CharacterNotNull;
				InternalSendMessage(c, cre, msg, "System", SpeechType.Server, 0, color);
			}
		}

		/*
			Method: SendServerMessage
				Displays a server message, but only to that client.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendServerMessage(TCPConnection<GameState> c, string msg, int color) {
			if (c != null) {
				InternalSendMessage(c, null, msg, "System", SpeechType.Server, 0, color);
			}
		}

		public static void SendDenyResultMessage(TCPConnection<GameState> c, Thing t, DenyResult denyResult) {
			switch (denyResult) {
				case DenyResult.Deny_RemoveFromView:
					if ((t != null) && (!t.IsDeleted)) {
						DeleteObjectOutPacket packet = Pool<DeleteObjectOutPacket>.Acquire();
						packet.Prepare(t);
						c.SendSinglePacket(packet);
					}
					break;
				case DenyResult.Deny_ThatDoesNotBelongToYou:
					SendClilocSysMessage(c, 500364, 0);		//You can't use that, it belongs to someone else.
					break;
				case DenyResult.Deny_ThatIsOutOfSight:
					SendClilocSysMessage(c, 3000269, 0);	//That is out of sight.
					break;
				case DenyResult.Deny_ThatIsTooFarAway:
					SendClilocSysMessage(c, 3000268, 0);	//That is too far away.
					break;
				case DenyResult.Deny_YouAreAlreadyHoldingAnItem:
					SendClilocSysMessage(c, 3000271, 0);	//You are already holding an item.
					break;
				case DenyResult.Deny_YouCannotPickThatUp:
					SendClilocSysMessage(c, 3000267, 0);	//You cannot pick that up.
					break;
				case DenyResult.Deny_ThatIsLocked:
					SendClilocSysMessage(c, 501283, 0);		//That is locked.
					break;
				case DenyResult.Deny_ContainerClosed:
					SendClilocSysMessage(c, 500209, 0);		//You cannot peek into the container.
					//SendSystemMessage(c, "Tento kontejner není otevøený.", 0);
					break;
				//case TryReachResult.Failed_NoMessage:
				//case TryReachResult.Succeeded:
				//default:
			}
		}

		/*
			Method: BroadCast
				Broadcast a message to all clients (as a server message).
			Parameters:
				msg - What to send.
		*/
		internal static void BroadCast(string msg) {
			Console.WriteLine("Broadcasting: " + msg);
			foreach (GameState state in GameServer.AllClients) {
				SendServerMessage(state.Conn, msg, -1);
			}
		}

		internal static void InternalSendMessage(TCPConnection<GameState> c, Thing from, string msg, string sourceName, SpeechType type, ushort font, int color) {
			InternalSendMessage(c, from, msg, sourceName, type, font, color, c.State.Language);
		}

		//For use by Server's various message sending methods (Which send to one client).
		internal static void InternalSendMessage(TCPConnection<GameState> c, Thing from, string msg, string sourceName, SpeechType type, ushort font, int color, string lang) {
			if (Globals.supportUnicode && font == 3 && !(type == SpeechType.Name && Globals.asciiForNames)) {	//if it's another font, send it as ASCII
				UnicodeSpeechOutPacket packet = Pool<UnicodeSpeechOutPacket>.Acquire();
				if (string.IsNullOrEmpty(lang)) {
					lang = c.State.Language;
				}
				packet.Prepare(from, msg, sourceName, type, font, color, lang);
				c.SendSinglePacket(packet);
			} else {
				SendSpeechOutPacket packet = Pool<SendSpeechOutPacket>.Acquire();
				packet.Prepare(from, msg, sourceName, type, font, color);
				c.SendSinglePacket(packet);
			}
		}

		internal static GameOutgoingPacket PrepareMessagePacket(Thing from, string msg, string sourceName, SpeechType type, ushort font, int color, string lang) {
			if (Globals.supportUnicode && font == 3 && !(type == SpeechType.Name && Globals.asciiForNames)) {	//if it's another font, send it as ASCII
				UnicodeSpeechOutPacket packet = Pool<UnicodeSpeechOutPacket>.Acquire();
				packet.Prepare(from, msg, sourceName, type, font, color, lang);
				return packet;
			} else {
				SendSpeechOutPacket packet = Pool<SendSpeechOutPacket>.Acquire();
				packet.Prepare(from, msg, sourceName, type, font, color);
				return packet;
			}
		}

		#endregion messages

		public static void SendStatLocks(AbstractCharacter ch) {
			GameState state = ch.GameState;
			if (state != null) {
				ExtendedStatsOutPacket p = Pool<ExtendedStatsOutPacket>.Acquire();
				p.Prepare(ch.FlaggedUid, ch.StatLockByte);
				state.Conn.SendSinglePacket(p);
			}
		}
	}
}