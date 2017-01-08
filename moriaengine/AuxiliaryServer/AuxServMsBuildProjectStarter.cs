using System;
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	internal class AuxServMsBuildProjectStarter {
		private readonly BuildType build;
		private readonly string seRootPath;
		private readonly string targetTask;

		internal AuxServMsBuildProjectStarter(BuildType build, string seRootPath, string targetTask) {
			this.build = build;
			this.seRootPath = seRootPath;
			this.targetTask = targetTask;
		}

		public void Start() {
			var t = new Thread(this.CompileAndStart);
			t.Start();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public virtual void CompileAndStart() {
			try {
				var file = MsBuildLauncher.Compile(this.seRootPath, this.build, this.targetTask);
				Console.WriteLine("Starting " + file);
				this.StartProcess(file);

			} catch (Exception e) {
				Common.Logger.WriteError(e);
			}
		}

		public virtual void StartProcess(string file) {
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(file) {
				WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized
			};
			System.Diagnostics.Process.Start(psi);
		}
	}
}