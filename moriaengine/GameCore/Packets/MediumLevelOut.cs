//This software is released under GNU internal license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

using System;
using System.IO;
using System.Runtime.Serialization;
using SteamEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.Packets {
	
	public class MediumLevelOut : Compression {
		private static Random random = new Random();
		
		protected static void Compress() {
			if (groupState==GroupState.Open) {
				CompressAs(CompressedPacketType.Group);
			} else {
				CompressAs(CompressedPacketType.Single);
			}
		}
		
		protected static void CompressAs(CompressedPacketType cpt) {	//(Replaces the PrepareToSend* methods) 
			Logger.WriteInfo(PacketSenderTracingOn, "CompressAs("+cpt+") : Packet ID "+ucBuffer[0].ToString("x"));
			#if TRACE
			if (DumpAllPacketsCompressedForSending) {
				//Logger.WriteDebug("LogUCPacket Start.");
				LogUCPacket();
				//Logger.WriteDebug("LogUCPacket Done.");
				
			}
			#endif
			/*Requires generatingState == Generated, and:
				If CPT == Group, requires groupState==Open, adds packet to curGroup,
				If CPT == Single, requires groupState==Ready, sets groupState==SingleBlocking,
				
				Finally, sets generatingState=Ready, lastCPacketStart=lastCPacketEnd, 
				lastCPacketSize=compressedLen (from compression method), 
				lastCPacketEnd=lastCPacketSize+compressedLen.
			*/
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Generated,"CompressAs() called when generatingState is"+generatingState+" - Expected it to be Generated.");
			int r=random.Next(0,numCompressMethods);
			int compressedLen=0;
			long tcend=0;
			long tcstart=0;
			Logger.WriteInfo(PacketSenderTracingOn, "Compressing with algorithm "+r+".");
			switch (r) {
				case 0: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen = Compress1F(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 1: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen = Compress2F(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 2: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen = Compress3F(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 3: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen = Compress4F(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 4: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen = Compress5F(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 5: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen = Compress6F(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				#if USEFASTDLL
				} case 6: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify8(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 7: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify10(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 8: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify11(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 9: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify12(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 10: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify13(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 11: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify14(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 12: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify10no3(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				} case 13: {
					tcstart=HighPerformanceTimer.TickCount;
					compressedLen=FastDLL.Crushify13no10(lastCPacketEnd,gPacketSize);
					tcend=HighPerformanceTimer.TickCount;
					break;
				#endif
				}
			}
			//Check it for validity
			/*#if DEBUG
			int compressedLen2 = Compress6F(lastCPacketEnd+compressedLen,gPacketSize);
			bool fail=compressedLen!=compressedLen2;
			if (!fail) {
				for (int a=0; a<compressedLen; a++) {
					if (cBuffer[a+lastCPacketEnd]!=cBuffer[a+lastCPacketEnd+compressedLen]) {
						fail=true;
					}
				}
			}
			if (fail) {
				Logger.WriteError("Compression algorithm "+r+" failed to work right, on packetID 0x"+ucBuffer[0].ToString("x"));
				Logger.WriteDebug("Uncompressed packet data length: "+gPacketSize);
				OutputPacketLog(ucBuffer, 0, gPacketSize);
				Logger.WriteDebug("Correctly compressed packet data length: "+compressedLen2);
				OutputPacketLog(cBuffer, lastCPacketEnd+compressedLen, compressedLen2);
				Logger.WriteDebug("Incorrectly compressed packet data length (From algorithm "+r+"): "+compressedLen+"\n");
				OutputPacketLog(cBuffer, lastCPacketEnd, compressedLen);
				//use the correct one so nothing dies horribly. (I want to see which ones are bad without having to restart SE a bunch)
				//(Sadly, it dies horribly anyways. Blargh. I don't feel like trying to find the problem right now, so I'll just work without FastDLL for now. I'll slap a #warning on FastDLL so nobody else uses it, too.)
				if (compressedLen>0) {
					//I would have used Buffer.BlockCopy, except that I'm not certain whether it would work if
					//compressedLen2 was greater than compressedLen, since that would mean we would be writing over the
					//beginning of the 2nd compressed data with data from the end of it, so depending on the algorithm
					//BlockCopy uses, that might or might not work.
					//So we just do this instead.
					for (int a=lastCPacketEnd; a<lastCPacketEnd+compressedLen2; a++) {
						cBuffer[a]=cBuffer[a+compressedLen];
					}
				}
				compressedLen=compressedLen2;
			}
			#endif
			*/
			//Logger.WriteDebug("Adding entry.");
			
			//Temporarily disbled until it can be determined why it's really really slow:
			//AddEntry(r, compressedLen, 1, tcend-tcstart, ucBuffer[0]);
			
			//Logger.WriteDebug("Adding packet.");
			if (cpt==CompressedPacketType.Group) {
				Sanity.IfTrueThrow(groupState!=GroupState.Open,"CompressAs(Group) called when groupState is"+groupState+" - Expected it to be Open.");
#if DEBUG
				curGroup.AddCompressedPacket(lastCPacketEnd, compressedLen, ucBuffer[0]);
#else
				curGroup.AddCompressedPacket(lastCPacketEnd, compressedLen);
#endif
			} else if (cpt==CompressedPacketType.Single) {
				Sanity.IfTrueThrow(groupState!=GroupState.Ready,"CompressAs(Single) called when groupState is"+groupState+" - Expected it to be Ready.");
				groupState=GroupState.SingleBlocking;
			}
			//Logger.WriteDebug("Setting variables.");
			lastCPacketStart=lastCPacketEnd;	//Set start to just after the last packet.
			lastCPacketSize=compressedLen;		//Set size.
			lastCPacketEnd=lastCPacketStart+lastCPacketSize;	//Set end so the next packet knows where to compress to.
			generatingState=GeneratingState.Ready;
			
			
			//Logger.WriteDebug("Done.");
		}
		
		public static void SendTo(GameConn conn, bool discard) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendTo("+conn+","+discard+")");
			//Requres generatingState == Ready and groupState==SingleBlocking. If discard is set,
			//also sets lastCPacketEnd=lastGroupEnd, and groupState=Ready.
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "Send called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "Send called when groupState is "+groupState+" - Expected it to be SingleBlocking.");
			SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
			if (discard) {
				lastCPacketEnd=lastGroupEnd;
				groupState=GroupState.Ready;
			}
		}
		
		internal static void SendUncompressed(GameConn conn, bool discard) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendUncompressed("+conn+","+discard+") : packet ID "+ucBuffer[0].ToString("x"));
			//Requires that generatingState == Generated.
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Generated, "SendUncompressed called when generatingState is "+generatingState+" - Expected it to be Generated.");
			SendUncompressedBytes(conn, 0, gPacketSize);
			if (discard) {
				generatingState=GeneratingState.Ready;
			}
		}

		[Remark("This always discards the single-blocking-packet after sending.")]
		public static void SendToClientsInRange(IPoint4D point) {
			SendToClientsInRange(point, Globals.MaxUpdateRange);
		}

		[Remark("This always discards the single-blocking-packet after sending.")]
		public static void SendToClientsInRange(IPoint4D point, ushort range) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendToClientsInRange("+point+","+range+")");
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "SendToClientsInRange called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "SendToClientsInRange called when groupState is "+groupState+" - Expected it to be SingleBlocking.");
			foreach (GameConn conn in point.GetMap().GetClientsInRange(point.X, point.Y, range)) {
				SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
			}
			generatingState=GeneratingState.Ready;
			lastCPacketEnd=lastGroupEnd;
			groupState=GroupState.Ready;
		}

		[Remark("This always discards the single-blocking-packet after sending.")]
		public static void SendToClientsInRect(byte mapplane, Rectangle2D rect) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendToClientsInRect("+mapplane+","+rect+")");
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "SendToClientsInRect called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "SendToClientsInRect called when groupState is "+groupState+" - Expected it to be SingleBlocking.");
			foreach (GameConn conn in Map.GetMap(mapplane).GetClientsInRectangle(rect)) {
				SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
			}
			generatingState=GeneratingState.Ready;
			lastCPacketEnd=lastGroupEnd;
			groupState=GroupState.Ready;
		}
		

		[Remark("This always discards the single-blocking-packet after sending.")]
		public static void SendToClientsWhoCanSee(Thing thing) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendToClientsWhoCanSee("+thing+")");
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "SendToClientsWhoCanSee called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "SendToClientsWhoCanSee called when groupState is "+groupState+" - Expected it to be SingleBlocking.");

			bool sent = false;
			AbstractItem asItem = thing as AbstractItem;
			if (asItem != null) {
				AbstractItem contAsItem = asItem.Cont as AbstractItem;
				if (contAsItem != null) {
					foreach (GameConn conn in OpenedContainers.GetConnsWithOpened(contAsItem)) {
						SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
					}
					sent = true;
				}
			}
			if (!sent) {
				foreach (GameConn conn in thing.GetMap().GetClientsWhoCanSee(thing)) {
					SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
				}
			}

			generatingState=GeneratingState.Ready;
			lastCPacketEnd=lastGroupEnd;
			groupState=GroupState.Ready;
		}

		[Remark("This always discards the single-blocking-packet after sending.")]
		public static void SendToClientsWhoCanSee(AbstractCharacter thing) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendToClientsWhoCanSee("+thing+")");
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "SendToClientsWhoCanSee called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "SendToClientsWhoCanSee called when groupState is "+groupState+" - Expected it to be SingleBlocking.");

			foreach (GameConn conn in thing.GetMap().GetClientsWhoCanSee(thing)) {
				SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
			}

			generatingState=GeneratingState.Ready;
			lastCPacketEnd=lastGroupEnd;
			groupState=GroupState.Ready;
		}

		[Remark("This always discards the single-blocking-packet after sending.")]
		public static void SendToClientsWhoCanSee(AbstractItem thing) {
			Logger.WriteInfo(PacketSenderTracingOn, "SendToClientsWhoCanSee("+thing+")");
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "SendToClientsWhoCanSee called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "SendToClientsWhoCanSee called when groupState is "+groupState+" - Expected it to be SingleBlocking.");

			AbstractItem contAsItem = thing.Cont as AbstractItem;
			if (contAsItem != null) {
				foreach (GameConn conn in OpenedContainers.GetConnsWithOpened(contAsItem)) {
					SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
				}
			} else {
				foreach (GameConn conn in thing.GetMap().GetClientsWhoCanSee(thing)) {
					SendCompressedBytes(conn, lastCPacketStart, lastCPacketSize);
				}
			}

			generatingState=GeneratingState.Ready;
			lastCPacketEnd=lastGroupEnd;
			groupState=GroupState.Ready;
		}
		
		public static void DiscardLastPacket() {
			Logger.WriteInfo(PacketSenderTracingOn, "DiscardLastPacket()");
			//Requires generatingState==Ready, groupState==SingleBlocking, sets lastCPacketEnd=lastGroupEnd,
			//and sets groupState=Ready.
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready, "DiscardLastPacket() called when generatingState is "+generatingState+" - Expected it to be Ready.");
			Sanity.IfTrueThrow(groupState!=GroupState.SingleBlocking, "DiscardLastPacket() called when groupState is "+groupState+" - Expected it to be SingleBlocking.");
			lastCPacketEnd=lastGroupEnd;
			groupState=GroupState.Ready;
		}
			
		internal static void DiscardAll() {
			Logger.WriteInfo(PacketSenderTracingOn, "DiscardAll()");
			//Requires generatingState==Ready || generatingState==Generated, groupState==Ready ||
			//groupState==SingleBlocking, clears curGroup and groups, sets lastGroupEnd=0, 
			//groupState=Ready, generatingState=Ready.
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready && generatingState!=GeneratingState.Generated, "DiscardAll() called when generatingState is "+generatingState+" - Expected it to be either Ready or Generated.");
			Sanity.IfTrueThrow(groupState!=GroupState.Ready && groupState!=GroupState.SingleBlocking, "DiscardAll() called when groupState is "+groupState+" - Expected it to be either Ready or SingleBlocking.");
			curGroup=null;
			foreach (BoundPacketGroup group in groups) {
				group.Deleted();
			}
			groups.Clear();
			lastGroupEnd=0; 
			//lastCPacketEnd=0; lastCPacketStart=0; lastCPacketSize=0; gPacketSize=0;
			groupState=GroupState.Ready;
			generatingState=GeneratingState.Ready;
		}
		
		internal static void DiscardUncompressed() {
			Logger.WriteInfo(PacketSenderTracingOn, "DiscardUncompressed()");
			//Requires generatingState==Generated, sets generatingState=Ready.
			Sanity.IfTrueThrow(generatingState!=GeneratingState.Generated, "DiscardUncompressed() called when generatingState is "+generatingState+" - Expected it to be Generated.");
			generatingState=GeneratingState.Ready;
		}
		
		
		/**
			This tests the Encode* methods in LowLevelOut.
			Note that this is in MediumLevelOut because it uses DiscardUncompressed, which is also in MediumLevelOut -
			DiscardUncompressed would be inaccessable from LowLevelOut.
		*/
		[RegisterWithRunTests]
		public static void EncodingTests() {
			StartGenerating();
			
			Logger.Show("TestSuite", "Testing void EncodeBytes(byte[] array, int start)");
			byte[] arr = new byte[256];
			for (int a=0; a<256; a++) {
				arr[a]=(byte)a;
			}
			for (int startpos=0; startpos<15; startpos++) {
				EncodeBytes(arr, startpos);
				for (int idx=0; idx<256; idx++) {
					if (arr[idx]!=ucBuffer[idx+startpos]) {
						throw new SanityCheckException("The test of EncodeBytes(byte[] array, int start) failed. 'start' was "+startpos+", and array was "+ArrayToString(arr));
					}
				}
			}
			
			Logger.Show("TestSuite", "Testing void EncodeBytes(byte[,] array, int firstIndex, int start)");
			byte[,] arr2 = new byte[256, 256];
			for (int a=0; a<256; a++) {
				for (int b=0; b<256; b++) {
					arr2[a, b]=(byte)(((a|b)+(a&b)+(a%(b+1)))&0xff);	//just pick some number
				}
			}
			for (int firstIndex=0; firstIndex<256; firstIndex++) {
				for (int startpos=0; startpos<15; startpos++) {
					EncodeBytes(arr2, firstIndex, startpos);
					for (int idx=0; idx<256; idx++) {
						if (arr2[firstIndex, idx]!=ucBuffer[idx+startpos]) {
							throw new SanityCheckException("The test of EncodeBytes(byte[,] array, int firstIndex, int start) failed. 'firstIndex' was "+firstIndex+", 'start' was "+startpos+", and array was "+ArrayToString(arr2, firstIndex));
						}
					}
				}
			}
			
			Logger.Show("TestSuite", "Testing void EncodeBytesReversed(byte[] array, int start) {");
			arr = new byte[256];
			for (int a=0; a<256; a++) {
				arr[a]=(byte)a;
			}
			for (int startpos=0; startpos<15; startpos++) {
				EncodeBytesReversed(arr, startpos);
				for (int idx=0; idx<256; idx++) {
					if (arr[idx]!=ucBuffer[(255-idx)+startpos]) {
						throw new SanityCheckException("The test of EncodeBytesReversed(byte[] array, int start) failed. 'start' was "+startpos+", and array was "+ArrayToString(arr));
					}
				}
			}
			
			Logger.Show("TestSuite", "Testing void EncodeBytesReversed(byte[,] array, int firstIndex, int start)");
			arr2 = new byte[256, 256];
			for (int a=0; a<256; a++) {
				for (int b=0; b<256; b++) {
					arr2[a, b]=(byte)(((a|b)+(a&b)+(a%(b+1)))&0xff);	//just pick some number
				}
			}
			for (int firstIndex=0; firstIndex<256; firstIndex++) {
				for (int startpos=0; startpos<15; startpos++) {
					EncodeBytesReversed(arr2, firstIndex, startpos);
					for (int idx=0; idx<256; idx++) {
						if (arr2[firstIndex, idx]!=ucBuffer[(255-idx)+startpos]) {
							throw new SanityCheckException("The test of EncodeBytesReversed(byte[,] array, int firstIndex, int start) failed. 'firstIndex' was "+firstIndex+", 'start' was "+startpos+", and array was "+ArrayToString(arr2, firstIndex));
						}
					}
				}
			}
			
			Logger.Show("TestSuite", "Testing EncodeZeros(3, 15)");
			//Fill ucBuffer with non-zero junk.
			for (int a=0; a<256; a++) {
				int b = a+251;	//a prime number, for no particular reason.
				ucBuffer[a]=(byte)(((a|b)+(a&b)+(a%(b+1)))&0xff);
				if (ucBuffer[a]==0) ucBuffer[a]=0xff;
			}
			EncodeZeros(3, 15);
			Sanity.IfTrueThrow(ucBuffer[14]==0 || ucBuffer[15]!=0 || ucBuffer[16]!=0 || ucBuffer[17]!=0 || ucBuffer[18]==0, "The test of EncodeZeros failed.");
			
			Logger.Show("TestSuite", "Testing EncodeInt(-2032443317, 52)");
			int ival=-2032443317;
			EncodeInt(ival, 52);	//0x86DB604B
			Sanity.IfTrueThrow(ucBuffer[52]!=0x86 || ucBuffer[53]!=0xdb || ucBuffer[54]!=0x60 || ucBuffer[55]!=0x4b, "Test of EncodeInt("+ival+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeInt(2032443317, 59)");
			ival=2032443317;
			EncodeInt(ival, 59);	//0x79249FB5
			Sanity.IfTrueThrow(ucBuffer[59]!=0x79 || ucBuffer[60]!=0x24 || ucBuffer[61]!=0x9f || ucBuffer[62]!=0xb5, "Test of EncodeInt("+ival+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeUInt(2032443317, 41)");
			uint uival=2032443317;
			EncodeUInt(uival, 41);	//0x79249FB5
			Sanity.IfTrueThrow(ucBuffer[41]!=0x79 || ucBuffer[42]!=0x24 || ucBuffer[43]!=0x9f || ucBuffer[44]!=0xb5, "Test of EncodeUInt("+uival+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeUInt(4032443318, 77)");
			uival=4032443318;
			EncodeUInt(uival, 77);	//0xF05A33B6
			Sanity.IfTrueThrow(ucBuffer[77]!=0xf0 || ucBuffer[78]!=0x5a || ucBuffer[79]!=0x33 || ucBuffer[80]!=0xb6, "Test of EncodeUInt("+uival+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeShort(-31849, 33)");
			short sval=-31849;
			EncodeShort(sval, 33);	//0x8397
			Sanity.IfTrueThrow(ucBuffer[33]!=0x83 || ucBuffer[34]!=0x97, "Test of EncodeShort("+sval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeShort(31849, 24)");
			sval=31849;
			EncodeShort(sval, 24);	//0x7c69
			Sanity.IfTrueThrow(ucBuffer[24]!=0x7c || ucBuffer[25]!=0x69, "Test of EncodeShort("+sval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeUShort(31849, 79)");
			ushort usval=31849;
			EncodeUShort(usval, 79);	//0x7c69
			Sanity.IfTrueThrow(ucBuffer[79]!=0x7c || ucBuffer[80]!=0x69, "Test of EncodeUShort("+usval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeUShort(64724, 40)");
			usval=64724;
			EncodeUShort(usval, 40);	//0xfcd4
			Sanity.IfTrueThrow(ucBuffer[40]!=0xfc || ucBuffer[41]!=0xd4, "Test of EncodeUShort("+usval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeSByte(-120, 14)");
			sbyte sbval=-120;
			EncodeSByte(sbval, 14);	//0x88
			Sanity.IfTrueThrow(ucBuffer[14]!=0x88, "Test of EncodeSByte("+sbval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeSByte(52, 16)");
			sbval=52;
			EncodeSByte(sbval, 16);	//0x34
			Sanity.IfTrueThrow(ucBuffer[16]!=0x34, "Test of EncodeSByte("+sbval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeByte(52, 19)");
			byte bval=52;
			EncodeByte(bval, 19);	//0x34
			Sanity.IfTrueThrow(ucBuffer[19]!=0x34, "Test of EncodeByte("+bval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeByte(247, 20)");
			bval=247;
			EncodeByte(bval, 20);	//0xf7
			Sanity.IfTrueThrow(ucBuffer[20]!=0xf7, "Test of EncodeByte("+bval+") failed.");
			
			Logger.Show("TestSuite", "Testing EncodeBool(false, 4)");
			ucBuffer[4]=52;
			EncodeBool(false, 4);
			Sanity.IfTrueThrow(ucBuffer[4]!=0, "Test of EncodeBool(false) failed.");
			
			Logger.Show("TestSuite", "Testing EncodeBool(true, 6)");
			ucBuffer[6]=71;
			EncodeBool(true, 6);
			Sanity.IfTrueThrow(ucBuffer[6]!=1, "Test of EncodeBool(true) failed.");
			
			
			Logger.Show("TestSuite (Encoding Tests)", "Not testing EncodeUnicodeString (No test has been written for it).");
			Logger.Show("TestSuite (Encoding Tests)", "Not testing EncodeString (No test has been written for it).");
			Logger.Show("TestSuite (Encoding Tests)", "Not testing EncodeCurMaxVals (No test has been written for it).");
			//void EncodeCurMaxVals(short curval, short maxval, bool showReal, int pos) {
			DoneGenerating(1);
			Logger.Show("TestSuite", "Testing EncodeUInt((uint)(-2032443317), 41) - This is not a test of encoding, per se, but actually a test to confirm that when a negative int is cast to a uint, .NET does not change the bytes behind the variable when casting it.");
			ival=-2032443317;
			EncodeUInt((uint) ival, 52);	//0x86DB604B
			Sanity.IfTrueThrow(ucBuffer[52]!=0x86 || ucBuffer[53]!=0xdb || ucBuffer[54]!=0x60 || ucBuffer[55]!=0x4b, "Test of EncodeUInt((uint)"+ival+") failed.");
			
			DiscardUncompressed();
		}
		
		
		public static string ArrayToString(byte[] array) {
			string arrstr = "";
			int len=array.Length;
			for (int a=0; a<len; a++) {
				byte o = array[a];
				if (arrstr.Length==0) {
					arrstr=o.ToString();
				} else {
					arrstr+=", "+o.ToString();
				}
			}
			return "{"+arrstr+"}";
		}
		public static string ArrayToString(byte[,] array, int firstIndex) {
			string arrstr = "";
			int len=array.GetLength(1);
			for (int a=0; a<len; a++) {
				object o = array[firstIndex, a];
				if (arrstr.Length==0) {
					arrstr=o.ToString();
				} else {
					arrstr+=", "+o.ToString();
				}
			}
			return "{"+arrstr+"}";
		}
		
		[Conditional("TRACE")]
		private static void LogUCPacket() {
			byte[] array = ucBuffer;
			int len = gPacketSize;
			Logger.Log("Packet Contents: ("+len+" bytes)");
			string s = "";
			string t = "";
			for (int a=0; a<len; a++) {
				t = array[a].ToString("X");
				while (t.Length<2) {
					t="0"+t;
				}
				s += " "+t;
				if (a%10==0) {
					Logger.Log(s);
					s="";
				}
			}
			Logger.Log(s);
			s="";
			for (int a=0; a<len; a++) {
				t = ""+(char) array[a];
				if (array[a]<32 || array[a]>126) {
					t=""+(char) 128;
				}
				s += " "+t;
				if (a%10==0) {
					Logger.Log(s);
					s="";
				}
			}
			Logger.Log(s);
		}
		
	}
}