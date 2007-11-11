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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Configuration;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Regions;
/*
	Status of asynch & synch testing code:
		AsyncSend:		Written. Tested. Appears to be working.
		SyncSend:		Written. Tested. Appears to be working.
		Send/Write/Etc:	Written. Tested. Appears to be working.
		
		HasData:		Written. Tested. Appears to be working.
		Read:			Written. Tested. Appears to be working.
		AsyncRead:		Written. Tested. Appears to be working.
		SyncRead:		Written. Tested. Appears to be working.
		
		Statistics collection: Written. Undergoing testing.
		Statistics display: Written. Undergoing testing.
		
		-SL
*/

namespace SteamEngine {

	public delegate void OnTargon(GameConn conn, IPoint3D getback, object parameter);
	public delegate void OnTargon_Cancel(GameConn conn, object parameter);

	public class GameConn : Conn {
		public static bool GameConnTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["GameConn Trace Messages"]);
		public static bool MovementTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Movement Trace Messages"]);

		private static Statistics stats;
		private static StatType statsSyncIn;
		private static StatType statsAsyncIn;
		private static StatType statsSyncOut;
		private static StatType statsAsyncOut;

		//These numbers are based on me running/walking/etc in game and recording the "Time between movement"
		//output of the movement info stuff. They're slightly higher than the highest values I saw,
		//which means that like this they should slow all clients to the same rate, so perhaps different
		//clients with different graphics-drawing speeds will actually move at the same rates instead of
		//at different ones. Or maybe not, since I have a pretty good graphics card. (SL)
		private readonly long RunStepTime = HighPerformanceTimer.SecondsToTicks(0.18);
		//private readonly long WalkStepTime = HighPerformanceTimer.SecondsToTicks(0.35);
		private readonly long RidingRunStepTime = HighPerformanceTimer.SecondsToTicks(0.09);
		//private readonly long RidingWalkStepTime = HighPerformanceTimer.SecondsToTicks(0.18);

		//If I'm reading Wolfpack's code correctly, someone could hack their client to walk at run speeds in Wolfpack,
		//and thus not pay the stamina cost of running, since they only check mountedness, not whether the client
		//is running (This is in 12.9.8 though, I haven't downloaded any newer versions' source yet).

		//I think they got it right. After slowing down to walk speed, the client sends the first walk packet too fast.
		//Or at least that's what I've found out. So I too now check only mountednesss... Who cares about stamina anyway ;) -tar

		static GameConn() {
			stats = new Statistics("Game Connections");
			stats.MaxEntries=5000;

			statsSyncIn = stats.AddType("Synchronous server<-client (reading)");
			statsAsyncIn = stats.AddType("Asynchronous server<-client (reading)");
			statsSyncOut = stats.AddType("Synchronous server->client (writing)");
			statsAsyncOut = stats.AddType("Asynchronous server->client (writing)");

			statsSyncOut.Note("For synchronous writing, 'Time blocked' and 'Time until sent' and 'Time of actual send operation' are the same by nature.");
			statsSyncIn.Note("For synchronous reading, 'Time blocked in Read' and 'Time taken to receive' are the same by nature.");

			statsSyncOut.Value("L", "Bytes");
			statsSyncOut.Value("L", "Packets");
			statsSyncOut.Value("M5", "Time blocked");
			statsSyncOut.Value("M5", "Time until sent");
			statsSyncOut.Value("M5", "Time of actual send operation");
			statsAsyncOut.SameValuesAs(statsSyncOut);

			statsSyncIn.Value("L", "Bytes");
			statsSyncIn.Value("L", "Packets");
			statsSyncIn.Value("M5", "Time blocked in Read");
			statsSyncIn.Value("M5", "Time taken to receive");
			statsSyncIn.Value("M5", "Time blocked in HasData");
			statsAsyncIn.SameValuesAs(statsSyncIn);

			statsSyncOut.Rate(5, "Time blocked", "bytes", "Milliseconds that we were blocked per byte sent. (less is better)");
			statsSyncOut.Rate(5, "Time blocked", "packets", "Milliseconds that we were blocked per packet sent. (less is better)");
			statsSyncOut.Rate(5, "Time until sent", "bytes", "Milliseconds it takes to send a byte, from the time we request the send until the time the sending is complete (less is better)");
			statsSyncOut.Rate(5, "Time until sent", "packets", "Milliseconds it takes to send a packet, from the time we request the send until the time the sending is complete (less is better)");
			statsSyncOut.Rate(5, "Time of actual send operation", "bytes", "Milliseconds spent in the Send method, per byte (averaged) (less is better)");
			statsSyncOut.Rate(5, "Time of actual send operation", "packets", "Milliseconds spent in the Send method, per packet (averaged) (less is better)");

			statsAsyncOut.Rate(5, "Time blocked", "bytes", "Milliseconds that we were blocked per byte sent. (less is better)");
			statsAsyncOut.Rate(5, "Time blocked", "packets", "Milliseconds that we were blocked per packet sent. (less is better)");
			statsAsyncOut.Rate(5, "Time until sent", "bytes", "Milliseconds it takes to send a byte, from the time we request the send until the time the sending is complete (less is better)");
			statsAsyncOut.Rate(5, "Time until sent", "packets", "Milliseconds it takes to send a packet, from the time we request the send until the time the sending is complete (less is better)");
			statsAsyncOut.Rate(5, "Time of actual send operation", "bytes", "Milliseconds spent in the Send method, per byte (averaged) (less is better)");
			statsAsyncOut.Rate(5, "Time of actual send operation", "packets", "Milliseconds spent in the Send method, per packet (averaged) (less is better)");

			//13:59: Info: Adding to Synchronous server<-client (reading), rate count=5 var1=Time taken to receive var2=bytes description=Milliseconds it takes to send a byte, from the time we request the send until the time the sending is complete (less is better)
			statsSyncIn.Rate(5, "Time blocked in Read", "bytes", "Milliseconds that we were blocked in the read method per byte received. (less is better)");
			statsSyncIn.Rate(5, "Time blocked in Read", "packets", "Milliseconds that we were blocked in the read method per packet received. (less is better)");
			statsSyncIn.Rate(5, "Time taken to receive", "bytes", "Milliseconds it takes to receive a byte (less is better)");
			statsSyncIn.Rate(5, "Time taken to receive", "packets", "Milliseconds it takes to receive a packet (less is better)");
			statsSyncIn.Rate(5, "Time blocked in HasData", "bytes", "Milliseconds spent in the Send method, per byte (averaged) (less is better)");
			statsSyncIn.Rate(5, "Time blocked in HasData", "packets", "Milliseconds spent in the Send method, per packet (averaged) (less is better)");

			statsAsyncIn.Rate(5, "Time blocked in Read", "bytes", "Milliseconds that we were blocked in the read method per byte received. (less is better)");
			statsAsyncIn.Rate(5, "Time blocked in Read", "packets", "Milliseconds that we were blocked in the read method per packet received. (less is better)");
			statsAsyncIn.Rate(5, "Time taken to receive", "bytes", "Milliseconds it takes to receive a byte (less is better)");
			statsAsyncIn.Rate(5, "Time taken to receive", "packets", "Milliseconds it takes to receive a packet (less is better)");
			statsAsyncIn.Rate(5, "Time blocked in HasData", "bytes", "Milliseconds spent in HasData doing code specifiy to async or sync, per byte (averaged) (less is better)");
			statsAsyncIn.Rate(5, "Time blocked in HasData", "packets", "Milliseconds spent in HasData doing code specifiy to async or sync, per packet (averaged) (less is better)");
		}

