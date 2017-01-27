/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See then
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.UoData {
	//info about one item of multiItem

	public class MultiData {
		static readonly Dictionary<int, MultiData> multiItems = new Dictionary<int, MultiData>();

		private readonly List<MultiComponentDescription> parts;

		public MultiData(List<MultiComponentDescription> parts) {
			this.parts = parts;
		}

		public static MultiData GetByModel(int id) {
			MultiData retVal;
			multiItems.TryGetValue(id, out retVal);
			return retVal;
		}

		internal MultiItemComponent[] Create(ushort x, ushort y, sbyte z, Map map) {
			var n = this.parts.Count;
			var retVal = new MultiItemComponent[n];
			for (var i = 0; i < n; i++) {
				retVal[i] = this.parts[i].Create(x, y, z, map);
			}
			return retVal;
		}

		public MultiComponentDescription this[int index] => this.parts[index];

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static void Init() {
			if (Globals.UseMultiItems) {
				Console.WriteLine("Loading " + LogStr.File("Multi.mul") + " - multi items info.");

				var mulFileP = Path.Combine(Globals.MulPath, "multi.mul");
				var idxFileP = Path.Combine(Globals.MulPath, "multi.idx");

				if (File.Exists(mulFileP) && File.Exists(idxFileP)) {
					var idxfs = new FileStream(idxFileP, FileMode.Open, FileAccess.Read);
					var mulfs = new FileStream(mulFileP, FileMode.Open, FileAccess.Read);
					var idxbr = new BinaryReader(idxfs);
					var mulbr = new BinaryReader(mulfs);

					int slots = 0, items = 0;

					try {
						while (true) {
							int pos;
							if ((pos = idxbr.ReadInt32()) != -1) {
								var length = idxbr.ReadInt32();
								idxbr.BaseStream.Seek(4, SeekOrigin.Current);

								if (mulbr.BaseStream.Position != pos) {
									mulbr.BaseStream.Seek(pos, SeekOrigin.Begin);
								}
								var listOfItems = new List<MultiComponentDescription>(length / 12);
								for (var i = 0; i < length / 12; i++) {
									var id = mulbr.ReadUInt16();
									var offsetX = mulbr.ReadInt16();
									var offsetY = mulbr.ReadInt16();
									var offsetZ = mulbr.ReadInt16();
									var flags = mulbr.ReadInt32();

									//s nulou jsou "stiny" ocekavanych dynamickych itemu, jako treba dveri.
									if (flags > 0) {
										var part = new MultiComponentDescription(id, offsetX, offsetY, offsetZ, flags);
										listOfItems.Add(part);
									}
								}
								multiItems[slots + 16384] = new MultiData(listOfItems);
								items++;
							} else {
								idxbr.BaseStream.Seek(8, SeekOrigin.Current);
							}
							slots++;
						}
					} catch (EndOfStreamException) {
					} catch (Exception e) {
						Logger.WriteWarning("Exception while reading Multi.mul/idx", e);
					} finally {
						Logger.WriteDebug("Num of multiItem slots: " + slots);
						Logger.WriteDebug("Num of multiItems: " + items);
					}

					Logger.WriteDebug("Finished loading multi.mul");
					mulbr.Close();
					idxbr.Close();

					if (Globals.WriteMulDocsFiles) {
						var docsw = File.CreateText(Globals.GetMulDocPathFor("MultiItems.txt"));

						foreach (var entry in multiItems) {
							docsw.WriteLine("Item: " + entry.Key + "\t Num parts: " + entry.Value.parts.Count);
							foreach (var p in entry.Value.parts) {
								docsw.WriteLine("\t ItemId: " + p.ItemId);
								docsw.WriteLine("\t Offset X: " + p.OffsetX);
								docsw.WriteLine("\t Offset Y: " + p.OffsetY);
								docsw.WriteLine("\t Offset Z: " + p.OffsetZ);
								//docsw.WriteLine("\t Visible: "+p.flags);
								docsw.WriteLine();
							}
							docsw.WriteLine();
						}
						docsw.Close();
					}
				} else {
					Logger.WriteCritical("Unable to locate multi.idx or multi.mul. We're gonna crash soon ;)");
				}
			} else {
				Logger.WriteWarning("Ignoring multi.mul");
			}

		}
	}
}
