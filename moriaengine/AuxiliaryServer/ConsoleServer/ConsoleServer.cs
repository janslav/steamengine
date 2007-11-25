//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;

//using SteamEngine.Communication;
//using SteamEngine.Communication.TCP;
//using SteamEngine.Common;

//namespace SteamEngine.AuxiliaryServer.ConsoleServer {
//    public class LoginServer : TCPServer<ConsoleConnection> {
//        public LoginServer()
//            : base(12345) {

//        }

//        protected override IncomingPacket<ConsoleConnection> GetPacketImplementation(byte id) {
//            return Pool<ConsoleServerIncomingPacket>.Acquire();
//        }
//    }



//    public class ConsoleServerIncomingPacket : IncomingPacket<ConsoleConnection> {

//        protected override ReadPacketResult Read() {
//            return ReadPacketResult.DiscardSingle;
//        }

//        public override void Handle(ConsoleConnection packet) {
//            throw new Exception("The method or operation is not implemented.");
//        }
//    }


//    public class ConsoleServerOutgoingPacket : OutgoingPacket {

//        public override byte Id {
//            get { return 0; }
//        }

//        public override string Name {
//            get { return "ConsoleOutgoingPacket"; }
//        }

//        protected override void Write() {
			
//        }
//    }

//    //public class ConsoleServerPacketGroup : PacketGroup {
//    //    public ConsoleServerPacketGroup() {
//    //        base.SetType(PacketGroupType.SingleUse);
//    //    }
//    //}
//}
