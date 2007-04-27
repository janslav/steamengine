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

namespace SteamEngine.Packets {
	
	public class PacketStats {
		#if USEFASTDLL
		protected const int numCompressMethods = 14;
		#else
		protected const int numCompressMethods = 6;
		#endif
		private static string[] compressMethodNames = {
			"Compress1F", "Compress2F", "Compress3F", "Compress4F", "Compress5F", "Compress6F",
			"Crushify8", "Crushify10", "Crushify11", "Crushify12", "Crushify13", "Crushify14",
			"Crushify10no3", "Crushify13no10"};
		
		private static Statistics compressionStats;
		private static StatType[] compression = new StatType[numCompressMethods];
		private const int numSizeClasses=(Server.maxPacketLen>>3)+1;
		private static string s_Bytes = "Bytes";
		private static string s_Packets = "Packets";
		private static string s_Time = "Time needed to compress";
		private static string BYTES(int sizeClass) {
			int minSize=sizeClass<<3;
			int maxSize=((sizeClass+1)<<3)-1;
			return ""+minSize+" to "+maxSize+" bytes";
		}
		private static string s_OfSizeClass(int sizeClass) {
			return " for packets of size ["+BYTES(sizeClass)+"]";
		}
		private static string PID(int id) {
			string pid=id.ToString("x");
			while (pid.Length<2) {
				pid="0"+pid;
			}
			return "0x"+pid;
		}
		private static string s_OfPacketID(int id) {
			return " for packet ID "+PID(id);
		}
		private static string s_OfSize(int size) {
			return s_OfSizeClass(size>>3);
		}
				
		private static string sd_SpeedBytes="Compression speed in bytes per millisecond (higher is better)";
		private static string sd_SpeedPackets="Compression speed in packets per millisecond (higher is better)";
		
		static PacketStats() {
			compressionStats=new Statistics("Packets - Compression Methods");
			//compressionStats.MinEntries=0;
			//compressionStats.DiscardEntries=0;
			
			for (int method=0; method<numCompressMethods; method++) {
				compression[method]=compressionStats.AddType(compressMethodNames[method]);
				compression[method].NoWarnOnMissing=true;
			}
			StatType compression_compress1f=compression[0];
			
			compression_compress1f.Value("L",s_Bytes);
			compression_compress1f.Value("L",s_Packets);
			compression_compress1f.Value("M5",s_Time);
			
			compression_compress1f.Rate(5, s_Bytes, s_Time, sd_SpeedBytes);
			compression_compress1f.Rate(5, s_Packets, s_Time, sd_SpeedPackets);
			
			for (int sizeClass=0; sizeClass<numSizeClasses; sizeClass++) {
				string BVar = s_Bytes+s_OfSizeClass(sizeClass);
				string PVar = s_Packets+s_OfSizeClass(sizeClass);
				string TVar = s_Time+s_OfSizeClass(sizeClass);
				
				compression_compress1f.Value("L",BVar, PVar, 10);
				compression_compress1f.Value("L",PVar, PVar, 10);
				compression_compress1f.Value("M5",TVar, PVar, 10);
				
				compression_compress1f.Rate(5, BVar, TVar, "["+BYTES(sizeClass)+"] "+sd_SpeedBytes, PVar, 10);
				compression_compress1f.Rate(5, PVar, TVar, "["+BYTES(sizeClass)+"] "+sd_SpeedPackets, PVar, 10);
			}
			
			for (int packetID=0; packetID<256; packetID++) {
				string BVar = s_Bytes+s_OfPacketID(packetID);
				string PVar = s_Packets+s_OfPacketID(packetID);
				string TVar = s_Time+s_OfPacketID(packetID);
				
				//typecode, variable name, variable to check for minimum amount to show, minimum amount of the checked var needed to show this var.
				compression_compress1f.Value("L", BVar, PVar, 10);
				compression_compress1f.Value("L", PVar, PVar, 10);
				compression_compress1f.Value("M5", TVar, PVar, 10);
				
				compression_compress1f.Rate(5, BVar, TVar, "["+PID(packetID)+"] "+sd_SpeedBytes, PVar, 10);
				compression_compress1f.Rate(5, PVar, TVar, "["+PID(packetID)+"] "+sd_SpeedPackets, PVar, 10);
			}
			
			for (int method=1; method<numCompressMethods; method++) {
				compression[method].SameValuesAs(compression_compress1f);
				compression[method].SameRatesAs(compression_compress1f);
			}
		}
		
		public static void CompressionStats() {
			compressionStats.ShowAllStats(true, true);
		}
		
		protected static void AddEntry(int method, int bytes, int packets, long time, int packetID) {
			StatType type = compression[method];
			StatEntry entry = type.NewEntry();
			entry[s_Bytes]=bytes;
			entry[s_Packets]=packets;
			entry[s_Time]=time;
			
			//Add to the appropriate sizeClass.
			string PSub = s_OfSize(bytes);
			//Logger.WriteInfo(true,"PSub OFS="+PSub);
			entry[s_Bytes+PSub]=bytes;
			entry[s_Packets+PSub]=packets;
			entry[s_Time+PSub]=time;
			
			//Add to the appropriate packet ID.
			PSub = s_OfPacketID(packetID);
			//Logger.WriteInfo(true,"PSub PID="+PSub);
			entry[s_Bytes+PSub]=bytes;
			entry[s_Packets+PSub]=packets;
			entry[s_Time+PSub]=time;
			
			type.AddEntry(entry);
		}
	}
}