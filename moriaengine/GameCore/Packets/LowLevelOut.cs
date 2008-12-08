////This software is released under GNU internal license. See details in the URL:
////http://www.gnu.org/copyleft/gpl.html

//using System;
//using System.IO;
//using System.Runtime.Serialization;
//using System.Configuration;
//using System.Collections;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Sockets;
//using System.Diagnostics;
//using System.Text;
//using SteamEngine;
//using SteamEngine.Common;


//namespace SteamEngine.Packets {
	
//    public class LowLevelOut : PacketStats {
//        public static bool PacketSenderTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["PacketSender Trace Messages"]);
//        public static bool DumpAllPacketsCompressedForSending = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Dump all packets compressed for sending"]);
		
//        internal static byte[] ucBuffer = new byte [Server.maxPacketLen];
//        internal static byte[] cBuffer = new byte [Server.maxPacketLen];
//        internal static int[] flatBitTable;
//        internal static int[,] bitTable;
//        internal static uint[] bitAmtTable;
		
//        protected static int gPacketSize;				//- Set by Generate* methods.
//        protected static List<BoundPacketGroup> groups;
//        internal static BoundPacketGroup curGroup;			//Internal so PacketGroup can set it.
//        internal static GroupState groupState;			//Internal so PacketGroup can set it.
//        protected static GeneratingState generatingState;
//        protected static int lastCPacketStart=0;			//- The start pos of the last cpacket.
//        protected static int lastCPacketSize=0;				//- The size of the last cpacket.
//        protected static int lastCPacketEnd=0;				//- The first pos after the last byte of the last cpacket.
//        protected static int lastGroupEnd=0;				//- The first pos after the end of the last group's data.

//        //If groupState is not SingleBlocking, then lastCPacketStart, Size, and End have no meaning.
		
//        static LowLevelOut() {
//            gPacketSize=0;
//            groups = new List<BoundPacketGroup>();
//            curGroup=null;
//            groupState=GroupState.Ready;
//            generatingState=GeneratingState.Ready;
//            ConstructBitTables();
//        }
		
//        protected static void StartGenerating() {
//            Logger.WriteInfo(PacketSenderTracingOn, "StartGenerating()");
//            Sanity.IfTrueThrow(generatingState!=GeneratingState.Ready,"StartGenerating called when generatingState is not Ready!");
//            generatingState=GeneratingState.Generating;
//            //Requires generatingState==Ready, sets generatingState=Generating.
//        }
		
//        protected static void DoneGenerating(int gpLength) {
//            Logger.WriteInfo(PacketSenderTracingOn, "DoneGenerating("+gpLength+")");
//            Sanity.IfTrueThrow(generatingState!=GeneratingState.Generating,"DoneGenerating called when generatingState is not Generating!");
//            generatingState=GeneratingState.Generated;
//            gPacketSize=gpLength;
//        }
//        //Internal for PacketGroup
//        internal static void SendCompressedBytes(GameConn conn, int start, int len) {
//            Logger.WriteDebug("Sending packet 0x"+ucBuffer[0].ToString("x")+" to "+conn);
//            conn.Write(cBuffer, start, len);
//        }
//        protected static void SendUncompressedBytes(GameConn conn, int start, int len) {
//            Logger.WriteDebug("Sending packet 0x"+ucBuffer[0].ToString("x")+" to "+conn);
//            conn.Write(ucBuffer, start, len);
//        }

//        //---------------------------------------------------------------------------------------------------------------
//        // Data encoding methods:
		
//        protected static void EncodeBytes(byte[] array, int start) {
//            for (int idx=0; idx<array.Length; idx++) {
//                ucBuffer[start+idx]=array[idx];
//            }
//        }
		
//        /**
//            Given an array with two dimensions, and a specified index 'firstIndex' for the first dimension,
//            this copies all the elements of array[firstIndex,*] into the uncompressed-data-buffer at the specified
//            start position.
//        */
//        protected static void EncodeBytes(byte[,] array, int firstIndex, int start) {
//            Sanity.IfTrueThrow(array.Rank!=2, "EncodeBytes(byte[,] array, firstIndex, start) is only for use with two-dimensional arrays.");
//            for (int idx=0; idx<array.GetLength(1); idx++) {
//                ucBuffer[start+idx]=array[firstIndex, idx];
//            }
//        }
//        protected static void EncodeBytesReversed(byte[,] array, int firstIndex, int start) {
//            Sanity.IfTrueThrow(array.Rank!=2, "EncodeBytes(byte[,] array, firstIndex, start) is only for use with two-dimensional arrays.");
//            int len=array.GetLength(1)-1;
//            for (int idx=0; idx<=len; idx++) {
//                ucBuffer[start+idx]=array[firstIndex, len-idx];
//            }
//        }
		
