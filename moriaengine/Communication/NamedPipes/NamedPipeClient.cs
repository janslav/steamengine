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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.Communication.NamedPipes {
	public sealed class NamedPipeClientFactory<TState> :
		AsyncCore<NamedPipeConnection<TState>, TState, string>//,
		//IClientFactory<NamedPipeConnection<TState>, TState, string>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, string>, new() 
	{



		public NamedPipeClientFactory(IProtocol<NamedPipeConnection<TState>, TState, string> protocol, object lockObject)
			: base(protocol, lockObject) {
		}


		public NamedPipeConnection<TState> Connect(string pipeName) {
			SafeFileHandle handle =
			   ClientKernelFunctions.CreateFile(
				  pipeName,
				  ClientKernelFunctions.GENERIC_READ | ClientKernelFunctions.GENERIC_WRITE,
				  0,
				  IntPtr.Zero,
				  ClientKernelFunctions.OPEN_EXISTING,
				  ClientKernelFunctions.FILE_FLAG_OVERLAPPED,
				  IntPtr.Zero);

			if (handle.IsInvalid) {
				return null;
			}


			NamedPipeConnection<TState> newConn = Pool<NamedPipeConnection<TState>>.Acquire();
			newConn.SetFields(pipeName, handle);
			InitNewConnection(newConn);

			return newConn;
		}
	}

	internal static class ClientKernelFunctions {

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern SafeFileHandle CreateFile(
		   String pipeName,
		   uint dwDesiredAccess,
		   uint dwShareMode,
		   IntPtr lpSecurityAttributes,
		   uint dwCreationDisposition,
		   uint dwFlagsAndAttributes,
		   IntPtr hTemplate);

		internal const uint GENERIC_READ = (0x80000000);
		internal const uint GENERIC_WRITE = (0x40000000);
		internal const uint OPEN_EXISTING = 3;
		internal const uint FILE_FLAG_OVERLAPPED = (0x40000000);

	}
}
