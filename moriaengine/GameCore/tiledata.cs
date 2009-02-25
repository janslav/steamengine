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
using System.IO;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine {
	public class TileData {
		public const uint numLandTiles = 16384;
		public static uint[] landFlags;

		public static readonly string[] flagNames = new string[] {"background","weapon","transparent","translucent","wall",
			"damaging","impassable","wet","unknown","surface","bridge","stackable","window",
			"noshoot","prefixA","prefixAn","internal","foliage","partialHue","unknown_1","map",
			"container","wearable", "lightSource", "animated", "noDiagonal", "unknown_2", "armor", "roof",
			"door", "stairBack", "stairRight"};

		public const uint flag_background = 0x00000001;	//No idea. None whatsoever. Maybe it's the blackness.
		public const uint flag_weapon = 0x00000002;	//I smack thee with this here ... club?
		public const uint flag_transparent = 0x00000004;	//Yeah. So we can see through it?
		public const uint flag_translucent = 0x00000008;	//Okay...
		public const uint flag_wall = 0x00000010;	//Hey look, we can't walk through it!
		public const uint flag_damaging = 0x00000020;	//Lava, perhaps? Fires, hmm!
		public const uint flag_impassable = 0x00000040;	//Mountains and stuff, I'll wager.
		public const uint flag_wet = 0x00000080;	//Water? Or mud? Or a slick road in a rainstorm? Probably the first.
		public const uint flag_unknown = 0x00000100;	//Uh...
		public const uint flag_surface = 0x00000200;	//Tables or something?
		public const uint flag_bridge = 0x00000400;	//I wonder why they'd have a flag for that.
		public const uint flag_stackable = 0x00000800;
		public const uint flag_generic = 0x00000800;
		public const uint flag_window = 0x00001000;	//So we can see/shoot out?
		public const uint flag_noshoot = 0x00002000;	//? We can't shoot out or something? So, like a glass window maybe?
		public const uint flag_prefixA = 0x00004000;	//_A_ card
		public const uint flag_prefixAn = 0x00008000;	//_An_ apple
		public const uint flag_internal = 0x00010000;	//hair, beards, etc
		public const uint flag_foliage = 0x00020000;	//Probably bushes and tree leaves and stuff.
		public const uint flag_partialHue = 0x00040000;	//semi-glowy? (Or maybe it can accept the 08000 color flag)
		public const uint flag_unknown_1 = 0x00080000;	//Well, gee. I should see if it's used on anything...
		public const uint flag_map = 0x00100000;	//Sounds good to me.
		public const uint flag_container = 0x00200000;	//They flag these!?
		public const uint flag_wearable = 0x00400000;	//Omigod!
		public const uint flag_lightSource = 0x00800000;	//I'm getting tired of typing repetitive shiznit now.
		public const uint flag_animated = 0x01000000;	//Like fire again. And stuff. Those spinny propeller thingies!
		public const uint flag_noDiagonal = 0x02000000;	//!?!???!!?
		public const uint flag_unknown_2 = 0x04000000;	//I really hope some of these unknowns are n/w/s/e facing flags.
		public const uint flag_armor = 0x08000000;	//Armor, okay, so does that count shields? Hmmm?
		public const uint flag_roof = 0x10000000;	//"Don't fall through me!" Or why isn't it just flagged surface or something?
		public const uint flag_door = 0x20000000;	//Okay...
		public const uint flag_stairBack = 0x40000000;	//Don't we have stairs that go forward or left too? This could cover both...
		public const uint flag_stairRight = 0x80000000;	//Well, whatever, you can climb them, so, hey... Good use for a flag.
		//Yay, that's all the flags!

		public static string WriteToString(uint flags) {
			string result = "";
			for (int a = 0; a < 32; a++) {
				if (HasFlagNum(flags, a)) {
					result += " " + flagNames[a];
				}
			}
			if (result.Length > 0) {
				result = result.Substring(1);
			} else {
				result = "None";
			}
			return result;
		}

		public static bool HasFlag(uint whatsit, uint flag) {
			return ((whatsit & flag) == flag);
		}

		public static uint GetFlagFromFlagNum(int flagNum) {
			uint flag = (uint) (1 << flagNum);
			return flag;
		}

		public static bool HasFlagNum(uint whatsit, int flagNum) {
			uint flag = (uint) (1 << flagNum);
			return ((whatsit & flag) == flag);
		}

		public static bool IsIgnoredByMovement(ushort id) {
			return (id == 2 || id == 0x1DB || (id >= 0x1AE && id <= 0x1B5));
		}

		public static void Init() {
			string mulFileP = Path.Combine(Globals.mulPath, "tiledata.mul");
			Logger.WriteDebug("Loading " + LogStr.File("tiledata.mul") + " - terrain tile info.");
			// Kemoc - filenames will be solved later
			//#if MONO
			//if (!mulReader.Exists(tileDataFile)) {
			//	tileDataFile="tiledata.mul";
			//}
			//#endif
			if (File.Exists(mulFileP)) {
				landFlags = new uint[numLandTiles];

				FileStream mulfs = new FileStream(mulFileP, FileMode.Open, FileAccess.Read);
				BinaryReader mulbr = new BinaryReader(mulfs);
				StreamWriter mtfi = null;

				if (Globals.writeMulDocsFiles) {
					mtfi = File.CreateText(Globals.GetMulDocPathFor("TileData - map tiles.txt"));
				}
				ushort texId;
				int tileId = 0;
				string tileNameS;
				for (int block = 0; block < 512; block++) {
					mulbr.BaseStream.Seek(4, SeekOrigin.Current);	//header
					for (int tileNum = 0; tileNum < 32; tileNum++) {
						landFlags[tileId] = mulbr.ReadUInt32();
						texId = mulbr.ReadUInt16();
						tileNameS = Utility.GetCAsciiString(mulbr, 20);
						if (Globals.writeMulDocsFiles) {
							if (tileNameS.Length > 0 || texId != 0) {
								mtfi.WriteLine("TileID: 0x" + tileId.ToString("x") + " (" + tileId + ")\tName: " + tileNameS + "\tFlags: " + WriteToString(landFlags[tileId]) + "\ttexId: " + texId);
							}
						}
						tileId++;
					}
				}
				if (Globals.writeMulDocsFiles) {
					mtfi.Close();
				}
				Logger.WriteDebug("Loading " + LogStr.File("tiledata.mul") + " - item dispid info.");
				long bytes = 0;
				//bool multipleModels=false;	// Kemoc - I didnt find reason for this
				//uint lastAmount=0;
				//int dispIDNum=0;
				//bool firstModel=false;
				//ItemDispidInfo cur=null;
				ItemDispidInfo idi = null;
				uint flags;
				byte weight;
				byte quality;
				ushort unknown;
				byte minItemsToDisplayThisArt;
				byte quantity;
				ushort animID;
				byte unknown2;
				byte hue;
				ushort unknown3;
				byte height;

				try {
					while (true) {
						mulbr.BaseStream.Seek(4, SeekOrigin.Current);	//header
						for (int tileNum = 0; tileNum < 32; tileNum++) {
							flags = mulbr.ReadUInt32();
							weight = mulbr.ReadByte();
							quality = mulbr.ReadByte();
							unknown = mulbr.ReadUInt16();
							minItemsToDisplayThisArt = mulbr.ReadByte();
							quantity = mulbr.ReadByte();
							animID = mulbr.ReadUInt16();
							unknown2 = mulbr.ReadByte();
							hue = mulbr.ReadByte();
							unknown3 = mulbr.ReadUInt16();
							height = mulbr.ReadByte();
							tileNameS = Utility.GetCAsciiString(mulbr, 20);
							bytes += 20 + tileNameS.Length * 2;
							idi = new ItemDispidInfo(flags, weight, quality, unknown, minItemsToDisplayThisArt, quantity, animID, unknown2, hue, unknown3, height, tileNameS);
							/* Kemoc - not used for anything
							if (idi==null || multipleModels==false || idi.unknown<=lastAmount || (!(HasFlag(idi.flags,flag_stackable)))) {
								if (HasFlag(idi.flags,flag_stackable)) {
									multipleModels=true;
									firstModel=true;
									cur=idi;
								} else {
									multipleModels=false;
								}
							}
							if (HasFlag(idi.flags,flag_stackable)) {
								lastAmount=idi.unknown;
								//string aname = "Unnamed";
								//if (idi.name.Length>0) {
								//	aname=idi.name;
								//}
								//int amt=idi.unknown;
								//if (firstModel) {
								//	amt=1;
								//}
								//scr.WriteLine("stackmodel="+amt+":0x"+a.ToString("x")+":"+name);
								//firstModel=false;
								//dunno what does the firstModel and aname variable mean, but they wasn't used... maybe the values should be set back to the info instance somehow...?
							}
							*/
							//dispIDNum++;	// Kemoc - not used
						}
					}
				} catch (EndOfStreamException) {
				}
				long kilobytes = bytes / 1024;
				long megabytes = bytes / (1024 * 1024);
				Console.WriteLine("Finished loading tiledata.mul, item dispid info takes about " + LogStr.Number(bytes) + " bytes or " + LogStr.Number(kilobytes) + " KB or " + LogStr.Number(megabytes) + " MB (of RAM).");
				mulbr.Close();
			} else {
				Logger.WriteCritical("Unable to locate tiledata.mul. We're gonna crash soon ;)");
			}
		}

		public static void GenerateMissingDefs() {
			if (Globals.generateMissingDefs) {
				string itemdefsPath = Tools.CombineMultiplePaths(Globals.scriptsPath, "defaults", "itemdefs");
				Tools.EnsureDirectory(itemdefsPath, true);

				StreamWriter scr = File.AppendText(Path.Combine(itemdefsPath, "newItemDefsFromMuls.def"));
				StreamWriter nonexistant = null;
				string filepath = null;
				if (Globals.writeMulDocsFiles) {
					filepath = Globals.GetMulDocPathFor("Free (unused) itemID numbers.txt");
					nonexistant = File.CreateText(filepath);
				}
				int numWritten = 0;
				int numNonexistant = 0;
				int lastItemNum = 0;
				ItemDispidInfo lastItem = null;
				for (int a = 0; a < ItemDispidInfo.Num(); a++) {
					AbstractItemDef def = ThingDef.FindItemDef(a);
					if (def == null) {
						ItemDispidInfo idi = ItemDispidInfo.Get(a);
						if (idi.isEmpty) {
							numNonexistant++;
							if (Globals.writeMulDocsFiles) {
								nonexistant.WriteLine("0x" + a.ToString("x"));
							}
						} else {
							if (numWritten == 0) {
								scr.WriteLine("\n\n\n//-------------- Written " + DateTime.Now.ToString() + " --------------//\n");
							}
							numWritten++;
							if (lastItem == null || !lastItem.Equals(idi)) {
								lastItem = idi;
								lastItemNum = a;
								string name = "Unnamed";
								if (idi.singularName.Length > 0) {
									name = idi.singularName;
								}
								//defname, category, subsection, description
								scr.WriteLine("");
								string type = "ItemDef";
								if (HasFlag(idi.flags, flag_wearable)) {
									type = "EquippableDef";
								}
								if (HasFlag(idi.flags, flag_container)) {
									type = "ContainerDef";
								}
								scr.WriteLine("[" + type + " 0x" + a.ToString("x") + "]");
								scr.WriteLine("Model=0x" + a.ToString("x"));
								scr.WriteLine("Name=\"" + name + "\"");
								if (HasFlag(idi.flags, flag_wearable)) {
									scr.WriteLine("Layer=" + idi.quality);
								} else {
									scr.WriteLine("//Quality=" + idi.quality);
								}
								if (HasFlag(idi.flags, flag_container)) {
									scr.WriteLine("//MaxContents=" + idi.height);
								} else {
									scr.WriteLine("//Height=" + idi.height);
								}
								scr.WriteLine("//Weight=" + idi.weight);
								scr.WriteLine("//Unknown=" + idi.unknown);
								scr.WriteLine("//Min Items to display this art=" + idi.minItemsToDisplayThisArt);
								scr.WriteLine("//AnimID=0x" + idi.animID.ToString("x"));
								scr.WriteLine("//Quantity (Wpn/armor type)=" + idi.quantity);
								scr.WriteLine("//Unknown2=" + idi.unknown2);
								scr.WriteLine("//Hue=" + idi.hue);
								scr.WriteLine("//Unknown3=" + idi.unknown3);
								//if (idi.weight==255) {
								//    scr.WriteLine("Flag=attr_move_never");
								//}
								//if (HasFlag(idi.flags,flag_noshoot)) {
								//    scr.WriteLine("Flag=attr_block");
								//}
								//if (HasFlag(idi.flags,flag_window) || (HasFlag(idi.flags,flag_impassable) && !HasFlag(idi.flags,flag_noshoot))) {
								//    scr.WriteLine("Flag=attr_cover");
								//}
								if (HasFlag(idi.flags, flag_stackable)) {
									scr.WriteLine("Stackable=1");
								}
								scr.WriteLine("//Tiledata flags = " + WriteToString(idi.flags));
							} else {
								string type = "ItemDef";
								if (HasFlag(idi.flags, flag_wearable)) {
									type = "EquippableDef";
								}
								if (HasFlag(idi.flags, flag_container)) {
									type = "ContainerDef";
								}
								scr.WriteLine("[" + type + " 0x" + a.ToString("x") + "]");
								scr.WriteLine("Model=0x" + a.ToString("x"));
								scr.WriteLine("DupeItem=0x" + lastItemNum.ToString("x"));
							}

						}
					}
				}
				scr.Close();
				if (Globals.writeMulDocsFiles) {
					nonexistant.Close();
				}
				if (numWritten > 0) {
					Console.WriteLine("Wrote " + numWritten + " basic itemdefs (for which there were no itemdefs in the scripts), to " + itemdefsPath + "/fromTileData.def.");
				}
				if (Globals.writeMulDocsFiles && (numNonexistant > 0)) {
					Console.WriteLine("Tiledata.mul appears to contain " + numNonexistant + " free slots. These have been recorded in " + filepath + ".");
				}
				if (Globals.writeMulDocsFiles) {
					DumpInfoFromTileData();
				}
			}
		}

		public static void DumpInfoFromTileData() {
			string tiledataDocPath = Globals.GetMulDocPathFor("tiledata");
			Tools.EnsureDirectory(tiledataDocPath, true);

			StreamWriter scr = new StreamWriter(Path.Combine(tiledataDocPath, "item dispids.txt"));

			StreamWriter[] sw = new StreamWriter[32];
			for (int flagNum = 0; flagNum < 32; flagNum++) {
				sw[flagNum] = File.CreateText(Path.Combine(tiledataDocPath, flagNames[flagNum] + ".txt"));
				sw[flagNum].WriteLine("//This lists all items with the " + flagNames[flagNum] + " flag (0x" + GetFlagFromFlagNum(flagNum).ToString("x") + ")");
			}
			scr.WriteLine("//This is not a script.");
			for (int a = 0; a < ItemDispidInfo.Num(); a++) {
				ItemDispidInfo idi = ItemDispidInfo.Get(a);
				scr.WriteLine("");
				scr.WriteLine("[Dispid 0x" + a.ToString("x") + "]");
				string name = "Unnamed";
				if (idi.singularName.Length > 0) {
					name = idi.singularName;
				}
				scr.WriteLine("Name=" + name);
				if (HasFlag(idi.flags, flag_wearable)) {
					scr.WriteLine("Layer=" + idi.quality);
				} else {
					scr.WriteLine("Quality=" + idi.quality);
				}
				if (HasFlag(idi.flags, flag_container)) {
					scr.WriteLine("Height, or Size (Determines the max # of items that can be held in it?)=" + idi.height);
				} else {
					scr.WriteLine("Height=" + idi.height);
				}
				scr.WriteLine("Weight=" + idi.weight);
				if (idi.weight == 255) {
					scr.WriteLine("\t(Too heavy to move)");
				}
				scr.WriteLine("Unknown=" + idi.unknown);
				scr.WriteLine("Min Items to display this art (Probably used by the client with stackables)=" + idi.minItemsToDisplayThisArt);
				scr.WriteLine("AnimID=0x" + idi.animID.ToString("x"));
				scr.WriteLine("Quantity (Wpn/armor type)=" + idi.quantity);
				scr.WriteLine("Unknown2=" + idi.unknown2);
				scr.WriteLine("Hue=" + idi.hue);
				scr.WriteLine("Unknown3=" + idi.unknown3);
				scr.WriteLine("Tiledata flags = " + WriteToString(idi.flags));

				for (int flagNum = 0; flagNum < 32; flagNum++) {
					if (HasFlagNum(idi.flags, flagNum)) {
						sw[flagNum].WriteLine("0x" + a.ToString("x") + ") " + name);

					}
				}


			}
			for (int flagNum = 0; flagNum < 32; flagNum++) {
				sw[flagNum].Close();
			}
			scr.Close();
		}
	}
}
