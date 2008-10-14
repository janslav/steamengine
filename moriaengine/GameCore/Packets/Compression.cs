//This software is released under GNU internal license. See details in the URL: 
//http://www.gnu.org/copyleft/gpl.html 

using System;
using System.IO;
using System.Runtime.Serialization;
using SteamEngine;
using System.Collections;
using System.Diagnostics;
using SteamEngine.Common;
using System.Configuration;

namespace SteamEngine.Packets {
	
	public class Compression : LowLevelOut {
		
		protected static int Compress1F(int retstart, int len) {
			int numBits;
			int bitPos = 0;
			long value;
			int outIndex=retstart;
			int curByteValue=0;
			
			for (int inIndex=0; inIndex<len; inIndex++) {
				numBits = bitTable[ucBuffer[inIndex],0];
				value = bitTable[ucBuffer[inIndex],1];
				//numBits is always > 0, we always have at least one partial byte of data.
				
				while (numBits > 0) {
					numBits--;
					curByteValue = (curByteValue<<1) | ((int) ((value>>numBits)&0x1));
		
					bitPos++;
					if(bitPos==8) {
						cBuffer[outIndex]=(byte)curByteValue;
						curByteValue=0;
						bitPos=0;
						outIndex++;
					}
				}
			}
			
			numBits = bitTable[256,0];
			value = bitTable[256,1];
	
			while(numBits > 0) {
				numBits--;
				curByteValue = (curByteValue<<1) | ( (int) ((value>>numBits)&0x1));
	
				bitPos++;
				if(bitPos==8) {
					cBuffer[outIndex]=(byte) curByteValue;
					curByteValue=0;
					bitPos=0;
					outIndex++;
				}
			}
			
			if(bitPos>0) {
				curByteValue<<=8-bitPos;
				cBuffer[outIndex]=(byte) curByteValue;
				outIndex++;
			}
			
			return outIndex-retstart;
		}
		
		protected static int Compress2F(int retstart, int len) {
			int numBits;
			int bitPos = 0;
			uint value;
			int outIndex=retstart;
			uint curByteValue=0;
			int bitsWanted=0;
			
			for (int inIndex=0; inIndex<len; inIndex++) {
				numBits = bitTable[ucBuffer[inIndex],0];
				value = (uint) bitTable[ucBuffer[inIndex],1];
				//numBits is always > 0, we always have at least one partial byte of data.
				
				while (numBits > 0) {
					bitsWanted = 8-bitPos;
					if (bitsWanted<=numBits) {
						numBits-=bitsWanted;
						bitPos=0;
						uint tmpVal=value>>numBits;
						curByteValue = (curByteValue<<bitsWanted) | tmpVal;
						value=value & bitAmtTable[numBits];
						cBuffer[outIndex]=(byte)curByteValue;
						outIndex++;
					} else {	//we have to use up numBits, and it won't get us up to 8 bits retrieved.
						bitPos+=numBits;
						curByteValue = (curByteValue<<numBits) | value;
						numBits=0;
						//value would be set to 0 if it wasn't going to be erased automatically anyhow
					}
				}
				
			}
			
			numBits = bitTable[256,0];
			value = (uint) bitTable[256,1];
	
			while(numBits > 0) {
				numBits--;
				curByteValue = (curByteValue<<1) | ( (uint) ((value>>numBits)&0x1));
	
				bitPos++;
				if(bitPos==8) {
					cBuffer[outIndex]=(byte) curByteValue;
					curByteValue=0;
					bitPos=0;
					outIndex++;
				}
			}
			
			if(bitPos>0) {
				curByteValue<<=8-bitPos;
				cBuffer[outIndex]=(byte) curByteValue;
				outIndex++;
			}
			
			return outIndex-retstart;
		}
		