		private AbstractCharacter curCharacter;
		public bool noResponse;
		public bool justConnected;
		internal byte moveSeqNum;
		internal byte reqMoveSeqNum;
		//public byte[] initCode = new byte [4];
		public const uint maxTargs = 1;
		public OnTargon[] targon = new OnTargon[maxTargs];
		public OnTargon_Cancel[] targon_cancel = new OnTargon_Cancel[maxTargs];
		public object[] targon_parameters = new object[maxTargs];
		public ushort nextTarg = 0;
		//public FastWalkStack FastWalk;
		public string lang = "enu";
		private Dictionary<uint, GumpInstance> gumpInstancesByUid = new Dictionary<uint, GumpInstance>();
		private Dictionary<Gump, LinkedList<GumpInstance>> gumpInstancesByGump = new Dictionary<Gump, LinkedList<GumpInstance>>();
		internal HashSet<AbstractItem> openedContainers = new HashSet<AbstractItem>();

		private bool godMode = false;
		private bool hackMove = false;
		internal ClientVersion clientVersion = ClientVersion.nullValue;
		//const int MaxMoveRequests = 4;
		private Queue<byte> moveRequests = new Queue<byte>();
		private long lastStepReserve = 0;
		private long secondLastStepReserve = 0;
		private long thirdLastStepReserve = 0;
		private long lastMovementTime = 0;
		private long nextMovementTime = 0;
		internal Encryption encryption = new Encryption();

		private byte updateRange=18;
		private int visionRange=18;	//for scripts to fiddle with
		private byte requestedUpdateRange=18;

		internal bool restoringUpdateRange=false;	//The client, if sent an update range packet on login, apparently then requests 18 again, but does it after sending some other packets - this sure makes it hard for the client to not have to set the update range every time they login... So we block the next update range packet if we get this, but the block is removed if we get a 0x73 (ping) first. We can't remove it for any incoming packets becaues the client sends other stuff before it gets around to sending this.

		public GameConn(Socket s)
			: base(s) {
			curAccount= null;
			curCharacter= null;
			justConnected = true;
			moveSeqNum = 0;
			reqMoveSeqNum = 0;
			noResponse = true;		//Close in 60 seconds if they do nothing.
		}

		protected GameConn()
			: base() {

		}

		public bool GodMode {
			get {
				return godMode;
			}
			set {
				godMode=value;
				Packets.Prepared.SendGodMode(this);
			}
		}
		public bool HackMove {
			get {
				return hackMove;
			}
			set {
				hackMove=value;
				Packets.Prepared.SendHackMove(this);
			}
		}
		//private Hashtable openedContainers = new Hashtable();

		internal byte RequestedUpdateRange {
			get {
				return requestedUpdateRange;
			}
			set {
				if (restoringUpdateRange) {
					restoringUpdateRange=false;
					return;
				}
				requestedUpdateRange=value;
				byte oldUpdateRange=updateRange;
				RecalcUpdateRange();
				if (CurCharacter!=null && oldUpdateRange!=updateRange) {
					PacketSender.SendUpdateRange(this, updateRange);
				}
			}
		}

		internal int VisionRange {
			get {
				return visionRange;
			}
			set {
				visionRange=value;
				byte oldUpdateRange=updateRange;
				RecalcUpdateRange();
				if (CurCharacter!=null && oldUpdateRange!=updateRange) {
					PacketSender.SendUpdateRange(this, updateRange);
				}
			}
		}
		public byte UpdateRange {
			get {
				return updateRange;
			}
		}
		private void RecalcUpdateRange() {
			if (visionRange<=requestedUpdateRange) {
				if (visionRange<0) {
					updateRange=0;
				} else if (visionRange>Globals.MaxUpdateRange) {
					updateRange=Globals.MaxUpdateRange;
				} else {
					updateRange=(byte) visionRange;
				}
			} else {
				updateRange=requestedUpdateRange;
			}
		}

