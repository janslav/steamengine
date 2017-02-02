using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using SteamEngine.Transactionality;

namespace SteamEngine.Tests {
	// we mainly test atomicity of change operations
	[TestClass]
	public class FieldValueTests {

		#region FieldValueType.Typed
		[TestMethod]
		public void TypedValueWorksAsLoadedFromScripts() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypedValueWorksAsLoadedFromScriptsAndThenChanged() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Transaction.InTransaction(() => sut.CurrentValue = 2);
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(2, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypedValueWorksAsLoadedFromCode() {
			var sut = Transaction.InTransaction(() => new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), value: 1));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidTypedValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "x"));

			Action getDefault = () => {
				var dflt = Transaction.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = Transaction.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				Transaction.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}

		[TestMethod]
		public void TypedValueRollsBackInvalidChange() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "1"));

			((Action) (() => { Transaction.InTransaction(() => sut.CurrentValue = "x"); })).ShouldThrow<Exception>();

			// after an failed attempt at setting an invalid value, the values are back to what they were
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}
		#endregion FieldValueType.Typed

		#region FieldValueType.Model
		[TestMethod]
		public void ModelValueWorksAsLoadedFromScripts() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void ModelValueWorksAsLoadedFromScriptsAndThenChanged() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Transaction.InTransaction(() => sut.CurrentValue = 2);
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(2, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void ModelValueWorksAsLoadedFromCode() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), value: 1));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidModelValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "x"));

			Action getDefault = () => {
				var dflt = Transaction.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = Transaction.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				Transaction.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}

		[TestMethod]
		public void ModelValueRollsBackInvalidChange() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "1"));

			((Action) (() => { Transaction.InTransaction(() => sut.CurrentValue = "x"); })).ShouldThrow<Exception>();

			// after an failed attempt at setting an invalid value, the values are back to what they were
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}
		#endregion FieldValueType.Model

		#region FieldValueType.ThingDefType
		// for thingdefs we use null as a valid value, we don't want to involve def loading in the test. Maybe one day we can improve that.
		[TestMethod]
		public void ThingDefTypeValueWorksAsLoadedFromScripts() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: null));
			Assert.AreEqual(null, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(null, Transaction.InTransaction(() => sut.CurrentValue));
		}

		//[TestMethod]
		//public void ThingDefTypeValueWorksAsLoadedFromScriptsAndThenChanged() {
		//	var sut = new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: null);
		//	sut.CurrentValue = 2;
		//	Assert.AreEqual(1, sut.DefaultValue);
		//	Assert.AreEqual(2, sut.CurrentValue);
		//}

		[TestMethod]
		public void ThingDefTypeValueWorksAsLoadedFromCode() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), value: null));
			Assert.AreEqual(null, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(null, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidThingDefTypeValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: "x"));

			Action getDefault = () => {
				var dflt = Transaction.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = Transaction.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				Transaction.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}

		[TestMethod]
		public void ThingDefTypeValueRollsBackInvalidChange() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: "NULL"));

			((Action) (() => { Transaction.InTransaction(() => sut.CurrentValue = "x"); })).ShouldThrow<Exception>();

			// after an failed attempt at setting an invalid value, the values are back to what they were
			Assert.AreEqual(null, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(null, Transaction.InTransaction(() => sut.CurrentValue));
		}
		#endregion FieldValueType.ThingDefType

		#region FieldValueType.Typeless
		[TestMethod]
		public void TypelessValueWorks() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypelessValueWorksAsLoadedFromScriptsAndThenChanged() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Transaction.InTransaction(() => sut.CurrentValue = 2);
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(2, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypelessValueWorksAsLoadedFromCode() {
			var sut = Transaction.InTransaction(() => new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), value: 1));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, Transaction.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidTypelessValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), filename: "somefile", line: 123, value: "@"));

			Action getDefault = () => {
				var dflt = Transaction.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = Transaction.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				Transaction.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}
		#endregion FieldValueType.Typeless

		private int n = 10000;

		[TestMethod]
		public void ParallelismTest_CurrentValue() {
			var sut = Transaction.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "0"));

			Parallel.For(0, n, (_) => {
				Transaction.InTransaction(() => {
					sut.CurrentValue = ((int) sut.CurrentValue) + 1;
				});
			});

			Assert.AreEqual(n, Transaction.InTransaction(() => sut.CurrentValue));
		}

		//[TestMethod]
		//public void ParallelismTest_DefaultValue() {
		//	var sut = SeShield.InTransaction(() =>
		//		new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "0"));

		//	Parallel.For(0, n, (_) => {
		//		SeShield.InTransaction(() => {
		//			var defValuePlusOne = ((int) sut.DefaultValue) + 1;
		//			sut.SetFromScripts(filename: "somefile", line: 123, value: defValuePlusOne.ToString());
		//		});
		//	});

		//	Assert.AreEqual(n, SeShield.InTransaction(() => sut.CurrentValue));
		//}
	}
}

