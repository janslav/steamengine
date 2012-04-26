using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchyUtils;

namespace SaveCruncher {
	class SaveParser {
		public static async Task Parse(string saveFilePath) {
			Logger.Write("Starting loading '" + saveFilePath + "'");

			int c = 0;

			await Task.Run(() => {
				foreach (var line in File.ReadLines(saveFilePath)) {
					c++;
				}
			});

			Logger.Write("Lines: " + c);
			Logger.Write("Finished loading '" + saveFilePath + "'");
		}
	}
}
