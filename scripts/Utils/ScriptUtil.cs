using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Scripting;
using SteamEngine.Scripting.Compilation;
using SteamEngine.Scripting.Objects;

namespace SteamEngine.CompiledScripts {
	public static class ScriptUtil {
		public static ArrayList ArrayListFromEnumerable(IEnumerable enumerable) {
			return ArrayListFromEnumerator(enumerable.GetEnumerator());
		}

		public static ArrayList ArrayListFromEnumerator(IEnumerator enumerator) {
			var list = new ArrayList();
			while (enumerator.MoveNext()) {
				list.Add(enumerator.Current);
			}
			return list;
		}

		public static string GetLogString(GameState state, string message) {
			var acc = state.Account;
			var ch = state.Character;
			if (acc != null)
			{
				if (ch != null) {
					return string.Concat(
						"Acc '", acc.Name, "', char '", ch.Name, "' (#", ch.Uid.ToString("x"), "): " + message);
				}
				return string.Concat(
					"Acc '", acc.Name, "': " + message);
			}
			return string.Concat(
				"Client ", state.Uid, ": " + message);
		}

		/// <summary>
		/// returns a number based on the bounds and ratio
		/// </summary>
		/// <remarks>
		/// When ratio == 0, returns min; when ratio == 1.0, returns max.
		/// Otherwise, returns a number based on simple linear interpolation. 
		/// Note that ratio can also be negative or greater than 1
		/// </remarks>
		public static double EvalRangeDouble(double ratio, double min, double max) {
			var range = max - min;
			return min + (range * ratio);
		}

		/// <summary>Works like EvalRangeDouble, only the ratio parameter is in per mille (i.e. typical for skills).</summary>
		public static double EvalRangePermille(double pmratio, double min, double max) {
			return EvalRangeDouble(pmratio / 1000.0, min, max);
		}

		public static double EvalRangePermille(double pmratio, params double[] arr) {
			return EvalRangeDouble(pmratio / 1000.0, arr);
		}

		public static double EvalRangeDouble(double ratio, params double[] arr) {
			double segSize;
			int minIdx;

			var len = arr.Length;
			switch (len) {
				case 0:
					return 0;
				case 1:
					return arr[0];
				case 2:
					minIdx = 0;
					segSize = 1;
					break;
				case 3:
					//optimisation
					if (ratio >= 0.5) {
						minIdx = 1;
						ratio -= 0.5;
					} else {
						minIdx = 0;
					}
					segSize = 0.5;
					break;
				default:
					//generic array
					minIdx = (int) (ratio * len);
					len--;
					if (minIdx < 0) {
						minIdx = 0;
					}
					if (minIdx >= len) {
						minIdx = len - 1;
					}
					segSize = 1.0 / len;
					ratio -= minIdx * segSize;
					break;
			}

			var min = arr[minIdx];
			var max = arr[minIdx + 1];
			return min + (((max - min) * ratio) / segSize);
		}

		public static int EvalRandomFaktor(int ratio, int min, int max) {
			var randomValue = Globals.dice.Next(ratio * min, ratio * max);
			return randomValue / 1000;
		}

		public static double GetRandInRange(double min, double max) {
			var randomValue = Globals.dice.NextDouble();
			var diff = max - min;
			return min + (randomValue * diff);
		}

		public static void ListScripts(string args) {
			var testString = string.Concat(args);
			Globals.SrcWriteLine("Listing scripts containing string '<testString>' in their defname:");

			var re = new Regex(testString);

			foreach (var script in AbstractScript.AllScripts) {
				var defname = script.Defname;
				if ((defname != null) && re.IsMatch(defname)) {
					Globals.SrcWriteLine(script.ToString());
				} else {
					var def = script as AbstractDef;
					if (def != null) {
						defname = def.Altdefname;
						if ((defname != null) && re.IsMatch(defname)) {
							Globals.SrcWriteLine(script.ToString());
						}
					}
				}
			}
		}

		[SteamFunction("ListScripts")]
		public static void ListScriptsSF(object ignored, ScriptArgs sa) {
			ListScripts(sa.Args);
		}

		private static ScriptHolder periodicSaveInformationFunction;
		private static ScriptHolder PeriodicSaveInformationFunction {
			get {
				if (periodicSaveInformationFunction == null) {
					periodicSaveInformationFunction = ScriptHolder.GetFunction("periodicSaveInformation");
				}
				if (periodicSaveInformationFunction != null) {
					if (!periodicSaveInformationFunction.IsUnloaded) {
						return periodicSaveInformationFunction;
					}
				}
				return null;
			}
		}

		[SteamFunction]
		public static void Information() {
			Globals.Src.WriteLine(string.Format(CultureInfo.InvariantCulture,
				@"Steamengine - {0}, Name = ""{1}"", Clients = {2}{6}Items = {3}, Chars = {4}, Mem = {5} kB",
				Globals.Version, Globals.ServerName, GameServer.AllClients.Count, AbstractItem.Instances, AbstractCharacter.Instances,
				GC.GetTotalMemory(false) / 1024, Environment.NewLine));

			var saveInfoFunc = PeriodicSaveInformationFunction;
			if (saveInfoFunc != null) {
				ConvertTools.ToString(saveInfoFunc.Run(Globals.Instance, (object[]) null));
			}
		}
	}
}