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
using SteamEngine.Regions;

namespace SteamEngine.Networking {

	public delegate void OnTargon(GameState state, IPoint4D getback, object parameter);
	public delegate void OnTargon_Cancel(GameState state, object parameter);

	public class GameState : Poolable, IConnectionState<TCPConnection<GameState>, GameState, IPEndPoint> {

		static int uids;

		private int uid;

		IEncryption encryption;

		AbstractAccount account;
		AbstractCharacter character;
		IPEndPoint ip;
		TCPConnection<GameState> conn;

		private byte updateRange = 18;
		private int visionRange = 18;	//for scripts to fiddle with
		private byte requestedUpdateRange = 18;
		//internal bool restoringUpdateRange = false;	//The client, if sent an update range packet on login, apparently then requests 18 again, but does it after sending some other packets - this sure makes it hard for the client to not have to set the update range every time they login... So we block the next update range packet if we get this, but the block is removed if we get a 0x73 (ping) first. We can't remove it for any incoming packets becaues the client sends other stuff before it gets around to sending this.

		private ClientVersion clientVersion = ClientVersion.nullValue;

		private bool allShow = false;

		public OnTargon targonDeleg;
		public OnTargon_Cancel targonCancelDeleg;
		public object targonParameters;

		internal readonly MovementState movementState;

		private string clientLanguage = "enu";
		private Language language = Language.Default;

		public int lastSkillMacroId;
		public int lastSpellMacroId;

		private Dictionary<uint, Gump> gumpInstancesByUid = new Dictionary<uint, Gump>();
		private Dictionary<GumpDef, LinkedList<Gump>> gumpInstancesByGump = new Dictionary<GumpDef, LinkedList<Gump>>();

		public int charBackupUid;

		public GameState() {
			this.movementState = new MovementState(this);
			this.movementState.On_Reset();
		}

		protected override void On_Reset() {
			this.encryption = new GameEncryption();
			this.uid = uids++;
			this.ip = null;
			this.account = null;
			this.conn = null;

			if (this.movementState != null) {
				this.movementState.On_Reset();
			}

			base.On_Reset();
		}

		public IEncryption Encryption {
			get {
				return encryption;
			}
		}

		public ICompression Compression {
			get {
				return GameCompression.instance; //GameCompression is stateless
			}
		}

		public TCPConnection<GameState> Conn {
			get {
				return this.conn;
			}
		}

		public void On_Init(TCPConnection<GameState> conn) {
			GameServer.On_ClientInit(this);

			this.conn = conn;
			this.ip = conn.EndPoint;

			Console.WriteLine(LogStr.Ident(this.ToString()) + (" connected from " + conn.EndPoint.ToString()));

			Globals.instance.TryTrigger(TriggerKey.clientAttach, new ScriptArgs(this, this.conn)); // 
		}

