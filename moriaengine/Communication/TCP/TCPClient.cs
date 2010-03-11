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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.Communication.TCP {
	public sealed class TcpClientFactory<TState> :
		AsyncCore<TcpConnection<TState>, TState, IPEndPoint>,
		IClientFactory<TcpConnection<TState>, TState, IPEndPoint>
		where TState : IConnectionState<TcpConnection<TState>, TState, IPEndPoint>, new() {

		public TcpClientFactory(IProtocol<TcpConnection<TState>, TState, IPEndPoint> protocol, object lockObject)
			: base(protocol, lockObject) {

		}

		public TcpConnection<TState> Connect(IPEndPoint endpoint) {
			Socket socket = TcpServer<TState>.CreateSocket(endpoint.AddressFamily);
			socket.Connect(endpoint);

			TcpConnection<TState> newConn = Pool<TcpConnection<TState>>.Acquire();
			newConn.socket = socket;
			InitNewConnection(newConn);

			return newConn;
		}

		//public static void AcquireEndpoint(
	}
}
