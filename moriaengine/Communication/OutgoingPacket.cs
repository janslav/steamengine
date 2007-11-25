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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using SteamEngine.Common;

namespace SteamEngine.Communication {
	public abstract class OutgoingPacket : Poolable {
		private byte[] buffer;
		private int offset;
		private int position;
		private int lastPosition;

		public abstract byte Id { get; }

		public virtual string Name {
			get {
				return this.GetType().Name;
			}
		}

		public int Write(byte[] bytes, int offset) {
			this.buffer = bytes;
			this.offset = offset;
			this.position = offset;
			this.lastPosition = offset;

			this.EncodeByte(this.Id);
			this.Write();

			int retVal = lastPosition - offset;
			Sanity.IfTrueThrow(retVal < 0, "OutgoingPacket.Write: lastPosition < start. This should not happen.");
			return retVal;
		}

		protected abstract void Write();

		public string FullName {
			get {
				return string.Concat(this.Name, " ( 0x", this.Id.ToString("x"), " )");
			}
		}

		public void SeekFromStart(int count) {
			this.position = this.offset + count;
			this.lastPosition = Math.Max(this.position, this.lastPosition);
		}

		public void SeekFromCurrent(int count) {
			this.position += count;
			this.lastPosition = Math.Max(this.position, this.lastPosition);
		}

		protected void EncodeBytes(byte[] array) {
			int len = array.Length;
			System.Buffer.BlockCopy(array, 0, this.buffer, this.position, len);
			SeekFromCurrent(len);
		}

		//protected void EncodeBytes(byte[] array, int length) {
		//	this.position += len;
		//    System.Buffer.BlockCopy(array, 0, this.buffer, this.position, length);
		//}

		protected void EncodeBytesReversed(byte[] array) {
			int len = array.Length-1;
			for (int i=0; i<=len; i++) {
				this.buffer[this.position + i] = array[len - i];
			}
			SeekFromCurrent(len+1);
		}

		/**
		    Encodes the string as unicode data at the specified start position, and returns the number of bytes
		    written.
			
		    You can do something like this, if you want: blockSize+=EncodeUnicodeString(line, blockSize);
			
		    But you can't do that with the EncodeUnicodeString method which takes a maxlen parameter.

		    @returns the number of bytes written, which is actually always double the length of the string.
			
		    Example:
		        int numBytes = EncodeUnicodeString("foo", 0);
		        That writes "foo" in unicode, which takes 6 bytes, so numBytes would be 6.
		*/
		protected void EncodeBigEndianUnicodeString(string value) {
			int len=value.Length;
			SeekFromCurrent(Encoding.BigEndianUnicode.GetBytes(value, 0, len, this.buffer, this.position));
		}

		protected void EncodeLittleEndianUnicodeString(string value) {
			int len=value.Length;
			SeekFromCurrent(Encoding.Unicode.GetBytes(value, 0, len, this.buffer, this.position));
		}

		/**
		    Encodes the string as unicode data at the specified start position, 
		    with the specified maximum length (in characters, NOT bytes),
		    and if less characters are written than the maximum length, then nulls are written
		    to fill up the space.

		    @returns the number of bytes written (not counting the extra zeros), which is either double the length
		    of the string, or double maxlen, whichever is lower.
			
		    Example #1:
		        int numBytes = EncodeUnicodeString("foo", 0, 5);
		        That writes "foo" in unicode, which takes 6 bytes, so numBytes would be 6, but since only 3
		        of the 5 maximum characters were written, the space which the other 2 characters would have gone in
		        would instead be filled with zeros (So there would four zeroes written, since each character is
		        two bytes in unicode). numBytes would still be 6, however, even though the number of bytes which
		        were written was 10. The rationale for this is that you can easily predict the number of bytes which
		        will actually be written, since it's always maxlen+maxlen. Of course, you could also easily predict
		        the number of bytes which the string would take up, since it's just double the string's length, EXCEPT
		        that you'd really also need to check whether the string was longer than maxlen - and since this method
		        is already doing that, there's no sense in doing that twice, so that's what the method returns.
				
				
		*/
		protected void EncodeBigEndianUnicodeString(string value, int maxlen) {
			int len = Math.Min(value.Length, maxlen);
			int written=Encoding.BigEndianUnicode.GetBytes(value, 0, len, this.buffer, this.position);
			SeekFromCurrent(written);
			EncodeZeros(maxlen-written);
		}

