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

using SteamEngine.Common;

namespace SteamEngine.Network {
	public abstract class OutgoingPacket : Packet {
		internal protected byte[] buffer;
		internal protected int start;
		internal protected int position;


		public abstract byte Id { get; }

		public abstract string Name { get; }

		public int Write(byte[] bytes, int offset) {
			this.buffer = bytes;
			this.start = offset;
			this.position = offset;
			this.Write();

			int retVal = position - start;
			Sanity.IfTrueThrow(retVal < 0, "OutgoingPacket.Write: position < start. This should not happen.");
			return retVal;
		}

		protected abstract void Write();


		public string FullName {
			get {
				return string.Concat(this.Name, " ( 0x", this.Id.ToString("x"), " )");
			}
		}
	}
}
