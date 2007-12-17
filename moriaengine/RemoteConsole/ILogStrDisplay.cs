using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public interface ILogStrDisplay {
		void WriteLine(string data);
		void WriteLine(LogStr data);
		void Write(string data);
		void Write(LogStr data);
	}
}