//        protected static void EncodeBytesReversed(byte[] array, int start) {
//            int len=array.Length-1;
//            for (int idx=0; idx<=len; idx++) {
//                ucBuffer[start+idx]=array[len-idx];
//            }
//        }
					
//        /**
//            Encodes the string as unicode data at the specified start position, and returns the number of bytes
//            written.
			
//            You can do something like this, if you want: blockSize+=EncodeUnicodeString(line, blockSize);
			
//            But you can't do that with the EncodeUnicodeString method which takes a maxlen parameter.

//            @returns the number of bytes written, which is actually always double the length of the string.
			
//            Example:
//                int numBytes = EncodeUnicodeString("foo", 0);
//                That writes "foo" in unicode, which takes 6 bytes, so numBytes would be 6.
//        */
//        protected static int EncodeUnicodeString(string value, int start) {
//            int len=value.Length;
//            return Encoding.BigEndianUnicode.GetBytes(value, 0, len, ucBuffer, start);
//        }
		
//        protected static int EncodeLittleEndianUnicodeString(string value, int start) {
//            int len=value.Length;
//            return Encoding.Unicode.GetBytes(value, 0, len, ucBuffer, start);
//        }
		
//        /**
//            Encodes the string as unicode data at the specified start position, 
//            with the specified maximum length (in characters, NOT bytes),
//            and if less characters are written than the maximum length, then nulls are written
//            to fill up the space.

//            @returns the number of bytes written (not counting the extra zeros), which is either double the length
//            of the string, or double maxlen, whichever is lower.
			
//            Example #1:
//                int numBytes = EncodeUnicodeString("foo", 0, 5);
//                That writes "foo" in unicode, which takes 6 bytes, so numBytes would be 6, but since only 3
//                of the 5 maximum characters were written, the space which the other 2 characters would have gone in
//                would instead be filled with zeros (So there would four zeroes written, since each character is
//                two bytes in unicode). numBytes would still be 6, however, even though the number of bytes which
//                were written was 10. The rationale for this is that you can easily predict the number of bytes which
//                will actually be written, since it's always maxlen+maxlen. Of course, you could also easily predict
//                the number of bytes which the string would take up, since it's just double the string's length, EXCEPT
//                that you'd really also need to check whether the string was longer than maxlen - and since this method
//                is already doing that, there's no sense in doing that twice, so that's what the method returns.
				
				
//        */
//        protected static int EncodeUnicodeString(string value, int start, int maxlen) {
//            int len=value.Length;
//            int written=Encoding.BigEndianUnicode.GetBytes(value, 0, (maxlen>len?len:maxlen), ucBuffer, start);
//            maxlen+=maxlen;
//            len+=len;
//            if (len<maxlen) {
//                EncodeZeros((maxlen-len), start+len);
//            } else {
//                len=maxlen;
//            }
//            return written;
//        }
		
//        protected static int EncodeLittleEndianUnicodeString(string value, int start, int maxlen) {
//            int len=value.Length;
//            int written=Encoding.Unicode.GetBytes(value, 0, (maxlen>len?len:maxlen), ucBuffer, start);
//            maxlen+=maxlen;
//            len+=len;
//            if (len<maxlen) {
//                EncodeZeros((maxlen-len), start+len);
//            } else {
//                len=maxlen;
//            }
//            return written;
//        }

//        protected static int EncodeString(string value, int start) {
//            Encoding.ASCII.GetBytes(value, 0, value.Length, ucBuffer, start);
//            return value.Length;
//        }

//        protected static int EncodeString(string value, int start, int maxlen) {
//            Encoding.ASCII.GetBytes(value, 0, (maxlen>value.Length?value.Length:maxlen), ucBuffer, start);
//            if (value.Length<maxlen) {
//                EncodeZeros(maxlen-value.Length, start+value.Length);
//            }
//            return maxlen;
//        }

//        //This method is used to encode more than one zeros. You should use this instead of any of the other
//        //encode methods, because this is much faster because it doesn't have to do >>s. Additionally, if you're
//        //doing more than 4 zeros, it is also faster than repeatedly calling EncodeByte.
//        protected static void EncodeZeros(int amount, int startpos) {
//            while (amount>4) {	//This is faster than the 2nd loop.
//                amount-=4;
//                ucBuffer[startpos+amount]=0;
//                ucBuffer[startpos+amount+1]=0;
//                ucBuffer[startpos+amount+2]=0;
//                ucBuffer[startpos+amount+3]=0;
//            }
//            while (amount>0) {
//                amount--;
//                ucBuffer[startpos+amount]=0;
//            }
//        }
		
