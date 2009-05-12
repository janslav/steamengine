////This software is released under GNU internal license. See details in the URL: 
////http://www.gnu.org/copyleft/gpl.html 

//using System;
//using System.IO;
//using System.Diagnostics;
//using System.Runtime.Serialization;
//using System.Collections;
//using System.Net;
//using System.Net.Sockets;
//using SteamEngine.Common;
//using System.Text;
//using SteamEngine;

//namespace SteamEngine.Packets {

//    internal class PacketBase : object {

//        internal PacketBase() {
//        }

//        //internal void EncodeUnicodeStringInArray(string value, byte[] array, int start) {
//        //    Encoding.BigEndianUnicode.GetBytes(value, 0, value.Length, array, start);
//        //    /*int len=value.Length*2;
//        //    for (int a=start; a<start+len; a+=2) {
//        //        byte temp=array[a];//switching "endianness"...? -tar
//        //        array[a]=array[a+1];
//        //        array[a+1]=temp;
//        //    }*/
//        //}

//        //internal int EncodeUnicodeStringInArray(string value, byte[] array, int start, int maxlen) {
//        //    int len=value.Length*2;
//        //    Encoding.BigEndianUnicode.GetBytes(value, 0, (maxlen>len?len:maxlen), array, start);
//        //    if (len<maxlen) {
//        //        for (int a=start+len; a<start+maxlen; a++) {
//        //            array[a]=0;
//        //        }
//        //    } else {
//        //        len=maxlen;
//        //    }
//        //    /*for (int a=start; a<start+len; a+=2) {
//        //        byte temp=array[a];
//        //        array[a]=array[a+1];
//        //        array[a+1]=temp;
//        //    }*/
//        //    return maxlen;
//        //}

//        //internal int EncodeStringInArray(string value, byte[] array, int start) {
//        //    Encoding.UTF8.GetBytes(value, 0, value.Length, array, start);
//        //    return value.Length;
//        //}

//        //internal int EncodeStringInArray(string value, byte[] array, int start, int maxlen) {
//        //    Encoding.UTF8.GetBytes(value, 0, (maxlen>value.Length?value.Length:maxlen), array, start);
//        //    if (value.Length<maxlen) {
//        //        for (int a=start+value.Length; a<start+maxlen; a++) {
//        //            array[a]=0;
//        //        }
//        //    }
//        //    return maxlen;
//        //}


//        //internal void EncodeIntInArray(int value, byte[] array, int startpos) {
//        //    array[startpos] = (byte) (value>>24);	//first byte
//        //    array[startpos+1] = (byte) (value>>16);	//second byte
//        //    array[startpos+2] = (byte) (value>>8);	//third byte
//        //    array[startpos+3] = (byte) (value);	//fourth byte
//        //}
//        //internal void EncodeUIntInArray(uint value, byte[] array, int startpos) {
//        //    array[startpos] = (byte) (value>>24);	//first byte
//        //    array[startpos+1] = (byte) (value>>16);	//second byte
//        //    array[startpos+2] = (byte) (value>>8);	//third byte
//        //    array[startpos+3] = (byte) (value);	//fourth byte
//        //}

//        //internal void EncodeShortInArray(short value, byte[] array, int startpos) {
//        //    array[startpos] = (byte) (value>>8);	//third byte
//        //    array[startpos+1] = (byte) (value);	//fourth byte
//        //}

//        //internal void EncodeUShortInArray(ushort value, byte[] array, int startpos) {
//        //    array[startpos] = (byte) (value>>8);	//third byte
//        //    array[startpos+1] = (byte) (value);	//fourth byte
//        //}
//        //internal void EncodeSByteInArray(sbyte value, byte[] array, int startpos) {
//        //    array[startpos]=(byte) value;
//        //}

//        [Conditional("DEBUG")]
//        internal static void OutputPacketLog(Byte[] array, int len) {
//            Logger.WriteDebug("Packet Contents: (" + len + " bytes)");
//            string s = "";
//            string t = "";
//            for (int a = 0; a < len; a++) {
//                t = array[a].ToString("X", System.Globalization.CultureInfo.InvariantCulture);
//                while (t.Length < 2) {
//                    t = "0" + t;
//                }
//                s += " " + t;
//                if (a % 10 == 0) {
//                    Logger.WriteDebug(s);
//                    s = "";
//                }
//            }
//            Logger.WriteDebug(s);
//            s = "";
//            for (int a = 0; a < len; a++) {
//                t = "" + (char) array[a];
//                if (array[a] < 32 || array[a] > 126) {
//                    t = "" + (char) 128;
//                }
//                s += " " + t;
//                if (a % 10 == 0) {
//                    Logger.WriteDebug(s);
//                    s = "";
//                }
//            }
//            Logger.WriteDebug(s);
//        }



//    }
//}