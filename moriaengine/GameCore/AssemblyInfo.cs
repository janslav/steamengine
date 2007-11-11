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
using System.Reflection;
using System.Runtime.CompilerServices;

//Namespaces where the permissions are found
//using System.Data.Common;			//Doesn't exist - missing a reference?
//using System.Data.OracleClient;	//Doesn't exist - missing a reference?
//using System.Drawing.Printing;	//Doesn't exist - missing a reference?
//using System.Messaging;			//Doesn't exist - missing a reference?
using System.Net;
using System.Security.Permissions;
using System.Web;


// Information about this assembly is defined by the following
// attributes.
//
// change them to the information which is associated with the assembly
// you compile.





//[assembly: CLSCompliant(true)]

[assembly: AssemblyTitle("SteamEngine")]
[assembly: AssemblyDescription("MMORPG Server Application compatible with UO clients")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SteamEngine Team")]
[assembly: AssemblyProduct("SteamEngine")]


// The assembly version has following format :
//
// Major.Minor.Build.Revision
//
// You can specify all values by your own or you can build default build and revision
// numbers with the '*' character (the default):

[assembly: AssemblyVersion("0.0.*")]

// The following attributes specify the key for the sign of your assembly. See the
// .NET Framework documentation for more information about signing.
// This is not required, if you don't want signing let these attributes like they're.
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("")]

/*

//With these permissions, it fails when we try to compile stuff. :( 	-SL

// The following security permissions and the comments for them have been taken from the
// .NET documentation and parsed into a pretty comment and such with a search-and-replace
// command. And then I set some to minimum, some to optional, the rest to refused,
// and grouped them together like that and added extra comments on a few of them (to the
// right of the attribute instead of above it).
//  -SL
	//Minimum required permissions:
//Read, append, or write files or directories. 
[assembly:FileIOPermission(SecurityAction.RequestMinimum,Unrestricted=true)]	
//Discover information about a type at run time. 
[assembly:ReflectionPermission(SecurityAction.RequestMinimum,Unrestricted=true)]	
//Access user interface functionality. 
[assembly:UIPermission(SecurityAction.RequestMinimum,Unrestricted=true)]	

	//Optional permissions:
//Access to Domain Name System (DNS). 
[assembly:DnsPermission(SecurityAction.RequestOptional,Unrestricted=true)]	
//Make or accept connections on a Web address. 
[assembly:WebPermission(SecurityAction.RequestOptional,Unrestricted=true)]	
//Access files that have been selected by the user in an Open dialog box. 
[assembly:FileDialogPermission(SecurityAction.RequestOptional,Unrestricted=true)]	
//Execute, assert permissions, call into unmanaged code, skip verification, and other rights. 
[assembly:SecurityPermission(SecurityAction.RequestOptional,Unrestricted=true)]	

	//Permissions we DON'T need (and so we refuse them):
//Access resources in ASP.NET-hosted environments. 
[assembly:AspNetHostingPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Read or write environment variables. 
[assembly:EnvironmentPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access isolated storage, which is storage that is associated with a specific user and with some aspect of the code's identity, such as its Web site, publisher, or signature. 
[assembly:IsolatedStorageFilePermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access printers. 
[assembly:PrintingPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Read, write, create, or delete registry keys and values. 
[assembly:RegistryPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Make or accept connections on a transport address. 
[assembly:SocketPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	

//	Ones we can't access because we're missing a reference or something:
//Access SQL databases. 
//[assembly:SqlClientPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access performance counters.  (We use our own performance-testing code, so we don't need this)
//[assembly:PerformanceCounterPermission(SecurityAction.RequestRefuse,Unrestricted=true)]
//Access message queues through the managed Microsoft Message Queuing (MSMQ) interfaces. 
//[assembly:MessageQueuePermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access an ODBC data source. 
//[assembly:OdbcPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access databases using OLE DB. 
//[assembly:OleDbPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access an Oracle database. 
//[assembly:OraclePermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Access to the System.DirectoryServices classes. 
//[assembly:DirectoryServicesPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	
//Read or write access to event log services.  (We use our own logging code, so we don't need this)
//[assembly:EventLogPermission(SecurityAction.RequestRefuse,Unrestricted=true)]
//Access running or stopped services. 
//[assembly:ServiceControllerPermission(SecurityAction.RequestRefuse,Unrestricted=true)]	



*/