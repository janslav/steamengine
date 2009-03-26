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
using SteamEngine.Regions;
//using SteamEngine.PScript;

namespace SteamEngine {

	public interface IThingFactory {
		Thing Create(int x, int y, int z, byte m);
		Thing Create(IPoint4D point);
		Thing Create(Thing cont);
	}

	public abstract class ThingDef : AbstractDefTriggerGroupHolder, IThingFactory {
		internal FieldValue name;
		internal FieldValue model;
		private FieldValue weight;
		internal FieldValue height;

		private FieldValue color;

		internal MultiData multiData;

		private static Dictionary<Type, Type> thingDefTypesByThingType = new Dictionary<Type, Type>();
		private static Dictionary<Type, Type> thingTypesByThingDefType = new Dictionary<Type, Type>();

		private static Dictionary<string, Type> thingDefTypesByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<Type, ConstructorInfo> thingDefCtors = new Dictionary<Type, ConstructorInfo>();


		//Highest itemdef model #: 21384	(0x5388)	<-- That's a multi. The last real item is 0x3fff.
		//Highest chardef model #: 987 (0x03db)

		//In case someone adds more on the end, we've set these higher.
		//public const int MaxItemModels = 0x6000;
		//public const int MaxCharModels = 0xf000;
		//private static AbstractItemDef[] itemModelDefs = new AbstractItemDef[MaxItemModels];
		//private static AbstractCharacterDef[] charModelDefs = new AbstractCharacterDef[MaxCharModels];

		private static Dictionary<int, AbstractItemDef> itemModelDefs = new Dictionary<int, AbstractItemDef>();
		private static Dictionary<int, AbstractCharacterDef> charModelDefs = new Dictionary<int, AbstractCharacterDef>();
		private static int highestItemModel = 0;
		private static int highestCharModel = 0;

		internal ThingDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.name = InitField_Typed("name", "", typeof(string));
			this.color = InitField_Typed("color", 0, typeof(int));

			this.model = InitField_Model("model", 0);
			this.weight = InitField_Typed("weight", 0, typeof(float));
			this.height = InitField_Typed("height", 0, typeof(int));
			int modelNum;
			if (TagMath.TryParseInt32(defname.Substring(2), out modelNum)) {
				this.model.SetFromScripts(filename, headerLine, modelNum.ToString());
			} else if (this is AbstractItemDef) {
				this.model.SetFromScripts(filename, headerLine, Globals.defaultItemModel.ToString());
			} else if (this is AbstractCharacterDef) {
				this.model.SetFromScripts(filename, headerLine, Globals.defaultCharModel.ToString());
			} else {
				throw new ScriptException("Char or item? This should NOT happen!");
			}
		}

		public virtual string Name {
			get {
				return (string) this.name.CurrentValue;
			}
			set {
				this.name.CurrentValue = value;
			}
		}

		public int Model {
			get {
				return (int) this.model.CurrentValue;
			}
			set {
				this.model.CurrentValue = value;
			}
		}

		public int Color {
			get {
				return (int) this.color.CurrentValue;
			}
			set {
				this.color.CurrentValue = value;
			}
		}

		public float Weight {
			get {
				return (float) this.weight.CurrentValue;
			}
			set {
				this.weight.CurrentValue = value;
			}
		}

		public virtual int Height {
			get {
				if (this.height.IsDefaultCodedValue) {
					return Map.PersonHeight;
				} else {
					return (int) this.height.CurrentValue;
				}
			}
			set {
				this.height.CurrentValue = value;
			}
		}

		public override string ToString() {
			if (this.model.CurrentValue == null) {
				return Name + ": " + Defname + "//" + altdefname + " (null model!)";
			} else {
				return Name + ": " + Defname + "//" + altdefname + " (" + model.CurrentValue + ")";
			}
		}

