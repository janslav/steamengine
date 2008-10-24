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
	public abstract class GameIncomingPacket : IncomingPacket<TCPConnection<GameState>, GameState, IPEndPoint> {

	}

	public abstract class DynamicLenInPacket : GameIncomingPacket {
		protected override sealed ReadPacketResult Read() {
			int blockSize = this.DecodeShort();
			if ((blockSize - 1) > this.LengthIn) {//-1 because "start" is after the first byte
				return ReadPacketResult.NeedMoreData;
			}
			ReadPacketResult retVal = this.ReadDynamicPart(blockSize);
			this.SeekFromStart(blockSize - 1); //-1 because "start" is after the first byte
			return retVal;
		}

		protected abstract ReadPacketResult ReadDynamicPart(int blockSize);
	}

	public sealed class  GeneralInformationInPacket : DynamicLenInPacket {
		SubPacket subPacket;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			int subCmd = this.DecodeShort();

			switch (subCmd) {
				case 0x05:
					this.subPacket = Pool<ScreenSizeSubPacket>.Acquire();
					break;
				case 0x0b:
					this.subPacket = Pool<SetLanguageSubPacket>.Acquire();
					break;
				default:
					Logger.WriteDebug("Unknown packet 0xbf - subpacket 0x" + subCmd.ToString("x") + " (len " + blockSize + ")");
					return ReadPacketResult.DiscardSingle;
			}

			Logger.WriteDebug("Handling packet 0xbf - subpacket 0x" + subCmd.ToString("x") + " (len " + blockSize + ")");
			return this.subPacket.ReadSubPacket(this, blockSize);
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			this.subPacket.Handle(this, conn, state);
		}

		public abstract class SubPacket : Poolable {
			internal protected abstract ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize);
			internal protected abstract void Handle(GeneralInformationInPacket packet, TCPConnection<GameState> conn, GameState state);
		}

		public sealed class ScreenSizeSubPacket : SubPacket {
			short x, y;

			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				packet.SeekFromCurrent(2);
				this.x = packet.DecodeShort();
				this.y = packet.DecodeShort();
				packet.SeekFromCurrent(2);
#if DEBUG
				return ReadPacketResult.Success;
#else
				return ReadPacketResult.DiscardSingle; //why should we care about screen resolution, right?
#endif
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TCPConnection<GameState> conn, GameState state) {
				Logger.WriteDebug(state + " reports screen resolution " + this.x + "x" + this.y);
			}
		}


		public sealed class SetLanguageSubPacket : SubPacket {
			string language;

			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				this.language = packet.DecodeAsciiString(4);
				return ReadPacketResult.Success;
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TCPConnection<GameState> conn, GameState state) {
				state.Language = this.language;
			}
		}

	}

	public sealed class GameServerLoginInPacket : GameIncomingPacket {
		string accName, pass;

		protected override ReadPacketResult Read() {
			this.SeekFromStart(4);
			this.accName = this.DecodeAsciiString(30);
			this.pass = this.DecodeAsciiString(30);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractAccount acc;
			LoginAttemptResult result = AbstractAccount.HandleLoginAttempt(this.accName, this.pass, state, out acc);

			switch (result) {
				case LoginAttemptResult.Success:
					Console.WriteLine(LogStr.Ident(state) + " logged in.");

					PacketGroup pg = PacketGroup.AcquireSingleUsePG();
					pg.AcquirePacket<EnableLockedClientFeaturesOutPacket>().Prepare(Globals.featuresFlags);
					pg.AcquirePacket<CharactersListOutPacket>().Prepare(acc, Globals.loginFlags);
					conn.SendPacketGroup(pg);

					break;
				case LoginAttemptResult.Failed_AlreadyOnline:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.SomeoneIsAlreadyUsingThisAccount);
					conn.Close("Account '" + acc + "' already online.");
					break;
				case LoginAttemptResult.Failed_BadPassword:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.InvalidAccountCredentials);
					conn.Close("Bad password for account '" + acc + "'");
					break;
				case LoginAttemptResult.Failed_Blocked:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.Blocked);
					conn.Close("Account '" + acc + "' blocked.");
					break;
				case LoginAttemptResult.Failed_NoSuchAccount:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.NoAccount);
					conn.Close("No account '" + this.accName + "'");
					break;
			}
		}
	}

	public sealed class LoginCharacterInPacket : GameIncomingPacket {
		int charIndex;

		protected override ReadPacketResult Read() {
			this.SeekFromStart(67);
			this.charIndex = this.DecodeByte();
			this.SeekFromCurrent(4);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			state.LoginCharacter(this.charIndex);
		}
	}


	public sealed class ClientVersionInPacket : GameIncomingPacket {
		string ver;

		protected override ReadPacketResult Read() {
			ushort size = this.DecodeUShort();
			this.ver = this.DecodeAsciiString(size - 3);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			state.SetClientVersion(ClientVersion.Get(this.ver));
		}

	}

	public sealed class GetPlayerStatusInPacket : GameIncomingPacket {
		byte type;
		int uid;

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(4); //BYTE[4] pattern (0xedededed) 

			this.type = this.DecodeByte();
			this.uid = this.DecodeInt();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractCharacter viewer = state.CharacterNotNull;

			AbstractCharacter target = Thing.UidGetCharacter(this.uid);
			if (target != null) {
				switch (this.type) {
					case 0x04:
						target.ShowStatusBarTo(viewer, conn);
						return;
					case 0x05:
						if ((viewer == target) || viewer.IsPlevelAtLeast(Globals.plevelOfGM)) {
							target.ShowSkillsTo(conn, state);
							//} else {
							//    Server.SendSystemMessage(c, "Illegal skills request.", 0);
						} else {
							Logger.WriteDebug(state + ": Illegal skills request.");
						}
						return;

					default:
						//Server.SendSystemMessage(c, "Unknown type=" + getType.ToString("x") + " in GetPlayerStatusPacket.", 0);
						Logger.WriteDebug(state + ": Unknown status/skills request type=" + this.type.ToString("x") + " in GetPlayerStatusPacket.");
						return;
				}
			} else {
				PacketSequences.SendRemoveFromView(conn, this.uid);
			}
		}
	}

	public sealed class RequestMultiplePropertiesInPacket : DynamicLenInPacket {
		List<int> uids = new List<int>();

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.uids.Clear();
			if (Globals.aos) {
				int dataBlockSize = blockSize - 3;
				if ((dataBlockSize >= 0) && ((dataBlockSize % 4) == 0)) {
					int count = dataBlockSize / 4;
					for (int i = 0; i < count; i++) {
						this.uids.Add(this.DecodeInt());
					}
				}
				return ReadPacketResult.Success;
			} else {
				return ReadPacketResult.DiscardSingle;
			}
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			if (Globals.aos) {
				AbstractCharacter curChar = state.CharacterNotNull;
				foreach (int uid in this.uids) {
					Thing t = Thing.UidGetThing(uid);
					if ((t != null) && (!t.IsDeleted)) {
						ObjectPropertiesContainer iopc = t.GetProperties();
						if (iopc != null) {
							if (curChar.CanSeeForUpdate(t)) {
								iopc.SendDataPacket(conn, state);
							}
						}
					}
				}
			}
		}
	}

	public sealed class TalkRequestInPacket : DynamicLenInPacket {
		byte type;
		ushort color, font;
		string speech;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.type = this.DecodeByte();
			this.color = this.DecodeUShort();
			this.font = this.DecodeUShort();
			this.speech = this.DecodeAsciiString(blockSize - 8);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			if (this.speech.IndexOf(Globals.commandPrefix) == 0) {
				Commands.PlayerCommand(state, speech.Substring(Globals.commandPrefix.Length));
			} else if (this.speech.IndexOf(Globals.alternateCommandPrefix) == 0) {
				Commands.PlayerCommand(state, speech.Substring(Globals.alternateCommandPrefix.Length));
			} else {
				//this.font = 3;	//We don't want them speaking in runic or anything. -SL
				//we don't? not sure here, we'll see -tar

				//actually speak it
				state.CharacterNotNull.Speech(this.speech, 0, (SpeechType) this.type, this.color, this.font, null, null);
			}
		}
	}

	public sealed class UnicodeOrAsciiSpeechRequestInPacket : DynamicLenInPacket {
		byte type;
		ushort color, font;
		string speech, language;
		int[] keywords;

		private HashSet<int> keywordsSet = new HashSet<int>();

		private static int[] emptyInts = new int[0];

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.type = this.DecodeByte();
			this.color = this.DecodeUShort();
			this.font = this.DecodeUShort();
			this.language = this.DecodeAsciiString(4);

			this.keywords = emptyInts;

			if ((type & 0xc0) == 0xc0) {
				int value = this.DecodeShort();
				int numKeywords = (value & 0xFFF0) >> 4;
				int hold = value & 0xF;

				for (int i = 0; i < numKeywords; ++i) {
					int speechID;
					if ((i & 1) == 0) {
						hold <<= 8;
						hold |= this.DecodeByte();
						speechID = hold;
						hold = 0;
					} else {
						value = this.DecodeShort();
						speechID = (value & 0xFFF0) >> 4;
						hold = value & 0xF;
					}

					keywordsSet.Add(speechID);
				}

				int n = keywordsSet.Count;
				if (n > 0) {
					this.keywords = new int[n];
					keywordsSet.CopyTo(this.keywords, 0);
					keywordsSet.Clear();
				}
				this.type = (byte) (this.type & ~0xc0);

				int speechlen = blockSize - this.Position;

				speech = this.DecodeAsciiString(speechlen);
			} else {
				speech = this.DecodeBigEndianUnicodeString(blockSize - 12);
			}

			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			if (this.speech.IndexOf(Globals.commandPrefix) == 0) {
				Commands.PlayerCommand(state, this.speech.Substring(Globals.commandPrefix.Length));
			} else if (this.speech.IndexOf(Globals.alternateCommandPrefix) == 0) {
				Commands.PlayerCommand(state, this.speech.Substring(Globals.alternateCommandPrefix.Length));
			} else {
				//actually speak it
				state.CharacterNotNull.Speech(this.speech, 0, (SpeechType) this.type, this.color, this.font, this.language, this.keywords, null);
			}
		}
	}

	public sealed class SetLanguageInPacket : GameIncomingPacket {
		string language;

		protected override ReadPacketResult Read() {
			this.language = this.DecodeAsciiString(4);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			state.Language = this.language;
		}
	}


	public sealed class TargetCursorCommandsInPacket : GameIncomingPacket {
		bool targGround;
		uint uid;
		ushort x, y;
		sbyte z;
		ushort model;

		protected override ReadPacketResult Read() {
			this.targGround = this.DecodeBool();
			this.SeekFromCurrent(5);
			this.uid = this.DecodeUInt();
			this.x = this.DecodeUShort();
			this.y = this.DecodeUShort();
			this.SeekFromCurrent(1);//skip the first byte of Z
			this.z = this.DecodeSByte();
			this.model = this.DecodeUShort();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			state.HandleTarget(this.targGround, this.uid, this.x, this.y, this.z, this.model);
		}
	}

	public sealed class PingPongInPacket : GameIncomingPacket {

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(1);
			return ReadPacketResult.DiscardSingle;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			throw new Exception("The method or operation is not implemented.");
		}
	}

	public sealed class ClientViewRangeInPacket : GameIncomingPacket {
		byte range;

		protected override ReadPacketResult Read() {
			this.range = this.DecodeByte();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			state.RequestedUpdateRange = this.range;
		}
	}

	public sealed class SingleClickInPacket : GameIncomingPacket {
		int uid;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			Thing thing = Thing.UidGetThing(this.uid);
			if (thing != null) {
				if (Globals.aos && state.Version.aosToolTips) {
					thing.Trigger_AosClick(state.CharacterNotNull);
				} else {
					thing.Trigger_Click(state.CharacterNotNull);
				}
			}
		}
	}

	public sealed class DoubleClickInPacket : GameIncomingPacket {
		uint uid;
		bool paperdollFlag;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeUInt();
			this.paperdollFlag = ((this.uid & 0x80000000) == 0x80000000);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractCharacter ch = state.CharacterNotNull;
			Thing thing;
			if (this.uid == 0x3cfff) {
				thing = ch;
			} else {
				thing = Thing.UidGetThing(this.uid);
				if ((thing == null) || (!ch.CanSeeForUpdate(thing))) {
					PacketSequences.SendRemoveFromView(conn, this.uid);
					return;
				}
			}

			if (thing.IsItem) {
				thing.Trigger_DClick(ch);
			} else {
				if (this.paperdollFlag) {
					((AbstractCharacter) thing).ShowPaperdollTo(ch, conn);
				} else {
					thing.Trigger_DClick(ch);
				}
			}
		}
	}

	public sealed class CreateCharacterInPacket : GameIncomingPacket {
		CreateCharArguments args = new CreateCharArguments();

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(9);
			this.args.charname = this.DecodeAsciiString(30, true);
			this.SeekFromCurrent(30);//password
			this.args.gender = this.DecodeByte();
			this.args.startStr = this.DecodeByte();
			this.args.startDex = this.DecodeByte();
			this.args.startInt = this.DecodeByte();
			this.args.skillId1 = this.DecodeByte();
			this.args.skillValue1 = this.DecodeByte();
			this.args.skillId2 = this.DecodeByte();
			this.args.skillValue2 = this.DecodeByte();
			this.args.skillId3 = this.DecodeByte();
			this.args.skillValue3 = this.DecodeByte();
			this.args.skinColor = (ushort) (this.DecodeUShort() & ~0x8000);
			this.args.hairStyle = this.DecodeUShort();
			this.args.hairColor = this.DecodeUShort();
			this.args.facialHair = this.DecodeUShort();
			this.args.facialHairColor = this.DecodeUShort();
			this.SeekFromCurrent(10);//some versions may not be sending this part... we'll see how it turns out

			this.args.shirtColor = this.DecodeUShort();
			this.args.pantsColor = this.DecodeUShort();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractAccount acc = state.Account;
			if (!acc.HasFreeSlot) {
				state.WriteLine("You already have the maximum number of characters in your account.");
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Tried to create more chars than allowed");
				return;
			}

			string charname = this.args.charname;
			for (int a = 0; a < charname.Length; a++) {
				if (!(charname[a] == ' ' || (charname[a] >= 'a' && charname[a] <= 'z') || (charname[a] >= 'A' && charname[a] <= 'Z'))) {
					state.WriteLine("Illegal name - Contains something other than letters and spaces.");
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
					conn.Close("Illegal name - Contains something other than letters and spaces.");
					return;
				}
			}
			if (charname[0] == ' ' || charname[charname.Length - 1] == ' ') {
				state.WriteLine("Illegal name - Starts or ends with spaces.");
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal name - Starts or ends with spaces.");
				return;
			}
			if (charname.IndexOf("  ") > -1) {
				state.WriteLine("Illegal name - More than one space in a row.");
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal name - More than one space in a row.");
				return;
			}
			if (this.args.gender < 0 || this.args.gender > 1) {
				state.WriteLine("Illegal gender=" + this.args.gender);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal gender=" + this.args.gender);
				return;
			}
			if (this.args.skinColor < 0x3ea || this.args.skinColor > 0x422) {
				state.WriteLine("Illegal skinColor=" + this.args.skinColor);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal skinColor=" + this.args.skinColor);
				return;
			}
			if (this.args.hairStyle == 0) {
			} else if (this.args.hairStyle < 0x203B || this.args.hairStyle > 0x204A ||
					   (this.args.hairStyle > 0x203D && this.args.hairStyle < 0x2044)) {
				state.WriteLine("Illegal hairStyle=" + this.args.hairStyle);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal hairStyle=" + this.args.hairStyle);
				return;
			}
			if (this.args.hairColor < 0x44e || this.args.hairColor > 0x4ad) {
				state.WriteLine("Illegal hairColor=" + this.args.hairColor);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal hairColor=" + this.args.hairColor);
				return;
			}
			if (this.args.facialHair == 0) {
			} else if (this.args.facialHair < 0x203E || this.args.facialHair > 0x204D || (this.args.facialHair > 0x2041 && this.args.facialHair < 0x204B)) {
				state.WriteLine("Illegal facialHair=" + this.args.facialHair);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal facialHair=" + this.args.facialHair);
				return;
			}
			if (this.args.facialHairColor < 0x44e || this.args.facialHairColor > 0x4ad) {
				state.WriteLine("Illegal facialHairColor=" + this.args.facialHairColor);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal facialHairColor=" + this.args.facialHairColor);
				return;
			}
			if (this.args.shirtColor < 0x02 || this.args.shirtColor > 0x3e9) {
				state.WriteLine("Illegal shirtColor=" + this.args.shirtColor);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal shirtColor=" + this.args.shirtColor);
				return;
			}
			if (this.args.pantsColor < 0x02 || this.args.pantsColor > 0x3e9) {
				state.WriteLine("Illegal pantsColor=" + this.args.pantsColor);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal pantsColor=" + this.args.pantsColor);
				return;
			}

			//TODO: check skills and stats? Can't be bothered for moria, for now... -tar

			ScriptArgs sa = new ScriptArgs(this.args);
			object o = CreatePlayerCharacterFunction.Run(Globals.instance, sa);//creates the character

			AbstractCharacter cre = o as AbstractCharacter;
			if (cre == null) {
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("The CreatePlayerCharacter function failed to create a new character.");
				return;
			}
			int charSlot = cre.MakeBePlayer(acc);
			Globals.SetSrc(cre);

			Logger.WriteDebug("Logging in character " + cre);
			state.LoginCharacter(charSlot);
		}

		private static ScriptHolder createPlayerCharacterFunction;
		public static ScriptHolder CreatePlayerCharacterFunction {
			get {
				if (createPlayerCharacterFunction == null) {
					createPlayerCharacterFunction = ScriptHolder.GetFunction("CreatePlayerCharacter");
					if (createPlayerCharacterFunction == null) {
						throw new SEException("CreatePlayerCharacter function not declared! It needs to have 1 parameter of type CreateCharArguments, and return an AbstractCharacter instance");
					}
				}
				return createPlayerCharacterFunction;
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

			public byte skillId1;
			public byte skillValue1;
			public byte skillId2;
			public byte skillValue2;
			public byte skillId3;
			public byte skillValue3;
		}
	}

	public sealed class DisconnectNotificationInPacket : GameIncomingPacket {
		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(4);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			conn.Close("Client logoff - returning to main menu");
		}
	}
	
	public sealed class MoveRequestInPacket : GameIncomingPacket {
		byte direction,sequence;

		protected override ReadPacketResult Read() {
			this.direction = (byte) (this.DecodeByte() & 0x87); //we only want 0x80 and 0..7
			this.sequence = this.DecodeByte();
			//TODO? check this.LengthIn for fastwalk presence
			this.SeekFromCurrent(4);//skip the fastwalk key, we don't use it
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			MovementState ms = state.movementState;

			if (ms.MovementSequenceIn == this.sequence) {
				ms.MovementRequest(this.direction);
			} else {
				Logger.WriteError("Invalid seqNum " + LogStr.Number(this.sequence) + ", expecting " + LogStr.Number(ms.MovementSequenceIn));
				CharMoveRejectionOutPacket packet = Pool<CharMoveRejectionOutPacket>.Acquire();
				packet.Prepare(this.sequence, state.CharacterNotNull);
				conn.SendSinglePacket(packet);
				ms.ResetMovementSequence();
			}
		}
	}


	public sealed class RequestAttackInPacket : GameIncomingPacket {
		uint uid;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeUInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractCharacter cre = Thing.UidGetCharacter(this.uid);
			if ((cre == null) || (cre.IsDeleted)) {
				PacketSequences.SendRemoveFromView(conn, this.uid);
			} else {
				AbstractCharacter self = state.CharacterNotNull;
				self.Trigger_PlayerAttackRequest(cre);
			}
		}
	}

	public sealed class PickUpItemInPacket : GameIncomingPacket {
		int uid;
		ushort amount;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.amount = this.DecodeUShort();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractItem item = Thing.UidGetItem(this.uid);
			if (item != null) {
				AbstractCharacter cre = state.CharacterNotNull;
				DenyResult result = cre.TryPickupItem(item, this.amount);
				if (result != DenyResult.Allow) {
					if (result < DenyResult.Allow) {
						PreparedPacketGroups.SendRejectMoveItemRequest(conn, result);
					} else {
						PacketSequences.SendDenyResultMessage(conn, item, result);
						PreparedPacketGroups.SendRejectMoveItemRequest(conn, DenyResult.Deny_NoMessage);
					}
				}
			} else {
				PacketSequences.SendRemoveFromView(conn, this.uid);
			}
		}
	}	
	
	public sealed class DropItemInPacket : GameIncomingPacket {
		int itemUid, contUid;
		ushort x, y;
		sbyte z;

		protected override ReadPacketResult Read() {
			this.itemUid = this.DecodeInt();
			this.x = this.DecodeUShort();
			this.y = this.DecodeUShort();
			this.z = this.DecodeSByte();
			this.contUid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			//always an item, may or may not have the item-flag.
			AbstractItem i = Thing.UidGetItem(this.itemUid);
			AbstractCharacter cre = state.CharacterNotNull;
			if (i != null && cre.HasPickedUp(i)) {
				DenyResult result;
				if (this.contUid == -1) {//dropping on ground
					result = cre.TryPutItemOnGround(x, y, z);
				} else {
					Thing co = Thing.UidGetThing(this.contUid);
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
						PacketSequences.SendRemoveFromView(conn, this.contUid);
						result = DenyResult.Deny_ThatIsOutOfSight;
					}
				}

				if (result != DenyResult.Allow) {
					PacketSequences.SendDenyResultMessage(conn, i, result);
					cre.TryGetRidOfDraggedItem();
				}
			} else {
				PacketSequences.SendRemoveFromView(conn, this.itemUid);
			}
		}
	}

	public sealed class DropToWearItemInPacket : GameIncomingPacket {
		int itemUid, contUid;

		protected override ReadPacketResult Read() {
			this.itemUid = this.DecodeInt();
			this.SeekFromCurrent(1); //we don't care which layer the client thinks it belongs to
			this.contUid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractItem i = Thing.UidGetItem(this.itemUid);
			AbstractCharacter cre = state.CharacterNotNull;
			if (i != null && cre.HasPickedUp(i)) {
				AbstractCharacter contChar = Thing.UidGetCharacter(this.contUid);
				if (contChar == null) {
					contChar = cre;
				}
				DenyResult result = cre.TryEquipItemOnChar(contChar);

				if (result != DenyResult.Allow && cre.HasPickedUp(i)) {//we check if the player still has it in hand after the triggers
					PacketSequences.SendDenyResultMessage(conn, contChar, result);
					cre.TryGetRidOfDraggedItem();
				}
			} else {
				PacketSequences.SendRemoveFromView(conn, this.itemUid);
			}
		}
	}

	public sealed class RequestSkillEtcUseInPacket : DynamicLenInPacket {
		byte type;
		string actionStr;
		int actionParseResult;
		bool actionParsed;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.type = this.DecodeByte();
			this.actionStr = this.DecodeAsciiString(blockSize - 4);
			string firstPart = this.actionStr.Split(Tools.whiteSpaceChars)[0];
			this.actionParsed = ConvertTools.TryParseInt32(firstPart, out this.actionParseResult);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			switch (this.type) {
				//case 0: //God mode teleport?
				//case 0x6b: //God mode command?

				case 0x24: //skill
					if (this.actionParsed) {
						int skillId;
						if (this.actionParseResult == 0) {//last skill
							skillId = state.lastSkillMacroId;
						} else {
							skillId = this.actionParseResult;
						}
						AbstractSkillDef skillDef = AbstractSkillDef.ById(skillId);
						if (skillDef.StartByMacroEnabled) {
							state.lastSkillMacroId = skillId;
							state.CharacterNotNull.SelectSkill(skillDef);
							return;
						}
					}
					break;
				case 0x56: //spell
					if (this.actionParsed) {
						if (this.actionParseResult == 0) {
							Temporary.UseLastSpellRequest(state.CharacterNotNull);
						} else {
							Temporary.UseSpellNumberRequest(state.CharacterNotNull, this.actionParseResult);
						}
					}
					break;
				case 0x58: //opendoor
					Temporary.OpenDoorMacroRequest(state.CharacterNotNull);
					break;
				case 0x7c: //anim
					switch (this.actionStr) {
						case "bow":
							Temporary.AnimRequest(state.CharacterNotNull, RequestableAnim.Bow);
							break;
						case "salute":
							Temporary.AnimRequest(state.CharacterNotNull, RequestableAnim.Salute);
							break;
					}
					break;
			}

			
			this.OutputPacketLog("Error or unexpected value in Request skill/spell/opendoor/anim packet.");
		}
	}	
	
	public sealed class StatLockChangeInPacket : GameIncomingPacket {
		byte stat;
		StatLockType lockType;

		protected override ReadPacketResult Read() {
			this.stat = this.DecodeByte();
			byte l = this.DecodeByte();
			if (l > 2) {
				l = 0;
			}
			this.lockType = (StatLockType) l;
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			switch (stat) {
				case 0:
					state.CharacterNotNull.StrLock = this.lockType;
					break;
				case 1:
					state.CharacterNotNull.DexLock = this.lockType;
					break;
				case 2:
					state.CharacterNotNull.IntLock = this.lockType;
					break;
			}
		}
	}

	public sealed class SetSkillLockStateInPacket : DynamicLenInPacket {
		short skillId;
		SkillLockType lockType;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.skillId = this.DecodeShort();
			byte l = this.DecodeByte();
			if (l > 2) {
				l = 0;
			}
			this.lockType = (SkillLockType) l;
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			if ((this.skillId >= 0) && (this.skillId < AbstractSkillDef.SkillsCount)) {
				state.CharacterNotNull.SetSkillLockType(this.skillId, this.lockType);
			} else {
				this.OutputPacketLog("SkillLock of skillid " + this.skillId + " requested out of range.");
			}
		}
	}
	
	public sealed class RequestWarModeInPacket : GameIncomingPacket {
		bool warModeEnabled;

		protected override ReadPacketResult Read() {
			this.warModeEnabled = this.DecodeBool();
			this.SeekFromCurrent(3);//unknown
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			state.CharacterNotNull.Flag_WarMode = this.warModeEnabled;
		}
	}

	public sealed class DeleteCharacterInPacket : GameIncomingPacket {
		int charSlot;

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(30);//password
			this.charSlot = this.DecodeInt();
			this.SeekFromCurrent(4);//IP
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			if (this.charSlot < 0 || this.charSlot >= AbstractAccount.maxCharactersPerGameAccount) {
				//Server.DisconnectAndLog(c, "Illegal char index "+charIndex);
				PreparedPacketGroups.SendRejectDeleteCharacter(conn, DeleteCharacterResult.Deny_NonexistantCharacter);
				return;
			}
			AbstractAccount acc = state.Account;
			DeleteCharacterResult result = acc.RequestDeleteCharacter(charSlot);
			if (result == DeleteCharacterResult.Allow) {
				ResendCharactersAfterDeleteOutPacket p = Pool<ResendCharactersAfterDeleteOutPacket>.Acquire();
				p.Prepare(acc.Characters);
				conn.SendSinglePacket(p);
			} else if (result != DeleteCharacterResult.Deny_NoMessage) {
				PreparedPacketGroups.SendRejectDeleteCharacter(conn, result);
			}
		}
	}

	public sealed class AllNamesInPacket : DynamicLenInPacket {
		int uid;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			AbstractCharacter c = Thing.UidGetCharacter(this.uid);
			if ((c != null) && (state.CharacterNotNull.CanSeeForUpdate(c))) {
				AllNamesOutPacket p = Pool<AllNamesOutPacket>.Acquire();
				p.Prepare(this.uid, c.Name);
				conn.SendSinglePacket(p);
			}
		}
	}
	
	public sealed class RequestHelpInPacket : GameIncomingPacket {
		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(257);//some worthless data
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			//TODO
			state.WriteLine("Sorry, there isn't a help menu yet.");
		}
	}

	public sealed class GumpMenuSelectionInPacket : DynamicLenInPacket {
		uint gumpUid;
		uint pressedButton;
		uint[] selectedSwitches;
		ResponseText[] responseTexts;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.SeekFromCurrent(4); //skip the focus uid
			this.gumpUid = this.DecodeUInt();
			this.pressedButton = this.DecodeUInt();

			int switchesCount = this.DecodeInt();
			this.selectedSwitches = new uint[switchesCount];
			for (int i = 0; i < switchesCount; i++) {
				selectedSwitches[i] = this.DecodeUInt();
			}

			uint entriesCount = this.DecodeUInt();
			this.responseTexts = new ResponseText[entriesCount];
			for (int i = 0; i < entriesCount; i++) {
				ushort id = this.DecodeUShort();
				int len = this.DecodeUShort() * 2;
				string text = this.DecodeBigEndianUnicodeString(len);
				this.responseTexts[i] = new ResponseText(id, text);
			}
			return ReadPacketResult.Success;
		}

		protected override void Handle(TCPConnection<GameState> conn, GameState state) {
			Gump gi = state.PopGump(this.gumpUid);
			if (gi != null) {
				int n = (gi.numEntryIDs != null) ? gi.numEntryIDs.Count : 0;
				ResponseNumber[] responseNumbers = new ResponseNumber[n];
				for (int i = 0; i < n; i++) {
					foreach (ResponseText rt in responseTexts) {
						if (gi.numEntryIDs[i] == rt.id) {
							double number;
							if (ConvertTools.TryParseDouble(rt.text, out number)) {
								responseNumbers[i] = new ResponseNumber(rt.id, number);
							} else {
								state.WriteLine("'" + rt.text + " is not a number!");
								SendGumpBack(conn, state, gi, responseTexts);
								return;
							}
							break;
						}
					}
					//we could fill the possible gap here... or not. The clients should expect nulls in the array.
				}
				gi.OnResponse(this.pressedButton, this.selectedSwitches, this.responseTexts, responseNumbers);
			}

			this.selectedSwitches = null;
			this.responseTexts = null;
		}

		private static void SendGumpBack(TCPConnection<GameState> conn, GameState state, Gump gi, ResponseText[] responseTexts) {
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
			state.SentGump(gi);
			SendGumpMenuDialogPacket p = Pool<SendGumpMenuDialogPacket>.Acquire();
			p.Prepare(gi);
			conn.SendSinglePacket(p);
		}
	}
}