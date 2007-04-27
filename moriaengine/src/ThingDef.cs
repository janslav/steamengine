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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
//using SteamEngine.PScript;
	
namespace SteamEngine {
	public interface IThingFactory {
		Thing Create(ushort x, ushort y, sbyte z, byte m);
		Thing Create(IPoint4D point);
		Thing Create(Thing cont);
	}

	public abstract class ThingDef : AbstractDefTriggerGroupHolder, IThingFactory {
		private FieldValue name;
		internal FieldValue model;
		private FieldValue weight;
		private FieldValue height;

		internal MultiData multiData;
		
		private static Hashtable thingDefTypesByThingName = new Hashtable(StringComparer.OrdinalIgnoreCase);
			//string-Type pairs  ("Item" - class ItemDef)
		private static Hashtable thingDefTypesByName = new Hashtable(StringComparer.OrdinalIgnoreCase);
			//string-Type pairs  ("ItemDef" - class ItemDef)
		private static Hashtable thingDefCtors = new Hashtable();
			//Type-ConstructorInfo pairs (class ItemDef - Itemdef.ctor)
		
		//Highest itemdef model #: 21384	(0x5388)	<-- That's a multi. The last real item is 0x3fff.
		//Highest chardef model #: 987 (0x03db)
		
		//In case someone adds more on the end, we've set these higher.
		//public const int MaxItemModels = 0x6000;
		//public const int MaxCharModels = 0xf000;
		//private static AbstractItemDef[] itemModelDefs = new AbstractItemDef[MaxItemModels];
		//private static AbstractCharacterDef[] charModelDefs = new AbstractCharacterDef[MaxCharModels];

		private static Dictionary<uint, AbstractItemDef> itemModelDefs = new Dictionary<uint, AbstractItemDef>();
		private static Dictionary<uint, AbstractCharacterDef> charModelDefs = new Dictionary<uint, AbstractCharacterDef>();
		private static uint highestItemModel = 0;
		private static uint highestCharModel = 0;
		
		internal ThingDef(string defname, string filename, int headerLine) : base(defname, filename, headerLine) {
			name = InitField_Typed("name", "", typeof(string));
			model = this.InitField_Model("model", "0");
			weight = InitField_Typed("weight", "0", typeof(float));
			height = InitField_Typed("height", "0", typeof(int));
			ushort modelNum;
			if (TagMath.TryParseUInt16(defname.Substring(2), out modelNum)) {
				model.SetFromScripts(filename, headerLine, modelNum.ToString());
			} else if (this is AbstractItemDef) {
				model.SetFromScripts(filename, headerLine, Globals.defaultItemModel.ToString());
			} else if (this is AbstractCharacterDef) {
				model.SetFromScripts(filename, headerLine, Globals.defaultCharModel.ToString());
			} else {
				throw new ScriptException("Char or item? This should NOT happen!");
			}
		}
		
		public virtual string Name { 
			get { 
				return (string) name.CurrentValue;
			} 
			set {
				name.CurrentValue = value;
			}
		}
		
		public ushort Model { 
			get {
				return (ushort) model.CurrentValue; 
			} 
			set {
				model.CurrentValue = value; 
			}
		}
		
		public float Weight {
			get { 
				return (float) weight.CurrentValue; 
			} 
			set { 
				weight.CurrentValue = value; 
			} 
		}
		
		public int Height { 
			get { 
				return (int) height.CurrentValue; 
			} 
			set {
				height.CurrentValue = value;
			}
		}
		
		public override string ToString() {
			if (model.CurrentValue==null) {
				return Name+": "+defname+"//"+altdefname+" (null model!)";
			} else {
				return Name+": "+defname+"//"+altdefname+" ("+model.CurrentValue+")";
			}
		}
		
		public abstract bool IsItemDef { get; }
		public abstract bool IsCharDef { get; }
		
		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch(param) {
				case "event":
				case "events":
				//case "type":
				case "triggergroup":
				case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE?
					DelayedResolver.DelayResolve(new DelayedMethod(ResolveTriggerGroup), (object) args);
					break;
				default:
					if ("flag".Equals(param)) {
						param = "flags";
					}
					base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
					break;
			}
		}
		
