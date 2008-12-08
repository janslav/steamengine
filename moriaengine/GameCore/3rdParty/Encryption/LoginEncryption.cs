
//using System;
//using System.Collections;

//namespace SteamEngine.Packets 
//{
//    public class LoginEncryption : IClientEncryption 
//    {
//        private String name;
//        private uint key1;
//        private uint key2;
//        private uint table1;
//        private uint table2;

//        public String Name 
//        {
//            get 
//            {
//                return name;
//            }
//        }

//        public uint Key1
//        {
//            get
//            {
//                return key1;
//            }
//        }

//        public uint Key2
//        {
//            get
//            {
//                return key2;
//            }
//        }

//        // Try to initialize the login encryption with the given buffer
//        public bool init(uint seed, byte[] buffer, int offset, int length) 
//        {
//            if (length < 62) // includes the 4 byte header
//                return false;

//            // Try to find a valid key
//            byte[] packet = new byte[62];

//            // Initialize our tables (cache them, they will be modified)
//            uint orgTable1 = ( ( ( ~seed ) ^ 0x00001357 ) << 16 ) | ( ( seed ^ 0xffffaaaa ) & 0x0000ffff );
//            uint orgTable2 = ( ( seed ^ 0x43210000 ) >> 16 ) | ( ( ( ~seed ) ^ 0xabcdffff ) & 0xffff0000 );

//            for (int i = 0, n = LoginKey.LoginKeys.Length; i<n; i++)
//            {
//                // Check if this key works on this packet
//                Buffer.BlockCopy(buffer, offset, packet, 0, 62);
//                table1 = orgTable1;
//                table2 = orgTable2;
//                key1 = LoginKey.LoginKeys[i].Key1;
//                key2 = LoginKey.LoginKeys[i].Key2;

//                clientDecrypt( ref packet, packet.Length );

//                // Check if it decrypted correctly
//                if (packet[0] == 0x80 && CheckCorrectASCIIString(packet, 1, 30) && CheckCorrectASCIIString(packet, 31, 30))
//                {
//                    // Reestablish our current state
//                    table1 = orgTable1;
//                    table2 = orgTable2;
//                    key1 = LoginKey.LoginKeys[i].Key1;
//                    key2 = LoginKey.LoginKeys[i].Key2;
//                    name = LoginKey.LoginKeys[i].Name;
//                    return true;
//                }
//            }

//            return false;
//        }

//        private bool CheckCorrectASCIIString(byte[] bytes, int start, int len) {
//            bool nullsFromNowOn = false;
//            for (int i = start, n = start+len; i<n; i++) {
//                byte value = bytes[i];
//                if (nullsFromNowOn) {
//                    if (value != 0) {
//                        return false;
//                    }
//                } else if (value == 0) {
//                    nullsFromNowOn = true;
//                } else if (value < 32 || value == 127) { //unprintable ASCII
//                    return false;
//                }
//            }
//            return true;
//        }

//        public void init(uint seed, uint key1, uint key2) 
//        {
//            table1 = ( ( ( ~seed ) ^ 0x00001357 ) << 16 ) | ( ( seed ^ 0xffffaaaa ) & 0x0000ffff );
//            table2 = ( ( seed ^ 0x43210000 ) >> 16 ) | ( ( ( ~seed ) ^ 0xabcdffff ) & 0xffff0000 );
//            this.key1 = key1;
//            this.key2 = key2;
//        }

//        public void serverEncrypt(byte[] buffer)
//        {
//            // There is no server->client encryption in the login stage
//        }

//        public void clientDecrypt(ref byte[] buffer, int length) 
//        {
//            uint eax, ecx, edx, esi;
//            for ( int i = 0; i < length; ++i ) 
//            {
//                buffer[i] = (byte)(buffer[i] ^ (byte) ( table1 & 0xFF ));
//                edx = table2;
//                esi = table1 << 31;
//                eax = table2 >> 1;
//                eax |= esi;
//                eax ^= key1 - 1;
//                edx <<= 31;
//                eax >>= 1;
//                ecx = table1 >> 1;
//                eax |= esi;
//                ecx |= edx;
//                eax ^= key1;
//                ecx ^= key2;
//                table1 = ecx;
//                table2 = eax;
//            }
//        }
//    }
//}
