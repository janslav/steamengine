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
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Timers;

namespace SteamEngine.Persistence {
	
	public delegate void LoadObject(object resolvedObject, string filename, int line);
	public delegate void LoadObjectParam(object resolvedObject, string filename, int line, object additionalParameter);

	[Summary("Use this interface to implement saving and loading of custom object types.")]
	[Remark("Note that once you write a class implementing this interface in the scripts, it is supposed to be"
	+ "automatically registered with the ObjectSaver class. "
	+ "Therefore, it needs to have a public constructor with no parameters so that the core ClassManager class can instantiate it.")]
	public interface ISaveImplementor {
		[Summary("Write out the section from which this object can be loaded later.")]
		[Remark("First, note that you actually implement only saving of the \"body\" of the section, "
		+ "not it's header, because that is reserved for use by the ObjectSaver class."
		+ "Also note that there are certain format restrictions you should (or must) follow. "
		+ "For example, the obvious one is that you can't use the format of the header ;)"
		+ "Basically, you should follow the \"name = value\" way of saving.")]
		[Param(0, "The object to be saved")]
		[Param(1, "The text stream for the save to be written to. "
		+ "Note that this may not be immediately written to a file. (Or, in some extreme case, maybe not saved at all)")]
		void Save(object objToSave, SaveStream writer);
		
		
		[Summary("Returns the .NET Type of the object that it can load and save.")]
		[Remark("The returned type is used when determining which implementor to use when saving"
		+ "individual objects. Therefore, there can be only one implementor for loading of one class, "
		+ "and this class can not be auto-loadable (by decorating it by SaveableClassAttribute,etc.), "
		+ "because for those classes, ISaveImplementor instances are created, too.")]
		Type HandledType { get; }
		
		
		[Summary("Load the object from the previously saved section.")]
		[Return("The loaded object")]
		[ExceptionDoc(typeof(UnrecognizedValueException), "if the format of the string is not recognized.")]
		[ExceptionDoc(typeof(InsufficientDataException), "if this section can not be resolved to the desired object. ")]
		object LoadSection(PropsSection input);
		
		[Summary("Get the string that identifies this implementor.")]
		[Remark("The returned string is used in the header of the saved sections. When the sections are loaded,"
		+ "we know that they should be passed to the \"LoadSection\" method of this instance. "
		+ "Note though that this string must be unique among all the other header names that can emerge"
		+ "in the saves.")]
		string HeaderName { get; }
	}

	[Summary("Use this interface to coordinate saving of a isntances of a class hierarchy with given base class.")]
	[Remark("Using this interface, the individual subclasses are still supposd to have their own ISaveImplementor.")]
	public interface IBaseClassSaveCoordinator {
		[Summary("File name without extension to which these instances will be saved.")]
		string FileNameToSave { get; }

		[Summary("Is called on the start of loading process")]
		void StartingLoading();

		[Summary("Implementation of the save of all instances. The SaveStream is created on the file of the name given by FileNameToSave property")]
		void SaveAll(SaveStream writer);

		[Summary("Is called on the end of loading process")]
		void LoadingFinished();

		[Summary("Base class of the hierarchy tree that is handled by this object.")]
		Type BaseType { get; }

		[Summary("Returns a line that it can later recognize as a one-line-reference to the instance, and which it will be able to delayed-load.")]
		[Remark("The line needs to have an unique format that can be recognied by the regex returned by ReferenceLineRecognizer property")]
		string GetReferenceLine(object value);

		[Summary("Regex that recognizes our.")]
		Regex ReferenceLineRecognizer { get; }

		[Summary("This will be typically called on the end of the loading process, and is supposed to return the loaded object by the Match of the recognizer regex")]
		object Load(Match m);
	}
	
	[Summary("Use this interface to implement saving and loading of simple custom object types.")]
	[Remark("Note that once you write a class implementing this interface in the scripts, it is supposed to be"
	+ "automatically registered with the ObjectSaver class. "
	+ "Therefore, it needs to have a public constructor with no parameters so that the core ClassManager class can instantiate it.")]
	public interface ISimpleSaveImplementor {
		[Summary("Returns the .NET Type of the object that it can load and save.")]
		[Remark("The returned type is used when determining which implementor to use when saving"
		+ "individual objects. Therefore, there can be only one implementor for loading of one class, "
		+ "and this class can not auto-loadable (by decorating it by SaveableClassAttribute,etc.), "
		+ "because for those classes, ISaveImplementor instances are created, too.")]
		Type HandledType { get; }
		
		
		[Summary("Get the regular expression object to match the saved strings.")]
		[Remark("The returned regex object will be used to match the saved strings. It is supposed "
		+ "to be unique among the format of other saved types, and the produced Match object must be recognisable"
		+ "by your Load method on this instance.")]
		Regex LineRecognizer { get; }
		
