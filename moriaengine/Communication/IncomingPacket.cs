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

			lengthOut = this.lastPosition - offset;
			if ((lengthOut < 0) || (lengthOut > lengthIn)) {
				lengthOut = 0;
				return ReadPacketResult.NeedMoreData;
			}

			Sanity.IfTrueThrow(offset + lengthOut > this.lastPosition, "IncomingPacket.Read: offset + count > lastPosition. This should not happen.");

			return result;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public void OutputPacketLog() {
			Logger.WriteDebug("Incoming packet 0x" + this.buffer[this.offset - 1].ToString("x"));
			CommunicationUtils.OutputPacketLog(this.buffer, this.offset, this.length);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public void OutputPacketLog(string message) {
			Logger.WriteDebug(string.Concat(message, " (packet 0x", this.buffer[this.offset - 1].ToString("x"), ")"));
			CommunicationUtils.OutputPacketLog(this.buffer, this.offset, this.length);
		}

		protected int LengthIn {
			get {
				return this.length;
			}
		}

		protected int Position {
			get {
				return this.position;
			}
		}

		protected abstract ReadPacketResult Read();

		internal protected abstract void Handle(TConnection conn, TState state);

		protected void SeekFromStart(int count) {
			this.position = this.offset + count;
			this.lastPosition = Math.Max(this.position, this.lastPosition);
		}

		protected void SeekFromCurrent(int count) {
			this.position += count;
			this.lastPosition = Math.Max(this.position, this.lastPosition);
		}

		[Summary("Decodes a unicode string, truncating it if it contains endlines (and replacing tabs with spaces).")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(0, "The number of bytes to decode (two per character)")]
		protected string DecodeBigEndianUnicodeString(int len) {
			return DecodeBigEndianUnicodeString(len, true);
		}

		[Summary("Decodes a unicode string.")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(1, "If true, truncates the string if it contains endlines (and replacing tabs with spaces).")]
		protected string DecodeBigEndianUnicodeString(int len, bool truncateEndlines) {
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

		[Summary("Decodes an ascii string, which is expected to be null-terminated, truncating it if it contains endlines (and replacing tabs with spaces).")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		protected string DecodeTerminatedAsciiString() {
			int indexOfEnd = Array.IndexOf<byte>(this.buffer, 0, this.position);
			int len = indexOfEnd - this.position;

			return DecodeAsciiString(len, true);
		}

		[Summary("Decodes an ascii string, truncating it if it contains endlines (and replacing tabs with spaces).")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(0, "The length of the string.")]
		protected string DecodeAsciiString(int len) {
			return DecodeAsciiString(len, true);
		}

		[Summary("Decodes an ascii string.")]
		[Remark("If the string contains a \0 (the 'end-of-string' character), it will be truncated.")]
		[Param(0, "The length of the string.")]
		[Param(1, "If true, truncates the string if it contains endlines (and replacing tabs with spaces).")]
		protected string DecodeAsciiString(int len, bool truncateEndlines) {
			string str = "";
			//try {
			str = Encoding.UTF8.GetString(this.buffer, this.position, len);
			//} catch (ArgumentOutOfRangeException) {
			//    //return null;
			//    throw;
			//}
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

		protected int DecodeInt() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(4);
			return ((packet[startpos] << 24) + (packet[startpos + 1] << 16) + (packet[startpos + 2] << 8) + packet[startpos + 3]);
		}

		[CLSCompliant(false)]
		protected uint DecodeUInt() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(4);
			return (uint) ((packet[startpos] << 24) + (packet[startpos + 1] << 16) + (packet[startpos + 2] << 8) + packet[startpos + 3]);
		}

		protected short DecodeShort() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(2);
			return (short) ((packet[startpos] << 8) + packet[startpos + 1]);
		}

		[CLSCompliant(false)]
		public ushort DecodeUShort() {
			byte[] packet = this.buffer;
			int startpos = this.position;
			this.SeekFromCurrent(2);
			return (ushort) ((packet[startpos] << 8) + packet[startpos + 1]);
		}

		[CLSCompliant(false)]
		protected sbyte DecodeSByte() {
			int startpos = this.position;
			this.SeekFromCurrent(1);
			return (sbyte) this.buffer[startpos];
		}

		protected byte DecodeByte() {
			int startpos = this.position;
			this.SeekFromCurrent(1);
			return this.buffer[startpos];
		}

		protected bool DecodeBool() {
			int startpos = this.position;
			this.SeekFromCurrent(1);
			return this.buffer[startpos] != 0;
		}


		//non-UO
		protected string DecodeUTF8String() {
			int bytesCount = DecodeInt();
			string retVal = Encoding.UTF8.GetString(this.buffer, this.position, bytesCount);
			SeekFromCurrent(bytesCount);
			return retVal;
		}
	}
}
