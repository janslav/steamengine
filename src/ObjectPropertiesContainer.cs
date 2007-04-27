using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Packets;

namespace SteamEngine {
	public class ObjectPropertiesContainer {
		private int uid;
		private readonly Thing thing;
		private bool frozen = false;
		private List<uint> ids = new List<uint>(3);
		private List<string> arguments = new List<string>(3);

		FreedPacketGroup oldIdGroup;
		FreedPacketGroup newIdGroup;
		FreedPacketGroup dataGroup;

		private static CacheDictionary<Thing, ObjectPropertiesContainer> cache = 
			new CacheDictionary<Thing, ObjectPropertiesContainer>(50000);//znate nekdo nejaky lepsi cislo? :)
		private static int uids;


		public ObjectPropertiesContainer(Thing thing) {
			this.thing = thing;
			cache[thing] = this;
			uid = uids++;
		}

		public static ObjectPropertiesContainer Get(Thing thing) {
			ObjectPropertiesContainer opc;
			cache.TryGetValue(thing, out opc);
			return opc;
		}

		public void Unfreeze() {
			if (frozen) {
				frozen = false;
				uid = uids++;
				ids.Clear();
				arguments.Clear();
				oldIdGroup = null;
				newIdGroup = null;
				dataGroup = null;
			}
		}

		public void Freeze() {
			if (!frozen) {
				frozen = true;
			}
		}

		public bool Frozen {
			get {
				return frozen;
			}
		}

		//public void AddLine(string plainText) {
		//    Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
		//    this.ids.Add(1042971); //1042971, 1070722 
		//    this.arguments.Add(plainText);
		//}

		public void AddLine(uint clilocId) {
			Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
			ids.Add(clilocId);
			arguments.Add(null);
		}

		public void AddLine(uint clilocId, string arg) {
			Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
			this.ids.Add(clilocId);
			this.arguments.Add(arg);
		}

		public void AddLine(uint clilocId, string arg0, string arg1) {
			Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
			this.ids.Add(clilocId);
			this.arguments.Add(string.Concat(arg0, "\t", arg1));
		}

		public void AddLine(uint clilocId, params string[] args) {
			Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
			this.ids.Add(clilocId);
			this.arguments.Add(string.Join("\t", args));
		}

		//0xbf 0x10 or 0xdc
		public void SendIdPacket(GameConn c) {
			if (c.Version.oldAosToolTips) {
				if (oldIdGroup == null) {
					BoundPacketGroup bpg = PacketSender.NewBoundGroup();
					PacketSender.PrepareOldPropertiesRefresh(thing, uid);
					oldIdGroup = bpg.Free();
				}
				oldIdGroup.SendTo(c);
			} else {
				if (newIdGroup == null) {
					BoundPacketGroup bpg = PacketSender.NewBoundGroup();
					PacketSender.PreparePropertiesRefresh(thing, uid);
					newIdGroup = bpg.Free();
				}
				newIdGroup.SendTo(c);
			}
		}

		public uint FirstId {
			get {
				return ids[0];
			}
		}

		public string FirstArgument {
			get {
				return arguments[0];
			}
		}

		//0xd6 - megacliloc
		public void SendDataPacket(GameConn c) {
			if (dataGroup == null) {
				BoundPacketGroup bpg = PacketSender.NewBoundGroup();
				PacketSender.PrepareMegaCliloc(thing, uid, ids, arguments);
				dataGroup = bpg.Free();
			}
			dataGroup.SendTo(c);
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