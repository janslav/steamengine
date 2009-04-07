using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine {

	public class AOSToolTips : Poolable {
		private int uid;
		private Thing thing;
		private List<int> ids = new List<int>(3);
		private List<string> arguments = new List<string>(3);
		private bool initDone;

		//SteamEngine.Packets.FreedPacketGroup oldIdGroup;
		//SteamEngine.Packets.FreedPacketGroup newIdGroup;
		//SteamEngine.Packets.FreedPacketGroup dataGroup;

		PacketGroup oldIdNGroup;
		PacketGroup newIdNGroup;
		PacketGroup dataNGroup;

		private static CacheDictionary<Thing, AOSToolTips> cache =
			new CacheDictionary<Thing, AOSToolTips>(50000, true);//znate nekdo nejaky lepsi cislo? :)
		private static int uids;

		public AOSToolTips() {
		}

		public static AOSToolTips GetFromCache(Thing thing) {
			AOSToolTips toolTips;
			cache.TryGetValue(thing, out toolTips);
			return toolTips;
		}

		public static void RemoveFromCache(Thing thing) {
			cache.Remove(thing);
		}

		protected override void On_Reset() {
			base.On_Reset();

			this.uid = uids++;
			this.ids.Clear();
			this.arguments.Clear();
			this.oldIdNGroup = null;
			this.newIdNGroup = null;
			this.dataNGroup = null;
			this.initDone = false;
			this.thing = null;
		}

		public override void Dispose() {
			if (this.initDone) {
				cache.Remove(this.thing);
				this.initDone = false;
			}

			base.Dispose();
		}

		public void InitDone(Thing thing) {
			this.thing = thing;
			cache[thing] = this;
			this.initDone = true;
		}

		public bool IsInitDone {
			get {
				return this.initDone;
			}
		}

		//public void AddLine(string plainText) {
		//    Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
		//    this.ids.Add(1042971); //1042971, 1070722 
		//    this.arguments.Add(plainText);
		//}

		public void AddLine(int clilocId) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			ids.Add(clilocId);
			arguments.Add(null);
		}

		public void AddLine(int clilocId, string arg) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(arg);
		}

		public void AddLine(int clilocId, string arg0, string arg1) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(string.Concat(arg0, "\t", arg1));
		}

		public void AddLine(int clilocId, params string[] args) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(string.Join("\t", args));
		}

		////0xbf 0x10 or 0xdc
		//[Obsolete("Use the alternative from Networking namespace", false)]
		//public void SendIdPacket(GameConn c) {
		//    if (c.Version.oldAosToolTips) {
		//        if (oldIdGroup == null) {
		//            SteamEngine.Packets.BoundPacketGroup bpg = SteamEngine.Packets.PacketSender.NewBoundGroup();
		//            SteamEngine.Packets.PacketSender.PrepareOldPropertiesRefresh(thing, uid);
		//            oldIdGroup = bpg.Free();
		//        }
		//        oldIdGroup.SendTo(c);
		//    } else {
		//        if (newIdGroup == null) {
		//            SteamEngine.Packets.BoundPacketGroup bpg = SteamEngine.Packets.PacketSender.NewBoundGroup();
		//            SteamEngine.Packets.PacketSender.PreparePropertiesRefresh(thing, uid);
		//            newIdGroup = bpg.Free();
		//        }
		//        newIdGroup.SendTo(c);
		//    }
		//}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public void SendIdPacket(GameState state, TCPConnection<GameState> conn) {
			if (state.Version.OldAosToolTips) {
				if (this.oldIdNGroup == null) {
					this.oldIdNGroup = PacketGroup.CreateFreePG();
					this.oldIdNGroup.AcquirePacket<OldPropertiesRefreshOutPacket>().Prepare(thing.FlaggedUid, this.uid);
				}
				conn.SendPacketGroup(this.oldIdNGroup);
			} else {
				if (this.newIdNGroup == null) {
					this.newIdNGroup = PacketGroup.CreateFreePG();
					this.newIdNGroup.AcquirePacket<PropertiesRefreshOutPacket>().Prepare(thing.FlaggedUid, this.uid);
				}
				conn.SendPacketGroup(this.newIdNGroup);
			}
		}

		public int FirstId {
			get {
				return this.ids[0];
			}
		}

		public string FirstArgument {
			get {
				return arguments[0];
			}
		}

		//0xd6 - megacliloc
		//public void SendDataPacket(GameConn c) {
		//    if (dataGroup == null) {
		//        SteamEngine.Packets.BoundPacketGroup bpg = SteamEngine.Packets.PacketSender.NewBoundGroup();
		//        SteamEngine.Packets.PacketSender.PrepareMegaCliloc(thing, uid, ids, arguments);
		//        dataGroup = bpg.Free();
		//    }
		//    dataGroup.SendTo(c);
		//}

		internal void SendDataPacket(TCPConnection<GameState> conn, GameState state) {
			if (this.dataNGroup == null) {
				this.dataNGroup = PacketGroup.CreateFreePG();
				this.dataNGroup.AcquirePacket<MegaClilocOutPacket>().Prepare(thing.FlaggedUid, uid, ids, arguments);
			}
			conn.SendPacketGroup(this.dataNGroup);
		}

		public static void ClearCache() {
			cache.Clear();
		}
	}
}

//1060658	~1_val~: ~2_val~
//1060659	~1_val~: ~2_val~
//1060660	~1_val~: ~2_val~
//1060661	~1_val~: ~2_val~
//1060662	~1_val~: ~2_val~
//1060663	~1_val~: ~2_val~

//1050044	~1_COUNT~ items, ~2_WEIGHT~ stones
//1050045	~1_PREFIX~~2_NAME~~3_SUFFIX~