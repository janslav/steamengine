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

namespace SteamEngine.Common {
	//taken from http://www.geocities.com/Jeff_Louie/OOP/oop28.htm

	public abstract class Disposable : IDisposable {
		internal bool disposed = false;

		// subclass should to implement these two methods
		virtual protected void On_DisposeManagedResources() {

		}

		virtual protected void On_DisposeUnmanagedResources() {

		}

		public virtual void Dispose() {
			lock (this) {
				Dispose(true);
			}

			GC.SuppressFinalize(this); // remove this from gc finalizer list
		}

		[Summary("Utility method for disposing the whole contents of some Enumerable (such as list etc)")]
		public static void DisposeAll<T>(IEnumerable<T> disposables) where T : IDisposable {
			foreach (IDisposable disposable in disposables) {
				disposable.Dispose();
			}
		}

		protected void ThrowIfDisposed() {
			if (this.disposed) {
				throw new ObjectDisposedException(this+" disposed");
			}
		}

		public bool IsDisposed {
			get {
				return disposed;
			}
		}

		private void Dispose(bool disposing) {
			if (!this.disposed) {
				if (disposing) // called from Dispose
                {
					On_DisposeManagedResources();
				}
				On_DisposeUnmanagedResources();
			}
			this.disposed = true;
		}

		~Disposable() // maps to finalize
		{
			Dispose(false);
		}
	}
}
