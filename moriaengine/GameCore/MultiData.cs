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
using System.IO;
using SteamEngine.Common;
using System.Collections;
using System.Collections.Generic;

namespace SteamEngine {
	public class MultiItemComponent : Static {
		internal MultiItemComponent prevInList;
		internal MultiItemComponent nextInList;
		internal MultiComponentLinkedList collection;

		public MultiComponentDescription mcd;
		private readonly int multiFlags;

		internal MultiItemComponent(MultiComponentDescription mcd, ushort id, byte m, int multiFlags)
				: base(id, m) {
			this.mcd = mcd;
			this.multiFlags = multiFlags;
		}

		internal void SetRelativePos(ushort centerX, ushort centerY, sbyte centerZ) {
			this.x = (ushort) (centerX+mcd.offsetX);
			this.y = (ushort) (centerY+mcd.offsetY);
			this.z = (sbyte) (centerZ+mcd.offsetZ);
		}

		//useless?
		public int MultiFlags { get {
			return multiFlags;
		} }
	}

	//info about one item of multiItem
	public class MultiComponentDescription {
		public readonly ushort itemID;
		public readonly short offsetX;
		public readonly short offsetY;
		public readonly short offsetZ;
		public readonly int flags;

		public MultiComponentDescription(ushort id, short offsetX, short offsetY, short offsetZ, int flags) {
			this.itemID = id;
			this.offsetX = offsetX;
			this.offsetY = offsetY;
			this.offsetZ = offsetZ;
			this.flags = flags;
		}

		internal MultiItemComponent Create(ushort centerX, ushort centerY, sbyte centerZ, byte m) {
			MultiItemComponent retVal = new MultiItemComponent(this, itemID, m, flags);
			retVal.SetRelativePos(centerX, centerY, centerZ);
			return retVal;
		}
	}

	public class MultiData {
		static Dictionary<int, MultiData> multiItems = new Dictionary<int, MultiData>();

		private readonly List<MultiComponentDescription> parts;

		public MultiData(int numI) {
			parts = new List<MultiComponentDescription>(numI);
		}

		public static MultiData Get(int id) {
			MultiData retVal;
			multiItems.TryGetValue(id, out retVal);
			return retVal;
		}

		internal MultiItemComponent[] Create(ushort x, ushort y, sbyte z, byte m) {
			int n = parts.Count;
			MultiItemComponent[] retVal = new MultiItemComponent[n];
			for (int i = 0; i<n; i++) {
				retVal[i] = parts[i].Create(x, y, z, m);
			}
			return retVal;
		}

		public MultiComponentDescription this[int index] {
			get {
			return parts[index];
		} }

		public static void Init() {
			if (Globals.useMultiItems) {
				Console.WriteLine("Loading "+LogStr.File("Multi.mul")+" - multi items info.");

				string mulFileP = Path.Combine(Globals.mulPath, "multi.mul");
				string idxFileP = Path.Combine(Globals.mulPath, "multi.idx");

				if (File.Exists(mulFileP) && File.Exists(idxFileP)) {
					FileStream idxfs = new FileStream(idxFileP, FileMode.Open, FileAccess.Read);
					FileStream mulfs = new FileStream(mulFileP, FileMode.Open, FileAccess.Read);
					BinaryReader idxbr = new BinaryReader(idxfs);
					BinaryReader mulbr = new BinaryReader(mulfs);

					int pos, length;
					int slots=0, items=0;
					MultiData listOfItems;
					MultiComponentDescription part;

					try {
						while (true) {
							if ((pos=idxbr.ReadInt32()) != -1) {
								length = idxbr.ReadInt32();
								idxbr.BaseStream.Seek(4, SeekOrigin.Current);

								if (mulbr.BaseStream.Position != pos) {
									mulbr.BaseStream.Seek(pos, SeekOrigin.Begin);
								}
								listOfItems = new MultiData(length/12);
								for (int i=0; i<length/12; i++) {
									ushort id = mulbr.ReadUInt16();
									short offsetX = mulbr.ReadInt16();
									short offsetY = mulbr.ReadInt16();
									short offsetZ = mulbr.ReadInt16();
									int flags = mulbr.ReadInt32();

									//s nulou jsou "stiny" ocekavanych dynamickych itemu, jako treba dveri.
									if (flags > 0) {
										part = new MultiComponentDescription(id, offsetX, offsetY, offsetZ, flags);
										listOfItems.parts.Add(part);
									}
								}
								multiItems[slots + 16384] = listOfItems;
								items++;
							} else {
								idxbr.BaseStream.Seek(8, SeekOrigin.Current);
							}
							slots++;
						}
					} catch (EndOfStreamException) {
					} catch (Exception e) {
						Logger.WriteWarning("Exceptio while reading Multi.mul/idx", e);
					}
					finally {
						Logger.WriteDebug("Num of multiItem slots: " + slots);
						Logger.WriteDebug("Num of multiItems: " + items);
					}

					Logger.WriteDebug("Finished loading multi.mul");
					mulbr.Close();
					idxbr.Close();

					if (Globals.writeMulDocsFiles) {
						StreamWriter docsw = File.CreateText(Globals.GetMulDocPathFor("MultiItems.txt"));

						foreach (KeyValuePair<int,MultiData> entry in multiItems) {
							docsw.WriteLine("Item: " + entry.Key + "\t Num parts: " + entry.Value.parts.Count);
							foreach (MultiComponentDescription p in entry.Value.parts) {
								docsw.WriteLine("\t ItemId: "+p.itemID);
								docsw.WriteLine("\t Offset X: "+p.offsetX);
								docsw.WriteLine("\t Offset Y: "+p.offsetY);
								docsw.WriteLine("\t Offset Z: "+p.offsetZ);
								//docsw.WriteLine("\t Visible: "+p.flags);
								docsw.WriteLine();
							}
							docsw.WriteLine();
						}
						docsw.Close();
					}
				}  else {
					Logger.WriteCritical("Unable to locate multi.idx or multi.mul. We're gonna crash soon ;)");
				}
			} else {
				Logger.WriteWarning("Ignoring multi.mul");
			}

		}
	}

