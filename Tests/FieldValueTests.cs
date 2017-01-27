using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Shielded;

namespace SteamEngine.Tests {
	// we mainly test atomicity of change operations
	[TestClass]
	public class FieldValueTests {

		#region FieldValueType.Typed
		[TestMethod]
		public void TypedValueWorksAsLoadedFromScripts() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypedValueWorksAsLoadedFromScriptsAndThenChanged() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			SeShield.InTransaction(() => sut.CurrentValue = 2);
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(2, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypedValueWorksAsLoadedFromCode() {
			var sut = SeShield.InTransaction(() => new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), value: 1));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidTypedValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "x"));

			Action getDefault = () => {
				var dflt = SeShield.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = SeShield.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				SeShield.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}

		[TestMethod]
		public void TypedValueRollsBackInvalidChange() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "1"));

			((Action) (() => { SeShield.InTransaction(() => sut.CurrentValue = "x"); })).ShouldThrow<Exception>();

			// after an failed attempt at setting an invalid value, the values are back to what they were
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}
		#endregion FieldValueType.Typed

		#region FieldValueType.Model
		[TestMethod]
		public void ModelValueWorksAsLoadedFromScripts() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void ModelValueWorksAsLoadedFromScriptsAndThenChanged() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			SeShield.InTransaction(() => sut.CurrentValue = 2);
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(2, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void ModelValueWorksAsLoadedFromCode() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), value: 1));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidModelValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "x"));

			Action getDefault = () => {
				var dflt = SeShield.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = SeShield.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				SeShield.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}

		[TestMethod]
		public void ModelValueRollsBackInvalidChange() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Model, type: typeof(int), filename: "somefile", line: 123, value: "1"));

			((Action) (() => { SeShield.InTransaction(() => sut.CurrentValue = "x"); })).ShouldThrow<Exception>();

			// after an failed attempt at setting an invalid value, the values are back to what they were
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}
		#endregion FieldValueType.Model

		#region FieldValueType.ThingDefType
		// for thingdefs we use null as a valid value, we don't want to involve def loading in the test. Maybe one day we can improve that.
		[TestMethod]
		public void ThingDefTypeValueWorksAsLoadedFromScripts() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: null));
			Assert.AreEqual(null, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(null, SeShield.InTransaction(() => sut.CurrentValue));
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
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), value: null));
			Assert.AreEqual(null, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(null, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidThingDefTypeValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: "x"));

			Action getDefault = () => {
				var dflt = SeShield.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = SeShield.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				SeShield.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}

		[TestMethod]
		public void ThingDefTypeValueRollsBackInvalidChange() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.ThingDefType, type: typeof(int), filename: "somefile", line: 123, value: "NULL"));

			((Action) (() => { SeShield.InTransaction(() => sut.CurrentValue = "x"); })).ShouldThrow<Exception>();

			// after an failed attempt at setting an invalid value, the values are back to what they were
			Assert.AreEqual(null, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(null, SeShield.InTransaction(() => sut.CurrentValue));
		}
		#endregion FieldValueType.ThingDefType

		#region FieldValueType.Typeless
		[TestMethod]
		public void TypelessValueWorks() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypelessValueWorksAsLoadedFromScriptsAndThenChanged() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), filename: "somefile", line: 123, value: "1"));
			SeShield.InTransaction(() => sut.CurrentValue = 2);
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(2, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void TypelessValueWorksAsLoadedFromCode() {
			var sut = SeShield.InTransaction(() => new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), value: 1));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.DefaultValue));
			Assert.AreEqual(1, SeShield.InTransaction(() => sut.CurrentValue));
		}

		[TestMethod]
		public void InvalidTypelessValueThrowsRepeatedly() {
			// when something invalid is loaded from saves/scripts, the value should remain unresolvable
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typeless, type: typeof(int), filename: "somefile", line: 123, value: "@"));

			Action getDefault = () => {
				var dflt = SeShield.InTransaction(() => sut.DefaultValue);
			};

			getDefault.ShouldThrow<Exception>();
			getDefault.ShouldThrow<Exception>();

			Action getCurrent = () => {
				var c = SeShield.InTransaction(() => sut.CurrentValue);
			};

			getCurrent.ShouldThrow<Exception>();
			getCurrent.ShouldThrow<Exception>();

			Action setCurrent = () => {
				SeShield.InTransaction(() => sut.CurrentValue = 1);
			};

			setCurrent.ShouldThrow<Exception>();
			setCurrent.ShouldThrow<Exception>();
		}
		#endregion FieldValueType.Typeless

		private int n = 10000;

		[TestMethod]
		public void ParallelismTest_CurrentValue() {
			var sut = SeShield.InTransaction(() =>
				new FieldValue(name: "ABC", fvType: FieldValueType.Typed, type: typeof(int), filename: "somefile", line: 123, value: "0"));

			Parallel.For(0, n, (_) => {
				SeShield.InTransaction(() => {
					sut.CurrentValue = ((int) sut.CurrentValue) + 1;
				});
			});

			Assert.AreEqual(n, SeShield.InTransaction(() => sut.CurrentValue));
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