		//this is not too elegant, but I can't think of anything better at the moment :\ -tar
		internal protected static ContOrPoint lastCreatedThingContOrPoint;
		internal protected static IPoint4D lastCreatedIPoint;

		public Thing CreateWhenLoading(ushort x, ushort y, sbyte z, byte m) {
			ThrowIfUnloaded();
			lastCreatedThingContOrPoint = ContOrPoint.Point;
			lastCreatedIPoint = new Point4D(x, y, z, m);
			return CreateImpl();
		}

		public Thing Create(ushort x, ushort y, sbyte z, byte m) {
			return Create(new Point4D(x, y, z, m));
		}

		public Thing Create(IPoint4D p) {
			ThrowIfUnloaded();
			lastCreatedThingContOrPoint = ContOrPoint.Point;
			lastCreatedIPoint = p;
			Thing retVal = CreateImpl();
			this.On_Create(retVal);
			retVal.TryTrigger(TriggerKey.create, null);
			retVal.On_Create();
			return retVal;
		}
		
		public Thing Create(Thing cont) {
			ThrowIfUnloaded();
			if (this.IsCharDef) {
				lastCreatedThingContOrPoint = ContOrPoint.Point;
			} else {
				lastCreatedThingContOrPoint = ContOrPoint.Cont;
			}
			lastCreatedIPoint = cont;
			Thing retVal = CreateImpl();
			this.On_Create(retVal);
			retVal.TryTrigger(TriggerKey.create, null);
			retVal.On_Create();
			return retVal;
		}

		protected virtual void On_Create(Thing t) {

		}
		
		protected abstract Thing CreateImpl();

