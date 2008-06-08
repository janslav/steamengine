using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public static class ScriptUtil {
		public static ArrayList ArrayListFromEnumerable(IEnumerable enumerable) {
			return ArrayListFromEnumerator(enumerable.GetEnumerator());
		}

		public static ArrayList ArrayListFromEnumerator(IEnumerator enumerator) {
			ArrayList list = new ArrayList();
			while (enumerator.MoveNext()) {
				list.Add(enumerator.Current);
			}
			return list;
		}

		public static string GetLogString(GameConn conn, string message) {
			AbstractAccount acc = conn.Account;
			AbstractCharacter ch = conn.CurCharacter;
			if (acc != null) {
				if (ch != null) {
					return string.Concat(
						"Acc '", acc.Name, "', char '", ch.Name, "' (#", ch.Uid.ToString("x"), "): " + message);
				} else {
					return string.Concat(
						"Acc '", acc.Name, "': " + message);
				}
			} else {
				return string.Concat(
					"Client ", conn.uid, ": " + message);
			}
		}

		[Summary("returns a number based on the bounds and ratio")]
		[Remark("When ratio == 0, returns min; when ratio == 1.0, returns max.<br>"
		+ "Otherwise, returns a number based on simple linear interpolation. "
		+ "Note that ratio can also be negative or >1")]
		public static double EvalRangeDouble(double ratio, double min, double max) {
			double range = max - min;
			return min + (range * ratio);
		}

		[Summary("Works like EvalRangeDouble, only the ratio parameter is in per mille (a.e. typical for skills).")]
		public static double EvalRangePermille(double pmratio, double min, double max) {
			return EvalRangeDouble(pmratio / 1000.0, min, max);
		}

		public static double EvalRangePermille(double pmratio, params double[] arr) {
			return EvalRangeDouble(pmratio / 1000.0, arr);
		}

		public static double EvalRangeDouble(double ratio, params double[] arr) {
			double segSize;
			int minIdx;

			int len = arr.Length;
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

			double min = arr[minIdx];
			double max = arr[minIdx + 1];
			return min + (((max - min) * ratio) / segSize);
		}

		public static int EvalRandomFaktor(int ratio, int min, int max) {
			int randomValue = Globals.dice.Next(ratio * min, ratio * max);
			return randomValue / 1000;
		}

		public static double GetRandInRange(double min, double max) {
			double randomValue = Globals.dice.NextDouble();
			double diff = max - min;
			return min + (randomValue * diff);
		}
	}
}