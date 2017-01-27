﻿/*
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
using System.Threading;
using Shielded;
using SteamEngine.Common;

namespace SteamEngine {
	public static class SeShield {
		private static long sequence;

		[ThreadStatic]
		private static long? transactionNumber;

		public static long? TransactionNumber => transactionNumber;

		public static void AssertNotInTransaction() {
			if (Shield.IsInTransaction) {
				throw new InvalidOperationException("Operation must not to be in a transaction.");
			}
		}

		public static void AssertInTransaction() {
			Shield.AssertInTransaction();
		}

		public static T InTransaction<T>(Func<T> act) {
			if (Shield.IsInTransaction) {
				return act();
			} else {

				Func<T> wrapped = () => {
					T r;
					try {
						transactionNumber = Interlocked.Increment(ref sequence);
						r = act();
					} catch (Exception) {
						Logger.WriteDebug("Rolling back transaction.");
						throw;
					} finally {
						transactionNumber = null;
					}
					return r;
				};

				return Shield.InTransaction(wrapped);
			}
		}

		public static void InTransaction(Action act) {
			if (Shield.IsInTransaction) {
				act();
			} else {

				Action wrapped = () => {
					try {
						transactionNumber = Interlocked.Increment(ref sequence);
						act();
					} catch (Exception) {
						Logger.WriteDebug("Rolling back transaction.");
						throw;
					} finally {
						transactionNumber = null;
					}
				};

				Shield.InTransaction(wrapped);
			}
		}
	}
}
