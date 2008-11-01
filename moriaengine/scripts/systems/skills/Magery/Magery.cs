using System;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class MagerySkillDef : SkillDef {

		public MagerySkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

		protected override void On_Start(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Fail(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Abort(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Stroke(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Success(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void On_Select(Character ch) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}