		internal void Trigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TagHolder.TGStoreNode curNode = triggerGroups;
				do {
					curNode.storedTG.Run(self, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}
		
		internal void TryTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TagHolder.TGStoreNode curNode = triggerGroups;
				do {
					curNode.storedTG.TryRun(self, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}
		
		internal bool CancellableTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TagHolder.TGStoreNode curNode = triggerGroups;
				do {
					object retVal = curNode.storedTG.Run(self, td, sa);
					try {
						int retInt = Convert.ToInt32(retVal);
						if (retInt == 1) {
							return true;
						}
					} catch (Exception) {
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return false;
		}
		
		internal bool TryCancellableTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (triggerGroups != null) {
				TagHolder.TGStoreNode curNode = triggerGroups;
				do {
					object retVal = curNode.storedTG.TryRun(self, td, sa);
					try {
						int retInt = Convert.ToInt32(retVal);
						if (retInt == 1) {
							return true;
						}
					} catch (Exception) {
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return false;
		}

		/**
			This does NOT check Constants (and must not, or it would trigger infinite recursion from Constant.EvaluateToDef(string)).
		*/
		public static new ThingDef Get(string defname) {
			AbstractScript script;
			byDefname.TryGetValue(defname, out script);
			return script as ThingDef;
		}
		
		/**
			This does NOT check Constants.
			
			Searches for and returns a ThingDef for the specified model #. It is recommended that you use
			FindItemDef or FindCharDef instead of using this method.
			
			This checks chardefs first, unless the defnumber is too high to be a chardef.
			It checks itemdefs if we didn't find a chardef (or if we didn't look, if the defnumber was too high).
			It will return null if the defnumber doesn't correspond to a chardef or itemdef.
			This calls FindCharDef and FindItemDef to do the real work.
			
			@param defnumber The ID# of the def, which must be >=0 and <MaxItemModels.
		*/
		public static ThingDef Get(uint defnumber) {
			ThingDef tdone = null;
			tdone=FindCharDef(defnumber);
			if (tdone==null) {
				return FindItemDef(defnumber);
			} else {
				return tdone;
			}
		}
		
		//for loading of thingdefs from .scp/.def scripts
		public static Type GetDefTypeByName(string name) {
			return (Type) thingDefTypesByName[name];
		}
		
		public static new bool ExistsDefType(string name) {
			return thingDefTypesByName.ContainsKey(name);
		}
		
		//checking type when loading...
		public static Type GetDefTypeByThingName(string name) {//
			return (Type) thingDefTypesByThingName[name];
		}
		
		public static bool ExistsThingSubtype(string name) {
			return thingDefTypesByThingName.ContainsKey(name);
		}
		
		private static Type[] thingDefConstructorParamTypes = new Type[] {typeof(string), typeof(string), typeof(int)};
		
		//this should be typically called by the Bootstrap methods of scripted ThingDef
		public static void RegisterThingDef(Type thingDefType, string thingTypeName) {
			object o = thingDefTypesByThingName[thingTypeName];
			if (o != null) {//we have already a Thing type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class "+LogStr.Ident(o)+" in the register of Thing classes.");
			}
			o = thingDefTypesByName[thingDefType.Name];
			if (o != null) { //we have already a ThingDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class "+LogStr.Ident(o)+" in the register of ThingDef classes.");
			}
			ConstructorInfo ci = thingDefType.GetConstructor(thingDefConstructorParamTypes);
			if (ci == null) {
				throw new Exception("Proper constructor not found.");
			}
			thingDefTypesByThingName[thingTypeName] = thingDefType;
			thingDefTypesByName[thingDefType.Name] = thingDefType;
			thingDefCtors[thingDefType] = MemberWrapper.GetWrapperFor(ci);
		}
		
		/**
			This does NOT check Constants.
			
			Returns the AbstractItemDef for the specified model # (which is declared [ItemClass 0xnumber], like [AbstractItem 0x3cf]).
			If there is no AbstractItemDef for that model #, this returns null.
			Passing a defnumber which is too high is an error.
			
			@param defnumber The ID# of the def, which must be >=0 and <MaxItemModels.
		*/
		public static AbstractItemDef FindItemDef(uint defnumber) {
			AbstractItemDef def;
			itemModelDefs.TryGetValue(defnumber, out def);
			return def;
		}
		
		/**
			This does NOT check Constants.
			
			Returns the AbstractItemDef for the specified model # (which is declared [ItemClass 0xnumber], like [AbstractItem 0x3cf]).
			If there is no AbstractItemDef for that model #, this returns null.
			Passing a defnumber which is too high is an error.
			
			@param defnumber The ID# of the def, which must be >=0 and <MaxItemModels.
		*/
		public static AbstractCharacterDef FindCharDef(uint defnumber) {
			AbstractCharacterDef def;
			charModelDefs.TryGetValue(defnumber, out def);
			return def;
		}
		
		public static ICollection AllThingDefs { get {
			return ThingDef.byDefname.Values;
		} }
		
		internal static void StartingLoading() {
			
		}
		
		internal static ThingDef LoadFromScripts(PropsSection input) {
			Type thingDefType = null;
			string typeName = input.headerType.ToLower();
			string defname = input.headerName.ToLower();
			//Console.WriteLine("loading section "+input.HeadToString());
			//[typeName defname]
						
			bool isNumeric=false;
			//Attempt to convert defname to a uint.
			
			int defnum;
			if (TagMath.TryParseInt32(defname, out defnum)) {
				defname="0x"+defnum.ToString("x");
				isNumeric=true;
			}
			
			thingDefType = ThingDef.GetDefTypeByName(typeName);
			if (thingDefType==null) {
				throw new SEException("Type "+LogStr.Ident(typeName)+" does not exist.");
			}
			
			if (thingDefType.IsAbstract) {
				throw new SEException("The ThingDef Type "+LogStr.Ident(thingDefType)+" is abstract, a.e. not meant to be directly used in scripts this way. Ignoring.");
			}
			
			if (thingDefType.IsSubclassOf(typeof(AbstractItemDef))) {
				if (isNumeric) {
					defname="i_"+defname;
				}
			} else if (thingDefType.IsSubclassOf(typeof(AbstractCharacterDef))) {
				//is character
				if (isNumeric) {
					defname="c_"+defname;
				}
			} else {//this probably can not happen, but one is never too sure :)
				throw new SEException("The ThingDef Type "+LogStr.Ident(thingDefType)+" is neither AbstractCharacterDef nor AbstractItemDef subclass. Ignoring.");
			}

			AbstractScript def;
			byDefname.TryGetValue(defname, out def);
			ThingDef thingDef = def as ThingDef;
			
			if (thingDef == null) {
				if (def != null) {//it isnt thingDef
					throw new OverrideNotAllowedException("ThingDef "+LogStr.Ident(defname)+" has the same name as "+LogStr.Ident(def));
				} else {
					ConstructorInfo cw = (ConstructorInfo) thingDefCtors[thingDefType];
					thingDef = (ThingDef) cw.Invoke(BindingFlags.Default, null, new object[] {defname, input.filename, input.headerLine}, null);
				}
			} else if (thingDef.unloaded) {
				if (thingDef.GetType() != thingDefType) {
					throw new OverrideNotAllowedException("You can not change the class of a Thingdef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				thingDef.unloaded = false;
				UnRegisterThingDef(thingDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("ThingDef "+LogStr.Ident(defname)+" defined multiple times. Ignoring.");
			}
			
			thingDef.defname = defname;
			byDefname[defname] = thingDef;
			
			thingDef.ClearTriggerGroups();//maybe clear other things too?
			
			if (isNumeric) {
				uint idefnum=(uint) defnum;
				if (thingDef is AbstractItemDef) {
					if (idefnum>highestItemModel) highestItemModel=idefnum;
					//Sanity.IfTrueThrow(idefnum>MaxItemModels, "defnum "+idefnum+" (0x"+idefnum.ToString("x")+") is higher than MaxItemModels ("+MaxItemModels+").");
					itemModelDefs[idefnum] = (AbstractItemDef) thingDef;
				} else if (thingDef is AbstractCharacterDef) {
					if (idefnum>highestCharModel) highestCharModel=idefnum;
					//Sanity.IfTrueThrow(idefnum>MaxCharModels, "defnum "+idefnum+" (0x"+idefnum.ToString("x")+") is higher than MaxCharModels ("+MaxCharModels+").");
					charModelDefs[idefnum] = (AbstractCharacterDef) thingDef;
				}
			}
		
			//header done. now we have the def instantiated.
			//now load the other fields
			thingDef.LoadScriptLines(input);
			
			//now do load the trigger code. 
			if (input.TriggerCount>0) {
				input.headerName = "t__"+input.headerName+"__";
				TriggerGroup tg = TriggerGroup.Load(input);
				thingDef.AddTriggerGroup(tg);
			}
			return thingDef;
		}
		
		private static void UnRegisterThingDef(ThingDef td) {
			byDefname.Remove(td.Defname);
			if (td.altdefname != null) {
				byDefname.Remove(td.altdefname);
			}
		}
		
		internal static void LoadingFinished() {
			//dump number of loaded instances?
			Logger.WriteDebug("Highest itemdef model #: "+highestItemModel+" (0x"+highestItemModel.ToString("x")+")");
			Logger.WriteDebug("Highest chardef model #: "+highestCharModel+" (0x"+highestCharModel.ToString("x")+")");
			
			int count = byDefname.Count;
			Logger.WriteDebug("Resolving itemdefs out of "+count+" abstractdefs");
			DateTime before = DateTime.Now;
			int a = 0;
			foreach (AbstractScript td in byDefname.Values) {
				if ((a%100)==0) {
					Logger.SetTitle("Resolving dupelists: "+((a*100)/count)+" %");
				}
				AbstractItemDef idef = td as AbstractItemDef;
				if (idef!=null) {
					try {
						AbstractItemDef dupeItem = idef.DupeItem;
						if (dupeItem!=null) {
							dupeItem.AddToDupeList(idef);
						}
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteWarning(e);
					}

					try {
						idef.multiData = MultiData.Get(idef.Model);
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteWarning(e);
					}

				}
				a++;
			}
			DateTime after = DateTime.Now;
			Logger.WriteDebug("...took "+(after-before));
			Logger.SetTitle("");
		}
		
		internal static void UnLoadAll() {
			thingDefTypesByThingName.Clear();//we can assume that inside core there are no non-abstract thingdefs
			thingDefTypesByName.Clear();
			thingDefCtors.Clear(); 

			
			//for (int idx=0; idx<MaxItemModels; idx++) {
			//    itemModelDefs[idx]=null;
			//}
			//for (int idx=0; idx<MaxCharModels; idx++) {
			//    charModelDefs[idx]=null;
			//}
			itemModelDefs.Clear();
			charModelDefs.Clear();
		}
	}
}