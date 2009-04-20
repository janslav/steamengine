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
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.Communication.NamedPipes {
	public class NamedPipeServer<TState> :
		AsyncCore<NamedPipeConnection<TState>, TState, string>,
		IServer<NamedPipeConnection<TState>, TState, string>
		where TState : Poolable, IConnectionState<NamedPipeConnection<TState>, TState, string>, new() {

		bool running = false;
		string pipename;
		Thread listenThread;

		public NamedPipeServer(IProtocol<NamedPipeConnection<TState>, TState, string> protocol, object lockObject)
			: base(protocol, lockObject) {

		}

		public void Bind(string pipename) {
			//start the listening thread
			this.pipename = pipename;

			this.listenThread = new Thread(ListenForClients);
			this.listenThread.IsBackground = true;
			this.listenThread.Name = Tools.TypeToString(this.GetType()) + "_PipeServerListener";
			this.listenThread.Start();
		}


		/// <summary>
		/// Listens for client connections
		/// </summary>
		private void ListenForClients() {
			Console.WriteLine("Listening on named pipe '" + this.pipename + "'");
			this.running = true;

			while (true) {
				SafeFileHandle clientHandle =
					ServerKernelFunctions.CreateNamedPipe(
						 this.pipename,
						 ServerKernelFunctions.DUPLEX | ServerKernelFunctions.FILE_FLAG_OVERLAPPED,
						 0,
						 255,
						 Buffer.bufferLen,
						 Buffer.bufferLen,
						 0,
						 IntPtr.Zero);

				//could not create named pipe
				if (clientHandle.IsInvalid) {
					try {
						clientHandle.Close();
					} catch { }
					throw new SEException("Failed to create listening named pipe '" + this.pipename + "'");
				}

				int success = ServerKernelFunctions.ConnectNamedPipe(clientHandle, IntPtr.Zero);

				//could not connect client
				if (success == 0) {
					try {
						clientHandle.Close();
					} catch { }
					throw new SEException("Failed to connect client to named pipe '" + this.pipename + "'");
				}

				NamedPipeConnection<TState> newConn = Pool<NamedPipeConnection<TState>>.Acquire();
				newConn.SetFields(this.pipename, clientHandle);
				InitNewConnection(newConn);
			}
		}

		public string BoundTo {
			get { return this.pipename; }
		}

		public bool IsBound {
			get { return this.running; }
		}

		public void UnBind() {
			throw new SEException("Can't UnBind a NamedPipeServer");

			//if (this.running) {
			//    Console.WriteLine("Stopped listening on named pipe '"+this.pipename+"'");
			//}
			//this.running = false;

			//this.listenThread.Abort();//this doesn't really work. 
			//the ConnectNamedPipe function blocks. We won't really need to unbind the 
		}


		//protected override void On_DisposeUnmanagedResources() {
		//    this.UnBind();

		//    base.On_DisposeUnmanagedResources();
		//}
	}

	internal static class ServerKernelFunctions {
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern SafeFileHandle CreateNamedPipe(
		   String pipeName,
		   uint dwOpenMode,
		   uint dwPipeMode,
		   uint nMaxInstances,
		   uint nOutBufferSize,
		   uint nInBufferSize,
		   uint nDefaultTimeOut,
		   IntPtr lpSecurityAttributes);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern int ConnectNamedPipe(
		   SafeFileHandle hNamedPipe,
		   IntPtr lpOverlapped);

		internal const uint DUPLEX = (0x00000003);
		internal const uint FILE_FLAG_OVERLAPPED = (0x40000000);
	}
}
