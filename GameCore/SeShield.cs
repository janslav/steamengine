using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