		protected static int Compress3F(int retstart, int len) {
			int numBits;
			int bitPos = 0;
			uint value;
			int outIndex=retstart;
			uint curByteValue=0;
			int bitsWanted=0;
			
			for (int inIndex=0; inIndex<len; inIndex++) {
				numBits = bitTable[ucBuffer[inIndex],0];
				value = (uint) bitTable[ucBuffer[inIndex],1];
				//numBits is always > 0, we always have at least one partial byte of data.
				
				while (numBits > 0) {
					bitsWanted = 8-bitPos;
					if (bitsWanted<=numBits) {
						numBits-=bitsWanted;
						bitPos=0;
						uint tmpVal=value>>numBits;
						curByteValue = (curByteValue<<bitsWanted) | tmpVal;
						value=value & bitAmtTable[numBits];
						cBuffer[outIndex]=(byte)curByteValue;
						outIndex++;
					} else {	//we have to use up numBits, and it won't get us up to 8 bits retrieved.
						bitPos+=numBits;
						curByteValue = (curByteValue<<numBits) | value;
						numBits=0;
						//value would be set to 0 if it wasn't going to be erased automatically anyhow
					}
				}
				
			}
			
			numBits = bitTable[256,0];
			value = (uint) bitTable[256,1];
	
			while (numBits > 0) {
				bitsWanted = 8-bitPos;
				if (bitsWanted<=numBits) {
					numBits-=bitsWanted;
					bitPos=0;
					uint tmpVal=value>>numBits;
					curByteValue = (curByteValue<<bitsWanted) | tmpVal;
					value=value & bitAmtTable[numBits];
					cBuffer[outIndex]=(byte)curByteValue;
					outIndex++;
				} else {	//we have to use up numBits, and it won't get us up to 8 bits retrieved.
					bitPos+=numBits;
					curByteValue = (curByteValue<<numBits) | value;
					numBits=0;
					//value would be set to 0 if it wasn't going to be erased automatically anyhow
				}
			}
			
			if(bitPos>0) {
				curByteValue<<=8-bitPos;
				cBuffer[outIndex]=(byte) curByteValue;
				outIndex++;
			}
			return outIndex-retstart;
		}
		
		protected static int Compress4F(int retstart, int len) {
			int numBits;
			int bitPos = 0;
			uint value;
			int outIndex=retstart;
			uint curByteValue=0;
			
			for (int inIndex=0; inIndex<len; inIndex++) {
				numBits = bitTable[ucBuffer[inIndex],0];
				value = (uint) bitTable[ucBuffer[inIndex],1];
				//numBits is always > 0, we always have at least one partial byte of data. It's also never above 12 or so.
				
				bitPos+=numBits;
				curByteValue = (curByteValue<<numBits) | value;
				while (bitPos>=8) {
					bitPos-=8;
					cBuffer[outIndex]=(byte) (curByteValue>>bitPos);
					outIndex++;
				}
			}
			
			numBits = bitTable[256,0];
			value = (uint) bitTable[256,1];
	
			if (numBits>0) {
				bitPos+=numBits;
				curByteValue = (curByteValue<<numBits) | value;
				while (bitPos>=8) {
					bitPos-=8;
					cBuffer[outIndex]=(byte) (curByteValue>>bitPos);
					outIndex++;
				}
			}
			
			if(bitPos>0) {
				cBuffer[outIndex]=(byte) (curByteValue<<(8-bitPos));
				outIndex++;
			}
			
			return outIndex-retstart;
		}
		
		protected static int Compress5F(int retstart, int len) {
			int numBits;
			int bitPos = 0;
			int value;
			int outIndex=retstart;
			int curByteValue=0;
			
			for (int inIndex=0; inIndex<len; inIndex++) {
				numBits = flatBitTable[(ucBuffer[inIndex] << 1)];
				value = flatBitTable[((ucBuffer[inIndex] << 1) | 1)];
				//numBits is always > 0, we always have at least one partial byte of data. It's also never above 12 or so.
				
				bitPos+=numBits;
				curByteValue = (curByteValue<<numBits) | value;
				while (bitPos>=8) {
					bitPos-=8;
					cBuffer[outIndex]=(byte) (curByteValue>>bitPos);
					outIndex++;
				}
			}
			
			numBits = flatBitTable[512];
			value = flatBitTable[513];
				
			if (numBits>0) {
				bitPos+=numBits;
				curByteValue = (curByteValue<<numBits) | value;
				while (bitPos>=8) {
					bitPos-=8;
					cBuffer[outIndex]=(byte) (curByteValue>>bitPos);
					outIndex++;
				}
			}
			
			if(bitPos>0) {
				cBuffer[outIndex]=(byte) (curByteValue<<(8-bitPos));
				outIndex++;
			}
			
			return outIndex-retstart;
		}
		
		
		protected static int Compress6F(int retstart, int len) {
			int bitPos = 0;
			int outIndex=retstart;
			int curByteValue=0;
			
			for (int inIndex=0; inIndex<len; inIndex++) {
				//numBits is always > 0, we always have at least one partial byte of data. It's also never above 12 or so.
				
				bitPos+=flatBitTable[(ucBuffer[inIndex] << 1)];
				curByteValue = (curByteValue<<flatBitTable[(ucBuffer[inIndex] << 1)]) + flatBitTable[((ucBuffer[inIndex] << 1) + 1)];
				while (bitPos>=8) {
					bitPos-=8;
					cBuffer[outIndex]=(byte) (curByteValue>>bitPos);
					outIndex++;
				}
			}
			
			if (flatBitTable[512]>0) {
				bitPos+=flatBitTable[512];
				curByteValue = (curByteValue<<flatBitTable[512]) + flatBitTable[513];
				while (bitPos>=8) {
					bitPos-=8;
					cBuffer[outIndex++]=(byte) (curByteValue>>bitPos);
				}
			}
			
			if(bitPos>0) {
				cBuffer[outIndex++]=(byte) (curByteValue<<(8-bitPos));
			}
			
			return outIndex-retstart;
		}
		
