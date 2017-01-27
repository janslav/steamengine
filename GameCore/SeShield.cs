using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shielded;

namespace SteamEngine {
	public static class SeShield {
		public static void AssertNotInTransaction() {
			if (SeShield.IsInTransaction) {
				throw new InvalidOperationException("Operation must not to be in a transaction.");
			}
		}

		public static void AssertInTransaction() {
			SeShield.AssertInTransaction();
		}

		public static T InTransaction<T>(Func<T> act) {
			return SeShield.InTransaction(act);
		}

		public static void InTransaction(Action act) {
			SeShield.InTransaction(act);
		}
	}
}
