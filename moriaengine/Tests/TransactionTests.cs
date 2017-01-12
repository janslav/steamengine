using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shielded;

namespace SteamEngine.Tests {
	[TestClass]
	public class TransactionTests {

		[TestMethod]
		public void Trivial() {
			Shield.InTransaction(() => {


			});
		}
	}
}