	//for storing of MultiItemComponents in sectors
	internal class MultiComponentLinkedList : IEnumerable {
		internal MultiItemComponent firstMultiComponent;
		internal ushort count;

		internal MultiComponentLinkedList() {
		}

		internal void Add(MultiItemComponent multiComponent) {
			Sanity.IfTrueThrow((multiComponent.prevInList != null || multiComponent.nextInList != null),
				"'"+multiComponent+"' being added into a MultiComponentList while being in another cont already");
			MultiItemComponent next=firstMultiComponent;
			firstMultiComponent=multiComponent;
			multiComponent.prevInList=null;
			multiComponent.nextInList=next;
			if (next!=null) {
				next.prevInList=multiComponent;
			}
			multiComponent.collection=this;
			count++;
		}

		internal bool Remove(MultiItemComponent multiComponent) {
			if (multiComponent.collection == this) {
				if (firstMultiComponent == multiComponent) {
					firstMultiComponent = multiComponent.nextInList;
				} else {
					multiComponent.prevInList.nextInList = multiComponent.nextInList;
				}
				if (multiComponent.nextInList != null) {
					multiComponent.nextInList.prevInList=multiComponent.prevInList;
				}
				multiComponent.prevInList=null;
				multiComponent.nextInList=null;
				count--;
				multiComponent.collection = null;
				return true;
			}
			return false;
		}

		internal MultiItemComponent Find(int x, int y, int z, int id) {
			MultiItemComponent mic = firstMultiComponent;
			while (mic != null) {
				if ((mic.x == x) &&  (mic.y == y) && (mic.z == z) && (mic.Id == id)) {
					return mic;
				}
				mic = mic.nextInList;
			}
			return null;
		}

		internal MultiItemComponent this[int index] {
			get {
				if ((index >= count) || (index < 0)) {
					return null;
				}
				MultiItemComponent i = firstMultiComponent;
				int counter = 0;
				while (i != null) {
					if (index == counter) {
						return i;
					}
					i = i.nextInList;
					counter++;
				}
				return null;
			}
		}

		public IEnumerator GetEnumerator() {
			return new MultiComponentListEnumerator(this);
		}

		private class MultiComponentListEnumerator : IEnumerator {
			MultiComponentLinkedList cont;
			MultiItemComponent current;
			MultiItemComponent next;//this is because of the possibility 
			//that the current will be removed from the container during the enumeration
			public MultiComponentListEnumerator(MultiComponentLinkedList c) {
				cont = c;
				current = null;
				next = cont.firstMultiComponent;
			}

			public void Reset() {
				current = null;
				next = cont.firstMultiComponent;
			}

			public bool MoveNext() {
				current=next;
				if (current==null) {
					return false;
				}
				next=current.nextInList;
				return true;
			}

			public MultiItemComponent Current {
				get {
					return current;
				}
			}

			object IEnumerator.Current {
				get {
					return current;
				}
			}
		}
	}
}