		public void On_Close(string reason) {
			Console.WriteLine(LogStr.Ident(this.ToString()) + LogStr.Warning(" closed: ") + reason);

			GameServer.On_ClientClose(this);


			if (this.character != null) {
				if (!this.character.IsDeleted) {
					this.character.Trigger_LogOut();
				}
				this.character = null;
			}
			if (this.account != null) {
				this.account.SetLoggedOut();
				this.account = null;
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("Client (uid=");
			sb.Append(this.uid);
			if (this.account != null) {
				sb.Append(", acc='").Append(this.account.Name).Append("'");
			}
			if (this.ip != null) {
				sb.Append(", IP=").Append(this.ip.ToString());
			}
			return sb.Append(")").ToString();
		}

		public override int GetHashCode() {
			return this.uid;
		}

		public override bool Equals(object obj) {
			return Object.ReferenceEquals(this, obj);
		}

		//public override void Handle(IncomingPacket packet) {
		//    ConsoleServerPacketGroup pg = Pool<ConsoleServerPacketGroup>.Acquire();

		//    pg.AddPacket(Pool<ConsoleServerOutgoingPacket>.Acquire());

		//    //MainClass.server.SendPacketGroup(this, pg);
		//}

		internal void SetLoggedIn(AbstractAccount acc) {
			this.account = acc;
		}

		public AbstractAccount Account {
			get {
				return this.account;
			}
		}

		public bool AllShow {
			get {
				return this.allShow;
			}
			set {
				if (value != this.allShow) {
					this.allShow = value;
					if (this.character != null) {
						this.character.Resync();
					}
				}
			}
		}

		public bool IsLoggedIn {
			get {
				return this.account != null;
			}
		}

		//this is called by InPackets.LoginChar
		internal void LoginCharacter(int charSlot) {
			Sanity.IfTrueThrow(charSlot < 0 || charSlot >= AbstractAccount.maxCharactersPerGameAccount, "charSlot < 0 || charSlot >= AbstractAccount.maxCharactersPerGameAccount, charSlot = " + charSlot);
			Sanity.IfTrueThrow(this.account == null, "this.account == null");

			if (this.character != null) {
				this.conn.Close("Character login with a character already logged, wtf?!");
				return;
			}

			AbstractCharacter ch = this.account.GetLingeringCharacter();
			if (ch == null) {//if we've already a lingering char on this acc, we ignore the selection and login the lingering one
				//otherwise, find the one in slot
				ch = this.account.GetCharacterInSlot(charSlot);
			}
			if (ch == null) {
				PreparedPacketGroups.SendLoginDenied(this.conn, LoginDeniedReason.CommunicationsProblem);
				this.conn.Close("Client tried to choose nonexisting character.");
				return;
			}

			this.character = ch;
			if (ch.TryLogIn()) {	//returns true for success
				if (ch.Account == null || ch.Account != this.account) {
					Logger.WriteWarning("Invalid account - Fixing.");
					ch.MakeBePlayer(this.account);
				}

				//PreparedPacketGroups.SendClientVersionQuery(this.conn);

				PacketSequences.DelayedLoginTimer timer = new PacketSequences.DelayedLoginTimer(ch);
				timer.DueInSeconds = 0;
				timer.PeriodInSeconds = 0.5;
			} else {
				this.character = null;//we don't want @logout called when @login was cancelled

				PreparedPacketGroups.SendLoginDenied(this.conn, LoginDeniedReason.CommunicationsProblem);
				this.conn.Close("Login denied by scripts or no character in that slot.");
				return;
			}
		}

		public AbstractCharacter Character {
			get {
				return this.character;
			}
		}

		public AbstractCharacter CharacterNotNull {
			get {
				if (this.character == null) {
					throw new SEException("There is no character set for this " + this.ToString());
				}
				return this.character;
			}
		}

		public ClientVersion Version {
			get {
				return clientVersion;
			}
		}

		internal void InternalSetClientVersion(ClientVersion clientVersion) {
			this.clientVersion = clientVersion;
		}

		public int Uid {
			get {
				return this.uid;
			}
		}

		internal byte RequestedUpdateRange {
			get {
				return this.requestedUpdateRange;
			}
			set {
				//if (this.restoringUpdateRange) {
				//    this.restoringUpdateRange = false;
				//    return;
				//}
				this.requestedUpdateRange = value;
				byte oldUpdateRange = updateRange;
				this.RecalcUpdateRange();
				if (this.character != null && oldUpdateRange != this.updateRange) {
					ClientViewRangeOutPacket packet = Pool<ClientViewRangeOutPacket>.Acquire();
					packet.Prepare(this.updateRange);
					this.conn.SendSinglePacket(packet);
				}
			}
		}

		internal int VisionRange {
			get {
				return this.visionRange;
			}
			set {
				this.visionRange = value;
				byte oldUpdateRange = this.updateRange;
				this.RecalcUpdateRange();
				if (this.character != null && oldUpdateRange != this.updateRange) {
					ClientViewRangeOutPacket packet = Pool<ClientViewRangeOutPacket>.Acquire();
					packet.Prepare(this.updateRange);
					this.conn.SendSinglePacket(packet);
				}
			}
		}

		public byte UpdateRange {
			get {
				return this.updateRange;
			}
		}

		private void RecalcUpdateRange() {
			if (this.visionRange <= this.requestedUpdateRange) {
				if (this.visionRange < 0) {
					this.updateRange = 0;
				} else if (this.visionRange > Globals.MaxUpdateRange) {
					this.updateRange = Globals.MaxUpdateRange;
				} else if (this.visionRange < Globals.MinUpdateRange) {
					this.updateRange = Globals.MinUpdateRange;
				} else {
					this.updateRange = (byte) this.visionRange;
				}
			} else {
				this.updateRange = this.requestedUpdateRange;
			}
		}

		public void DecreaseVisionRangeBy(int amount) {
			this.VisionRange -= amount;
		}
		public void IncreaseVisionRangeBy(int amount) {
			this.VisionRange += amount;
		}

		public void Target(bool ground, OnTargon targonDeleg, OnTargon_Cancel targonCancelDeleg, object targonParameters) {
			this.targonDeleg = targonDeleg;
			this.targonCancelDeleg = targonCancelDeleg;
			this.targonParameters = targonParameters;
			PreparedPacketGroups.SendTargettingCursor(this.conn, ground);
		}

		public void TargetForMultis(ushort model, OnTargon targonDeleg, OnTargon_Cancel targonCancelDeleg, object targonParameters) {
			this.targonDeleg = targonDeleg;
			this.targonCancelDeleg = targonCancelDeleg;
			this.targonParameters = targonParameters;
			GiveBoatOrHousePlacementViewOutPacket packet = Pool<GiveBoatOrHousePlacementViewOutPacket>.Acquire();
			packet.Prepare(model);
			this.conn.SendSinglePacket(packet);
		}

		public string ClientLanguage {
			get {
				return this.clientLanguage;
			}
			internal set {
				if (!StringComparer.OrdinalIgnoreCase.Equals(value, this.clientLanguage)) {
					this.clientLanguage = value;
					this.language = Loc.TranslateLanguageCode(value);
				}
			}
		}

		public Language Language {
			get {
				return this.language;
			}
		}

		public void WriteLine(string msg) {
			PacketSequences.SendSystemMessage(this.conn, msg, -1);
		}

		internal void HandleTarget(bool targGround, uint uid, ushort x, ushort y, sbyte z, ushort model) {
			Logger.WriteDebug("HandleTarget: TG=" + targGround + " uid=" + uid + " x=" + x + " y=" + y + " z=" + z + " dispId=" + model);
			//figure out what it is
			object parameter = this.targonParameters;
			this.targonParameters = null;
			OnTargon_Cancel targonCancel = this.targonCancelDeleg;
			this.targonCancelDeleg = null;
			OnTargon targ = this.targonDeleg;
			this.targonDeleg = null;

			if (x == 0xffff && y == 0xffff && uid == 0 && z == 0 && model == 0) {
				//cancel
				if (targonCancel != null) {
					targonCancel(this, parameter);
				}
				return;
			} else {
				if (targ != null) {
					if (!targGround) {
						Thing thing = Thing.UidGetThing(uid);
						if (thing != null) {
							if (this.CharacterNotNull.CanSeeForUpdate(thing)) {
								targ(this, thing, parameter);
								return;
							}
						}
					} else {
						if (model == 0) {
							AbstractCharacter self = this.CharacterNotNull;
							Point4D point = new Point4D(x, y, z, self.M);
							if (self.CanSeeCoordinates(point)) {
								targ(this, point, parameter);
								return;
							}
						} else {
							AbstractCharacter self = this.CharacterNotNull;
							if (self.CanSeeCoordinates(x, y, z, self.M)) {
								Map map = self.GetMap();
								Static sta = map.GetStatic(x, y, z, model);
								if (sta != null) {
									targ(this, sta, parameter);
									return;
								}
								MultiItemComponent mic = map.GetMultiComponent(x, y, z, model);
								if (mic != null) {
									targ(this, mic, parameter);
									return;
								}
							}
						}
					}
				}
			}
			PacketSequences.SendClilocSysMessage(this.conn, 1046439, 0);//That is not a valid target.
		}


		internal void SentGump(Gump gi) {
			this.gumpInstancesByUid[gi.uid] = gi;
			GumpDef thisGump = gi.def;
			LinkedList<Gump> instancesOfThisGump;
			if (!this.gumpInstancesByGump.TryGetValue(thisGump, out instancesOfThisGump)) {
				instancesOfThisGump = new LinkedList<Gump>();
				this.gumpInstancesByGump[thisGump] = instancesOfThisGump;
			}
			instancesOfThisGump.AddFirst(gi);
		}

		internal ICollection<Gump> FindGumpInstances(GumpDef gd) {
			LinkedList<Gump> retVal;
			if (this.gumpInstancesByGump.TryGetValue(gd, out retVal)) {
				return retVal;
			}
			return EmptyReadOnlyGenericCollection<Gump>.instance;
		}

		internal Gump PopGump(uint uid) {
			Gump gi;
			if (this.gumpInstancesByUid.TryGetValue(uid, out gi)) {
				this.gumpInstancesByUid.Remove(uid);

				GumpDef gd = gi.def;
				LinkedList<Gump> list;
				if (this.gumpInstancesByGump.TryGetValue(gd, out list)) {
					list.Remove(gi);
					if (list.Count == 0) {
						this.gumpInstancesByGump.Remove(gd);
					}
				}
				return gi;
			}
			return null;
		}


		internal void BackupLinksToCharacters() {
			if (this.character != null) {
				this.charBackupUid = this.character.Uid;
			} else {
				this.charBackupUid = -1;
			}
		}

		internal void RelinkCharacter() {
			if (this.charBackupUid != -1) {
				AbstractCharacter newChar = Thing.UidGetCharacter(this.charBackupUid);
				if (newChar == null) {
					this.conn.Close("Character lost while recompiling...?");
				} else {
					newChar.Account.SetLoggedIn(this);
					this.account = newChar.Account;
					this.character = newChar;
					newChar.ReLinkToGameState();
				}
			}

			gumpInstancesByUid.Clear();
			gumpInstancesByGump.Clear();
		}

		internal void RemoveBackupLinks() {
			this.charBackupUid = -1;
		}
	}
}