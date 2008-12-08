////This software is released under GNU internal license. See details in the URL: 
////http://www.gnu.org/copyleft/gpl.html 

//using System;
//using System.IO;
//using System.Runtime.Serialization;
//using SteamEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Sockets;
//using System.Diagnostics;
//using SteamEngine.Common;

//namespace SteamEngine.Packets {
	
//    /**
//        This is the parent class of PacketGroup and FreedPacketGroup. All of them have the same SendTo* methods.
//    */
//    public abstract class AbstractPacketGroup {
//        internal abstract bool HasPackets {
//            get;
//        }
//        public abstract bool IsDeleted {
//            get;
//        }
//        public abstract bool IsLocked {
//            get;
//        }
//        public abstract int Count {
//            get;
//        }
		
//        [Summary("This sends the packets in this PacketGroup to a specific GameConn.")]
//        public abstract void SendTo(GameConn conn);
//    }
	
//    /**
//        Example of usage:
//            using (PacketGroup packets = new PacketGroup()) {
//                PacketSender.PrepareOpenContainer(this);
//                if (Count>0) {
//                    PacketSender.PrepareContainerContents(this);
//                }
//                packets.SendTo(conn);
//            }
		
//        Basically, you create a group using a line like that 'using' line in the example, and then you
//        call Prepare* methods on PacketSender to get all your packets prepared, and then you use
//        the SendTo methods.
		
//        You can call Free() on a PacketGroup to get a FreedPacketGroup.
//    */
//    public sealed class BoundPacketGroup : AbstractPacketGroup, IDisposable {
//        internal List<int> packetStartPositions = new List<int>();
//        internal List<int> packetEndPositions = new List<int>();
//        internal List<int> packetSizes = new List<int>();
//#if DEBUG
//        internal List<byte> packetIds = new List<byte>();
//#endif
//        private bool deleted = false;
//        private bool locked = false;

//        public override bool IsDeleted {
//            get {
//                return deleted;
//            }
//        }
//        public override bool IsLocked {
//            get {
//                return locked;
//            }
//        }
//        internal void Locked() {
//            Sanity.IfTrueThrow(!HasPackets,"It would be rather pointless to lock a "+GetType()+" with no packets.");
//            locked=true;
//        }
//        internal void Deleted() {
//            deleted=true;
//        }

//        internal BoundPacketGroup() {
//        }

//        internal int End {
//            get {
//                Sanity.IfTrueThrow(!HasPackets, "This BoundPacketGroup has no packets, and thus has no data, and so it has no end-of-data position.");
//                return (int) packetEndPositions[Count-1];
//            }
//        }
//        public override int Count {
//            get {
//                return packetStartPositions.Count;
//            }
//        }
//        internal override bool HasPackets {
//            get {
//                return Count>0;
//            }
//        }
//#if DEBUG
//        internal void AddCompressedPacket(int startPos, int size, byte packetId) {
//            packetIds.Add(packetId);
//#else
//        internal void AddCompressedPacket(int startPos, int size) {
//#endif
//            packetStartPositions.Add(startPos);
//            packetEndPositions.Add(startPos+size);
//            packetSizes.Add(size);
//        }
//        public void Lock() {
//            if (!locked) {
//                BoundPacketGroup pg = PacketSender.LockGroup();
//                Sanity.IfTrueThrow(pg!=this, "This group wasn't locked, but calling PacketSender.LockGroup() returned a DIFFERENT group!");
//            }
//        }
//        public override void SendTo(GameConn conn) {
//            Lock();
//            int count = Count;
//            Logger.WriteInfo(PacketSender.PacketSenderTracingOn, "Group SendTo("+conn+"): Sending "+count+" packets.");
//            for (int packet=0; packet<count; packet++) {
//                PacketSender.SendCompressedBytes(conn, packetStartPositions[packet], packetSizes[packet]);
//            }
//        }
		
//        public FreedPacketGroup Free() {
//            FreedPacketGroup fpg = null;
//            //To be more efficient, we have a special FreedSPacketGroup class for groups with only one packet.
//            if (Count==1) {
//                fpg=new FreedSPacketGroup(this);
//            } else {
//                fpg=new FreedMPacketGroup(this);
//            }
//            PacketSender.DiscardGroup(this);
//            return fpg;
//        }
		
//        public void Dispose() {
//            if (!IsDeleted) {
//                PacketSender.DiscardGroup(this);
//            }
//        }
//    }
	
//    /**
//        You create one of these by calling Free() on a PacketGroup.
	
//        If you want to compress a packet or packets and then keep them to send (much) later to whoever you want,
//        FreedPacketGroups are an efficient way to do it.
		
//        Also there are actually two FreedPacketGroup classes, one for normal groups with multiple packets, and one
//        specifically for groups containing only one packet - you will get the appropriate one automatically and
//        you don't ever have to distinguish between them, but you'll know that if you use a single-packet group,
//        it will still be efficient. Having a class for single-packet groups ensures that these are just as
//        efficient (In fact, more efficient) in terms of memory usage than a conventional Packet object).
		