		public void DecreaseVisionRangeBy(int amount) {
			VisionRange-=amount;
		}
		public void IncreaseVisionRangeBy(int amount) {
			VisionRange+=amount;
		}

		public AbstractCharacter CurCharacter {
			get {
				return curCharacter;
			}
		}

		public ClientVersion Version {
			get {
				return clientVersion;
			}
		}

		public Encryption Encryption {
			get {
				return encryption;
			}
		}

		internal byte MoveSeqNumToSend() {
			byte retVal = moveSeqNum; // (byte) (moveSeqNum - moveRequests.Count);

			if (moveSeqNum==255) {
				Logger.WriteInfo(MovementTracingOn, "moveSeqNum wraps around to 1.");
				moveSeqNum=1;
			} else {
				moveSeqNum++;
			}

			return retVal;
		}

		//Exists only for convenience.
		//private void DisconnectOSI3DClient() {
		//	Server.SendServerMessage(this, "This server does not permit use of the OSI 3D client. "
		//		+"You must use the 2D client instead, or an alternative client such as UO Iris or PlayUO.", 60);
		//	Close("Disconnecting OSI 3D client - not allowed");
		//}

		//may be called from within lock()ed code, so don't lock InSyncRoot or OutSyncRoot here!
		public override void Close(string reason) {

			AbstractAccount accountToLogout = curAccount;
			AbstractCharacter charToLogout = curCharacter;
			// this is necessary, because AbstractCharacter.LogOut()
			// is calling this method recursively

			base.Close(reason);
			Server.RemoveConn(this);

			if (charToLogout != null) {
				if (!charToLogout.IsDeleted) {
					charToLogout.LogOut();
				}
				curCharacter = null;
			}
			if (accountToLogout != null) {
				curAccount = null;
				accountToLogout.LogOut();
			}
		}

		public override void Close(LogStr reason) {
			AbstractAccount accountToLogout = curAccount;
			AbstractCharacter charToLogout = curCharacter;

			base.Close(reason);
			Server.RemoveConn(this);

			if (curCharacter != null) {
				if (!charToLogout.IsDeleted) {
					charToLogout.LogOut();
				}
				curCharacter=null;
			}
			if (curAccount != null) {
				curAccount=null;
				accountToLogout.LogOut();
			}
		}

		public override bool IsLoggedIn {
			get {
				return (curAccount != null);
			}
		}

		internal override void Cycle() {
			if (HasData) {
				Server._in.Cycle(this);
			}
			if (curCharacter != null) {
				if (moveRequests.Count>0) {
					ProcessMovement(moveRequests.Dequeue());
				}
			}
		}

		internal void MovementRequest(byte dir) {
			if (moveRequests.Count > 0) {
				moveRequests.Enqueue(dir);
			} else {
				ProcessMovement(dir);
			}
		}

		private void ProcessMovement(byte dir) {
			if (curCharacter==null) return;

			bool running = false;
			Direction direction = (Direction) dir;
			if ((dir&0x80)==0x80) {
				direction = (Direction) (dir - 0x80);
				running = true;
			}

			if (direction != curCharacter.Direction) {//no speedcheck if we're just changing direction
				if (moveRequests.Count == 0) {
					Movement(direction, running);
				}
				return;
			}

			if ((CanMoveAgain()) || (Plevel >= Globals.plevelOfGM)) {
				Movement(direction, running);
			} else {
				moveRequests.Enqueue(dir);
			}
		}

		private bool CanMoveAgain() {
			long currentTime = HighPerformanceTimer.TickCount;
			long diff = currentTime - nextMovementTime;

			Logger.WriteInfo(MovementTracingOn, "Time between movement = "+HighPerformanceTimer.TicksToSeconds(currentTime-lastMovementTime));

			long reserves = lastStepReserve + secondLastStepReserve + thirdLastStepReserve;

			if (diff + reserves >= 0) {
				Logger.WriteInfo(MovementTracingOn, "Time later than allowed = "+HighPerformanceTimer.TicksToSeconds(diff + reserves));

				if (curCharacter.Flag_Riding) {
					nextMovementTime = (currentTime + RidingRunStepTime);
				} else {
					nextMovementTime = (currentTime + RunStepTime);
				}
				thirdLastStepReserve = secondLastStepReserve;
				secondLastStepReserve = lastStepReserve;
				lastStepReserve = Math.Max(diff, 0);
				lastMovementTime = currentTime;//...because sometimes the client tends to send more steps at once (or it looks like that), but it still isn't speedhacking
				return true;
			} else {
				////Logger.WriteInfo(MovementTracingOn, "Delaying movement, the packet is "+seconds+" seconds early.");
				//if (MovementTracingOn) { 
				//    //we need not spam the console with warnings. Players simply can't speedhack, end of story.
				//    Logger.WriteWarning("The client "+LogStr.Ident(this)+" is moving too fast. The packet came "+
				//        LogStr.Number(HighPerformanceTimer.TicksToSeconds(-(diff + reserves)))+" s earlier than allowed.");
				//}
				return false;
			}
		}

		private void Movement(Direction dir, bool running) {
			Logger.WriteInfo(MovementTracingOn, "Moving.");
			bool success=curCharacter.WalkRunOrFly((Direction) dir, running, true);	//we don't want to get a 0x77 for ourself, our client knows we're moving if it gets the verify move packet.
			if (success) {
				PacketSender.SendGoodMove(this);
				NetState.ProcessThing(curCharacter);//I think this is really needed. We can't wait to the end of the cycle, because movement 
				//should be as much synced between clients as possible
			} else {
				PacketSender.SendBadMove(this);
				return;
			}
		}

