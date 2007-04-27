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

/* Table of Contents (Select a line and hit F3. Or whatever you have to do to search for it.)

//Variables and Properties
	//- Constants
	//- Public
	//- Properties
	//- Private
//Static GameAccount methods
//Public methods
//Private & internal stuff
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Cryptography;
using SteamEngine.Packets;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {

	public class GameAccount : TagHolder {

		/// private static int firstFreeGameAccount=0; private static int
		/// lastUsedGameAccount=-1;                                      
		private static Dictionary<string, GameAccount> accounts = new Dictionary<string, GameAccount>(StringComparer.OrdinalIgnoreCase);
		//private string _password = null;	//http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpguide/html/cpconensuringdataintegritywithhashcodes.asp
		private Byte[] passwordHash = null;
		private string password = null;

		/// private uid representation.
		//private int uid = 0; public int Uid { get { return uid; } }

		private byte maxPlevel = 0;
		private byte plevel = 0;


		//------------------------
		//Variables and Properties

		//- Constants
		public const uint maxCharactersPerGameAccount = 5;

		//- Public
		private AbstractCharacter[] characters = new AbstractCharacter[maxCharactersPerGameAccount];

		private bool deleted = false;
		public bool blocked = false;
		private bool allShow = false;
		private string name = null;

		private GameConn conn = null;

		public static void ClearAll() {
			accounts.Clear();
		}

		public bool Online {
			get {
				return (conn != null);
			}
		}

		public GameConn Conn {
			get {
				return conn;
			}
		}

		public void Password(string pass) {
			Commands.AuthorizeCommandThrow(Globals.Src, "SetAccountPassword");
			if (Globals.hashPasswords) {
				this.passwordHash=HashPassword(pass);
				this.password=null;
			} else {
				this.password=pass;
				this.passwordHash=null;
			}
		}

		public static GameAccount Get(string acctname) {
			GameAccount acc;
			if (accounts.TryGetValue(acctname, out acc)) {
				return acc;
			}
			return null;
		}

		public AbstractCharacter GetLingeringCharacter() {
			foreach (AbstractCharacter ch in this.characters) {
				if ((ch != null) && (ch.IsLingering)) {
					return ch;
				}
			}
			return null;
		}

		public static void HandleLoginAttempt(string username, string password, GameConn c) {
			GameAccount acc = null;
			acc = GameAccount.Get(username);
			if (acc==null) {
				if ((Globals.autoAccountCreation) || (accounts.Count == 0)) {
					acc=CreateGameAccount(username, password);
					Console.WriteLine("Creating new account {0}", username);
					c.LogIn(acc);
					acc.LogIn(c);
					PacketSender.SendCharList(c);
					//Server._out.SendCharList(c);
				} else {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.NoAccount);
					c.Close("No account '"+username+"'");
				}
			} else {
				if (!acc.TestPassword(password)) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.InvalidAccountCredentials);
					c.Close("Bad password for account "+acc);
				} else if (acc.IsBlocked) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.Blocked);
					c.Close("Account '"+acc+"' blocked.");
				} else if (acc.Online) {
					Packets.Prepared.SendFailedLogin(c, FailedLoginReason.SomeoneIsAlreadyUsingThisAccount);
					c.Close("Account '"+acc+"' already online.");
				} else {
					c.LogIn(acc);
					acc.LogIn(c);
					PacketSender.SendCharList(c);
					//Server._out.SendCharList(c);
				}
			}
		}

		public static GameAccount HandleConsoleLoginAttempt(string username, string password) {
			GameAccount acc = null;
			if (accounts.Count==0) {
				Console.WriteLine("Creating new account {0}", username);
				acc=CreateGameAccount(username, password);
			}
			acc = GameAccount.Get(username);
			if (acc==null) {
				return null;
			} else if (acc.TestPassword(password)==false || (acc.maxPlevel<4) || (acc.IsBlocked)) {
				return null;
			} else {
				return acc;
			}
		}

		//commands:
		//Call one of these to make an account
		public static GameAccount CreateGameAccount(string acctname, string pass) {
			//can't be called "eof" cos it would break the account save file. We know that from sphere don't we ;)
			if (String.Equals("eof", acctname, StringComparison.OrdinalIgnoreCase)) {
				return null;
			}

			if (!accounts.ContainsKey(acctname)) {
				GameAccount acc = new GameAccount(acctname, pass,
					(accounts.Count == 0 ? Globals.maximalPlevel : (byte) 1));
				accounts[acctname] = acc;
				Logger.WriteDebug("GameAccount "+acctname+" created");
				return acc;
			} else {
				return null;
			}
		}

		public static void Create(string name, string pass) {
			Add(name, pass);
		}

		public static void Add(string name, string pass) {
			if (String.Equals("eof", name, StringComparison.OrdinalIgnoreCase)) {
				Globals.SrcWriteLine("EOF is an illegal account name");
				return;
			}
			GameAccount acc=CreateGameAccount(name, pass);
			if (acc==null) {
				Globals.SrcWriteLine("Failed to add account "+name+": That account already exists.");
			} else {
				Globals.SrcWriteLine("GameAccount "+name+" added successfully.");
			}
		}

		private GameAccount(string name, string password, byte plevel) {
			instances++;
			this.name=name;
			if (password==null) {
				this.passwordHash=null;
				this.password=null;
			} else {
				if (Globals.hashPasswords) {
					//Logger.WriteDebug("Hashing password.");
					this.passwordHash=HashPassword(password);
					this.password=null;
				} else {
					this.password=password;
					this.passwordHash=null;
				}
			}
			this.plevel=plevel;
			this.maxPlevel=plevel;
			for (int a=0; a<maxCharactersPerGameAccount; a++) {
				characters[a]=null;
			}

		}

		public bool AllShow {
			get {
				return allShow;
			}
			set {
				allShow=value;
				if (conn!=null && conn.CurCharacter!=null) {
					conn.CurCharacter.Resync();
				}
			}
		}


		//- Properties
		public static IEnumerable Enumerable {
			get {
				return accounts as IEnumerable;
			}
		}

		public override string Name {
			get {
				return name;
			}
		}

		public byte PLevel {
			get { return plevel; }
			set {
				if (value<maxPlevel && value<=Globals.maximalPlevel) {
					plevel=value;
				} else {
					plevel=maxPlevel;
				}
			}
		}
		public byte MaxPLevel {
			get { return maxPlevel; }
		}

		private static uint instances = 0;

		public static uint Instances {
			get {
				return instances;
			}
		}

		/*
			Method: Promote
			Promote this account to <plevel> maxPlevel. Both their maxPlevel and plevel are set to this, but note
			that they can change their own plevel (but can't set it above their maxPlevel). Yes, you can set
			your plevel to 1, and then later set it back to however high you want it, so long as that is not
			above your maxPlevel. The "GM" command toggles between player plevel and your maxPlevel.
			
			Parameters:
				name - Their account name
				plevel - The maxPlevel you want them to be.
		*/
		public void Promote(int plevel) {
			PromoteOrDemote(Globals.Src.MaxPlevel, GameAccount.Get(name), plevel);
		}

		public override bool IsDeleted { get { return deleted; } }

		public override void Delete() {
			DeleteGameAccount();
			Globals.SrcWriteLine("GameAccount "+name+" removed successfully.");
		}

		//Call this to delete an account
		public void DeleteGameAccount() {
			BeingDeleted();
			//	if (uid<firstFreeGameAccount) {
			//		firstFreeGameAccount=uid;
			//	}
			//	while (lastUsedGameAccount>-1 && accounts[lastUsedGameAccount]==null) {
			//		accounts.Remove(lastUsedGameAccount);
			//		lastUsedGameAccount--;
			//	}
		}

		public void Block() {
			if (IsBlocked) {
				Globals.SrcWriteLine("GameAccount "+name+" is already blocked.");
			} else {
				if (MaxPLevel<Globals.maximalPlevel) {
					Commands.AuthorizeCommandThrow(Globals.Src, "BlockAccount");
					blocked=true;
					Globals.SrcWriteLine("GameAccount "+name+" blocked successfully.");
				} else {
					Globals.SrcWriteLine("GameAccount "+name+" cannot be blocked; It's owner..");
				}
			}
		}

		public void UnBlock() {
			if (!IsBlocked) {
				Globals.SrcWriteLine("GameAccount "+name+" is not blocked.");
			} else {
				blocked=false;
				Commands.AuthorizeCommandThrow(Globals.Src, "UnBlockAccount");
				Globals.SrcWriteLine("GameAccount "+name+" unblocked successfully.");
			}
		}

		public void Block(bool yesorno) {
			if (yesorno) {
				Block();
			} else {
				UnBlock();
			}
		}

		public string Info() {
			string info="GameAccount "+name+"'s plevel is "+PLevel+" out of a maxPlevel of "+MaxPLevel+". It "+(IsBlocked?"is":"is not")+" blocked, and it has the following characters:";
			bool foundAny=false;
			for (int a=0; a<GameAccount.maxCharactersPerGameAccount; a++) {
				if (characters[a]!=null) {
					foundAny=true;
					AbstractCharacter cre=characters[a];
					info+=cre.ToString();
					//if (cre.Title!=null) {
					//	info+=" (Title '"+cre.Title+"')";
					//}
				}
			}
			if (foundAny==false) {
				info+="None.";
			}
			return info;
		}

		public override string ToString() {
			return name+" - Plevel "+PLevel+"/"+MaxPLevel;
		}

		public bool IsBlocked { get { return blocked; } }

		public override int GetHashCode() {
			return name.GetHashCode();
		}

		public override bool Equals(Object obj) {
			return Object.ReferenceEquals(this, obj);
		}

		//------------------------
		//Private & internal stuff

		/**
			You should call MakeBeNonPlayer on a character, and it will call this.
			Detaches a character from this account. This does NOT clear their account.
			If the character is not on this account, calling this method is considered
			an error and results in a thrown SanityCheckException.

			@param cre The character to detach from the account.
				
		*/
		internal void DetachCharacter(AbstractCharacter cre) {
			for (int i=0; i<maxCharactersPerGameAccount; i++) {
				if (characters[i]==cre) {
					characters[i]=null;
					return;
				}
			}
			throw new SanityCheckException("Call was made to DetachCharacterFromAccount, but the character ("+cre+") was not attached to this account ("+this+")!");
		}

		/**
			You should call MakeBePlayer on a character, and it will call this.
			Attaches a character to this account, if any slots are free. This does NOT set their account.
			If they're already in this account, this returns true (So that the account-fixing code works).
			
			@param cre The character to attach to the account.
				
			@return	True if the character was successfully attached, false otherwise.
		*/
		internal bool AttachCharacter(AbstractCharacter cre) {
			for (int i=0; i<maxCharactersPerGameAccount; i++) {
				if (characters[i]==cre) {//we have that char already
					return true;
				}
			}
			for (int i=0; i<maxCharactersPerGameAccount; i++) {
				if (characters[i]==null) {
					characters[i]=cre;
					return true;
				}
			}
			return false;
		}

		public AbstractCharacter GetCharacterInSlot(int index) {
			Sanity.IfTrueThrow(index<0 || index>=GameAccount.maxCharactersPerGameAccount, "Call was made to GetCharacterInSlot with an invalid character index "+index+", valid values being from 0 to "+(GameAccount.maxCharactersPerGameAccount-1)+".");
			//Sanity.IfTrueThrow(conn==null, "Call was made to LoginCharacter when account was null!"); //wtf?? why could it not be null? -tar
			if (characters[index]==null) {
				return null;
			} else {
				return characters[index];
			}
		}

		internal DeleteRequestReturnValue RequestDeleteCharacter(int index) {
			Sanity.IfTrueThrow(index<0 || index>=GameAccount.maxCharactersPerGameAccount, "Call was made to RequestDeleteCharacter with an invalid character index "+index+", valid values being from 0 to "+(GameAccount.maxCharactersPerGameAccount-1)+".");
			AbstractCharacter cre = characters[index];
			if (cre==null) {
				return DeleteRequestReturnValue.Reject_NonexistantCharacter;
			}
			if (!cre.Flag_Disconnected && !cre.IsLingering) {
				return DeleteRequestReturnValue.Reject_CharacterIsBeingPlayedRightNow;
			}
			//TODO: Trigger on=@deleteCharacter or something (someone else can decide what to put it on)
			// with return 1 to cancel it.

			Thing.DeleteThing(cre);
			characters[index]=null;
			return DeleteRequestReturnValue.AcceptedRequest;
		}

		internal static void PromoteOrDemote(int promotingPlevel, GameAccount whoToPromote, int promoteOrDemoteTo) {
			if (promoteOrDemoteTo>=0 && promoteOrDemoteTo<=Globals.maximalPlevel && promotingPlevel>=promoteOrDemoteTo) {
				whoToPromote.maxPlevel=(byte) promoteOrDemoteTo;
				whoToPromote.plevel=(byte) promoteOrDemoteTo;
			}
		}

		private void CheckReferences() {
			for (int i=0; i<maxCharactersPerGameAccount; i++) {
				if (characters[i]!=null) {
					if (characters[i].IsDeleted || characters[i].Account!=this) {
						//deleted or removed from account
						characters[i]=null;
					}
				}
			}
		}

		internal protected override void BeingDeleted() {
			Commands.AuthorizeCommandThrow(Globals.Src, "DeleteAccount");

			accounts.Remove(this.name);
			deleted = true;
			//delete characters, and conn
			instances--;
			if (conn!=null) {
				Packets.Prepared.SendFailedLogin(conn, FailedLoginReason.NoAccount);
				conn.Close("Account is being deleted.");
			}
			for (int a=0; a<maxCharactersPerGameAccount; a++) {
				if (characters[a]!=null) {
					Thing.DeleteThing(characters[a]);
					characters[a]=null;
				}
			}
			base.BeingDeleted();
		}

		internal int GetBlankChar() {
			for (int a=0; a<maxCharactersPerGameAccount; a++) {
				if (characters[a]==null) {
					return a;
				}
			}
			return -1;
		}

		internal void LogIn(GameConn conn) {
			this.conn=conn;
		}
		//Called by GameConn's Close method. Do not call this to log someone out.
		internal void LogOut() {
			this.conn=null;
		}

		private static int loaded;
		internal static void StartingLoading() {
			loaded=0;
		}

		internal static void Load(PropsSection input) {
			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			string name=input.headerType;

			GameAccount curAcc = new GameAccount(name, "", 1);
			accounts[name] = curAcc;
			curAcc.LoadSectionLines(input);
			loaded++;
		}

		//static Regex charuidRE= new Regex(@"charuid\[(?<index>\d+)\]\s*$",
		//	RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		protected override void LoadLine(string filename, int line, string name, string value) {
			//gets a name/value pair for each line in the file
			//Match m=charuidRE.Match(name);
			//if (m.Success) {
			//    int index=int.Parse(m.Groups["index"].Value, NumberStyles.Integer);
			//    if (index>=maxCharactersPerGameAccount) {
			//        throw new Exception("More than allowed characters per account");	
			//    }
			//    ObjectSaver.Load(value, new LoadObjectParam(CharLoad_Delayed), filename, line, index);
			//    return;
			//}
			switch (name) {
				case "password":
					Match m= TagMath.stringRE.Match(value);
					if (m.Success) {
						if (Globals.hashPasswords) {
							this.passwordHash = HashPassword(m.Groups["value"].Value);
							this.password = null;
						} else {
							this.password = m.Groups["value"].Value;
							this.passwordHash = null;
						}
					} else {
						if (Globals.hashPasswords) {
							this.passwordHash = HashPassword(value);
							this.password = null;
						} else {
							this.password = value;
							this.passwordHash = null;
						}
					}
					break;
				case "passwordHash":
					m= TagMath.stringRE.Match(value);
					if (m.Success) {
						if (Globals.hashPasswords) {
							if (this.passwordHash==null) {	//Allows admins to set password=xxx without erasing passwordHash, and the password=xxx will override the passwordHash.
								this.passwordHash = DecodeEncodedHash(m.Groups["value"].Value);
							}
							this.password = null;
						} else {
							if (this.password==null) {		//Allows admins to set password=xxx without erasing passwordHash, etc.
								this.passwordHash = null;
								this.password = m.Groups["value"].Value;
							}
						}
					} else {
						Logger.WriteError(filename, line, "Saved passwordHash for acc "+this+" has invalid format");
					}
					break;
				case "blocked":
					this.blocked=false;
					if (value.ToLower().StartsWith("true")) {
						this.blocked=true;
					} else if (!value.StartsWith("0")) {
						this.blocked=true;
					}
					break;
				case "plevel":
					this.plevel=byte.Parse(value, NumberStyles.Integer);
					break;
				case "maxplevel":
					this.maxPlevel=byte.Parse(value, NumberStyles.Integer);
					break;
				default:
					base.LoadLine(filename, line, name, value);
					break;
			}
		}

		internal static void LoadingFinished() {
			//accounts.LoadingFinished();
			Logger.WriteDebug("Loaded "+loaded+" accounts.");
			return;
		}

		public static void SaveAll(SaveStream output) {
			Logger.WriteDebug("Saving GameAccounts.");
			output.WriteComment("Textual SteamEngine save");
			output.WriteComment("GameAccounts");
			output.WriteLine();
			int numGameAccounts=accounts.Count;
			foreach (GameAccount acc in accounts.Values) {
				acc.Save(output);
				output.WriteLine();
				ObjectSaver.FlushCache(output);
			}
			output.WriteLine("[EOF]");
			Logger.WriteDebug("Saved "+numGameAccounts+" accounts.");
		}

		public override void Save(SaveStream output) {
			output.WriteLine("["+name+"]");
			//output.WriteValue("uid",uid);
			if (this.passwordHash!=null) {
				string hash=EncodeHashToString(this.passwordHash);
				output.WriteValue("passwordHash", hash);
			} else {
				output.WriteValue("password", this.password);
			}
			if (maxPlevel!=1) {
				output.WriteValue("plevel", plevel);
				output.WriteValue("maxplevel", maxPlevel);
			}
			if (blocked) {
				output.WriteValue("blocked", this.blocked);
			}
			CheckReferences();
			for (int i=0; i<maxCharactersPerGameAccount; i++) {
				if (this.characters[i]!=null) {
					//output.WriteValue("CharUID["+i+"]", this.characters[i]);
					output.WriteComment("CharUID["+i+"]=#"+this.characters[i].Uid);
				}
			}
			base.Save(output);//base save - TagHolder
		}

		//internal void CharLoad_Delayed(object resolvedChar, string filename, int line, object index) {
		//    int i = (int) index;
		//    AbstractCharacter ch = resolvedChar as AbstractCharacter;
		//    this.characters[i] = ch;
		//    if (ch == null) {
		//        Logger.WriteError("Failed resolving char at index "+LogStr.Number(index)+" for account '"+LogStr.Ident(name)+"'. Resolved "+LogStr.Ident(resolvedChar)+" instead.");
		//    //} else {
		//    //	if (ch.Account != this) {
		//    //		Logger.WriteError("The Character"+LogStr.Ident(ch)+" should belong to this account ("+LogStr.Ident(this)+"), but does not. Re-adding.");
		//    //		ch.MakeBePlayer(this);
		//    //	}
		//    }
		//}

		//If both password and passwordHash become set, then passwordHash takes
		//precedence. This should not happen, but this is here as an additional
		//security measure, in case someone manages to somehow set the string
		//password despite there being a hashed password already.
		private bool TestPassword(string pass) {
			//Preconditions
			Sanity.IfTrueThrow((this.passwordHash!=null && this.password!=null), "GameAccount ["+name+"]: Has both a password and hashed password.");
			Sanity.IfTrueThrow((this.passwordHash==null && this.password==null), "GameAccount ["+name+"]: Has neither a password nor hashed password.");

			if (this.passwordHash!=null) {
				if (TestHash(this.passwordHash, HashPassword(pass))) {
					if (!Globals.hashPasswords) {
						//record the password string and get rid of the hash now that we know what the password is again
						this.password=pass;
						this.passwordHash=null;
					}
					return true;
				} else {
					return false;
				}
			} else {
				//We should only get here if we're not using hashed passwords, but let's check anyways.
				if (!Globals.hashPasswords) {
					if (this.password!=null) {
						if (this.password==pass) {
							return true;
						} else {
							return false;
						}
					}
				} else {	//Eh, convert the account's password to a hash and THEN compare.
					this.passwordHash=HashPassword(this.password);
					this.password=null;

					if (TestHash(this.passwordHash, HashPassword(pass))) {
						return true;
					} else {
						return false;
					}
				}
			}
			return false;
		}

		private static Byte[] DecodeEncodedHash(string encodedHash) {
			//decode it back to the hash
			return Convert.FromBase64String(encodedHash);
		}

		private static string EncodeHashToString(Byte[] decodedHash) {
			//encode it into a string which we can write to a text save if necessary
			return Convert.ToBase64String(decodedHash);
		}

		private static Byte[] HashPassword(string password) {
			//use SHA512 to hash the password.
			Byte[] passBytes=Encoding.BigEndianUnicode.GetBytes(password);
			SHA512Managed sha = new SHA512Managed();
			Byte[] hash=sha.ComputeHash(passBytes);
			sha.Clear();
			return hash;
		}

		private static bool TestHash(Byte[] original, Byte[] test) {
			if (original.Length==test.Length) {
				for (int a=0; a<original.Length; a++) {
					if (original[a]!=test[a]) return false;
				}
			}
			return true;
		}

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();
		public static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		public override void Trigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				registeredTGs[i].Run(this, td, sa);
			}
			base.TryTrigger(td, sa);
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				registeredTGs[i].TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
		}

		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				object retVal = registeredTGs[i].Run(this, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return base.TryCancellableTrigger(td, sa);
		}

		public override bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				object retVal = registeredTGs[i].TryRun(this, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return base.TryCancellableTrigger(td, sa);
		}
	}
}