//        However, you can't recompile a PacketGroup if you want to change it, because it does not store its
//        original data, only the compressed data. For that, you would need a specialized Packet object
//        (or RepeatablePacket, if I code them, which would depend on whether I think we could gain
//        a performance/efficiency gain from having something like that for certain packets).
//    */
//    public abstract class FreedPacketGroup : AbstractPacketGroup {
				
//        public override bool IsDeleted {
//            get {
//                return false;
//            }
//        }
//        public override bool IsLocked {
//            get {
//                return true;
//            }
//        }
//        internal FreedPacketGroup(BoundPacketGroup pg) {
//        }
//    }
	
//    /**
//        This FPG is used when the PG it is created from has more than one packet.
//    */
//    public class FreedMPacketGroup : FreedPacketGroup {
//        protected int[] packetStartPositions;
//        protected int[] packetSizes;
//        private byte[] data;
//#if DEBUG
//        private byte[] packetIds;
//#endif
		
//        internal FreedMPacketGroup(BoundPacketGroup pg) : base(pg) {
//            Sanity.IfTrueThrow(pg.Count<1,"It would be rather pointless to create a FreedPacketGroup from a BoundPacketGroup with no packets.");
//            Sanity.IfTrueThrow(pg.Count==1,"FreedSPacketGroup should be being created on groups containing only a single packet.");
//            int start = pg.packetStartPositions[0];
//            int end = pg.packetEndPositions[pg.Count-1];
//            int size = end-start;
//            data = new byte[size];
//            Buffer.BlockCopy(PacketSender.cBuffer, start, data, 0, size);
//            int n = pg.Count;
//            packetStartPositions = new int[n];
//            packetSizes = new int[n];
//#if DEBUG
//            packetIds = new byte[n];
//#endif
//            for (int i=0; i<n; i++) {
//                packetStartPositions[i] = pg.packetStartPositions[i]-start;
//                packetSizes[i] = pg.packetSizes[i];
//#if DEBUG
//                packetIds[i] = pg.packetIds[i];
//#endif
//            }
//        }
//        public override int Count {
//            get {
//                return packetStartPositions.Length;
//            }
//        }
//        internal override bool HasPackets {
//            get {
//                return true;
//            }
//        }
		
//        public override void SendTo(GameConn conn) {
//            int count = Count;
//            Logger.WriteInfo(PacketSender.PacketSenderTracingOn, "Freed (multiple) Group SendTo("+conn+"): Sending "+count+" packets.");
//            for (int packet=0; packet<count; packet++) {
//#if DEBUG
//                Logger.WriteDebug("Sending packet 0x"+packetIds[packet].ToString("x")+" to "+conn);
//#endif
//                conn.Write(data, packetStartPositions[packet], packetSizes[packet]);
//            }
//        }
//    }
	
//    /**
//        This FPG is used when the PG it is created from has only one packet. This is more efficient
//        than a FreedMPacketGroup, and, in fact, is also more efficient than a Packet.
		
//        However, as with all FreedPacketGroups, you cannot modify the information and recompress it,
//        since the uncompressed data does not exist anymore and PGs and FPGs have no knowledge of how
//        to recreate it or any part of it - But that's one reason why these are more efficient than
//        Packet objects.
//    */
//    public class FreedSPacketGroup : FreedPacketGroup {
//        private byte[] data;
//#if DEBUG
//        private byte packetId;
//#endif
		
//        internal FreedSPacketGroup(BoundPacketGroup pg) : base(pg) {
//            Sanity.IfTrueThrow(pg.Count<1,"It would be rather pointless to create a FreedPacketGroup from a BoundPacketGroup with no packets.");
//            Sanity.IfTrueThrow(pg.Count>1,"FreedMPacketGroup should be being created on groups containing more than one packet.");
//            int start = pg.packetStartPositions[0];
//            int end = pg.packetEndPositions[0];
//            int size=end-start;
//            data = new byte[size];
//#if DEBUG
//            packetId = pg.packetIds[0];
//#endif
//            Buffer.BlockCopy(PacketSender.cBuffer, start, data, 0, size);
//        }
//        public override int Count {
//            get {
//                return 1;
//            }
//        }
//        internal override bool HasPackets {
//            get {
//                return true;
//            }
//        }
		
//        public override void SendTo(GameConn conn) {
//            //int count = Count;
//#if DEBUG
//            Logger.WriteDebug("Sending packet 0x"+packetId.ToString("x")+" to "+conn);
//#endif
//            Logger.WriteInfo(PacketSender.PacketSenderTracingOn, "Freed (single) Group SendTo("+conn+"): Sending 1 packet.");
//            conn.Write(data, 0, data.Length);
//        }
//    }
//}