		public object InSyncRoot { get { return waitingInPacketData; } }	//we could return their SyncRoot instead, but their SyncRoot is themselves, so that would be pointless.
		public object OutSyncRoot { get { return waitingOutPacketData; } }

		private bool readIsAsync = false;
		private bool readIsUndefined = true;
		private AsyncOutPacketInfo curOutPacket;
		private AsyncInPacketInfo curInPacket;

		//Set in HasData just before calling SyncHasData
		private static long timeEndedSyncHasData;		//reset every time we do a sync HasData

		//Set in HasData just after calling SyncHasData, just before returning from HasData.
		private static long timeStartedSyncHasData;		//reset every time we do a sync HasData


		private class AsyncOutPacketInfo {
			private byte[] data;
			private long callStarted;	//Set to the time just before we called AsyncSend.
			private long finishedSend;	//Set just after EndSend finishes.
			private long beginningSend;	//Set just before calling BeginSend. (There are two places that can be called from)
			private long doneCalling;	//Set just after calling AsyncSend.
			private long byteCount;
			public AsyncOutPacketInfo(byte[] data, long callStarted) {
				this.data=data; this.callStarted=callStarted;
			}
			public long FinishedSend { set { finishedSend=value; } }
			public long BeginningSend { set { beginningSend=value; } }
			public long DoneCalling { set { doneCalling=value; } }
			public long ByteCount { set { byteCount=value; } get { return byteCount; } }
			public byte[] Data {
				get {
					return data;
				}
				set {
					data=value;
				}
			}
			public long TimeBlocked {
				get {
					Sanity.IfTrueThrow(doneCalling==0, "doneCalling was never set on this AsyncOutPacketInfo!");
					Sanity.IfTrueThrow(callStarted==0, "callStarted was never set on this AsyncOutPacketInfo!");
					return doneCalling-callStarted;
				}
			}
			public long TimeUntilSent {
				get {
					Sanity.IfTrueThrow(finishedSend==0, "finishedSend was never set on this AsyncOutPacketInfo!");
					Sanity.IfTrueThrow(callStarted==0, "callStarted was never set on this AsyncOutPacketInfo!");
					return finishedSend-callStarted;
				}
			}
			public long TimeOfActualSendOperation {
				get {
					Sanity.IfTrueThrow(finishedSend==0, "finishedSend was never set on this AsyncOutPacketInfo!");
					Sanity.IfTrueThrow(beginningSend==0, "beginningSend was never set on this AsyncOutPacketInfo!");
					return finishedSend-beginningSend;
				}
			}
		}
		private class AsyncInPacketInfo {
			private byte[] data;
			private GameConn gc;
			private long finishedGet;	//set in DoneRead after everything's done and in an appropriate buffer, etc
			private long beginningGet;	//set in HasData before calling BeginRetreive
			private long collected;		//set in Read after AsyncRead returns.
			private long hasDataStart;	//set in HasData just inside if (readIsAsync) {
			private long hasDataEnd;	//set in HasData just before returning from it from the async section.
			private long byteCount;
			private long startRetrieve;	//Set in Read before calling AsyncRead (Technically the time is recorded before, but the value is set here to that time after, because we don't have access to this object before.
			public AsyncInPacketInfo(GameConn gc, byte[] data) {
				this.gc=gc;
				this.data=data;
			}
			public GameConn GC { get { return gc; } }
			public long FinishedGet { set { finishedGet=value; } }
			public long BeginningGet { set { beginningGet=value; } }
			public long Collected { set { collected=value; } }
			public long HasDataStart { set { hasDataStart=value; } }
			public long HasDataEnd { set { hasDataEnd=value; } }
			public long ByteCount { set { byteCount=value; } get { return byteCount; } }
			public long StartRetrieve { set { startRetrieve=value; } }

			public byte[] Data {
				get {
					return data;
				}
				set {
					data=value;
				}
			}
			public long HasDataTimeBlocked {
				get {
					Sanity.IfTrueThrow(hasDataEnd==0, "hasDataEnd was never set on this AsyncOutPacketInfo!");
					Sanity.IfTrueThrow(hasDataStart==0, "hasDataStart was never set on this AsyncOutPacketInfo!");
					return (hasDataEnd-hasDataStart);
				}
			}
			public long ReadTimeBlocked {
				get {
					Sanity.IfTrueThrow(collected==0, "collected was never set on this AsyncOutPacketInfo!");
					Sanity.IfTrueThrow(startRetrieve==0, "startRetrieve was never set on this AsyncOutPacketInfo!");
					return (collected-startRetrieve);
				}
			}
			public long TimeUntilReceived {
				get {
					Sanity.IfTrueThrow(finishedGet==0, "finishedGet was never set on this AsyncOutPacketInfo!");
					Sanity.IfTrueThrow(beginningGet==0, "beginningGet was never set on this AsyncOutPacketInfo!");
					return finishedGet-beginningGet;
				}
			}
		}
		private static void AddAsyncStats(AsyncOutPacketInfo info) {
			StatEntry entry=statsAsyncOut.NewEntry();
			entry["bytes"]=info.ByteCount;
			entry["packets"]=1;
			entry["Time blocked"]=info.TimeBlocked;
			entry["Time until sent"]=info.TimeUntilSent;	//includes time spent waiting for other packets to be sent first
			entry["Time of actual send operation"]=info.TimeOfActualSendOperation;
			statsAsyncOut.AddEntry(entry);
		}
		private static void AddAsyncStats(AsyncInPacketInfo info) {
			StatEntry entry=statsAsyncIn.NewEntry();
			entry["bytes"]=info.ByteCount;
			entry["packets"]=1;
			entry["Time blocked in read"]=info.ReadTimeBlocked;
			entry["Time taken to receive"]=info.TimeUntilReceived;
			entry["Time blocked in HasData"]=info.HasDataTimeBlocked;
			statsAsyncIn.AddEntry(entry);
		}
		private static void AddSyncInStats(long start, long end, long bytes) {
			long timeTaken=end-start;
			long hasTimeTaken=timeEndedSyncHasData-timeStartedSyncHasData;
			StatEntry entry=statsSyncIn.NewEntry();
			entry["bytes"]=bytes;
			entry["packets"]=1;
			entry["Time blocked in read"]=timeTaken;
			entry["Time taken to receive"]=timeTaken;
			entry["Time blocked in HasData"]=hasTimeTaken;
			statsSyncIn.AddEntry(entry);

		}
		private static void AddSyncOutStats(long start, long end, long bytes) {
			long timeTaken=end-start;
			StatEntry entry=statsSyncOut.NewEntry();
			entry["bytes"]=bytes;
			entry["packets"]=1;
			entry["Time blocked"]=timeTaken;
			entry["Time until sent"]=timeTaken;
			entry["Time of actual send operation"]=timeTaken;
			statsSyncOut.AddEntry(entry);
		}