//        protected static void EncodeInt(int value, int startpos) {
//            ucBuffer[startpos] = (byte) (value>>24);	//first byte
//            ucBuffer[startpos+1] = (byte) (value>>16);	//second byte
//            ucBuffer[startpos+2] = (byte) (value>>8);	//third byte
//            ucBuffer[startpos+3] = (byte) (value);		//fourth byte
//        }
//        protected static void EncodeUInt(uint value, int startpos) {
//            ucBuffer[startpos] = (byte) (value>>24);	//first byte
//            ucBuffer[startpos+1] = (byte) (value>>16);	//second byte
//            ucBuffer[startpos+2] = (byte) (value>>8);	//third byte
//            ucBuffer[startpos+3] = (byte) (value);		//fourth byte
//        }
		
//        protected static void EncodeShort(short value, int startpos) {
//            ucBuffer[startpos] = (byte) (value>>8);		//first byte
//            ucBuffer[startpos+1] = (byte) (value);		//second byte
//        }
		
//        protected static void EncodeUShort(ushort value, int startpos) {
//            ucBuffer[startpos] = (byte) (value>>8);		//first byte
//            ucBuffer[startpos+1] = (byte) (value);		//second byte
//        }
//        protected static void EncodeSByte(sbyte value, int startpos) {
//            ucBuffer[startpos]=(byte) value;
//        }
//        protected static void EncodeByte(byte value, int startpos) {
//            ucBuffer[startpos]=value;
//        }
//        protected static void EncodeBool(bool value, int startpos) {
//            ucBuffer[startpos]=(byte)(value?1:0);
//        }
//        protected static void EncodeCurMaxVals(short curval, short maxval, bool showReal, int pos) {
//            if (showReal) {
//                EncodeShort (maxval, pos);
//                EncodeShort (curval, pos+2);
//            } else {
//                EncodeShort (255, pos);
//                EncodeShort ((short)(((int)curval<<8)/maxval), pos+2);
				
//            }
//        }
		
//        [Conditional("DEBUG")]
//        protected static void OutputPacketLog(byte[] array, int len) {
//            OutputPacketLog(array, 0, len);
//        }
//        [Conditional("DEBUG")]
//        protected static void OutputPacketLog(byte[] array, int start, int len) {
//            Logger.WriteDebug("Packet Contents: ("+len+" bytes)");
//            string s = "";
//            string t = "";
//            for (int a=0; a<len; a++) {
//                t = array[a+start].ToString("X");
//                while (t.Length<2) {
//                    t="0"+t;
//                }
//                s += " "+t;
//                if (a%10==0) {
//                    Logger.WriteDebug(s);
//                    s="";
//                }
//            }
//            Logger.WriteDebug(s);
//            s="";
//            for (int a=0; a<len; a++) {
//                t = ""+(char) array[a+start];
//                if (array[a+start]<32 || array[a+start]>126) {
//                    t=""+(char) 128;
//                }
//                s += " "+t;
//                if (a%10==0) {
//                    Logger.WriteDebug(s);
//                    s="";
//                }
//            }
//            Logger.WriteDebug(s);
//        }
		
//        //This builds our compression-tables. bitAmtTable is used by some of the compression methods,
//        //but not the fastest ones.
//        //flatBitTable is faster to access than bitTable.
//        private static void ConstructBitTables() {
//            bitAmtTable = new uint[32];
//            for (int a=0; a<31; a++) {
//                bitAmtTable[a] = (uint) ((1<<a)-1);
//            }
//            bitAmtTable[31]=0xffffffff;	//our formula won't work for #31 because 1<<31 would be too big to fit in 32 bits, though (1<<31 - 1) would just fit, so we just store that...
			
