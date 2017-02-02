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
using System.Threading;
using Shielded;
using SteamEngine.Common;

namespace SteamEngine.Transactionality {
	public static class Transaction {
		private static long sequence;

		private static readonly ShieldedLocal<long> transactionNumber = new ShieldedLocal<long>();

		public static long? TransactionNumber {
			get {
				if (!Shield.IsInTransaction) {
					return null;
				}

				if (!transactionNumber.HasValue) {
					transactionNumber.Value = Interlocked.Increment(ref sequence);
				}

				return transactionNumber.Value;
			}
		}

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
					try {
						return act();
					} catch (TransException) {
						Logger.WriteDebug("Rolling back transaction due to conflict.");
						throw;
					} catch (Exception) {
						Logger.WriteDebug("Rolling back transaction.");
						throw;
					}
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
						act();
					} catch (TransException) {
						Logger.WriteDebug("Rolling back transaction due to conflict.");
						throw;
					} catch (Exception) {
						Logger.WriteDebug("Rolling back transaction.");
						throw;
					}
				};

				Shield.InTransaction(wrapped);
			}
		}
	}
}