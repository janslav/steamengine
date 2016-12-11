using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace SteamEngine.AuxiliaryServer
{
    /// <summary>A specialization of the ConsoleLogger that logs to SE log file instead of the console. </summary>
    public class MsBuildLogger : ConsoleLogger
    {
        public MsBuildLogger()
            : base(LoggerVerbosity.Minimal)
        {
            this.WriteHandler = this.Write;
        }

        private void Write(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                Common.Logger.StaticWriteLine(text.Trim());
            }
        }
    }
}