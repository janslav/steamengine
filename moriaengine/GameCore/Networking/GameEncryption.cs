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
using System.Text;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace SteamEngine.Networking {
	public class GameEncryption : IEncryption {

		private TwofishEncryption engine;

		private ushort recvPos; // Position in our CipherTable (Recv)
		private byte sendPos; // Offset in our XOR Table (Send)
		private byte[] cipherTable;
		private byte[] xorData; // This table is used for encrypting the server->client stream

		public EncryptionInitResult Init(byte[] bytesIn, int offsetIn, int lengthIn, out int bytesUsed) {
			if (lengthIn < 4) {
				bytesUsed = 0;
				return EncryptionInitResult.NotEnoughData;
			}

			uint seed = (uint) ((bytesIn[offsetIn] << 24) | (bytesIn[offsetIn + 1] << 16) | (bytesIn[offsetIn + 2] << 8) | bytesIn[offsetIn + 3]);
			bytesUsed = 4;

			if (bytesIn[offsetIn] == '\x91' && bytesIn[offsetIn + 1] == ((seed >> 24) & 0xFF) && bytesIn[offsetIn + 2] == ((seed >> 16) & 0xFF) && bytesIn[offsetIn + 3] == ((seed >> 8) & 0xFF) && bytesIn[offsetIn + 4] == (seed & 0xFF)) {
				return EncryptionInitResult.SuccessNoEncryption;
			}


			this.cipherTable = new byte[0x100];

			// Set up the crypt key
			byte[] key = new byte[16];
			key[0] = key[4] = key[8] = key[12] = (byte) ((seed >> 24) & 0xff);
			key[1] = key[5] = key[9] = key[13] = (byte) ((seed >> 16) & 0xff);
			key[2] = key[6] = key[10] = key[14] = (byte) ((seed >> 8) & 0xff);
			key[3] = key[7] = key[11] = key[15] = (byte) (seed & 0xff);

			byte[] iv = new byte[0];
			this.engine = new SteamEngine.Networking.TwofishEncryption(128, ref key, ref iv, CipherMode.ECB, SteamEngine.Networking.TwofishBase.EncryptionDirection.Decrypting);

			// Initialize table
			for (int i = 0; i < 256; ++i) {
				this.cipherTable[i] = (byte) i;
			}

			this.sendPos = 0;

			// We need to fill the table initially to calculate the MD5 hash of it
			refreshCipherTable();

			// Create a MD5 hash of the twofish crypt data and use it as a 16-byte xor table
			// for encrypting the server->client stream.
			MD5 md5 = new MD5CryptoServiceProvider();
			this.xorData = md5.ComputeHash(cipherTable);

			return EncryptionInitResult.SuccessUseEncryption;
		}

		private void refreshCipherTable() {
			uint[] block = new uint[4];

			for (int i = 0; i < 256; i += 16) {
				System.Buffer.BlockCopy(cipherTable, i, block, 0, 16);
				engine.blockEncrypt(ref block);
				System.Buffer.BlockCopy(block, 0, cipherTable, i, 16);
			}

			recvPos = 0;
		}

		public int Encrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length) {

			for (int i = offsetIn, n = offsetIn + length; i < n; ++i) {

				bytesOut[offsetOut] = (byte) (bytesIn[i] ^ this.xorData[this.sendPos++]);
				this.sendPos &= 0x0F; // Maximum Value is 0xF = 15, then 0xF + 1 = 0 again

				offsetOut++;
			}

			return length;
		}

		public int Decrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length) {
			for (int i = offsetIn, n = offsetIn + length; i < n; ++i) {

				// Recalculate table
				if (this.recvPos >= 0x100) {
					this.refreshCipherTable();
				}

				// Simple XOR operation
				bytesOut[offsetOut] = (byte) (bytesIn[i] ^ this.cipherTable[this.recvPos++]);

				offsetOut++;
			}

			return length;
		}

		public override string ToString() {
			return "GameClient Encryption";
		}
	}
}