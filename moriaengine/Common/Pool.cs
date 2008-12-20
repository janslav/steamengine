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
	public abstract class PoolBase {

		internal abstract void Release(Poolable p);
	}

	public class Pool<T> : PoolBase where T : Poolable, new() {
		static private SimpleQueue<T> queue = new SimpleQueue<T>();

		static Pool<T> pool = new Pool<T>();

		//int newInstances

		private Pool() {
		}

		internal override void Release(Poolable p) {
			T instance = (T) p;
			lock (pool) {
				Sanity.IfTrueThrow(queue.Contains(instance), "Pool.Release: '"+p.ToString()+"' already in queue. This should not happen");
				queue.Enqueue(instance);
			}
		}

		//internal T NewInstance() {
		//    T instance = new T();
		//    instance.myPool = pool;
		//    return instance;
		//}

		static public T Acquire() {
			lock (pool) {
				if (queue.Count > 0) {
					T instance = queue.Dequeue();
					instance.Reset();
					return instance;
				} else {
					T instance = new T();
					instance.myPool = pool;
					return instance;
				}
			}
		}
	}
}