//            bitTable = new int[257,2];
//            bitTable[0,0] = 0x2;
//            bitTable[0,1] = 0x0;
//            bitTable[1,0] = 0x5;
//            bitTable[1,1] = 0x1f;
//            bitTable[2,0] = 0x6;
//            bitTable[2,1] = 0x22;
//            bitTable[3,0] = 0x7;
//            bitTable[3,1] = 0x34;
//            bitTable[4,0] = 0x7;
//            bitTable[4,1] = 0x75;
//            bitTable[5,0] = 0x6;
//            bitTable[5,1] = 0x28;
//            bitTable[6,0] = 0x6;
//            bitTable[6,1] = 0x3b;
//            bitTable[7,0] = 0x7;
//            bitTable[7,1] = 0x32;
//            bitTable[8,0] = 0x8;
//            bitTable[8,1] = 0xe0;
//            bitTable[9,0] = 0x8;
//            bitTable[9,1] = 0x62;
//            bitTable[10,0] = 0x7;
//            bitTable[10,1] = 0x56;
//            bitTable[11,0] = 0x8;
//            bitTable[11,1] = 0x79;
//            bitTable[12,0] = 0x9;
//            bitTable[12,1] = 0x19d;
//            bitTable[13,0] = 0x8;
//            bitTable[13,1] = 0x97;
//            bitTable[14,0] = 0x6;
//            bitTable[14,1] = 0x2a;
//            bitTable[15,0] = 0x7;
//            bitTable[15,1] = 0x57;
//            bitTable[16,0] = 0x8;
//            bitTable[16,1] = 0x71;
//            bitTable[17,0] = 0x8;
//            bitTable[17,1] = 0x5b;
//            bitTable[18,0] = 0x9;
//            bitTable[18,1] = 0x1cc;
//            bitTable[19,0] = 0x8;
//            bitTable[19,1] = 0xa7;
//            bitTable[20,0] = 0x7;
//            bitTable[20,1] = 0x25;
//            bitTable[21,0] = 0x7;
//            bitTable[21,1] = 0x4f;
//            bitTable[22,0] = 0x8;
//            bitTable[22,1] = 0x66;
//            bitTable[23,0] = 0x8;
//            bitTable[23,1] = 0x7d;
//            bitTable[24,0] = 0x9;
//            bitTable[24,1] = 0x191;
//            bitTable[25,0] = 0x9;
//            bitTable[25,1] = 0x1ce;
//            bitTable[26,0] = 0x7;
//            bitTable[26,1] = 0x3f;
//            bitTable[27,0] = 0x9;
//            bitTable[27,1] = 0x90;
//            bitTable[28,0] = 0x8;
//            bitTable[28,1] = 0x59;
//            bitTable[29,0] = 0x8;
//            bitTable[29,1] = 0x7b;
//            bitTable[30,0] = 0x8;
//            bitTable[30,1] = 0x91;
//            bitTable[31,0] = 0x8;
//            bitTable[31,1] = 0xc6;
//            bitTable[32,0] = 0x6;
//            bitTable[32,1] = 0x2d;
//            bitTable[33,0] = 0x9;
//            bitTable[33,1] = 0x186;
//            bitTable[34,0] = 0x8;
//            bitTable[34,1] = 0x6f;
//            bitTable[35,0] = 0x9;
//            bitTable[35,1] = 0x93;
//            bitTable[36,0] = 0xa;
//            bitTable[36,1] = 0x1cc;
//            bitTable[37,0] = 0x8;
//            bitTable[37,1] = 0x5a;
//            bitTable[38,0] = 0xa;
//            bitTable[38,1] = 0x1ae;
//            bitTable[39,0] = 0xa;
//            bitTable[39,1] = 0x1c0;
//            bitTable[40,0] = 0x9;
//            bitTable[40,1] = 0x148;
//            bitTable[41,0] = 0x9;
//            bitTable[41,1] = 0x14a;
//            bitTable[42,0] = 0x9;
//            bitTable[42,1] = 0x82;
//            bitTable[43,0] = 0xa;
//            bitTable[43,1] = 0x19f;
//            bitTable[44,0] = 0x9;
//            bitTable[44,1] = 0x171;
//            bitTable[45,0] = 0x9;
//            bitTable[45,1] = 0x120;
//            bitTable[46,0] = 0x9;
//            bitTable[46,1] = 0xe7;
//            bitTable[47,0] = 0xa;
//            bitTable[47,1] = 0x1f3;
//            bitTable[48,0] = 0x9;
//            bitTable[48,1] = 0x14b;
//            bitTable[49,0] = 0x9;
//            bitTable[49,1] = 0x100;
//            bitTable[50,0] = 0x9;
//            bitTable[50,1] = 0x190;
//            bitTable[51,0] = 0x6;
//            bitTable[51,1] = 0x13;
//            bitTable[52,0] = 0x9;
//            bitTable[52,1] = 0x161;
//            bitTable[53,0] = 0x9;
//            bitTable[53,1] = 0x125;
//            bitTable[54,0] = 0x9;
//            bitTable[54,1] = 0x133;
//            bitTable[55,0] = 0x9;
//            bitTable[55,1] = 0x195;
//            bitTable[56,0] = 0x9;
//            bitTable[56,1] = 0x173;
//            bitTable[57,0] = 0x9;
//            bitTable[57,1] = 0x1ca;
//            bitTable[58,0] = 0x9;
//            bitTable[58,1] = 0x86;
//            bitTable[59,0] = 0x9;
//            bitTable[59,1] = 0x1e9;
//            bitTable[60,0] = 0x9;
//            bitTable[60,1] = 0xdb;
//            bitTable[61,0] = 0x9;
//            bitTable[61,1] = 0x1ec;
//            bitTable[62,0] = 0x9;
//            bitTable[62,1] = 0x8b;
//            bitTable[63,0] = 0x9;
//            bitTable[63,1] = 0x85;
//            bitTable[64,0] = 0x5;
//            bitTable[64,1] = 0xa;
//            bitTable[65,0] = 0x8;
//            bitTable[65,1] = 0x96;
//            bitTable[66,0] = 0x8;
//            bitTable[66,1] = 0x9c;
//            bitTable[67,0] = 0x9;
//            bitTable[67,1] = 0x1c3;
//            bitTable[68,0] = 0x9;
//            bitTable[68,1] = 0x19c;
//            bitTable[69,0] = 0x9;
//            bitTable[69,1] = 0x8f;
//            bitTable[70,0] = 0x9;
//            bitTable[70,1] = 0x18f;
//            bitTable[71,0] = 0x9;
//            bitTable[71,1] = 0x91;
//            bitTable[72,0] = 0x9;
//            bitTable[72,1] = 0x87;
//            bitTable[73,0] = 0x9;
//            bitTable[73,1] = 0xc6;
//            bitTable[74,0] = 0x9;
//            bitTable[74,1] = 0x177;
//            bitTable[75,0] = 0x9;
//            bitTable[75,1] = 0x89;
//            bitTable[76,0] = 0x9;
//            bitTable[76,1] = 0xd6;
//            bitTable[77,0] = 0x9;
//            bitTable[77,1] = 0x8c;
//            bitTable[78,0] = 0x9;
//            bitTable[78,1] = 0x1ee;
//            bitTable[79,0] = 0x9;
//            bitTable[79,1] = 0x1eb;
//            bitTable[80,0] = 0x9;
//            bitTable[80,1] = 0x84;
//            bitTable[81,0] = 0x9;
//            bitTable[81,1] = 0x164;
//            bitTable[82,0] = 0x9;
//            bitTable[82,1] = 0x175;
//            bitTable[83,0] = 0x9;
//            bitTable[83,1] = 0x1cd;
//            bitTable[84,0] = 0x8;
//            bitTable[84,1] = 0x5e;
//            bitTable[85,0] = 0x9;
//            bitTable[85,1] = 0x88;
//            bitTable[86,0] = 0x9;
//            bitTable[86,1] = 0x12b;
//            bitTable[87,0] = 0x9;
//            bitTable[87,1] = 0x172;
//            bitTable[88,0] = 0x9;
//            bitTable[88,1] = 0x10a;
//            bitTable[89,0] = 0x9;
//            bitTable[89,1] = 0x8d;
//            bitTable[90,0] = 0x9;
//            bitTable[90,1] = 0x13a;
//            bitTable[91,0] = 0x9;
//            bitTable[91,1] = 0x11c;
//            bitTable[92,0] = 0xa;
//            bitTable[92,1] = 0x1e1;
//            bitTable[93,0] = 0xa;
//            bitTable[93,1] = 0x1e0;
//            bitTable[94,0] = 0x9;
//            bitTable[94,1] = 0x187;
//            bitTable[95,0] = 0xa;
//            bitTable[95,1] = 0x1dc;
//            bitTable[96,0] = 0xa;
//            bitTable[96,1] = 0x1df;
//            bitTable[97,0] = 0x7;
//            bitTable[97,1] = 0x74;
//            bitTable[98,0] = 0x9;
//            bitTable[98,1] = 0x19f;
//            bitTable[99,0] = 0x8;
//            bitTable[99,1] = 0x8d;
//            bitTable[100,0] = 0x8;
//            bitTable[100,1] = 0xe4;
//            bitTable[101,0] = 0x7;
//            bitTable[101,1] = 0x79;
//            bitTable[102,0] = 0x9;
//            bitTable[102,1] = 0xea;
//            bitTable[103,0] = 0x9;
//            bitTable[103,1] = 0xe1;
//            bitTable[104,0] = 0x8;
//            bitTable[104,1] = 0x40;
//            bitTable[105,0] = 0x7;
//            bitTable[105,1] = 0x41;
//            bitTable[106,0] = 0x9;
//            bitTable[106,1] = 0x10b;
//            bitTable[107,0] = 0x9;
//            bitTable[107,1] = 0xb0;
//            bitTable[108,0] = 0x8;
//            bitTable[108,1] = 0x6a;
//            bitTable[109,0] = 0x8;
//            bitTable[109,1] = 0xc1;
//            bitTable[110,0] = 0x7;
//            bitTable[110,1] = 0x71;
//            bitTable[111,0] = 0x7;
//            bitTable[111,1] = 0x78;
//            bitTable[112,0] = 0x8;
//            bitTable[112,1] = 0xb1;
//            bitTable[113,0] = 0x9;
//            bitTable[113,1] = 0x14c;
//            bitTable[114,0] = 0x7;
//            bitTable[114,1] = 0x43;
//            bitTable[115,0] = 0x8;
//            bitTable[115,1] = 0x76;
//            bitTable[116,0] = 0x7;
//            bitTable[116,1] = 0x66;
//            bitTable[117,0] = 0x7;
//            bitTable[117,1] = 0x4d;
//            bitTable[118,0] = 0x9;
//            bitTable[118,1] = 0x8a;
//            bitTable[119,0] = 0x6;
//            bitTable[119,1] = 0x2f;
//            bitTable[120,0] = 0x8;
//            bitTable[120,1] = 0xc9;
//            bitTable[121,0] = 0x9;
//            bitTable[121,1] = 0xce;
//            bitTable[122,0] = 0x9;
//            bitTable[122,1] = 0x149;
//            bitTable[123,0] = 0x9;
//            bitTable[123,1] = 0x160;
//            bitTable[124,0] = 0xa;
//            bitTable[124,1] = 0x1ba;
//            bitTable[125,0] = 0xa;
//            bitTable[125,1] = 0x19e;
//            bitTable[126,0] = 0xa;
//            bitTable[126,1] = 0x39f;
//            bitTable[127,0] = 0x9;
//            bitTable[127,1] = 0xe5;
//            bitTable[128,0] = 0x9;
//            bitTable[128,1] = 0x194;
//            bitTable[129,0] = 0x9;
//            bitTable[129,1] = 0x184;
//            bitTable[130,0] = 0x9;
//            bitTable[130,1] = 0x126;
//            bitTable[131,0] = 0x7;
//            bitTable[131,1] = 0x30;
//            bitTable[132,0] = 0x8;
//            bitTable[132,1] = 0x6c;
//            bitTable[133,0] = 0x9;
//            bitTable[133,1] = 0x121;
//            bitTable[134,0] = 0x9;
//            bitTable[134,1] = 0x1e8;
//            bitTable[135,0] = 0xa;
//            bitTable[135,1] = 0x1c1;
//            bitTable[136,0] = 0xa;
//            bitTable[136,1] = 0x11d;
//            bitTable[137,0] = 0xa;
//            bitTable[137,1] = 0x163;
//            bitTable[138,0] = 0xa;
//            bitTable[138,1] = 0x385;
//            bitTable[139,0] = 0xa;
//            bitTable[139,1] = 0x3db;
//            bitTable[140,0] = 0xa;
//            bitTable[140,1] = 0x17d;
//            bitTable[141,0] = 0xa;
//            bitTable[141,1] = 0x106;
//            bitTable[142,0] = 0xa;
//            bitTable[142,1] = 0x397;
//            bitTable[143,0] = 0xa;
//            bitTable[143,1] = 0x24e;
//            bitTable[144,0] = 0x7;
//            bitTable[144,1] = 0x2e;
//            bitTable[145,0] = 0x8;
//            bitTable[145,1] = 0x98;
//            bitTable[146,0] = 0xa;
//            bitTable[146,1] = 0x33c;
//            bitTable[147,0] = 0xa;
//            bitTable[147,1] = 0x32e;
//            bitTable[148,0] = 0xa;
//            bitTable[148,1] = 0x1e9;
//            bitTable[149,0] = 0x9;
//            bitTable[149,1] = 0xbf;
//            bitTable[150,0] = 0xa;
//            bitTable[150,1] = 0x3df;
//            bitTable[151,0] = 0xa;
//            bitTable[151,1] = 0x1dd;
//            bitTable[152,0] = 0xa;
//            bitTable[152,1] = 0x32d;
//            bitTable[153,0] = 0xa;
//            bitTable[153,1] = 0x2ed;
//            bitTable[154,0] = 0xa;
//            bitTable[154,1] = 0x30b;
//            bitTable[155,0] = 0xa;
//            bitTable[155,1] = 0x107;
//            bitTable[156,0] = 0xa;
//            bitTable[156,1] = 0x2e8;
//            bitTable[157,0] = 0xa;
//            bitTable[157,1] = 0x3de;
//            bitTable[158,0] = 0xa;
//            bitTable[158,1] = 0x125;
//            bitTable[159,0] = 0xa;
//            bitTable[159,1] = 0x1e8;
//            bitTable[160,0] = 0x9;
//            bitTable[160,1] = 0xe9;
//            bitTable[161,0] = 0xa;
//            bitTable[161,1] = 0x1cd;
//            bitTable[162,0] = 0xa;
//            bitTable[162,1] = 0x1b5;
//            bitTable[163,0] = 0x9;
//            bitTable[163,1] = 0x165;
//            bitTable[164,0] = 0xa;
//            bitTable[164,1] = 0x232;
//            bitTable[165,0] = 0xa;
//            bitTable[165,1] = 0x2e1;
//            bitTable[166,0] = 0xb;
//            bitTable[166,1] = 0x3ae;
//            bitTable[167,0] = 0xb;
//            bitTable[167,1] = 0x3c6;
//            bitTable[168,0] = 0xb;
//            bitTable[168,1] = 0x3e2;
//            bitTable[169,0] = 0xa;
//            bitTable[169,1] = 0x205;
//            bitTable[170,0] = 0xa;
//            bitTable[170,1] = 0x29a;
//            bitTable[171,0] = 0xa;
//            bitTable[171,1] = 0x248;
//            bitTable[172,0] = 0xa;
//            bitTable[172,1] = 0x2cd;
//            bitTable[173,0] = 0xa;
//            bitTable[173,1] = 0x23b;
//            bitTable[174,0] = 0xb;
//            bitTable[174,1] = 0x3c5;
//            bitTable[175,0] = 0xa;
//            bitTable[175,1] = 0x251;
//            bitTable[176,0] = 0xa;
//            bitTable[176,1] = 0x2e9;
//            bitTable[177,0] = 0xa;
//            bitTable[177,1] = 0x252;
//            bitTable[178,0] = 0x9;
//            bitTable[178,1] = 0x1ea;
//            bitTable[179,0] = 0xb;
//            bitTable[179,1] = 0x3a0;
//            bitTable[180,0] = 0xb;
//            bitTable[180,1] = 0x391;
//            bitTable[181,0] = 0xa;
//            bitTable[181,1] = 0x23c;
//            bitTable[182,0] = 0xb;
//            bitTable[182,1] = 0x392;
//            bitTable[183,0] = 0xb;
//            bitTable[183,1] = 0x3d5;
//            bitTable[184,0] = 0xa;
//            bitTable[184,1] = 0x233;
//            bitTable[185,0] = 0xa;
//            bitTable[185,1] = 0x2cc;
//            bitTable[186,0] = 0xb;
//            bitTable[186,1] = 0x390;
//            bitTable[187,0] = 0xa;
//            bitTable[187,1] = 0x1bb;
//            bitTable[188,0] = 0xb;
//            bitTable[188,1] = 0x3a1;
//            bitTable[189,0] = 0xb;
//            bitTable[189,1] = 0x3c4;
//            bitTable[190,0] = 0xa;
//            bitTable[190,1] = 0x211;
//            bitTable[191,0] = 0xa;
//            bitTable[191,1] = 0x203;
//            bitTable[192,0] = 0x9;
//            bitTable[192,1] = 0x12a;
//            bitTable[193,0] = 0xa;
//            bitTable[193,1] = 0x231;
//            bitTable[194,0] = 0xb;
//            bitTable[194,1] = 0x3e0;
//            bitTable[195,0] = 0xa;
//            bitTable[195,1] = 0x29b;
//            bitTable[196,0] = 0xb;
//            bitTable[196,1] = 0x3d7;
//            bitTable[197,0] = 0xa;
//            bitTable[197,1] = 0x202;
//            bitTable[198,0] = 0xb;
//            bitTable[198,1] = 0x3ad;
//            bitTable[199,0] = 0xa;
//            bitTable[199,1] = 0x213;
//            bitTable[200,0] = 0xa;
//            bitTable[200,1] = 0x253;
//            bitTable[201,0] = 0xa;
//            bitTable[201,1] = 0x32c;
//            bitTable[202,0] = 0xa;
//            bitTable[202,1] = 0x23d;
//            bitTable[203,0] = 0xa;
//            bitTable[203,1] = 0x23f;
//            bitTable[204,0] = 0xa;
//            bitTable[204,1] = 0x32f;
//            bitTable[205,0] = 0xa;
//            bitTable[205,1] = 0x11c;
//            bitTable[206,0] = 0xa;
//            bitTable[206,1] = 0x384;
//            bitTable[207,0] = 0xa;
//            bitTable[207,1] = 0x31c;
//            bitTable[208,0] = 0xa;
//            bitTable[208,1] = 0x17c;
//            bitTable[209,0] = 0xa;
//            bitTable[209,1] = 0x30a;
//            bitTable[210,0] = 0xa;
//            bitTable[210,1] = 0x2e0;
//            bitTable[211,0] = 0xa;
//            bitTable[211,1] = 0x276;
//            bitTable[212,0] = 0xa;
//            bitTable[212,1] = 0x250;
//            bitTable[213,0] = 0xb;
//            bitTable[213,1] = 0x3e3;
//            bitTable[214,0] = 0xa;
//            bitTable[214,1] = 0x396;
//            bitTable[215,0] = 0xa;
//            bitTable[215,1] = 0x18f;
//            bitTable[216,0] = 0xa;
//            bitTable[216,1] = 0x204;
//            bitTable[217,0] = 0xa;
//            bitTable[217,1] = 0x206;
//            bitTable[218,0] = 0xa;
//            bitTable[218,1] = 0x230;
//            bitTable[219,0] = 0xa;
//            bitTable[219,1] = 0x265;
//            bitTable[220,0] = 0xa;
//            bitTable[220,1] = 0x212;
//            bitTable[221,0] = 0xa;
//            bitTable[221,1] = 0x23e;
//            bitTable[222,0] = 0xb;
//            bitTable[222,1] = 0x3ac;
//            bitTable[223,0] = 0xb;
//            bitTable[223,1] = 0x393;
//            bitTable[224,0] = 0xb;
//            bitTable[224,1] = 0x3e1;
//            bitTable[225,0] = 0xa;
//            bitTable[225,1] = 0x1de;
//            bitTable[226,0] = 0xb;
//            bitTable[226,1] = 0x3d6;
//            bitTable[227,0] = 0xa;
//            bitTable[227,1] = 0x31d;
//            bitTable[228,0] = 0xb;
//            bitTable[228,1] = 0x3e5;
//            bitTable[229,0] = 0xb;
//            bitTable[229,1] = 0x3e4;
//            bitTable[230,0] = 0xa;
//            bitTable[230,1] = 0x207;
//            bitTable[231,0] = 0xb;
//            bitTable[231,1] = 0x3c7;
//            bitTable[232,0] = 0xa;
//            bitTable[232,1] = 0x277;
//            bitTable[233,0] = 0xb;
//            bitTable[233,1] = 0x3d4;
//            bitTable[234,0] = 0x8;
//            bitTable[234,1] = 0xc0;
//            bitTable[235,0] = 0xa;
//            bitTable[235,1] = 0x162;
//            bitTable[236,0] = 0xa;
//            bitTable[236,1] = 0x3da;
//            bitTable[237,0] = 0xa;
//            bitTable[237,1] = 0x124;
//            bitTable[238,0] = 0xa;
//            bitTable[238,1] = 0x1b4;
//            bitTable[239,0] = 0xa;
//            bitTable[239,1] = 0x264;
//            bitTable[240,0] = 0xa;
//            bitTable[240,1] = 0x33d;
//            bitTable[241,0] = 0xa;
//            bitTable[241,1] = 0x1d1;
//            bitTable[242,0] = 0xa;
//            bitTable[242,1] = 0x1af;
//            bitTable[243,0] = 0xa;
//            bitTable[243,1] = 0x39e;
//            bitTable[244,0] = 0xa;
//            bitTable[244,1] = 0x24f;
//            bitTable[245,0] = 0xb;
//            bitTable[245,1] = 0x373;
//            bitTable[246,0] = 0xa;
//            bitTable[246,1] = 0x249;
//            bitTable[247,0] = 0xb;
//            bitTable[247,1] = 0x372;
//            bitTable[248,0] = 0x9;
//            bitTable[248,1] = 0x167;
//            bitTable[249,0] = 0xa;
//            bitTable[249,1] = 0x210;
//            bitTable[250,0] = 0xa;
//            bitTable[250,1] = 0x23a;
//            bitTable[251,0] = 0xa;
//            bitTable[251,1] = 0x1b8;
//            bitTable[252,0] = 0xb;
//            bitTable[252,1] = 0x3af;
//            bitTable[253,0] = 0xa;
//            bitTable[253,1] = 0x18e;
//            bitTable[254,0] = 0xa;
//            bitTable[254,1] = 0x2ec;
//            bitTable[255,0] = 0x7;
//            bitTable[255,1] = 0x62;
//            bitTable[256,0] = 0x4;
//            bitTable[256,1] = 0xd;
			
//            flatBitTable = new int [514];
//            for (int a=0; a<257; a++) {
//                flatBitTable[a<<1]=bitTable[a,0];
//                flatBitTable[(a<<1)|1]=bitTable[a,1];
//            }
			
//        }
//    }
//}