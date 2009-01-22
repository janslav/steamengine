////This software is released under GNU internal license. See details in the URL: 
////http://www.gnu.org/copyleft/gpl.html 

//using System;
//using System.IO;
//using System.Runtime.Serialization;
//using SteamEngine;
//using System.Collections;
//using System.Net;
//using System.Net.Sockets;
//using SteamEngine.Common;

//namespace SteamEngine.Packets {

//    public class HighLevelOut : MediumLevelOut {
//        //See docs/plans/packetsender.txt

//        static HighLevelOut() {}

//        //----------------- Internal Methods, used for PacketGroups, but now only used internally -----------------

//        public static BoundPacketGroup NewBoundGroup() {
//            BoundPacketGroup bpg = new BoundPacketGroup();
//            if (PacketSender.groupState == GroupState.Open) {
//                PacketSender.LockGroup();
//            }
//            Sanity.IfTrueThrow(PacketSender.groupState!=GroupState.Ready, "When making a new BoundPacketGroup, we expected a group state of Ready or Open, but it is "+PacketSender.groupState+" instead.");
//            PacketSender.curGroup = bpg;
//            PacketSender.groupState = GroupState.Open;
//            return bpg;
//        }

//        /**
//            This locks the group so it can be sent, and returns a reference to the group.
//            This is called by PacketGroup the first time it is told to send the group,
//            or when you call Lock() on the PacketGroup.

//            You shouldn't ever need to call this yourself.
//        */
//        internal static BoundPacketGroup LockGroup() {
//            //Requres groupState == Open, sets groupState=Ready, adds curGroup to groups, sets
//            //lastGroupEnd=lastCPacketEnd, returns the ID# of the group.
//            Sanity.IfTrueThrow(groupState!=GroupState.Open,"LockGroup() called when there is no group open.");
//            groupState=GroupState.Ready;
//            lastGroupEnd=lastCPacketEnd;
//            groups.Add(curGroup);
//            curGroup.Locked();
//            return curGroup;
//        }

//        /**
//            This discards a specific PacketGroup.
//            You shouldn't ever need to call this yourself. Either make the group with a 'using' statement
//            or call Dispose() on the group when you're done with it.

//            This can't be used if you have a single packet blocking
//            operations (i.e. If you had no open groups and called a Prepare* method). In that case, you would
//            have to call DiscardLastPacket to discard that packet before any groups could be discarded.
//        */
//        internal static void DiscardGroup(BoundPacketGroup group) {
//            Sanity.IfTrueThrow(groupState!=GroupState.Open && groupState!=GroupState.Ready,"LockGroup() called when there is no group open.");
//            Sanity.IfTrueThrow(group.IsDeleted, "You just asked to discard a group which has already been discarded.");
//            if (groupState==GroupState.Open) {
//                if (group==curGroup) {
//                    curGroup.Deleted();
//                    groupState=GroupState.Ready;
//                    while (groups.Count>0) {
//                        int lastGroupIndex=groups.Count-1;
//                        if (groups[lastGroupIndex]==null) {	//Should not happen: || !groups[lastGroupIndex].HasPackets
//                            groups.RemoveAt(lastGroupIndex);
//                        } else {
//                            lastCPacketEnd=(groups[lastGroupIndex]).End;	//A SanityCheck in there will trigger if it has no packets, but that shouldn't happen (but that's why it's a sanity check :P)
//                            break;
//                        }
//                    }
//                    if (groups.Count==0) {
//                        lastCPacketEnd=0;
//                    }
//                } else {
//                    Sanity.IfTrueThrow(true, "Until you lock the group you've opened (or free it, or discard it), you can't discard any other groups.");
//                }
//            } else if (groupState==GroupState.SingleBlocking) {
//                throw new SanityCheckException("Sorry, you can't discard any groups while there is a single packet blocking operations.");
//            } else if (groupState==GroupState.Ready) {
//                Sanity.IfTrueThrow(groups.Count==0, "There aren't any packet groups to discard!");
//                int index=groups.IndexOf(group);
//                Sanity.IfTrueThrow(index==-1, "You just asked to delete a group which we don't know anything about, yet it isn't marked deleted, and our groupState isn't Open...");
//                groups[index]=null;
//                group.Deleted();
//                while (groups.Count>0) {
//                    int lastGroupIndex=groups.Count-1;
//                    if (groups[lastGroupIndex]==null) {	//Should not happen: || !groups[lastGroupIndex].HasPackets
//                        groups.RemoveAt(lastGroupIndex);
//                    } else {
//                        lastCPacketEnd=(groups[lastGroupIndex]).End;	//A SanityCheck in there will trigger if it has no packets, but that shouldn't happen (but that's why it's a sanity check :P)
//                        break;
//                    }
//                }
//                if (groups.Count==0) {
//                    lastCPacketEnd=0;
//                }
//            }
//        }	
//    }
//}