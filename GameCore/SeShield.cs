using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shielded;

namespace SteamEngine {
	public static class SeShield {
		public static void AssertNotInTransaction() {
			if (Shield.IsInTransaction) {
				throw new InvalidOperationException("Operation must not to be in a transaction.");
			}
		}

		public static void AssertInTransaction() {
			Shield.AssertInTransaction();
		}

		public static T InTransaction<T>(Func<T> act) {
			return Shield.InTransaction(act);
		}

		public static void InTransaction(Action act) {
			Shield.InTransaction(act);
		}
	}
}