		[Summary("Return the one-line string, from which this object can be loaded later.")]
		[Remark("The returned string must really be one-line, and it must be compatible with the regex"
		+ "returned by the LineRecognizer property. Note that if called by the ObjectSaver class, "
		+ "the passed argument will be of the appropriate type (the one returned by HandledType property).")]
		[Param(0, "The object to be saved")]
		[Return("The the one-line string to be written in the save file.")]
		string Save(object objToSave);
		
		[Summary("Load the object from the previously saved line.")]
		[Param(0, "The Match object produced by the Regex of this instance. "
		+ "It is passed here only after a succesful match.")]
		[Return("The loaded object")]
		[ExceptionDoc(typeof(UnrecognizedValueException), "if the format of the string is not recognized.")]
		[ExceptionDoc(typeof(InsufficientDataException), "if this string can not be resolved to the desired object. "
		+ "That most probably means that the string is just a reference to something that has "
		+ "not yet been loaded in this loading session. In such cases, we suggest using one of "
		+ "the Load methods that support delayed loading.")]
		object Load(Match match);

		[Summary("Return the prefix used to identify this item type in the save file")]
		[Remark("The returned value is e.g. (4D) for point4D, (IP) for IP address etc. We will use it"+
				"in settings dialogs for better writing out the values (without these texts)")]		
		string Prefix { get; }
	}
	
