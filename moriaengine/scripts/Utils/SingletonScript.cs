using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	[Summary("Generic-based store for singleton AbstractScript descendants, for easy access in C# scripts.")]
	public static class SingletonScript<T> where T : AbstractScript {
		static T instance;

		public static T Instance {
			get {
				if (instance == null) {
					FindSingletonInstance();
					if (instance == null) {
						throw new FatalException("No instance found of class " + typeof(T) + ".");
					}
				}
				return instance;
			}
		}

		private static void FindSingletonInstance() {
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				T castScript = script as T;
				if (castScript != null) {
					if (instance != null) {
						throw new FatalException(typeof(T)+" is not a singleton script class, you can't use it as such.");
					}
					instance = castScript;
				}
			}
		}
	}
}