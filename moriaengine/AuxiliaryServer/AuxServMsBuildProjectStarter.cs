using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.Build.Logging;
using NAnt.Core;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer
{
    internal class AuxServMsBuildProjectStarter
    {
        private SEBuild build;
        private string seRootPath;
        private string targetTask;
        private string filenameProperty;

        internal AuxServMsBuildProjectStarter(SEBuild build, string seRootPath, string targetTask, string filenameProperty)
        {
            this.build = build;
            this.seRootPath = seRootPath;
            this.targetTask = targetTask;
            this.filenameProperty = filenameProperty;
        }

        public void Start()
        {
            Thread t = new Thread(this.CompileAndStart);
            t.Start();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public virtual void CompileAndStart()
        {
            try
            {
                //NantLauncher nant = new NantLauncher(Path.Combine(this.seRootPath, NantLauncher.defaultPathInProject));
                //nant.SetLogger(this);
                //nant.SetPropertiesAndSymbols(this.build);
                ////nant.SetDebugMode(this.build == SEBuild.Debug);
                ////nant.SetOptimizeMode(this.build == SEBuild.Optimised);

                //nant.SetTarget(this.targetTask);
                //nant.Execute();

                //if (nant.WasSuccess()) {
                //	string file = nant.GetCompiledAssemblyName(this.seRootPath, filenameProperty);

                //	
                //	
                //}

                var msBuild = new MsBuildLauncher();
                var file = msBuild.Compile(this.seRootPath, this.build, this.targetTask, new MsBuildLogger());
                Console.WriteLine("Starting " + file);
                StartProcess(file);

            }
            catch (Exception e)
            {
                Logger.WriteError(e);
            }
        }

        public virtual void StartProcess(string file)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(file);
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            System.Diagnostics.Process.Start(psi);
        }
    }
}