		public abstract bool IsItemDef { get; }
		public abstract bool IsCharDef { get; }

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if ("flag".Equals(param)) {
				param = "flags";
			}
			base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
		}

		internal Thing CreateWhenLoading(ushort x, ushort y, sbyte z, byte m) {
			this.ThrowIfUnloaded();
			return CreateImpl();
		}

		public Thing Create(int x, int y, int z, byte m) {
			this.ThrowIfUnloaded();
			Thing retVal = CreateImpl();
			PutOnGround(retVal, x, y, z, m);
			this.Trigger_Create(retVal);
			return retVal;
		}

		public Thing Create(IPoint4D p) {
			p = p.TopPoint;
			return Create(p.X, p.Y, p.Z, p.M);
		}

		public Thing Create(Thing cont) {
			this.ThrowIfUnloaded();
			Thing retVal = this.CreateImpl();
			AbstractItem item = retVal as AbstractItem;
			if (item == null) {//we are char
				MutablePoint4D p = cont.TopObj().point4d;
				PutOnGround(retVal, p.x, p.y, p.z, p.m);
			} else {
				AbstractCharacter contAsChar = cont as AbstractCharacter;
				if (contAsChar != null) {
					if (retVal.IsEquippable) {
						PutInCont(item, contAsChar);
					} else {
						PutInCont(item, contAsChar.Backpack);
					}
				} else if (cont.IsContainer) {
					PutInCont(item, cont);
				} else {
					MutablePoint4D p = cont.TopObj().point4d;
					PutOnGround(item, p.x, p.y, p.z, p.m);
				}
			}

			Trigger_Create(retVal);
			return retVal;
		}

		private static void PutOnGround(Thing t, int x, int y, int z, byte m) {
			//MarkAsLimbo(t);
			AbstractItem item = t as AbstractItem;
			if (item != null) {
				item.Trigger_EnterRegion(x, y, z, m);
			} else {
				t.point4d.SetP(x, y, z, m);
				Map.GetMap(m).Add(t);
			}
		}

		private static void PutInCont(AbstractItem item, Thing cont) {
			AbstractItem contItem = cont as AbstractItem;
			if (contItem == null) {
				//MarkAsLimbo(item);
				byte layer = item.Layer;
				if (layer > 0) {
					item.Trigger_EnterChar((AbstractCharacter) cont, layer);
				} else {
					throw new SEException("Item '" + item + "' is equippable, but has Layer not set.");
				}
			} else {
				//MarkAsLimbo(item);
				int x, y;
				contItem.GetRandomXYInside(out x, out y);
				item.Trigger_EnterItem(contItem, x, y);
			}
		}

		public void Trigger_Create(Thing createdThing) {
			this.On_Create(createdThing);
			createdThing.TryTrigger(TriggerKey.create, null);
			try {
				createdThing.On_Create();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		protected virtual void On_Create(Thing t) {

		}

		protected abstract Thing CreateImpl();

		internal void Trigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					curNode.storedTG.Run(self, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		internal void TryTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
				do {
					curNode.storedTG.TryRun(self, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		internal bool CancellableTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
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
			if (firstTGListNode != null) {
				PluginHolder.TGListNode curNode = firstTGListNode;
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
		public static ThingDef Get(int defnumber) {
			ThingDef tdone = null;
			tdone = FindCharDef(defnumber);
			if (tdone == null) {
				return FindItemDef(defnumber);
			} else {
				return tdone;
			}
		}

		//for loading of thingdefs from .scp/.def scripts
		public static Type GetDefTypeByName(string name) {
			Type defType;
			thingDefTypesByName.TryGetValue(name, out defType);
			return defType;
		}

		public static new bool ExistsDefType(string name) {
			return thingDefTypesByName.ContainsKey(name);
		}

		//checking type when loading...
		public static Type GetDefTypeByThingType(Type thingType) {//
			Type defType;
			thingDefTypesByThingType.TryGetValue(thingType, out defType);
			return defType;
		}

		private static Type[] thingDefConstructorParamTypes = new Type[] { typeof(string), typeof(string), typeof(int) };

		//this should be typically called by the Bootstrap methods of scripted ThingDef
		public static void RegisterThingDef(Type thingDefType, Type thingType) {
			Type t;
			if (thingDefTypesByThingType.TryGetValue(thingDefType, out t)) {
				throw new OverrideNotAllowedException("ThingDef type " + LogStr.Ident(thingDefType.FullName) + " already has it's Thing type -" + t.FullName + ".");
			}
			if (thingTypesByThingDefType.TryGetValue(thingType, out t)) {
				throw new OverrideNotAllowedException("Thing type " + LogStr.Ident(thingType.FullName) + " already has it's ThingDef type -" + t.FullName + ".");
			}

			ConstructorInfo ci = thingDefType.GetConstructor(thingDefConstructorParamTypes);
			if (ci == null) {
				throw new SEException("Proper constructor not found.");
			}
			thingDefTypesByThingType[thingType] = thingDefType;
			thingTypesByThingDefType[thingDefType] = thingType;
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
		public static AbstractItemDef FindItemDef(int defnumber) {
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
		public static AbstractCharacterDef FindCharDef(int defnumber) {
			AbstractCharacterDef def;
			charModelDefs.TryGetValue(defnumber, out def);
			return def;
		}

		public static ICollection AllThingDefs {
			get {
				return ThingDef.byDefname.Values;
			}
		}

		internal static void StartingLoading() {

		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			Type thingDefType = null;
			string typeName = input.headerType.ToLower();
			string defname = input.headerName.ToLower();
			//Console.WriteLine("loading section "+input.HeadToString());
			//[typeName defname]

			bool isNumeric = false;
			//Attempt to convert defname to a uint.

			int defnum;
			if (TagMath.TryParseInt32(defname, out defnum)) {
				defname = "0x" + defnum.ToString("x");
				isNumeric = true;
			}

			thingDefType = ThingDef.GetDefTypeByName(typeName);
			if (thingDefType == null) {
				throw new SEException("Type " + LogStr.Ident(typeName) + " does not exist.");
			}

			if (thingDefType.IsAbstract) {
				throw new SEException("The ThingDef Type " + LogStr.Ident(thingDefType) + " is abstract, a.e. not meant to be directly used in scripts this way. Ignoring.");
			}

			if (thingDefType.IsSubclassOf(typeof(AbstractItemDef))) {
				if (isNumeric) {
					defname = "i_" + defname;
				}
			} else if (thingDefType.IsSubclassOf(typeof(AbstractCharacterDef))) {
				//is character
				if (isNumeric) {
					defname = "c_" + defname;
				}
			} else {//this probably can not happen, but one is never too sure :)
				throw new SEException("The ThingDef Type " + LogStr.Ident(thingDefType) + " is neither AbstractCharacterDef nor AbstractItemDef subclass. Ignoring.");
			}

			AbstractScript def;
			byDefname.TryGetValue(defname, out def);
			ThingDef thingDef = def as ThingDef;

			if (thingDef == null) {
				if (def != null) {//it isnt thingDef
					throw new OverrideNotAllowedException("ThingDef " + LogStr.Ident(defname) + " has the same name as " + LogStr.Ident(def));
				} else {
					ConstructorInfo cw = thingDefCtors[thingDefType];
					thingDef = (ThingDef) cw.Invoke(BindingFlags.Default, null, new object[] { defname, input.filename, input.headerLine }, null);
				}
			} else if (thingDef.unloaded) {
				if (thingDef.GetType() != thingDefType) {
					throw new OverrideNotAllowedException("You can not change the class of a Thingdef while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				thingDef.unloaded = false;
				UnRegisterThingDef(thingDef);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("ThingDef " + LogStr.Ident(defname) + " defined multiple times. Ignoring.");
			}

			thingDef.Defname = defname;
			byDefname[defname] = thingDef;

			thingDef.ClearTriggerGroups();//maybe clear other things too?

			if (isNumeric) {
				if (thingDef is AbstractItemDef) {
					if (defnum > highestItemModel) highestItemModel = defnum;
					//Sanity.IfTrueThrow(idefnum>MaxItemModels, "defnum "+idefnum+" (0x"+idefnum.ToString("x")+") is higher than MaxItemModels ("+MaxItemModels+").");
					itemModelDefs[defnum] = (AbstractItemDef) thingDef;
				} else if (thingDef is AbstractCharacterDef) {
					if (defnum > highestCharModel) highestCharModel = defnum;
					//Sanity.IfTrueThrow(idefnum>MaxCharModels, "defnum "+idefnum+" (0x"+idefnum.ToString("x")+") is higher than MaxCharModels ("+MaxCharModels+").");
					charModelDefs[defnum] = (AbstractCharacterDef) thingDef;
				}
			}

			//header done. now we have the def instantiated.
			//now load the other fields
			thingDef.LoadScriptLines(input);

			//now do load the trigger code. 
			TriggerGroup tg = null;
			if (input.TriggerCount > 0) {
				input.headerName = "t__" + defname + "__";
				tg = ScriptedTriggerGroup.Load(input);
				thingDef.AddTriggerGroup(tg);
			}
			if (tg == null) {
				return thingDef;
			} else {
				return new UnloadableGroup(thingDef, tg);
			}
		}

		private static void UnRegisterThingDef(ThingDef td) {
			byDefname.Remove(td.Defname);
			if (td.altdefname != null) {
				byDefname.Remove(td.altdefname);
			}
		}

		internal static void LoadingFinished() {
			//dump number of loaded instances?
			Logger.WriteDebug("Highest itemdef model #: " + highestItemModel + " (0x" + highestItemModel.ToString("x") + ")");
			Logger.WriteDebug("Highest chardef model #: " + highestCharModel + " (0x" + highestCharModel.ToString("x") + ")");

			int count = byDefname.Count;
			using (StopWatch.StartAndDisplay("Resolving dupelists and multidata...")) {
				int a = 0;
				foreach (AbstractScript td in byDefname.Values) {
					if ((a % 100) == 0) {
						Logger.SetTitle("Resolving dupelists and multidata: " + ((a * 100) / count) + " %");
					}
					AbstractItemDef idef = td as AbstractItemDef;
					if (idef != null) {
						try {
							AbstractItemDef dupeItem = idef.DupeItem;
							if (dupeItem != null) {
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
			}
			Logger.SetTitle("");
		}

		internal static void ClearAll() {
			thingDefTypesByThingType.Clear();//we can assume that inside core there are no non-abstract thingdefs
			thingTypesByThingDefType.Clear();//we can assume that inside core there are no non-abstract thingdefs
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

	public class UnloadableGroup : IUnloadable {
		IUnloadable[] array;

		public UnloadableGroup(params IUnloadable[] array) {
			this.array = array;
		}

		public void Unload() {
			foreach (IUnloadable member in array) {
				if (member != null) {
					member.Unload();
				}
			}
		}

		public bool IsUnloaded {
			get {
				foreach (IUnloadable member in array) {
					if (member != null) {
						if (member.IsUnloaded) {
							return true;
						}
					}
				}
				return false;
			}
		}
	}
}