		protected void EncodeLittleEndianUnicodeString(string value, int maxlen) {
			int len = Math.Min(value.Length, maxlen);
			int written = Encoding.Unicode.GetBytes(value, 0, (maxlen>len?len:maxlen), this.buffer, this.position);
			SeekFromCurrent(written);
			EncodeZeros(maxlen-written);
		}

		protected void EncodeASCIIString(string value) {
			SeekFromCurrent(Encoding.ASCII.GetBytes(value, 0, value.Length, this.buffer, this.position));
		}

		protected void EncodeASCIIString(string value, int maxlen) {
			int len = Math.Min(value.Length, maxlen);
			int written = Encoding.ASCII.GetBytes(value, 0, len, this.buffer, this.position);
			SeekFromCurrent(written);
			EncodeZeros(maxlen-written);
		}

		static readonly byte[] zeroBytes = new byte[Buffer.bufferLen];

		//This method is used to encode more than one zeros. You should use this instead of any of the other
		//encode methods, because this is much faster because it doesn't have to do >>s. Additionally, if you're
		//doing more than 4 zeros, it is also faster than repeatedly calling EncodeByte.
		protected void EncodeZeros(int amount) {
			System.Buffer.BlockCopy(zeroBytes, 0, this.buffer, this.position, amount);
			SeekFromCurrent(amount);
		}

		protected void EncodeInt(int value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value>>24);	//first byte
			packet[startpos+1] = (byte) (value>>16);	//second byte
			packet[startpos+2] = (byte) (value>>8);	//third byte
			packet[startpos+3] = (byte) (value);		//fourth byte
			SeekFromCurrent(4);
		}
		protected void EncodeUInt(uint value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value>>24);	//first byte
			packet[startpos+1] = (byte) (value>>16);	//second byte
			packet[startpos+2] = (byte) (value>>8);	//third byte
			packet[startpos+3] = (byte) (value);		//fourth byte
			SeekFromCurrent(4);
		}

		protected void EncodeShort(short value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value>>8);		//first byte
			packet[startpos+1] = (byte) (value);		//second byte
			SeekFromCurrent(2);
		}

		protected void EncodeUShort(ushort value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value>>8);		//first byte
			packet[startpos+1] = (byte) (value);		//second byte
			SeekFromCurrent(2);
		}

		protected void EncodeSByte(sbyte value) {
			this.buffer[this.position]=(byte) value;
			SeekFromCurrent(1);
		}

		protected void EncodeByte(byte value) {
			this.buffer[this.position]=value;
			SeekFromCurrent(1);
		}

		protected void EncodeBool(bool value) {
			this.buffer[this.position]=(byte) (value?1:0);
			SeekFromCurrent(1);
		}

		protected void EncodeCurMaxVals(short curval, short maxval, bool showReal) {
			if (showReal) {
				EncodeShort(maxval);
				EncodeShort(curval);
			} else {
				EncodeShort(255);
				EncodeShort((short) (((int) curval<<8)/maxval));
			}
		}

		[Conditional("DEBUG")]
		protected void OutputPacketLog() {
			OutputPacketLog(this.offset, this.position);
		}
		[Conditional("DEBUG")]
		protected void OutputPacketLog(int start, int len) {
			Logger.WriteDebug("Packet Contents: ("+len+" bytes)");
			string s = "";
			string t = "";
			for (int a=0; a<len; a++) {
				t = this.buffer[a+start].ToString("X");
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
			for (int a=0; a<len; a++) {
				t = ""+(char) this.buffer[a+start];
				if (this.buffer[a+start]<32 || this.buffer[a+start]>126) {
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
	}
}
