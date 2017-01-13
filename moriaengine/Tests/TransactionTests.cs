﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shielded;

namespace SteamEngine.Tests {
	[TestClass]
	public class TransactionTests {
		private int n = 100000;

		[TestMethod]
		public void TrivialParallelismFails() {
			int a = 0;

			Parallel.For(0, n, (_) => {
				a++;
			});

			Assert.AreNotEqual(n, a);
		}

		[TestMethod]
		public void TrivialParallelismWithTransactionsWorks() {
			var a = ShieldedExt.CreateValue(0);

			Parallel.For(0, n, (_) => {
				Shield.InTransaction(() => {
					a.Value++;
				});
			});

			Assert.AreEqual(n, a);
		}

		[TestMethod]
		public void TrivialParallelismWithInterlockedAlsoWorks() {
			var a = 0;

			Parallel.For(0, n, (_) => {
				Shield.InTransaction(() =>
				{
					Interlocked.Add(ref a, 1);
				});
			});

			Assert.AreEqual(n, a);
		}
	}

	public static class ShieldedExt {
		public static Shielded<T> CreateValue<T>(T initial) {
			return new Shielded<T>(initial: initial);
		}
	}
}
