using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Scripting.Interpretation;
using SteamEngine.Timers;

namespace SteamEngine.Tests {
	[TestClass]
	public class LScriptTests {
		[TestMethod]
		public void TrivialScript() {
			Exception exception;
			var result = LScriptMain.TryRunSnippet(null, @"return ""abc""", out exception);
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void RecursionRecoveryTest() {
			var expressions = new[] {
				"Srdcem Mordoru je temne mesto Barad -Dur z nehoz vladne Marghul.  Zde vladne absolutni disciplina a jakekoliv neuposechnuti rozkazu je odmeneno smrti..",
				"a odplula na zapad, ale stale jsou to bytosti, ktere budou ovlivnovat budoucnost.'."
			};
			foreach (var e in expressions) {
				Exception exception;
				var returnExpression = "return " + e;
				var result = LScriptMain.TryRunSnippet(null, returnExpression, out exception);
				Assert.AreEqual(null, result);
			}
		}

		//temporary for direct launching while writing the tests
		//public void def__RunLScriptTests(TagHolder ignored) {
		//	RunLScriptTests();
		//}

		[TestMethod]
		public void TestCallingMethods() {
			var testObj = new LScriptTesterObject();

			//Logger.Show("TestSuite", "Running ");


			//calling methods
			TestSnippet(testObj, 1, "TestMethod_NoParams", "calling zero param method without caller ()");
			TestSnippet(testObj, 1, "TestMethod_NoParams()", "calling zero param method with ()");
			TestSnippet(testObj, 0, "TestMethod_NoParams=", typeof(SEException), "calling zero param method with =");
			//TestSnippet(0, "TestMethod_NoParams 186",
			//	"Class member (method/property/field/constructor) 'TestMethod_NoParams' is getting wrong arguments",
			//	"calling zero param method with an argument after whitespace");
			TestSnippet(testObj, 0, "TestMethod_NoParams(786)",
				"Class member (method/property/field/constructor) 'TestMethod_NoParams' is getting wrong arguments",
				"calling zero param method with an argument in ()");
			TestSnippet(testObj, 0, "TestMethod_NoParams =74123",
				"Class member (method/property/field/constructor) 'TestMethod_NoParams' is getting wrong arguments",
				"calling zero param method with an argument after =");

			TestSnippet(testObj, 3789, "TestMethod_OneParam 3789", "calling one param method without parens");
			TestSnippet(testObj, 7893, "TestMethod_OneParam(7893)", "calling one param method with parens, syntax 1");
			TestSnippet(testObj, 7893, "TestMethod_OneParam (7893)", "calling one param method with parens, syntax 2");
			TestSnippet(testObj, 7893, "TestMethod_OneParam ( 7893 )", "calling one param method with parens, syntax 3");
			TestSnippet(testObj, 7893, "TestMethod_OneParam( 7893)", "calling one param method with parens, syntax 4");
			TestSnippet(testObj, 7893, "TestMethod_OneParam(7893 )", "calling one param method with parens, syntax 5");
			TestSnippet(testObj, 8492, "TestMethod_OneParam=8492", "calling one param method with =, syntax 1");
			TestSnippet(testObj, 8492, "TestMethod_OneParam= 8492", "calling one param method with =, syntax 2");
			TestSnippet(testObj, 8492, "TestMethod_OneParam =8492", "calling one param method with =, syntax 3");
			TestSnippet(testObj, 8492, "TestMethod_OneParam = 8492", "calling one param method with =, syntax 4");
			TestSnippet(testObj, 0x2195, "TestMethod_OneParam 02195", "calling one param method without parens, hexa without xX");
			TestSnippet(testObj, 0x2195, "TestMethod_OneParam 0x2195", "calling one param method without parens, hexa without x");
			TestSnippet(testObj, 1.2195, "TestMethod_OneParam 1.2195",
				"calling one param method without parens, decimal number with leading number");
			TestSnippet(testObj, 0.2195, "TestMethod_OneParam .2195",
				"calling one param method without parens, decimal number without leading number");

			//TestSnippet(0, "TestMethod_OneParam",
			//	"Class member (method/property/field/constructor) 'TestMethod_OneParam' is getting wrong arguments",
			//	"calling one param method without parens and without any parameter");
			TestSnippet(testObj, 0, "TestMethod_OneParam()",
				"Class member (method/property/field/constructor) 'TestMethod_OneParam' is getting wrong arguments",
				"calling one param method with parens without any argument");

			TestSnippet(testObj, 1, "TestMethod_OneStringParam gfdyushgd",
				"calling one string param method without parens");
			TestSnippet(testObj, 1, "TestMethod_OneStringParam(gfdyushgd)",
				"calling one string param method with parens");
			TestSnippet(testObj, 1, "TestMethod_OneStringParam=gfdyushgd",
				"calling one string param method with =");

			TestSnippet(testObj, 1, "TestMethod_OneStringParam gf gfds hgf456 fd",
				"calling one string param method without parens with multiple params");
			TestSnippet(testObj, 1, "TestMethod_OneStringParam(gf gfds 7hgf456 fd)",
				"calling one string param method with parens with multiple params");
			TestSnippet(testObj, 1, "TestMethod_OneStringParam=gf gfds 7hgf456 fd",
				"calling one string param method with = with multiple params");

			//TestSnippet(testObj, 2, "TestMethod_NoParams;TestMethod_NoParams", "calling two expressions in a row, syntax 1");
			//TestSnippet(testObj, 2, "TestMethod_NoParams; TestMethod_NoParams", "calling two expressions in a row, syntax 2");
			TestSnippet(testObj, 2, "TestMethod_NoParams ;TestMethod_NoParams", "calling two expressions in a row, syntax 3");
			TestSnippet(testObj, 2, "TestMethod_NoParams ; TestMethod_NoParams", "calling two expressions in a row, syntax 4");

			Exception exception;
			TestSnippet(testObj, Globals.Port, "return Globals.port", "dotted expression");
			Sanity.IfTrueThrow(!Equals(LScriptMain.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList())", out exception), "[]"),
				"dotted expression: method and constructor witn no params"); //this could outcome false if someone changed the ObjToString method...
			Sanity.IfTrueThrow(!Equals(LScriptMain.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList(0))", out exception), "[]"),
				"dotted expression: method and constructor witn one param with parens"); //this could outcome false if someone changed the ObjToString method...
			Sanity.IfTrueThrow(!Equals(LScriptMain.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList=0)", out exception), "[]"),
				"dotted expression: method and constructor witn one param with with ="); //this could outcome false if someone changed the ObjToString method...	
																						 //this doesn't work (it should never have I think...)
																						 //Sanity.IfTrueThrow(!string.Equals(LScript.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList 0)"), "[]"), 
																						 //	"dotted expression: method and constructor with one param without parens"); //this could outcome false if someone changed the ObjToString method...
		}

		[TestMethod]
		public void TestReturningValue() {
			var testObj = new LScriptTesterObject();

			//returning value
			Sanity.IfTrueThrow(5 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return 5")), "Error while return integer");
			Sanity.IfTrueThrow(
				!Equals(LScriptMain.RunSnippet(testObj, "return \"some test string 7hgf456\""), "some test string 7hgf456"),
				"Error while returning quoted string");
			Sanity.IfTrueThrow(!Equals(LScriptMain.RunSnippet(testObj, "return \" \\\" \""), " \" "),
				"Error while returning quoted string with escaped character");

			Sanity.IfTrueThrow(
				!Equals(LScriptMain.RunSnippet(testObj, "return some test string 7hgf456"), "some test string 7hgf456"),
				"Error while returning nonquoted string");

			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return GetInteger")),
				"Error while returning integer from a method");
			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return <GetInteger>")),
				"Error while returning integer from a <method>");
			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return <GetInteger()>")),
				"Error while returning integer from a <method()>");
			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return <?GetInteger?>")),
				"Error while returning integer from a <method>");