		private delegate int CompressionMethodDelegate (int retstart, int len);
		private static ArrayList compressionMethods;
		
		//[RegisterWithRunTests]
		//public static void CompressionMethodTests() {
		//    compressionMethods = new ArrayList();
		//    compressionMethods.Add(new CompressionMethodDelegate(Compress1F));
		//    compressionMethods.Add(new CompressionMethodDelegate(Compress2F));
		//    compressionMethods.Add(new CompressionMethodDelegate(Compress3F));
		//    compressionMethods.Add(new CompressionMethodDelegate(Compress4F));
		//    compressionMethods.Add(new CompressionMethodDelegate(Compress5F));
		//    compressionMethods.Add(new CompressionMethodDelegate(Compress6F));
		//    #if USEFASTDLL
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify8));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify10));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify11));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify12));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify13));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify14));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify10no3));
		//    compressionMethods.Add(new CompressionMethodDelegate(FastDLL.Crushify13no10));
		//    #endif
		//    //SimulConn c = new SimulConn();		//a simulated connection
			
		//    int numMetaTests=13;
		//    Logger.Show("TestSuite","Testing compression methods.");
			
		//    AbstractCharacterDef c_man = (AbstractCharacterDef) ThingDef.Get("c_man");
		//    AbstractCharacter that = (AbstractCharacter) c_man.Create(150,200,10,0);
				
		//    try {
		//        for (int meta=0; meta<numMetaTests; meta++) {
		//            switch (meta) {
		//                case 0: {
		//                    PacketSender.PrepareItemInformation(that.Newitem((AbstractItemDef)ThingDef.Get("I_gold")));
		//                    Logger.Show("TestSuite", "Testing with: ItemInformation packet for a new I_gold (len "+gPacketSize+")");
		//                    break;
		//                } case 1: {
		//                    PacketSender.PreparePaperdollItem(that.Backpack);
		//                    Logger.Show("TestSuite", "Testing with: PaperdollItem packet for our Backpack (len "+gPacketSize+")");
		//                    break;
		//                } case 2: {
		//                    PacketSender.PrepareItemInContainer(that.Newitem((AbstractItemDef)ThingDef.Get("I_gold")));
		//                    Logger.Show("TestSuite", "Testing with: ItemInContainer packet for a new I_gold (len "+gPacketSize+")");
		//                    break;
		//                } case 3: {
		//                    PacketSender.PrepareUpdateStats(that, true);
		//                    Logger.Show("TestSuite", "Testing with: UpdateStats packet (len "+gPacketSize+")");
		//                    break;
		//                } case 4: {
		//                    PacketSender.PrepareUpdateHitpoints(that, true);
		//                    Logger.Show("TestSuite", "Testing with: UpdateHitpoints packet (len "+gPacketSize+")");
		//                    break;
		//                } case 5: {
		//                    PacketSender.PrepareStatusBar(that, StatusBarType.Me);
		//                    Logger.Show("TestSuite", "Testing with: StatusBar packet for ourselves (len "+gPacketSize+")");
		//                    break;
		//                } case 6: {
		//                    PacketSender.PrepareStatusBar(that, StatusBarType.Pet);
		//                    Logger.Show("TestSuite", "Testing with: StatusBar packet for a pet (len "+gPacketSize+")");
		//                    break;
		//                } case 7: {
		//                    PacketSender.PrepareStatusBar(that, StatusBarType.Other);
		//                    Logger.Show("TestSuite", "Testing with: StatusBar packet for someone else (len "+gPacketSize+")");
		//                    break;
		//                } case 8: {
		//                    PacketSender.PrepareWarMode(that);
		//                    Logger.Show("TestSuite", "Testing with: WarMode packet (len "+gPacketSize+")");
		//                    break;
		//                } case 9: {
		//                    PacketSender.PrepareMovingCharacter(that, true, HighlightColor.NoColor);
		//                    Logger.Show("TestSuite", "Testing with: MovingCharacter packet (len "+gPacketSize+")");
		//                    break;
		//                } case 10: {
		//                    PacketSender.PrepareSound(that, 41); //Thunder sound
		//                    Logger.Show("TestSuite", "Testing with: Sound packet (len "+gPacketSize+")");
		//                    break;
		//                } case 11: {
		//                    PacketSender.PrepareAnimation(that, 21, 1, false, false, 1);
		//                    Logger.Show("TestSuite", "Testing with: Animation (action) packet (len "+gPacketSize+")");
		//                    break;
		//                } case 12: {
		//                    PacketSender.PrepareUnicodeMessage(null, 0x01, "Source", "A unicode message", SpeechType.Speech, 3, 0x21, "enu");
		//                    Logger.Show("TestSuite","Testing with: UnicodeMessage packet.");
		//                    break;
		//                }
						
		//            }
		//            //we only care about it writing to opacket
		//            foreach (CompressionMethodDelegate method in compressionMethods) {
		//                Logger.Show("TestSuite","Testing compression method "+method.Method);
		//                TestCompressionMethod(method, (ushort)gPacketSize);
		//                Logger.Show("TestSuite","Compression method "+method.Method+" passed.");
		//            }
		//            Logger.Show("TestSuite","Finished testing with that packet.");
		//            PacketSender.DiscardLastPacket();
		//        }
		//    } finally {
		//        that.InternalDelete();
		//    }
		//    Logger.Show("TestSuite","Finished compression tests.");
		//}
		
		//private static void TestCompressionMethod(CompressionMethodDelegate method, ushort plen) {
		//    int retval=pack(ucBuffer, cBuffer, 0, plen);
		//    int retval2=method(retval, plen);
		//    if (retval!=retval2) {
		//        throw new SanityCheckException("Test of compression method "+method.Method+" failed. Compressed packet is the wrong size: It is "+retval2+" when it should be "+retval+".");
		//    } else {
		//        for (int a=0; a<retval; a++) {
		//            if (cBuffer[a]!=cBuffer[retval+a]) {
		//                throw new SanityCheckException("Test of compression method "+method.Method+" failed. Byte "+a+" does not match. It is "+cBuffer[retval+a]+" when it should be "+cBuffer[a]+".");
		//            }
		//        }
		//    }
		//}
		
		//Do not modify pack! If you really want to, make a copy and modify THAT. 
		//This is used to test the other compression functions for accuracy.
		//This is also really slow, but it's not marked obsolete because it IS
		//used for testing... It's also private because it is ONLY used for
		//testing. -SL
		private static int pack(byte[] unpacked, byte[] retval, int retstart, int len) {
			int InIdx = 0;
			int numBits;
			int curByte = 0;
			int bitByte = 0;
			long value;
			int[] packed = new int[len*4];
			while(len-- > 0) {
				try {
					numBits = bitTable[unpacked[InIdx],0];
					value = bitTable[unpacked[InIdx++],1];
				}
				catch(Exception e) {
					Console.WriteLine("Pack caught exception (inside) "+e);	//I wanted to see if these ever really get triggered - and they don't seem to. Go figure.
					numBits = bitTable[unpacked[InIdx]+256,0];
					value = bitTable[unpacked[InIdx++]+256,1];
				}
				
		
				while(numBits-- > 0) {
					packed[curByte] = (packed[curByte] << 1) | ((int) ((value >> numBits) & 0x1));
		
					bitByte = (bitByte+1)&0x07;
					if(bitByte==0) 
						curByte++;
				}
			}
			
			numBits = bitTable[256,0];
			value = bitTable[256,1];
	
			while(numBits-- > 0) {
				packed[curByte] = (packed[curByte] << 1) | ( (int) ((value >> numBits) & 0x1));
	
				bitByte = (bitByte+1)&0x07;
				if(bitByte==0) 
					curByte++;
			}
	
			if(bitByte!=0) {
				while(bitByte<8) {
					packed[curByte]<<=1;
					bitByte++;
				}
				curByte++;
			}
			for(int i=0; i<curByte; i++)
				retval[i+retstart] = (byte) packed[i];
			return curByte;
		}
	}
}
