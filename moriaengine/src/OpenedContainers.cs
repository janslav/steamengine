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

namespace SteamEngine {
	public static class OpenedContainers {
		private readonly static Dictionary<AbstractItem, LinkedList<GameConn>> openedByConns = new Dictionary<AbstractItem, LinkedList<GameConn>>();

		private readonly static List<AbstractItem> emptyItemList = new List<AbstractItem>(0);
		private readonly static List<GameConn> emptyGameConnList = new List<GameConn>(0);

		public static bool HasContainerOpen(GameConn conn, AbstractItem container) {
			if (conn.openedContainers.Contains(container)) {
				AbstractCharacter curCharacter = conn.CurCharacter;
				if (curCharacter != null && curCharacter.CanReach(container)) {
			        return true;
			    } else {
					SetContainerClosed(conn, container);
			    }
			}
			return false;
		}

		internal static bool HasContainerOpenFromAt(GameConn conn, IPoint4D fromPoint, IPoint4D targetPoint, AbstractItem container, bool checkTopobj) {
			if (conn.openedContainers.Contains(container)) {
				AbstractCharacter curCharacter = conn.CurCharacter;
				if (curCharacter != null && curCharacter.CanReachFromAt(fromPoint, targetPoint, container, checkTopobj)) {
					return true;
				} else {
					SetContainerClosed(conn, container);
				}
			}
			return false;
		}

		public static void SetContainerOpened(GameConn conn, AbstractItem container) {
			conn.openedContainers.Add(container);
			LinkedList<GameConn> openedBy;
			if (!openedByConns.TryGetValue(container, out openedBy)) {
				openedBy = new LinkedList<GameConn>();
				openedByConns[container] = openedBy;
			}
			if (!openedBy.Contains(conn)) {
				openedBy.AddFirst(conn);
			}
		}

		public static void SetContainerClosed(GameConn conn, AbstractItem container) {
			conn.openedContainers.Remove(container);
			LinkedList<GameConn> openedBy;
			if (openedByConns.TryGetValue(container, out openedBy)) {
				openedBy.Remove(conn);
				if (openedBy.Count == 0) {
					openedByConns.Remove(container);//opened for no one
				}
			}
		}

		public static void SetContainerClosed(AbstractItem container) {
			LinkedList<GameConn> openedBy;
			if (openedByConns.TryGetValue(container, out openedBy)) {
				openedByConns.Remove(container);
				foreach (GameConn conn in openedBy) {
					conn.openedContainers.Remove(container);
				}
			}
		}

		public static IEnumerable<AbstractItem> GetOpenedContainers(GameConn conn) {
			AbstractCharacter curCharacter = conn.CurCharacter;
			if (curCharacter != null) {
				HashSet<AbstractItem> openedContainers = conn.openedContainers;
				List<AbstractItem> toRemove = null;

				foreach (AbstractItem con in openedContainers) {
					if (con.IsDeleted || !curCharacter.CanReach(con)) {
						if (toRemove == null) {
							toRemove = new List<AbstractItem>();
						}
						toRemove.Add(con);
					}
				}
				if (toRemove != null) {
					foreach (AbstractItem con in toRemove) {
						openedContainers.Remove(con);
					}
				}
				return openedContainers;
			}
			return emptyItemList;
		}

		public static IEnumerable<GameConn> GetConnsWithOpened(AbstractItem container) {
			LinkedList<GameConn> openedBy;
			if (openedByConns.TryGetValue(container, out openedBy)) {
				List<LinkedListNode<GameConn>> toRemove = null;
				LinkedListNode<GameConn> node = openedBy.First;
				while (node != null) {
					GameConn conn = node.Value;
					AbstractCharacter curCharacter = conn.CurCharacter;
					if (curCharacter != null) {
						if (!curCharacter.CanReach(container)) {
							if (toRemove == null) {
								toRemove = new List<LinkedListNode<GameConn>>();
							}
							toRemove.Add(node);
						}
					} else {
						if (toRemove == null) {
							toRemove = new List<LinkedListNode<GameConn>>();
						}
						toRemove.Add(node);
					}
					node = node.Next;
				}
				if (toRemove != null) {
					foreach (LinkedListNode<GameConn> nodeToRemove in toRemove) {
						openedBy.Remove(nodeToRemove);
					}
				}
				if (openedBy.Count == 0) {
					openedByConns.Remove(container);
				}
				return openedBy;
			}
			return emptyGameConnList;
		}

		internal static void ClearAll() {
			foreach (Conn c in Server.connections) {
				GameConn conn = c as GameConn;
				if (conn != null) {
					conn.openedContainers.Clear();
				}
			}
		}
	}
}