		public static void NetStats() {
			stats.ShowAllStats(true, true);
		}

		private static AsyncCallback DoneReadCallback = new AsyncCallback(DoneRead);


		private static void DoneRead(IAsyncResult ar) {
			Logger.WriteInfo(GameConnTracingOn, "DoneRead.");
			AsyncInPacketInfo info = (AsyncInPacketInfo) ar.AsyncState;
			GameConn gc = (GameConn) info.GC;
			gc.curInPacket = info;
			lock (gc.InSyncRoot) {
				Sanity.IfTrueSay(gc.readIsUndefined==true, "Why did DoneReadCallback get called if we weren't reading anything?");
				Sanity.IfTrueSay(gc.readIsAsync!=true, "Why did DoneReadCallback get called if we were not reading asynchronously?");
				int read=0;
				try {
					read = gc.client.EndReceive(ar);
				} catch (ObjectDisposedException) {
					gc.Close("Connection lost");
					return;
				} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
					gc.Close("Connection lost");
					return;
				} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
					gc.Close("Connection lost");
					return;
				}
				if (read==gc.inBuffer.Length && gc.SyncHasData) {	//Hmm! Read more!
					byte[] newBuffer = new byte[read+read];
					Logger.WriteInfo(GameConnTracingOn, "Unfinished read, boosting buffer size to "+(newBuffer.Length));
					Buffer.BlockCopy(gc.inBuffer, 0, newBuffer, 0, read);
					try {
						gc.client.BeginReceive(gc.inBuffer, read, read, SocketFlags.None, DoneReadCallback, gc);	//yep, start at 'read' and read 'read' bytes, because we just doubled the size of our completely full array which was 'read' bytes long...
					} catch (ObjectDisposedException) {
						gc.Close("Connection lost");
					} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
						gc.Close("Connection lost");
					} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
						gc.Close("Connection lost");
					}
					return;
				} else {
					Logger.WriteInfo(GameConnTracingOn, "Return data.");
					byte[] newBuffer = new byte[read];	//We're making and dropping arrays all over the place, so we might as well do it here too instead of trying to store the length, which would make more sense if you ask me -- but very little of this makes any logical sense to me... -SL

					//Oooh, we can reuse our original buffer now! That did make sense, in a twisted bizzaro-world sort of way! -SL
					Buffer.BlockCopy(gc.inBuffer, 0, newBuffer, 0, read);
					info.Data=newBuffer;
					gc.waitingInPacketData.Enqueue(info);
					info.FinishedGet=HighPerformanceTimer.TickCount;
				}
			}
		}
		private bool SyncHasData {
			get {
				//Logger.WriteInfo(GameConnTracingOn, "SyncHasData.");
				try {
					return client.Poll(0, SelectMode.SelectRead);
				} catch (Exception) {
					return false;
				}
			}
		}

		//Change this to disable sync or async easily.
		private bool GetRandAsync() {
			return false;
			//return (random.NextDouble()>.5);
		}
		private bool HasData {
			get {
				//Logger.WriteInfo(GameConnTracingOn, "HasData.");
				lock (InSyncRoot) {
					if (waitingInPacketData.Count>0) {
						Logger.WriteInfo(GameConnTracingOn, "Data is waiting.");
						return true;
					} else {
						if (readIsUndefined) {
							readIsAsync=GetRandAsync();
							readIsUndefined=false;
							Logger.WriteInfo(GameConnTracingOn, "Read undef, picked "+readIsAsync+".");
							if (readIsAsync) {
								if (inBuffer==null) inBuffer = new byte[1024];	//We will resize it later if it's too small. :O
								AsyncInPacketInfo info = new AsyncInPacketInfo(this, inBuffer);
								info.HasDataStart=HighPerformanceTimer.TickCount;
								try {
									info.BeginningGet=HighPerformanceTimer.TickCount;
									client.BeginReceive(inBuffer, 0, inBuffer.Length, SocketFlags.None, DoneReadCallback, info);
								} catch (ObjectDisposedException) {
									Close("Connection lost");
									return false;
								} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
									Close("Connection lost");
									return false;
								} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
									Close("Connection lost");
									return false;
								}
								info.HasDataEnd=HighPerformanceTimer.TickCount;
								return false;
							} else {
								timeStartedSyncHasData=HighPerformanceTimer.TickCount;
								bool b = SyncHasData;
								timeEndedSyncHasData=HighPerformanceTimer.TickCount;
								return b;

							}
						} else {
							if (readIsAsync) {
								return false;
							} else {
								return SyncHasData;
							}
						}
					}
				}
			}
		}

		public int Read(ref byte[] array, int start, int len) {
			Logger.WriteInfo(GameConnTracingOn, "Read.");
			bool async=false;
			lock (InSyncRoot) {
				async=(waitingInPacketData.Count>0);
			}
			int ret=0;
			if (async) {
				lock (InSyncRoot) {
					long asyncStart=HighPerformanceTimer.TickCount;
					ret=AsyncRead(ref array, start, len);
					curInPacket.StartRetrieve=asyncStart;
					curInPacket.ByteCount=ret;
					curInPacket.Collected=HighPerformanceTimer.TickCount;
					AddAsyncStats(curInPacket);	//add stats
				}
			} else {
				long timeStartedSyncRead=HighPerformanceTimer.TickCount;
				ret=SyncRead(ref array, start, len);
				long timeEndedSyncRead=HighPerformanceTimer.TickCount;
				AddSyncInStats(timeStartedSyncRead, timeEndedSyncRead, ret); //add stats
			}
			return ret;
		}

		private int SyncRead(ref byte[] array, int start, int len) {
			Logger.WriteInfo(GameConnTracingOn, "SyncRead.");
			try {
				lock (InSyncRoot) {
					readIsUndefined=true;
					return (client.Receive(array, start, len, SocketFlags.None));
				}
			} catch (Exception) {
				return -1;
			}
		}
		private int AsyncRead(ref byte[] array, int start, int len) {
			Logger.WriteInfo(GameConnTracingOn, "AsyncRead.");
			lock (InSyncRoot) {
				Sanity.IfTrueSay(waitingInPacketData.Count==0, "Why are we in AsyncRead if there is no async data done reading? HasData should have returned false!");
				AsyncInPacketInfo info = (AsyncInPacketInfo) waitingInPacketData.Dequeue();
				curInPacket=info;
				byte[] buff = (byte[]) info.Data;
				array=buff;	//point it at our buffer instead!
				if (waitingInPacketData.Count==0) {
					readIsUndefined=true;
				}

				return buff.Length;
			}
		}

		internal override sealed void LogIn(AbstractAccount acc) {
			base.LogIn(acc);
			for (int a=0; a<maxTargs; a++) {
				targon[a]=null;
			}
		}

		public void Target(bool ground, OnTargon targon, OnTargon_Cancel targCancel, object targon_parameter) {
			this.targon[0]=targon;
			this.targon_cancel[0]=targCancel;
			this.targon_parameters[0]=targon_parameter;
			Packets.Prepared.SendTargettingCursor(this, ground);
		}

		public void TargetForMultis(int model, OnTargon targon, OnTargon_Cancel targCancel, object targon_parameter) {
			this.targon[0]=targon;
			this.targon_cancel[0]=targCancel;
			this.targon_parameters[0]=targon_parameter;
			PacketSender.PrepareTargettingCursorForMultis(model);
			PacketSender.SendTo(this, true);
		}

		public void ClearTarget(int targNum) {
			if (targNum>=0 && targNum<maxTargs) {
				targon[targNum]=null;
			}
		}

		public void HandleTarget(byte targGround, int uid, ushort x, ushort y, sbyte z, ushort dispId) {
			int targNum=0;
			Logger.WriteDebug("HandleTarget: TG="+targGround+" uid="+uid+" x="+x+" y="+y+" z="+z+" dispId="+dispId);
			//figure out what it is
			OnTargon targpoint4d = this.targon[targNum];
			object parameter = this.targon_parameters[targNum];
			targpoint4d = null;
			if (x==0xffff && y==0xffff && uid==0 && z==0 && dispId==0) {
				//cancel
				this.ClearTarget(targNum);
				OnTargon_Cancel targcancel = this.targon_cancel[targNum];
				if (targcancel!=null) {
					targcancel(this, parameter);
				}
				return;
			} else {
				if (targGround==0) {
					uid = Thing.UidClearFlags(uid);
					Thing thing = Thing.UidGetThing(uid);
					if (thing!=null) {
						OnTargon targ = this.targon[targNum];
						this.ClearTarget(targNum);
						if (targ!=null) {
							targ(this, thing, parameter);
							return;
						}
					}
				} else {
					if (dispId==0) {
						OnTargon targ = this.targon[targNum];
						this.ClearTarget(targNum);
						if (targ!=null) {
							targ(this, new Point3D(x, y, z), parameter);
							return;
						}
					} else {
						Map map = this.curCharacter.GetMap();
						Static sta = map.GetStatic(x, y, z, dispId);
						if (sta != null) {
							OnTargon targ = this.targon[targNum];
							this.ClearTarget(targNum);
							if (targ!=null) {
								targ(this, sta, parameter);
								return;
							}
						}
						MultiItemComponent mic = map.GetMultiComponent(x, y, z, dispId);
						if (mic != null) {
							OnTargon targ = this.targon[targNum];
							this.ClearTarget(targNum);
							if (targ!=null) {
								targ(this, mic, parameter);
								return;
							}
						}
					}
				}
			}
			Server.SendClilocSysMessage(this, 1046439, 0);//That is not a valid target.
		}

		public ushort GetTargNum() {
			ushort orig = nextTarg;
			nextTarg++;
			if (nextTarg>maxTargs-1) {
				nextTarg=0;
			}
			while (targon[nextTarg]!=null) {
				if (nextTarg>maxTargs-1) {
					nextTarg=0;
				}
				if (nextTarg==orig) {
					break;
					//throw new ServerException("Out of targetting cursors.");
				}
				nextTarg++;
			}
			return orig;
		}

		//stuff for asynchronous sending/receiving.
		private bool sending = false;
		private byte[] outBuffer = null;
		private byte[] inBuffer = null;
		private Queue waitingOutPacketData = new Queue();
		private Queue waitingInPacketData = new Queue();

		/*
		Quoth the .NET docs:
			"Asynchronous sockets are appropriate for applications that make heavy
			use of the network or that cannot wait for network operations to complete
			before continuing."
		Of course, I was under the impression that I had gotten the normal socket stuff not to
		block on sending and receiving and such, but ... We'll see. All that's left now is
		to add performance measuring stuff, and then we'll have async and sync both running, 
		and collecting data, so we can see which one really works better with a real server.
		
		-SL
		*/

		//We save the cost of having a delegate for every GameConn by making the delegate and the method static...
		//We're giving the damn thing a state object already, might as well be the GameConn.
		private static AsyncCallback DoneSendCallback = new AsyncCallback(DoneSend);

		private static void DoneSend(IAsyncResult ar) {
			Logger.WriteInfo(GameConnTracingOn, "DoneSend.");
			GameConn gc = (GameConn) ar.AsyncState;
			lock (gc.OutSyncRoot) {
				Sanity.IfTrueSay(gc.sending==false, "Why did DoneSendCallback get called if we weren't sending anything?");
				bool success=false;
				int sent=0;
				try {
					sent = gc.client.EndSend(ar);
					success=true;
				} catch (ObjectDisposedException) {
					gc.Close("Connection lost");
				} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
					gc.Close("Connection lost");
				} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
					gc.Close("Connection lost");
				}
				if (success) {
					Sanity.IfTrueSay(sent!=gc.outBuffer.Length, "The asynchronous socket said we sent "+sent+" bytes, but we told it to send "+gc.outBuffer.Length+"!");
					AsyncOutPacketInfo info = gc.curOutPacket;
					info.FinishedSend=HighPerformanceTimer.TickCount;
					AddAsyncStats(info);
					if (gc.waitingOutPacketData.Count>0) {
						info = (AsyncOutPacketInfo) gc.waitingOutPacketData.Dequeue();
						gc.curOutPacket=info;
						gc.outBuffer=info.Data;
						info.BeginningSend=HighPerformanceTimer.TickCount;
						try {
							gc.client.BeginSend(gc.outBuffer, 0, gc.outBuffer.Length, SocketFlags.None, DoneSendCallback, gc);
						} catch (ObjectDisposedException) {
							gc.Close("Connection lost");
						} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
							gc.Close("Connection lost");
						} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
							gc.Close("Connection lost");
						}
					} else {
						gc.outBuffer=null;
						gc.sending=false;
						gc.curOutPacket=null;
					}
				}
			}
		}

		private AsyncOutPacketInfo AsyncSend(byte[] array, int start, int len, long startTime) {
			Logger.WriteInfo(GameConnTracingOn, "AsyncSend.");
			byte[] buff = new byte[len];
			Buffer.BlockCopy(array, start, buff, 0, len);
			AsyncOutPacketInfo info = new AsyncOutPacketInfo(buff, startTime);
			lock (OutSyncRoot) {
				if (sending) {
					waitingOutPacketData.Enqueue(info);
				} else {
					curOutPacket=info;
					outBuffer=buff;
					info.BeginningSend=HighPerformanceTimer.TickCount;
					try {
						client.BeginSend(outBuffer, 0, outBuffer.Length, SocketFlags.None, DoneSendCallback, this);
					} catch (ObjectDisposedException) {
						Close("Connection lost");
					} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
						Close("Connection lost");
					} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
						Close("Connection lost");
					}
					sending=true;
					info.DoneCalling=HighPerformanceTimer.TickCount;
				}
			}
			return info;

		}



		private void SyncSend(byte[] array, int start, int len) {
			Logger.WriteInfo(GameConnTracingOn, "SyncSend.");
			try {
				client.Send(array, start, len, SocketFlags.None);
			} catch (ObjectDisposedException) {
				Close("Connection lost");
			} catch (IOException) {	//added by SL, because without these if someone's client disconnects in the middle of being sent stuff by Send, then SE will crash.
				Close("Connection lost");
			} catch (SocketException) {	//Yes, both IOException and SocketException need to be checked for.
				Close("Connection lost");
			}
		}

		//public void SendArray(byte[] array) {
		//	int len = array.Length;
		//	encryption.EncodeOutgoingPacket(array, 0, len);
		//	Logger.WriteInfo(GameConnTracingOn, "SendArray.");
		//	if (!GetRandAsync()) {
		//		long timeStartedSyncWrite=HighPerformanceTimer.TickCount;
		//		SyncSend(array, 0, len);
		//		long timeEndedSyncWrite=HighPerformanceTimer.TickCount;
		//		AddSyncOutStats(timeStartedSyncWrite, timeEndedSyncWrite, len); 
		//	} else {
		//		long startTime=HighPerformanceTimer.TickCount;
		//		AsyncOutPacketInfo info = AsyncSend(array,0,len, startTime);
		//		info.DoneCalling=HighPerformanceTimer.TickCount;
		//		info.ByteCount=len;
		//	}
		//}
		public virtual void Write(byte[] array, int start, int len) {
			Logger.WriteInfo(GameConnTracingOn, "Write.");

			//Console.WriteLine("encoding: packet 0x"+array[start].ToString("x")+", len "+len);
			byte[] a = new byte[len];
			Buffer.BlockCopy(array, start, a, 0, len);

			encryption.ServerEncrypt(a);

			if (!GetRandAsync()) {
				long timeStartedSyncWrite=HighPerformanceTimer.TickCount;
				SyncSend(a, 0, len);
				long timeEndedSyncWrite=HighPerformanceTimer.TickCount;
				AddSyncOutStats(timeStartedSyncWrite, timeEndedSyncWrite, len);
			} else {
				long startTime=HighPerformanceTimer.TickCount;
				AsyncOutPacketInfo info = AsyncSend(a, 0, len, startTime);
				info.DoneCalling=HighPerformanceTimer.TickCount;
				info.ByteCount=len;
			}
		}
		public override void WriteLine(string data) {
			Server.SendSystemMessage(this, data, 0);
		}

		//public override void WriteLine(LogStr data){
		//    Server.SendSystemMessage(this, data.RawString, 0);
		//}

		public void SuspiciousError(object data) {
			if (Globals.kickOnSuspiciousErrors) {
				WriteLine("You've been kicked because your client sent something suspicious.");
				Close(LogStr.Warning("KICKED FOR SUSPICIOUS ERROR: ")+LogStr.WarningData(data));
			}
			Logger.WriteError(data);
		}
		public void SuspiciousError(LogStr data) {
			if (Globals.kickOnSuspiciousErrors) {
				WriteLine("You've been kicked because your client sent something suspicious.");
				Close(LogStr.Warning("KICKED FOR SUSPICIOUS ERROR: ")+data);
			}
			Logger.WriteError(data);
		}

		internal void SentGump(GumpInstance gi) {
			gumpInstancesByUid[gi.uid] = gi;
			Gump thisGump = gi.def;
			LinkedList<GumpInstance> instancesOfThisGump;
			if (!gumpInstancesByGump.TryGetValue(thisGump, out instancesOfThisGump)) {
				instancesOfThisGump = new LinkedList<GumpInstance>();
				gumpInstancesByGump[thisGump] = instancesOfThisGump;
			}
			instancesOfThisGump.AddFirst(gi);
		}

		private readonly static LinkedList<GumpInstance> emptyGIList = new LinkedList<GumpInstance>();

		internal LinkedList<GumpInstance> FindGumpInstances(Gump gd) {
			LinkedList<GumpInstance> retVal;
			if (gumpInstancesByGump.TryGetValue(gd, out retVal)) {
				return retVal;
			}
			return emptyGIList;
		}

		internal GumpInstance PopGump(uint uid) {
			GumpInstance gi;
			if (gumpInstancesByUid.TryGetValue(uid, out gi)) {
				gumpInstancesByUid.Remove(uid);

				Gump gd = gi.def;
				LinkedList<GumpInstance> list;
				if (gumpInstancesByGump.TryGetValue(gd, out list)) {
					list.Remove(gi);
					if (list.Count == 0) {
						gumpInstancesByGump.Remove(gd);
					}
				}
				return gi;
			}
			return null;
		}

		private static TagKey charUidLinkTK = TagKey.Get("__charUidLink__");

		internal void AboutToRecompile() {
			if (curCharacter != null) {
				this.SetTag(charUidLinkTK, curCharacter.Uid);
			}
		}

		internal void RelinkCharacter() {
			object o = this.GetTag(charUidLinkTK);
			if (o != null) {
				AbstractCharacter newChar = Thing.UidGetCharacter(Convert.ToInt32(o));
				if (newChar == null) {
					Close("Character lost while recompiling...?");
				} else {
					newChar.Account.LogIn(this);
					curAccount = newChar.Account;
					curCharacter = newChar;
					newChar.ReLinkToGameConn();
				}
			}

			gumpInstancesByUid.Clear();
			gumpInstancesByGump.Clear();
			//clear old opened containers and gumps... they just get lost and player has to open them again
			//theres no other simple way...
		}

		internal void RecompilingFinished() {
			this.RemoveTag(charUidLinkTK);
		}

		//this is called by InPackets.LoginChar
		internal AbstractCharacter LoginCharacter(int index) {
			Sanity.IfTrueThrow(index<0 || index>=AbstractAccount.maxCharactersPerGameAccount, "Call was made to LoginCharacter with an invalid character index "+index+", valid values being from 0 to "+AbstractAccount.maxCharactersPerGameAccount+".");
			Sanity.IfTrueThrow(curAccount==null, "curAccount is null!");
			if (curCharacter!=null) {
				Close("Character login with a character already logged, wtf?!");
				return null;
			}
			AbstractCharacter cre = curAccount.GetLingeringCharacter();
			if (cre==null) {//if we've already a lingering char on this acc, 
				//we ignore the selection and login the lingering one
				cre=curAccount.GetCharacterInSlot(index);
			}

			if (cre!=null) {
				curCharacter=cre;
				if (!cre.AttemptLogIn()) {	//returns true for success
					curCharacter=null;
				}
			}
			return curCharacter;
		}

		public override int GetHashCode() {
			return uid;
		}

		public override bool Equals(Object obj) {
			GameConn conn = obj as GameConn;
			if (conn != null) {
				return (conn.uid==this.uid);
			}
			return false;
		}
		public override string ToString() {
			return "Client ("+base.ToString()+")";
		}

		public void CancelMovement() {
			moveSeqNum=0;
			reqMoveSeqNum=0;
			moveRequests.Clear();
		}				
	}
}