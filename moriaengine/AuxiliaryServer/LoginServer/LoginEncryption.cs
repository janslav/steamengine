using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginEncryption : IEncryption {
		private String name;
		private uint key1;
		private uint key2;
		private uint table1;
		private uint table2;

		public override string ToString() {
			return this.name;
		}


		public EncryptionInitResult Init(byte[] buffer, int packetOffset, int length, out int bytesUsed) {
			bytesUsed = 0;
			if (length < 66) {
				return EncryptionInitResult.NotEnoughData;
			}


			if ((buffer[packetOffset + 4] == 0x80 || buffer[packetOffset + 4] == 0xcf) && buffer[packetOffset + 34] == 0x00 && buffer[packetOffset + 64] == 0x00) {
				if (CheckCorrectASCIIString(buffer, 5, 30) && CheckCorrectASCIIString(buffer, 35, 30)) {
					bytesUsed = 4;
					return EncryptionInitResult.SuccessNoEncryption;
				}
			}

			if (this.InitEncrypion(buffer, packetOffset, length)) {
				bytesUsed = 4;
				return EncryptionInitResult.SuccessUseEncryption;
			} else {
				return EncryptionInitResult.InvalidData;
			}

		}

		private bool InitEncrypion(byte[] buffer, int offset, int length) 
		{
			uint seed = (uint) ((buffer[offset] << 24) | (buffer[offset+1] << 16) | (buffer[offset+2] << 8) | buffer[offset+3]);

			// Try to find a valid key

			// Initialize our tables (cache them, they will be modified)
			uint orgTable1 = ( ( ( ~seed ) ^ 0x00001357 ) << 16 ) | ( ( seed ^ 0xffffaaaa ) & 0x0000ffff );
			uint orgTable2 = ( ( seed ^ 0x43210000 ) >> 16 ) | ( ( ( ~seed ) ^ 0xabcdffff ) & 0xffff0000 );

			using (Communication.Buffer b = Pool<Communication.Buffer>.Acquire()) {
				byte[] bytes = b.bytes;

				for (int i = 0, n = LoginKey.loginKeys.Length; i<n; i++) {
					table1 = orgTable1;
					table2 = orgTable2;
					key1 = LoginKey.loginKeys[i].key1;
					key2 = LoginKey.loginKeys[i].key2;


					this.Decrypt(buffer, 4, bytes, 0, length - 4);

					// Check if it decrypted correctly
					if ((bytes[0] == 0x80 || bytes[0] == 0xcf) && CheckCorrectASCIIString(bytes, 1, 30) && CheckCorrectASCIIString(bytes, 31, 30)) {
						// Reestablish our current state
						table1 = orgTable1;
						table2 = orgTable2;
						key1 = LoginKey.loginKeys[i].key1;
						key2 = LoginKey.loginKeys[i].key2;
						name = LoginKey.loginKeys[i].name;
						return true;
					}
				}
			}

			return false;
		}

		private bool CheckCorrectASCIIString(byte[] bytes, int start, int len) {
			bool nullsFromNowOn = false;
			for (int i = start, n = start+len; i<n; i++) {
				byte value = bytes[i];
				if (nullsFromNowOn) {
					if (value != 0) {
						return false;
					}
				} else if (value == 0) {
					nullsFromNowOn = true;
				} else if (value < 32 || value == 127) { //unprintable ASCII
					return false;
				}
			}
			return true;
		}

		public int Encrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length) {
			// There is no server->client encryption in the login stage

			System.Buffer.BlockCopy(bytesIn, offsetIn, bytesOut, offsetOut, length);

			return length;
		}

		public int Decrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length) {
			uint eax, ecx, edx, esi;

			for (int i = 0; i < length; i++) {
				bytesOut[i] = (byte) (bytesIn[offsetIn+i] ^ (byte) (table1 & 0xFF));
				edx = table2;
				esi = table1 << 31;
				eax = table2 >> 1;
				eax |= esi;
				eax ^= key1 - 1;
				edx <<= 31;
				eax >>= 1;
				ecx = table1 >> 1;
				eax |= esi;
				ecx |= edx;
				eax ^= key1;
				ecx ^= key2;
				table1 = ecx;
				table2 = eax;
			}

			return length;
		}
	}
}