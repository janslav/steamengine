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
using System.Diagnostics;

using SteamEngine.Common;

namespace SteamEngine.Communication {
	public enum ReadPacketResult {
		Success, 
		DiscardSingle,
		DiscardAll,
		NeedMoreData
	}

	public abstract class IncomingPacket<TConnection, TState, TEndPoint> : Poolable
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		private byte[] buffer;
		private int offset;
		private int position;
		private int lastPosition;
		private int length;

		internal ReadPacketResult Read(byte[] bytes, int offset, int lengthIn, out int lengthOut) {
			this.buffer = bytes;
			this.offset = offset;
			this.position = offset;
			this.lastPosition = offset;
			this.length = lengthIn;

			ReadPacketResult result;
			try {
				result = this.Read();
			} catch (Exception e) {
				Logger.WriteDebug(e);
				lengthOut = 0;
				return ReadPacketResult.DiscardAll;
			}

			Sanity.IfTrueThrow(offset + lengthIn > this.lastPosition, "IncomingPacket.Read: offset + count > lastPosition. This should not happen.");

			lengthOut = this.lastPosition - offset;
			if (lengthOut < 0) {
				lengthOut = 0;
				return ReadPacketResult.NeedMoreData;
			}

			return result;
		}

		protected abstract ReadPacketResult Read();

		internal protected abstract void Handle(TConnection conn, TState state);


		internal void OutputPacketLog() {
			OutputPacketLog(this.buffer, this.offset, this.length);
		}

		[Conditional("DEBUG")]
		public static void OutputPacketLog(Byte[] array, int offset, int length) {
			Logger.WriteDebug("Packet Contents: ("+length+" bytes)");
			string s = "";
			string t = "";
			for (int a=0; a<length; a++) {
				t = array[a+offset].ToString("X");
				while (t.Length<2) {
					t="0"+t;
				}
				s += " "+t;
				if (a%10==0) {
					Logger.WriteDebug(s);
					s="";
				}
			}
			Logger.WriteDebug(s);
			s="";
			for (int a=0; a<length; a++) {
				t = ""+(char) array[a];
				if (array[a+offset]<32 || array[a+offset]>126) {
					t=""+(char) 128;
				}
				s += " "+t;
				if (a%10==0) {
					Logger.WriteDebug(s);
					s="";
				}
			}
			Logger.WriteDebug(s);
		}

		public void SeekFromStart(int count) {
			this.position = this.offset + count;
			this.lastPosition = Math.Max(this.position, this.lastPosition);
		}

		public void SeekFromCurrent(int count) {
			this.position += count;
			this.lastPosition = Math.Max(this.position, this.lastPosition);
		}

		[Summary("Decodes a unicode string, truncating it if it contains endlines (and replacing tabs with spaces).")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(0, "The number of bytes to decode (two per character)")]
		public string DecodeUnicodeString(int len) {
			return DecodeUnicodeString(len, true);
		}

		[Summary("Decodes a unicode string.")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(1, "If true, truncates the string if it contains endlines (and replacing tabs with spaces).")]
		public string DecodeUnicodeString(int len, bool truncateEndlines) {
			string str = Encoding.BigEndianUnicode.GetString(this.buffer, this.position, len);
			int indexOfZero = str.IndexOf((char) 0);
			if (indexOfZero > -1) {
				str = str.Substring(0, indexOfZero);
			}
			this.SeekFromCurrent(len);
			if (truncateEndlines) {
				return ConvertTools.RemoveIllegalChars(str);
			} else {
				return str;
			}
		}

		[Summary("Decodes an ascii string, truncating it if it contains endlines (and replacing tabs with spaces).")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(0, "The length of the string.")]
		public string DecodeAsciiString(int len) {
			return DecodeAsciiString(len, true);
		}

		[Summary("Decodes an ascii string.")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(0, "The length of the string.")]
		[Param(1, "If true, truncates the string if it contains endlines (and replacing tabs with spaces).")]
		public string DecodeAsciiString(int len, bool truncateEndlines) {
			string str="";
			try {
				str = Encoding.UTF8.GetString(this.buffer, this.position, len);
			} catch (ArgumentOutOfRangeException) {
				//return null;
				throw;
			}
			int indexOfZero = str.IndexOf((char) 0);
			if (indexOfZero > -1) {
				str = str.Substring(0, indexOfZero);
			}
			this.SeekFromCurrent(len);
			if (truncateEndlines) {
				return ConvertTools.RemoveIllegalChars(str);
			} else {
				return str;
			}
		}

		public int DecodeInt() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(4);
			return ((packet[startpos]<<24)+(packet[startpos+1]<<16)+(packet[startpos+2]<<8)+packet[startpos+3]);
		}

		public uint DecodeUInt() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(4);
			return (uint) ((packet[startpos]<<24)+(packet[startpos+1]<<16)+(packet[startpos+2]<<8)+packet[startpos+3]);
		}

		public short DecodeShort() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(2);
			return (short) ((packet[startpos]<<8)+packet[startpos+1]);
		}

		public ushort DecodeUShort() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(2);
			return (ushort) ((packet[startpos]<<8)+packet[startpos+1]);
		}

		public sbyte DecodeSByte() {
			int startpos = this.position;
			this.SeekFromCurrent(1);
			return (sbyte) this.buffer[startpos];
		}

		public byte DecodeByte() {
			int startpos = this.position;
			this.SeekFromCurrent(1);
			return this.buffer[startpos];
		}

	}
}
