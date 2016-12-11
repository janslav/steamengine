/*
	This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace SteamEngine.Common
{


    /// <summary>Use this class to run tasks from the sln/csproj files.</summary>
    public class MsBuildLauncher
    {
        private string slnName = "SteamEngine.sln";

        public string Compile(string seRootPath, SEBuild build, string targetTask, ILogger msBuildLogger)
        {
            var slnPath = Path.Combine(seRootPath, slnName);
            var globalProperties = new Dictionary<string, string>();
            var buildRequest = new BuildRequestData(slnPath, globalProperties, null, new string[] { targetTask }, null);
            var pc = new ProjectCollection();
            pc.SetGlobalProperty("Configuration", build.ToString());
            var buildParameters = new BuildParameters(pc) { Loggers = new[] { msBuildLogger } };
            var result = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);

            if (result.OverallResult == BuildResultCode.Success)
            {
                var compiledFilePath = result.ResultsByTarget[targetTask].Items[0].ItemSpec;
                if (File.Exists(compiledFilePath))
                {
                    return compiledFilePath;
                }
                else
                {
                    throw new Exception("The compiled file '" + compiledFilePath + "' doesn't exist.");
                }
            }

            throw new Exception("Compilation failed.", result.Exception);
        }

        //private class Logger : ILogger
        //{
        //    public void Initialize(IEventSource eventSource)
        //    {

        //    }

        //    public void Shutdown()
        //    {

        //    }

        //    public LoggerVerbosity Verbosity { get; set; }
        //    public string Parameters { get; set; }
        //}

    }
}