	[Summary("Class used to load and save (serialize and deserialize) various non-standard objects to savefiles.")]
	[Remark("There are basically three ways to implement the saving and loading of your custom (scripted) classes."
	+ "One is writing a helper class implementing the ISimpleSaveImplementor, second to write ISaveImplementor. "
	+ "The third way is to decorate your class with certain attributes. "
	+ "Basically, you will use the ISaveImplementor only for classes which you can not "
	+ "change, a.e. mostly the classes from .NET standard library, and that is because you have to write the "
	+ "saving and loading of all the data by hand. Using the Attributes, on the other hand, you can choose the rate"
	+ "of automatisation and own code that best suits your needs. Note that this class is NOT supposed to be used"
	+ "For loading of core-saveable objects, such as Things, ThinDefs, Regions, Timers, etc. "
	+ "On the other hand, it can and should be used to everything that can be written on one line, such as references "
	+ "to scripted objects (like TriggerGroups) or simple wrappers (TriggerKey, TimerKey, ...)."
	+ "ISimpleSaveImplementor will mostly be used for very simple classes that can be saved on one line, like valuetypes.")]
	[SeeAlso(typeof(ISaveImplementor))][SeeAlso(typeof(SaveableClassAttribute))]
	[SeeAlso(typeof(ISimpleSaveImplementor))]
	public static partial class ObjectSaver {
		public static readonly Regex abstractScriptRE = new Regex(@"^\s*#(?<value>[a-z_][a-z0-9_]+)\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static readonly Regex genericUidRE = new Regex(@"^\s*\((?<name>.*)\s*\)\s*(?<uid>\d+)\s*$",
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		//public static Regex dateTimeRE = new Regex(@"^\s*\((?<name>.*)\s*\)\s*(?<uid>\d+)\s*$",
			//RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		
		private static DelayedLoader loadersList; //this is the list of the pending jobs :)
		private static DelayedLoader lastLoader;  //it is emptied when LoadingFinished is called
		private static ArrayList loadedObjectsByUid;
			
		private static DelayedSaver saversList;//this is the cache to flush by FlushCache :)
		private static DelayedSaver lastSaver;
		private static Dictionary<object, uint> savedUidsByObjects = new Dictionary<object,uint>(new ReferenceEqualityComparer());
		//object-uint pairs. uses Object.ReferenceEquality for finding out if the objects are or are not equal.
		
		private static Dictionary<string, ISaveImplementor> implementorsByName = new Dictionary<string,ISaveImplementor>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<Type, ISaveImplementor> implementorsByType = new Dictionary<Type, ISaveImplementor>();
		private static Dictionary<Type, IBaseClassSaveCoordinator> coordinatorsByType = new Dictionary<Type, IBaseClassSaveCoordinator>();
		private static Dictionary<ISaveImplementor, IBaseClassSaveCoordinator> coordinatorsByImplementor = new Dictionary<ISaveImplementor, IBaseClassSaveCoordinator>();
		private static List<RGBCSCPair> coordinatorsRGs = new List<RGBCSCPair>();

		private static List<RGSSIPair> simpleImplementorsRGs = new List<RGSSIPair>();
		//regex-ISimpleSaveImplementor pairs (RGSSIPair instances)
		private static Dictionary<Type, ISimpleSaveImplementor> simpleImplementorsByType = new Dictionary<Type, ISimpleSaveImplementor>();
		//Type-ISimpleSaveImplementor pairs		

		private static uint uids = 0;
		
		[Summary("Method for generic saving of various objects.")]
		[Remark("There are two possible outcomes of this method."
		+ "Either, if the object being saved is trivial enough (like a known value type),"
		+ "the returned string will contain the entire needed information to load it later."
		+ "Or, if it is a reference type, like a Character or an ArrayList for example,"
		+ "it will return a string which will be then, later when it's loaded again, identified by this class"
		+ "as a reference to an item which will be in fact saved elsewhere.")]
		[Param(0, "The object to be saved")]
		[ExceptionDoc(typeof(UnsaveableTypeException), "if we do not know how to save this object.")]
		[Return("A single-line string to be written in the save file.")]
		public static string Save(object value) {
			if (value == null) {
				return "null";
			}
			IDeletable asDeletable = value as IDeletable;
			if (asDeletable != null) {
				if (asDeletable.IsDeleted) {
					return "null";
				}
			}

			Type t=value.GetType();

			if (t.IsEnum) {
				return Convert.ToUInt64(value).ToString();
			} else if (TagMath.IsNumberType(t)) {
				return value.ToString();
			} else if (t.Equals(typeof(String))) {
				//TODO: multiline strings
				string stringAsSingleLine = Utility.EscapeNewlines((string) value);
				return "\""+stringAsSingleLine+"\""; //returns the string in ""
			} else if (typeof(AbstractScript).IsAssignableFrom(t)) {
				return "#"+((AbstractScript) value).PrettyDefname; //things have #1234, abstractScripts have #name
			} else if (value == Globals.instance) {
				return "#globals";
			} else {
				ISimpleSaveImplementor iss;
				if (simpleImplementorsByType.TryGetValue(t, out iss)) {
					return iss.Save(value);
				}

				if (t.IsArray) {
					t = typeof(Array);
				} else if (t.IsGenericType) {
					t = t.GetGenericTypeDefinition();
				}

				ISaveImplementor isi;
				if (implementorsByType.TryGetValue(t, out isi)) {
					IBaseClassSaveCoordinator coordinator;
					if (coordinatorsByImplementor.TryGetValue(isi, out coordinator)) {
						//PushDelayedSaver(new DelayedSaver(value, isi));
						return coordinator.GetReferenceLine(value);
					} else {
						uint uid;
						if (savedUidsByObjects.TryGetValue(value, out uid)) {
							uid = savedUidsByObjects[value];
						} else {
							uid = uids++;
							savedUidsByObjects[value] = uid;
							PushDelayedSaver(new GenericDelayedSaver(value, isi, uid));
						}
						return string.Concat("(", isi.HeaderName, ")", uid);
					}
				}

				throw new UnsaveableTypeException("The object is of an unsaveable type "+t);
			}
		}
		
		[Summary("Writes out the object previously cached by the Save method.")]
		[Remark("If any of the previous calls to the Save method required to save some more complex "
		+ "object,only a string-reference to them has been returned by it, and they were cached here. "
		+ "Now they will be written to the supplied SaveStream, each in it's own proper section, etc. "
		+ "Note that objects that are supposed to be written by other mechanisms, such as Things, will not be written out here.")]
		[Param(0, "The SaveStream instance to flush the cached object to.")]
		public static void FlushCache(SaveStream writer) {
			while (SaverListIsNotEmpty) {
				try {
					PopDelayedSaver().Run(writer);
					
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(e);
				}
				writer.WriteLine();
			}
		}

		[Summary("Find out if the specified type is known to be saveable by the ObjectSaver class.")]
		public static bool IsSaveableType(Type t) {
			if (TagMath.IsNumberType(t)) {
			} else if (t.Equals(typeof(String))) {
			} else if (typeof(AbstractScript).IsAssignableFrom(t)) {
			} else if (typeof(Globals).IsAssignableFrom(t)) {
			} else if (simpleImplementorsByType.ContainsKey(t)) {
			} else if (implementorsByType.ContainsKey(t)) {//should we somehow also support type hierarchy?
			} else if ((t.IsArray) && implementorsByType.ContainsKey(typeof(Array))) {
			} else {
				return false;
			}
			return true;
		}
		
		public static bool IsSimpleSaveableType(Type t) {
			if (TagMath.IsNumberType(t)) {
			} else if (t.Equals(typeof(String))) {
			} else if (typeof(Globals).IsAssignableFrom(t)) {
			} else if (simpleImplementorsByType.ContainsKey(t)) {
			} else {
				return false;
			}
			return true;
		}

		public static bool IsKnownSectionName(string name) {
			return implementorsByName.ContainsKey(name);
		}
		
		[Summary("Load the one-line string previously returned by the Save method.")]
		[Remark("This is the simplest method for object loading. Use it only if you know that the "
		+ "object saved on the given line was simple enough to be saved just in this line, "
		+ "a.e. that it was not written by the FlushCache method.")]
		[ExceptionDoc(typeof(UnrecognizedValueException), "if the format of the string is not recognized.")]
		[ExceptionDoc(typeof(InsufficientDataException), "if this string can not be resolved to the desired object. "
		+ "That most probably means that the string is just a reference to something that has "
		+ "not yet been loaded in this loading session. In such cases, we suggest using one of "
		+ "the Load methods that support delayed loading.")]
		[Return("The object loaded from the string.")]
		[Param(0, "The string to load the object from.")]
		public static object Load(string input) {
			object retVal;
			if (LoadSimple(input, out retVal)) {
				return retVal;
			}

			Match m;
			for (int i = 0, n = coordinatorsRGs.Count; i<n; i++) {
				RGBCSCPair pair = coordinatorsRGs[i];
				m = pair.re.Match(input);
				if (m.Success) {
					return pair.bcsc.Load(m);
				}
			}
			m = genericUidRE.Match(input);
			if (m.Success) {
				int uid = ConvertTools.ParseInt32(m.Groups["uid"].Value);//should we also check something using the "name" part?
				int loadedObjectsCount = loadedObjectsByUid.Count;
				if (uid < loadedObjectsCount) {
					object o = loadedObjectsByUid[uid];
					if (o != typeof(void)) {
						return o;
					}
				}
				throw new InsufficientDataException("The object with uid "+LogStr.Number(uid)+" is not (yet?) known.");
			}
			throw new UnrecognizedValueException("We really do not know what could the loaded string '"+LogStr.Ident(input)+"' refer to.");
		}
		
		private static bool LoadSimple(string input, out object retVal) {
			retVal = null;

			if (TryLoadScriptReference(input, ref retVal)) {
				return true;
			}
			if (TryLoadString(input, ref retVal)) {
				return true;
			}
			if (ConvertTools.TryParseAnyNumber(input, out retVal)) {
				return true;
			}

			if (string.Compare(input, "true", true)==0) {//true: ignore case
				retVal = true;
				return true;
			}
			if (string.Compare(input, "false", true)==0) {
				retVal = false;
				return true;
			}

			if (string.Compare(input, "null", true)==0) {
				retVal = null;
				return true;
			}
			for (int i = 0, n = simpleImplementorsRGs.Count; i<n; i++) {
				RGSSIPair pair = simpleImplementorsRGs[i];
				Match m = pair.re.Match(input);
				if (m.Success) {
					retVal = pair.ssi.Load(m);
					return true;
				}
			}
			return false;
		}

		private static bool TryLoadScriptReference(string input, ref object retVal) {
			Match m = abstractScriptRE.Match(input);
			if (m.Success) {
				string defname = m.Groups["value"].Value;
				if (string.Compare("globals", defname, true) == 0) {
					retVal = Globals.instance;
					return true;
				}

				AbstractScript script = AbstractScript.Get(defname);
				if (script != null) {
					retVal = script;
					return true;
				} else {
					throw new InsufficientDataException("The AbstractScript '"+LogStr.Ident(defname)+"' is not known.");
				}
			}
			return false;
		}

		private static bool TryLoadString(string input, ref object retVal) {
			Match m = ConvertTools.stringRE.Match(input);
			if (m.Success) {
				string stringAsSingleLine = m.Groups["value"].Value;
				retVal = Utility.UnescapeNewlines(stringAsSingleLine);
				return true;
			}
			return false;
		}
		
		[Summary("Load the one-line string previously returned by the Save method.")]
		[Remark("This is the method for delayed object loading. It will recognize the supplied string, "
		+ "and return the object that is associated with it as soon as possible. "
		+ "However, it will not return it directly, it will use the supplied callback delegate instead.")]
		[Param(0, "The string to load the object from.")]
		[Param(1, "The callback delegate to call when the object is loaded.")]
		[ExceptionDoc(typeof(UnrecognizedValueException), "if the format of the string is not recognized.")]
		public static void Load(string input, LoadObject deleg, string filename, int line) {
			object retVal;
			if (LoadSimple(input, out retVal)) {
				deleg(retVal, filename, line);
				return;
			}

			Match m;
			for (int i = 0, n = coordinatorsRGs.Count; i<n; i++) {
				RGBCSCPair pair = coordinatorsRGs[i];
				m = pair.re.Match(input);
				if (m.Success) {
					PushDelayedLoader(new BaseClassDelayedLoader_NoParam(
						deleg, filename, line, m, pair.bcsc));
				}
			}

			m = genericUidRE.Match(input);
			if (m.Success) {
				int uid = ConvertTools.ParseInt32(m.Groups["uid"].Value);//should we also check something using the "name" part?
				PushDelayedLoader(new GenericDelayedLoader_NoParam(deleg, filename, line, uid));
				return;
			}
		}
		
		[Summary("Load the one-line string previously returned by the Save method.")]
		[Remark("This method works similarly as the \"public static void Load(string value, LoadObject deleg)\", "
		+ "only it allows you to pass additional parameter to the callback delegate.")]
		[Param(0, "The string to load the object from.")]
		[Param(1, "The callback delegate to call when the object is loaded.")]
		[ExceptionDoc(typeof(UnrecognizedValueException), "if the format of the string is not recognized.")]
		public static void Load(string input, LoadObjectParam deleg, string filename, int line, object additionalParameter) {
			object retVal;
			if (LoadSimple(input, out retVal)) {
				deleg(retVal, filename, line, additionalParameter);
				return;
			}
			
			Match m;
			for (int i = 0, n = coordinatorsRGs.Count; i<n; i++) {
				RGBCSCPair pair = coordinatorsRGs[i];
				m = pair.re.Match(input);
				if (m.Success) {
					PushDelayedLoader(new BaseClassDelayedLoader_Param(
						deleg, filename, line, additionalParameter, m, pair.bcsc));
					return;
				}
			}

			m = genericUidRE.Match(input);
			if (m.Success) {
				uint uid = uint.Parse(m.Groups["uid"].Value);//should we also check something using the "name" part?
				PushDelayedLoader(new GenericDelayedLoader_Param(deleg, filename, line, additionalParameter, uid));
				return;
			}
			throw new UnrecognizedValueException("We really do not know what could the loaded string '"+LogStr.Ident(input)+"' refer to.");
		}

		[Summary("Use this if you know that the loaded value is supposed to be a string. "
		+"If it's not, this method will try to load it anyway, by calling the standard Load(string) method.")]
		public static object OptimizedLoad_String(string input) {
			object retVal = null;
			if (TryLoadString(input, ref retVal)) {
				return retVal;
			}
			return Load(input);//we failed to load string, lets try all other possibilities
		}

		[Summary("Use this if you know that the loaded value is supposed to be an AbstractScript instance. "
		+"If it's not, this method will try to load it anyway, by calling the standard Load(string) method.")]
		public static object OptimizedLoad_Script(string input) {
			object retVal = null;
			if (TryLoadScriptReference(input, ref retVal)) {
				return retVal;
			}
			return Load(input);//we failed to load string, lets try all other possibilities
		}

		[Summary("Use this if you know that the loaded value is supposed to be of a simple-saveable type (other than a number or enum). "
		+"If it's not, this method will try to load it anyway, by calling the standard Load(string) method.")]
		public static object OptimizedLoad_SimpleType(string input, Type suggestedType) {
			ISimpleSaveImplementor issi;
			if (simpleImplementorsByType.TryGetValue(suggestedType, out issi)) {
				Match m = issi.LineRecognizer.Match(input);
				if (m.Success) {
					return issi.Load(m);
				}
			}
			return Load(input);//we failed to load the type, lets try all other possibilities
		}

		//or should this be public?
		//or should this all not be public at all? :)
		//in other words: do scripters need enabling saving/loading other than normal core worldsaving&loading?
		//I am not sure what all would be needed to do that...
		internal static void LoadSection(PropsSection input) {
			string name = input.headerType;

			ISaveImplementor isi;
			if (implementorsByName.TryGetValue(name, out isi)) {
				try {
					IBaseClassSaveCoordinator coordinator;
					if (coordinatorsByImplementor.TryGetValue(isi, out coordinator)) {
						isi.LoadSection(input);
					} else {
						uint uid = uint.Parse(input.headerName);
						//[name uid]
						object loaded = isi.LoadSection(input);
						while (loadedObjectsByUid.Count <= uid) {
							loadedObjectsByUid.Add(typeof(void));//so that we know what was already loaded and what not.
						}
						loadedObjectsByUid[(int) uid] = loaded;//should we check if there was something already...?
					}
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(input.filename, input.headerLine, e);
				}
			} else {
				throw new UnrecognizedValueException("We really do not know what could the loaded header name '"+LogStr.Ident(name)+"' refer to.");
			}
		}

		//called by ClassManager
		internal static void RegisterCoordinator(IBaseClassSaveCoordinator coordinator) {
			Type type = coordinator.BaseType;
			if (coordinatorsByType.ContainsKey(type)) {
				throw new OverrideNotAllowedException("There is already a IBaseClassSaveCoordinator ("+implementorsByType[type]+") registered for handling the type "+type);
			}
			coordinatorsByType[type] = coordinator;
			coordinatorsRGs.Add(new RGBCSCPair(coordinator, coordinator.ReferenceLineRecognizer));

			foreach (KeyValuePair<Type, ISaveImplementor> pair in implementorsByType) {
				if (type.IsAssignableFrom(pair.Key)) {//one of our implementors
					IBaseClassSaveCoordinator ibcsc;
					if (coordinatorsByImplementor.TryGetValue(pair.Value, out ibcsc)) {
						if (ibcsc != coordinator) {
							throw new Exception("ISaveImplementor "+pair.Value+" is supposedly handled by two IBaseClassSaveCoordinators: '"+ibcsc+"' and '"+coordinator+"'. This should not happen.");
						} else {
							continue;
						}
					}
					coordinatorsByImplementor[pair.Value] = coordinator;
				}
			}
		}

		public static IEnumerable<IBaseClassSaveCoordinator> AllCoordinators {
			get {
				foreach (RGBCSCPair pair in coordinatorsRGs) {
					yield return pair.bcsc;
				}
			}
		}
		
		//called by ClassManager
		internal static void RegisterImplementor(ISaveImplementor implementor) {
			Type type = implementor.HandledType;
			if (implementorsByType.ContainsKey(type)) {
				throw new OverrideNotAllowedException("There is already a ISaveImplementor ("+implementorsByType[type]+") registered for handling the type "+type);  
			}
			implementorsByType[type] = implementor;
			string name = implementor.HeaderName;
			implementorsByName[name] = implementor;
			foreach (KeyValuePair<Type,IBaseClassSaveCoordinator> pair in coordinatorsByType) {
				if (pair.Key.IsAssignableFrom(type)) {
					IBaseClassSaveCoordinator ibcsc;
					if (coordinatorsByImplementor.TryGetValue(implementor, out ibcsc)) {
						if (ibcsc != pair.Value) {
							throw new Exception("ISaveImplementor "+implementor+" is supposedly handled by two IBaseClassSaveCoordinators: '"+ibcsc+"' and '"+pair.Value+"'. This should not happen.");
						} else {
							continue;
						}
					}
					coordinatorsByImplementor[implementor] = pair.Value;
				}
			}
		}
		
		//called by ClassManager
		internal static void RegisterSimpleImplementor(ISimpleSaveImplementor implementor) {
			Type type = implementor.HandledType;
			if (simpleImplementorsByType.ContainsKey(type)) {
				throw new OverrideNotAllowedException("There is already a ISimpleSaveImplementor ("+simpleImplementorsByType[type]+") registered for handling the type "+type);  
			}
			simpleImplementorsByType[type] = implementor;
			simpleImplementorsRGs.Add(new RGSSIPair(implementor, implementor.LineRecognizer));
		}
		
		[Summary("Call this before you start using this class for loading.")]
		public static void StartingLoading() {
			loadedObjectsByUid = new ArrayList();
			foreach (RGBCSCPair pair in coordinatorsRGs) {
				pair.bcsc.StartingLoading();
			}
		}
		
		[Summary("Call this after you finish loading using this class.")]
		public static void LoadingFinished() {
			while (LoaderListIsNotEmpty) {
				DelayedLoader loader = null;
				try {
					loader = PopDelayedLoader();
					loader.Run();
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(loader.filename, loader.line, e);
				}
			}
			loadedObjectsByUid = null;

			foreach (RGBCSCPair pair in coordinatorsRGs) {
				pair.bcsc.LoadingFinished();
			}
		}
		
		[Summary("Call this before you start using this class for saving.")]
		public static void StartingSaving() {
			saversList = null;
			lastSaver = null;
			savedUidsByObjects.Clear();
			uids = 0;
		}
		
		[Summary("Call this after you finish saving using this class.")]
		public static void SavingFinished() {

		}
		
		//unloads instences that come from scripts.
		internal static void UnloadScripts() {
			Assembly coreAssembly = ClassManager.CoreAssembly;
			List<ISaveImplementor> isis = new List<ISaveImplementor>(implementorsByName.Values);
			implementorsByName.Clear();
			implementorsByType.Clear();
			foreach (ISaveImplementor isi in isis) {
				if (coreAssembly == isi.GetType().Assembly) {
					RegisterImplementor(isi);
				}
			}

			List<ISimpleSaveImplementor> issis = new List<ISimpleSaveImplementor>(simpleImplementorsByType.Values);
			simpleImplementorsRGs.Clear();
			simpleImplementorsByType.Clear();
			foreach (ISimpleSaveImplementor issi in issis) {
				if (coreAssembly == issi.GetType().Assembly) {
					RegisterSimpleImplementor(issi);
				}
			}

			List<IBaseClassSaveCoordinator> ibcscs = new List<IBaseClassSaveCoordinator>(coordinatorsByType.Values);
			coordinatorsRGs.Clear();
			coordinatorsByImplementor.Clear();
			coordinatorsByType.Clear();
			foreach (IBaseClassSaveCoordinator ibcsc in ibcscs) {
				if (coreAssembly == ibcsc.GetType().Assembly) {
					RegisterCoordinator(ibcsc);
				}
			}
		}
		
		internal static void ClearJobs() {
			loadersList = null;
			lastLoader = null;
			loadedObjectsByUid = null;
			
			saversList = null;
			lastSaver = null;
			savedUidsByObjects.Clear();
		}
		
		private static bool LoaderListIsNotEmpty { get {
			return loadersList != null;
		} }
		
		private static void PushDelayedLoader(DelayedLoader dl) {
			if (loadersList == null) {
				loadersList = dl;
			}
			if (lastLoader != null) {
				lastLoader.next = dl;
			}
			lastLoader = dl;
		}
		
		private static DelayedLoader PopDelayedLoader() {
			DelayedLoader dl = loadersList;
			loadersList = loadersList.next;//throws nullpointerexc...
			return dl;
		}
		
		private static bool SaverListIsNotEmpty { get {
			return saversList != null;
		} }
		
		private static void PushDelayedSaver(DelayedSaver ds) {
			if (saversList == null) {
				saversList = ds;
			}
			if (lastSaver != null) {
				lastSaver.next = ds;
			}
			lastSaver = ds;
		}
		
		private static DelayedSaver PopDelayedSaver() {
			DelayedSaver ds = saversList;
			saversList = saversList.next;//throws nullpointerexc...
			return ds;
		}

		[Remark("Forwards call for finding a SimpleSaveImplementor for given Type."+
				"Returns the ISimpleSaveImplementor instance or null if nothing was found")]
		public static ISimpleSaveImplementor GetSimpleSaveImplementorByType(Type t) {
			ISimpleSaveImplementor retVal = null;
			simpleImplementorsByType.TryGetValue(t, out retVal);
			return retVal;
		}



//embedded classes:

		private class DelayedSaver {
			internal DelayedSaver next;//instances will be all stored in a linked list
			internal object objToSave;
			internal ISaveImplementor implementor;

			internal DelayedSaver(object objToSave, ISaveImplementor implementor) {
				this.objToSave = objToSave;
				this.implementor = implementor;
			}
			
			internal virtual void Run(SaveStream writer) {
				implementor.Save(objToSave, writer);
			}
		}

		private class GenericDelayedSaver : DelayedSaver {
			internal uint uid;

			internal GenericDelayedSaver(object objToSave, ISaveImplementor implementor, uint uid)
					: base(objToSave, implementor) {
				this.uid = uid;
			}
			
			internal override void Run(SaveStream writer) {
				writer.WriteSection(implementor.HeaderName, uid.ToString());
				base.Run(writer);
			}
		}

		
		
		private abstract class DelayedLoader {
			internal DelayedLoader next;//instances will be all stored in a linked list
			internal string filename;
			internal int line;

			internal DelayedLoader(string filename, int line) {
				this.filename = filename;
				this.line = line;
			}

			internal abstract void Run();
		}
		
		private abstract class DelayedLoader_NoParam : DelayedLoader {
			protected LoadObject deleg;
			
			internal DelayedLoader_NoParam(LoadObject deleg, string filename, int line) : base(filename, line) {
				this.deleg = deleg;
			}
		}
		
		private abstract class DelayedLoader_Param : DelayedLoader {
			protected LoadObjectParam deleg;
			protected object param;

			internal DelayedLoader_Param(LoadObjectParam deleg, string filename, int line, object param)
					: base(filename, line) {
				this.deleg = deleg;
				this.param = param;
			}
		}
		
		private class BaseClassDelayedLoader_NoParam : DelayedLoader_NoParam {
			Match m;
			IBaseClassSaveCoordinator coordinator;

			internal BaseClassDelayedLoader_NoParam(LoadObject deleg, string filename, int line, Match m, IBaseClassSaveCoordinator coordinator)
					: base(deleg, filename, line) {
				this.m = m;
				this.coordinator = coordinator;
			}
			
			internal override void Run() {
				object o = coordinator.Load(m);
				deleg(o, filename, line);
			}
		}
		
		private class BaseClassDelayedLoader_Param : DelayedLoader_Param {
			Match m;
			IBaseClassSaveCoordinator coordinator;

			internal BaseClassDelayedLoader_Param(LoadObjectParam deleg, string filename, int line, object param, Match m, IBaseClassSaveCoordinator coordinator)
					: base(deleg, filename, line, param) {
				this.m = m;
				this.coordinator = coordinator;
			}
			
			internal override void Run() {
				object o = coordinator.Load(m);
				deleg(o, filename, line, param);
			}
		}
		
		private class GenericDelayedLoader_NoParam : DelayedLoader_NoParam {
			int objectUid;
			internal GenericDelayedLoader_NoParam(LoadObject deleg, string filename, int line, int objectUid)
					: base(deleg, filename, line) {
				this.objectUid = objectUid;
			}
			
			internal override void Run() {
				int loadedObjectsCount = loadedObjectsByUid.Count;
				if (objectUid < loadedObjectsCount) {
					object o = loadedObjectsByUid[objectUid];
					if (o != typeof(void)) {
						deleg(o, filename, line);
						return;
					}
				}
				throw new NonExistingObjectException("There is no object with uid "+LogStr.Number(objectUid)+" to load.");
			}
		}
		
		private class GenericDelayedLoader_Param : DelayedLoader_Param {
			uint objectUid;
			internal GenericDelayedLoader_Param(LoadObjectParam deleg, string filename, int line, object param, uint objectUid)
					: base(deleg, filename, line, param) {
				this.objectUid = objectUid;
			}
			
			internal override void Run() {
				int loadedObjectsCount = loadedObjectsByUid.Count;
				if (objectUid < loadedObjectsCount) {
					object o = loadedObjectsByUid[(int) objectUid];
					if (o != typeof(void)) {
						deleg(o, filename, line, param);
						return;
					}
				}
				throw new NonExistingObjectException("There is no object with uid "+LogStr.Number(objectUid)+" to load.");
			}
		}
		
		private struct RGSSIPair {
			internal ISimpleSaveImplementor ssi;
			internal Regex re;
			
			internal RGSSIPair(ISimpleSaveImplementor ssi, Regex re) {
				this.ssi = ssi;
				this.re = re;
			}
		}

		private struct RGBCSCPair {
			internal IBaseClassSaveCoordinator bcsc;
			internal Regex re;

			internal RGBCSCPair(IBaseClassSaveCoordinator bcsc, Regex re) {
				this.bcsc = bcsc;
				this.re = re;
			}
		}

		internal class ReferenceEqualityComparer : IEqualityComparer<object> {
			public new bool Equals(object x, object y) {
				return object.ReferenceEquals(x, y);
			}

			public int GetHashCode(object obj) {
				return obj.GetHashCode();
			}
		}
	}

	public class SaveStream {
		TextWriter writer; 
		
		public SaveStream(TextWriter writer) {
			this.writer = writer;
		}
		
		public void WriteLine(string value) {
			writer.WriteLine(value);
		}	
		
		public void WriteLine() {
			writer.WriteLine();
		}
		
		public void Close() {
			try {
				writer.Flush();
			} catch { }
			try {
				writer.Close();
			} catch { }
			writer = null;
		}
		
		public void WriteValue(string name, object value) {
			//TagMath.SaveValue(writer, name+"=", value, -1);
			writer.Write(name);
			writer.Write("=");
			writer.WriteLine(ObjectSaver.Save(value));
		}
		
		public void WriteSection(string type, string name) {
			writer.Write("[");
			writer.Write(type);
			writer.Write(" ");
			writer.Write(name);
			writer.WriteLine("]");
		}
		
		public void WriteComment(string line) {
			writer.WriteLine("//"+line);
		}	
	}
}