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
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public static class ServerUtils {

		static byte[] fullMask = { 255, 255, 255, 255 };

		static InterfaceEntry[] interfaces = InitInterfaces();

		private static InterfaceEntry[] InitInterfaces() {
			var list = new List<InterfaceEntry>();

			list.Add(new InterfaceEntry(
				new byte[] { 127, 0, 0, 1 },
				new byte[] { 255, 255, 255, 255 }
				));

#if !MONO
			foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces()) {
				var properties = adapter.GetIPProperties();
				foreach (var info in properties.UnicastAddresses) {
					var addressBytes = info.Address.GetAddressBytes();
					if (addressBytes.Length == 4) {
						var mask = fullMask;
						if (info.IPv4Mask != null) {
							mask = info.IPv4Mask.GetAddressBytes();
						}

						list.Add(new InterfaceEntry(addressBytes, mask));
					}
				}
			}
#endif

			list.Sort();
			return list.ToArray();
		}

		internal static void Init() {
		}

		public static byte[] GetMatchingInterfaceAddress(byte[] remoteIP) {
			foreach (var entry in interfaces) {
				if (entry.MatchesInterface(remoteIP)) {
					return entry.ip;
				}
			}
			throw new SEException("No matching interface for " + new IPAddress(remoteIP));
			//return new byte[] { 123, 123, 123, 100 };
		}

		private class InterfaceEntry : IComparable<InterfaceEntry> {
			private readonly byte[] maskedIp;
			private readonly byte[] mask;
			internal readonly byte[] ip;

			internal InterfaceEntry(byte[] ip, byte[] mask) {
				this.mask = mask;
				this.ip = ip;

				var n = ip.Length;
				Sanity.IfTrueThrow(n != 4, "Unsupported IP bytes array");
				Sanity.IfTrueThrow(mask.Length != 4, "Unsupported IP bytes array");

				this.maskedIp = new byte[n];
				for (var i = 0; i < n; i++) {
					this.maskedIp[i] = (byte) (ip[i] & mask[i]);
				}
			}

			public int CompareTo(InterfaceEntry other) {
				var otherMask = other.mask;

				for (int i = 0, n = this.mask.Length; i < n; i++) {
					var result = otherMask[i].CompareTo(this.mask[i]);
					if (result != 0) {
						return result;
					}
				}

				return 0;
			}

			public bool MatchesInterface(byte[] otherIp) {
				for (int i = 0, n = this.mask.Length; i < n; i++) {
					if ((otherIp[i] & this.mask[i]) != this.maskedIp[i]) {
						return false;
					}
				}
				return true;
			}

			public override string ToString() {
				return "InterfaceEntry(ip=" + new IPAddress(this.ip) + ", mask=" + new IPAddress(this.mask) + ")";
			}
		}
	}
}