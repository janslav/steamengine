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
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.Communication.NamedPipes {
	public sealed class NamedPipeConnection<TProtocol, TState> :
		AbstractConnection<TProtocol, NamedPipeConnection<TProtocol, TState>, TState, string>
		where TState : IConnectionState<TProtocol, NamedPipeConnection<TProtocol, TState>, TState, string>, new()
		where TProtocol : IProtocol<TProtocol, NamedPipeConnection<TProtocol, TState>, TState, string>, new() {


		public override bool IsConnected {
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public override string EndPoint {
			get { throw new Exception("The method or operation is not implemented."); }
		}

		protected override void BeginSend(AbstractConnection<TProtocol, NamedPipeConnection<TProtocol, TState>, TState, string>.BufferToSend toSend) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}