			LScriptMain.RunSnippet(testObj, "return {0 1}");
			LScriptMain.RunSnippet(testObj, "return { 0 1}");
			LScriptMain.RunSnippet(testObj, "return {0 1 }");
			LScriptMain.RunSnippet(testObj, "return { 0 1 }");
			LScriptMain.RunSnippet(testObj, "return {0,1}");
			LScriptMain.RunSnippet(testObj, "return { 0,1}");
			LScriptMain.RunSnippet(testObj, "return {0,1 }");
			LScriptMain.RunSnippet(testObj, "return { 0,1 }");
			LScriptMain.RunSnippet(testObj, "return({0 1})");
			LScriptMain.RunSnippet(testObj, "return({ 0 1})");
			LScriptMain.RunSnippet(testObj, "return({0 1 })");
			LScriptMain.RunSnippet(testObj, "return({ 0 1 })");
			LScriptMain.RunSnippet(testObj, "return({0,1})");
			LScriptMain.RunSnippet(testObj, "return({ 0,1})");
			LScriptMain.RunSnippet(testObj, "return({0,1 })");
			LScriptMain.RunSnippet(testObj, "return({ 0,1 })");
		}

		[TestMethod]
		public void TestOperators() {
			var testObj = new LScriptTesterObject();

			//these will be a bit difficult... :\
			//TestSnippet(+1, "TestMethod_OneParam +1", "integer unary '+' without parens");
			//TestSnippet(-1, "TestMethod_OneParam -1", "integer unary '-' without parens");
			//TestSnippet(564, "TestMethod_OneParam +GetInteger()", "general expression unary '+' without parens");
			//TestSnippet(-564, "TestMethod_OneParam -GetInteger()", "general expression unary '-' without parens");				
			//Sanity.IfTrueThrow(!SteamEngine.Timers.TimerKey.Get("testtimerkey").Equals(LScript.RunSnippet(testObj, "return %testtimerkey")), "timerkey");
			//Sanity.IfTrueThrow(!SteamEngine.Timers.TriggerKey.Get("testtriggerkey").Equals(LScript.RunSnippet(testObj, "return @testtriggerkey ")), "triggerkey");

			TestSnippet(testObj, 1, "TestMethod_OneParam(+1)", "integer unary '+' with parens");
			TestSnippet(testObj, 1, "TestMethod_OneParam=+1", "integer unary '+' with =");
			TestSnippet(testObj, -1, "TestMethod_OneParam(-1)", "integer unary '-' with parens");
			TestSnippet(testObj, -1, "TestMethod_OneParam=-1", "integer unary '-' with =");
			TestSnippet(testObj, 564, "TestMethod_OneParam(+GetInteger())", "general expression unary '+' with parens");
			TestSnippet(testObj, 564, "TestMethod_OneParam=+GetInteger()", "general expression unary '+' with =");
			TestSnippet(testObj, -564, "TestMethod_OneParam(-GetInteger())", "general expression unary '-' with parens");
			TestSnippet(testObj, -564, "TestMethod_OneParam=-GetInteger()", "general expression unary '-' with =");

			TestSnippet(testObj, 56 + 7, "TestMethod_OneParam 56+7", "integer binary '+' without parens");
			TestSnippet(testObj, 56 + 7, "TestMethod_OneParam(56+7)", "integer binary '+' with parens");
			TestSnippet(testObj, 56 + 7, "TestMethod_OneParam=56+7", "integer binary '+' with =");
			TestSnippet(testObj, 56 - 7, "TestMethod_OneParam 56-7", "integer binary '-' without parens");
			TestSnippet(testObj, 56 - 7, "TestMethod_OneParam(56-7)", "integer binary '-' with parens");
			TestSnippet(testObj, 56 - 7, "TestMethod_OneParam=56-7", "integer binary '-' with =");
			TestSnippet(testObj, 56 * 7, "TestMethod_OneParam 56*7", "integer binary '*' without parens");
			TestSnippet(testObj, 56 * 7, "TestMethod_OneParam(56*7)", "integer binary '*' with parens");
			TestSnippet(testObj, 56 * 7, "TestMethod_OneParam=56*7", "integer binary '*' with =");
			TestSnippet(testObj, 56 / 7, "TestMethod_OneParam 56/7", "integer binary '/' without parens");
			TestSnippet(testObj, 56 / 7, "TestMethod_OneParam(56/7)", "integer binary '/' with parens");
			TestSnippet(testObj, 56 / 7, "TestMethod_OneParam=56/7", "integer binary '/' with =");

			TestSnippet(testObj, double.PositiveInfinity, "TestMethod_OneParam(56/0)", "division by zero");
		}

		[TestMethod]
		public void TestVariables() {
			var testObj = new LScriptTesterObject();

			TestSnippet(testObj, 1, "arg testlocal 1; TestMethod_OneParam arg testlocal", "ARG/LOCAL, syntax 1");
			TestSnippet(testObj, 2, "arg testlocal = 2; TestMethod_OneParam arg testlocal", "ARG/LOCAL, syntax 2");
			TestSnippet(testObj, 3, "arg.testlocal 3; TestMethod_OneParam arg.testlocal", "ARG/LOCAL, syntax 3");
			TestSnippet(testObj, 4, "arg.testlocal = 4; TestMethod_OneParam arg.testlocal", "ARG/LOCAL, syntax 4");
			TestSnippet(testObj, 5, "arg(testlocal,5); TestMethod_OneParam arg(testlocal)", "ARG/LOCAL, syntax 5");
			TestSnippet(testObj, 6, "arg(testlocal, 6); TestMethod_OneParam arg(testlocal)", "ARG/LOCAL, syntax 6");
			TestSnippet(testObj, 1, "local testlocal 1; TestMethod_OneParam local testlocal", "ARG/LOCAL, syntax 7");
			TestSnippet(testObj, 2, "local testlocal = 2; TestMethod_OneParam local testlocal", "ARG/LOCAL, syntax 8");
			TestSnippet(testObj, 3, "local.testlocal 3; TestMethod_OneParam local.testlocal", "ARG/LOCAL, syntax 9");
			TestSnippet(testObj, 4, "local.testlocal = 4; TestMethod_OneParam local.testlocal", "ARG/LOCAL, syntax 10");
			TestSnippet(testObj, 5, "local(testlocal,5); TestMethod_OneParam local(testlocal)", "ARG/LOCAL, syntax 11");
			TestSnippet(testObj, 6, "local(testlocal, 6); TestMethod_OneParam local(testlocal)", "ARG/LOCAL, syntax 12");

			TestSnippet(testObj, 1, "var testvariable 1; TestMethod_OneParam var testvariable", "VAR, syntax 1");
			TestSnippet(testObj, 2, "var testvariable = 2; TestMethod_OneParam var testvariable", "VAR, syntax 2");
			TestSnippet(testObj, 3, "var.testvariable 3; TestMethod_OneParam var.testvariable", "VAR, syntax 3");
			TestSnippet(testObj, 4, "var.testvariable = 4; TestMethod_OneParam var.testvariable", "VAR, syntax 4");
			TestSnippet(testObj, 5, "var(testvariable,5); TestMethod_OneParam var(testvariable)", "VAR, syntax 5");
			TestSnippet(testObj, 6, "var(testvariable, 6); TestMethod_OneParam var(testvariable)", "VAR, syntax 6");

			TestSnippet(testObj, 1, "tag testtag 1; TestMethod_OneParam tag testtag", "TAG, syntax 1");
			TestSnippet(testObj, 2, "tag testtag = 2; TestMethod_OneParam tag testtag", "TAG, syntax 2");
			TestSnippet(testObj, 3, "tag.testtag 3; TestMethod_OneParam tag.testtag", "TAG, syntax 3");
			TestSnippet(testObj, 4, "tag.testtag = 4; TestMethod_OneParam tag.testtag", "TAG, syntax 4");
			TestSnippet(testObj, 5, "tag(testtag,5); TestMethod_OneParam tag(testtag)", "TAG, syntax 5");
			TestSnippet(testObj, 6, "tag(testtag, 6); TestMethod_OneParam tag(testtag)", "TAG, syntax 6");
		}

		[TestMethod]
		public void TestOverloading() {
			var testObj = new LScriptTesterObject();

			//fails because ClassManager is not initialized. Oh well.

			//ambiguity test
			TestSnippet(testObj, 5, "TestMethod_IPointParam(Point4D(1,2,3,4))", "ambiguity test 1");
			TestSnippet(testObj, 6, "TestMethod_IPointParam(Point3D(1,2,3))", "ambiguity test 2");
			TestSnippet(testObj, 7, "TestMethod_IPointParam(Point2D(1,2))", "ambiguity test 3");

			TestSnippet(testObj, 2, "TestMethod_PointAndIpointParam(Point4D(1,2,3,4))", "ambiguity test 4");
			TestSnippet(testObj, 3, "TestMethod_PointAndIpointParam(Point3D(1,2,3))", "ambiguity test 5");
			TestSnippet(testObj, 4, "TestMethod_PointAndIpointParam(Point2D(1,2))", "ambiguity test 6");

			TestSnippet(testObj, 5, "TestMethod_IPointParam(LScriptTesterIPoint4D())", "ambiguity test 7");
			TestSnippet(testObj, 5, "TestMethod_PointAndIpointParam(LScriptTesterIPoint4D())", "ambiguity test 8");
		}

		[TestMethod]
		public void TestSpecialLiterals() {
			var testObj = new LScriptTesterObject();

			Sanity.IfTrueThrow(!TimerKey.Acquire("testtimerkey").Equals(LScriptMain.RunSnippet(testObj, "return(%testtimerkey)")), "timerkey");
			Sanity.IfTrueThrow(!TriggerKey.Acquire("testtriggerkey").Equals(LScriptMain.RunSnippet(testObj, "return(@testtriggerkey)")), "triggerkey");
		}

		private static void TestSnippet(LScriptTesterObject testObj, double difference, string script, Type expectedException, string errormessage) {
			testObj.currentCounter = 0;
			try {
				LScriptMain.RunSnippet(testObj, script);
			} catch (Exception e) {
				if ((expectedException != null)) {
					while (e.InnerException != null) {
						e = e.InnerException;
					}
					if (expectedException.IsInstanceOfType(e)) {
						testObj.CheckCounter(difference, errormessage);
						return;
					}
				}
				throw new SanityCheckException("Error while " + errormessage + ": " + e);
			}
			throw new SanityCheckException("Error while " + errormessage + ": Expected exception was not thrown.");
		}

		private static void TestSnippet(LScriptTesterObject testObj, double difference, string script, string expectedExcString, string errormessage) {
			testObj.currentCounter = 0;
			if ((expectedExcString != null) && (expectedExcString.Length == 0)) {
				expectedExcString = null;
			}
			var wasException = false;
			try {
				LScriptMain.RunSnippet(testObj, script);
			} catch (Exception e) {
				if ((expectedExcString != null) &&
						(expectedExcString == e.Message)) {
					wasException = true;
				} else {
					throw new SanityCheckException("Error while " + errormessage + ": " + e);
				}
			}
			if ((expectedExcString != null) && (!wasException)) {
				throw new SanityCheckException("Error while " + errormessage + ": Expected exception was not thrown.");
			}
			testObj.CheckCounter(difference, errormessage);
		}

		private static void TestSnippet(LScriptTesterObject testObj, double difference, string script, string errormessage) {
			testObj.currentCounter = 0;
			try {
				LScriptMain.RunSnippet(testObj, script);
			} catch (Exception e) {
				throw new SanityCheckException("Error while " + errormessage + ": " + e);
			}
			testObj.CheckCounter(difference, errormessage);
		}

		public class LScriptTesterIPoint4D : IPoint4D {
			public int X { get { return 0; } }
			public int Y { get { return 0; } }
			public int Z { get { return 0; } }
			public byte M { get { return 0; } }

			public IEnumerable ThingsInRange() { return null; }
			public IEnumerable ItemsInRange() { return null; }
			public IEnumerable CharsInRange() { return null; }
			public IEnumerable PlayersInRange() { return null; }
			public IEnumerable StaticsInRange() { return null; }
			public IEnumerable DisconnectsInRange() { return null; }
			public IEnumerable MultiComponentsInRange() { return null; }

			public IEnumerable ThingsInRange(ushort range) { return null; }
			public IEnumerable ItemsInRange(ushort range) { return null; }
			public IEnumerable CharsInRange(ushort range) { return null; }
			public IEnumerable PlayersInRange(ushort range) { return null; }
			public IEnumerable StaticsInRange(ushort range) { return null; }
			public IEnumerable DisconnectsInRange(ushort range) { return null; }

			public Map GetMap() { return null; }

			public IPoint4D TopPoint {
				get { return this; }
			}

			IPoint3D IPoint3D.TopPoint {
				get { return this; }
			}

			IPoint2D IPoint2D.TopPoint {
				get { return this; }
			}
		}


		public class LScriptTesterObject : PluginHolder {
			public double currentCounter;

			private void Incr() {
				this.currentCounter++;
			}

			private void Incr(double difference) {
				this.currentCounter += difference;
			}

			internal void CheckCounter(double expected, string failureMessage) {
				Sanity.IfTrueThrow(expected != this.currentCounter, $"Error while '{failureMessage}'. expected:{expected} != actual:{this.currentCounter}");
			}

			public void TestMethod_NoParams() {
				this.Incr();
			}

			public void TestMethod_OneParam(double difference) {
				//Console.WriteLine("TestMethod_OneParam("+difference+")");
				this.Incr(difference);
			}

			public void TestMethod_OneParam(int difference) {
				//Console.WriteLine("TestMethod_OneParam("+difference+")");
				this.Incr(difference);
			}



			public void TestMethod_PointAndIpointParam(Point4D point) {
				Console.WriteLine("TestMethod_PointAndIpointParam(Point4D)");
				this.Incr(2);
			}

			public void TestMethod_PointAndIpointParam(Point3D point) {
				Console.WriteLine("TestMethod_PointAndIpointParam(Point3D)");
				this.Incr(3);
			}

			public void TestMethod_PointAndIpointParam(Point2D point) {
				Console.WriteLine("TestMethod_PointAndIpointParam(Point2D)");
				this.Incr(4);
			}

			public void TestMethod_PointAndIpointParam(IPoint4D point) {
				Console.WriteLine("TestMethod_OneParam(IPoint4D)");
				this.Incr(5);
			}

			public void TestMethod_PointAndIpointParam(IPoint3D point) {
				Console.WriteLine("TestMethod_OneParam(IPoint3D)");
				this.Incr(6);
			}

			public void TestMethod_PointAndIpointParam(IPoint2D point) {
				Console.WriteLine("TestMethod_OneParam(IPoint2D)");
				this.Incr(7);
			}

			public void TestMethod_IPointParam(IPoint4D point) {
				Console.WriteLine("TestMethod_OneParam(IPoint4D)");
				this.Incr(5);
			}

			public void TestMethod_IPointParam(IPoint3D point) {
				Console.WriteLine("TestMethod_OneParam(IPoint3D)");
				this.Incr(6);
			}

			public void TestMethod_IPointParam(IPoint2D point) {
				Console.WriteLine("TestMethod_OneParam(IPoint2D)");
				this.Incr(7);
			}

			public void TestMethod_OneStringParam(string str) {
				this.Incr();
			}

			public int GetInteger() {
				return 564;
			}

			public string GetString() {
				return "test string";
			}

			public object GetSelf() {
				return this;
			}
		}
	}
}



//briefly what must be tested, from the grammar file...

//done tests

//Number: int, hex, float
//SimpleExpression: Caller Indexer OperatorAssigner WhiteSpaceAssigner
//VarExpression (TAG VAR ARG/LOCAL)
//TriggerKey
//TimerKey
//QuotedString, ESCAPEDCHAR
//EvalExpression StrongEvalExpression
//DottedExpressionChain


//partly done

//BinaryOperator: + - * / & % | && || 
//ComparOperators: == != < > <= >=
//UnaryOperator: ! ~ + -



//tests to do yet

//Script
//IfBlock ElseIfBlock ElseBlock
//ForeachBlock
//WhileBlock
//SwitchBlock
//CaseBlock
//AddTimerExpression
//Argument


//not even implemented yet ;)

//RandomExpression
//CROSSHASH


