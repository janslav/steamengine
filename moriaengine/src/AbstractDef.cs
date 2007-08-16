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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public abstract class AbstractDef : AbstractScript, ITagHolder {
		static int uids;
		private int uid;

		//internal protected static Dictionary<string, AbstractDef> byDefname = new Dictionary<string, AbstractDef>(StringComparer.OrdinalIgnoreCase);
		protected static Dictionary<string, Type> defTypesByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		//string-Type pairs  ("ItemDef" - class ItemDef)


		internal Hashtable fieldValues = new Hashtable(StringComparer.OrdinalIgnoreCase);
		public string filename;
		public int headerLine;
		//internal protected bool unloaded;
		//public string defname = null;
		public string altdefname = null;
		private bool alreadySaved;

		protected AbstractDef(string defname, string filename, int headerLine)
			: base(defname) {
			this.filename=filename;
			this.headerLine=headerLine;
			this.unloaded = false;
			this.uid = uids++;
		}

		public override string ToString() {
			return defname+"/"+altdefname;
		}

		public string Filepos {
			get {
				return "line "+headerLine+" of "+filename;
			}
		}


		public static new AbstractDef Get(string name) {
			AbstractScript script;
			byDefname.TryGetValue(name, out script);
			return script as AbstractDef;
		}

		public override string PrettyDefname {
			get {
				//from the two available defnames (like c_0x190 and c_man) it returns the more understandable (c_man)
				if (defname.IndexOf("_0x") > 0) {
					if (altdefname!=null) {
						return altdefname;
					}
				}
				return defname;
			}
		}

		//used by loaders (Thing, GameAccount, Thingdefs)
		public void LoadScriptLines(PropsSection ps) {
			foreach (PropsLine p in ps.props.Values) {
				try {
					LoadScriptLine(ps.filename, p.line, p.name.ToLower(), p.value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.filename, p.line, ex);
				}
			}
		}

		protected virtual void LoadScriptLine(string filename, int line, string param, string args) {
			Match m = TagHolder.tagRE.Match(param);
			FieldValue fieldValue;
			if (m.Success) {	//If the name begins with 'tag.'
				string tagName=m.Groups["name"].Value;
				TagKey td = TagKey.Get(tagName);
				fieldValue = (FieldValue) fieldValues[td];
				if (fieldValue == null) {
					tagName = "tag."+tagName;
					fieldValue = new FieldValue(tagName, FieldValueType.Typeless, null, filename, line, args);
					fieldValues[td] = fieldValue;
					fieldValues[tagName] = fieldValue;
				} else {
					fieldValue.SetFromScripts(filename, line, args);
				}
				return;
			}

			switch (param) {
				case "category":
				case "subsection":
				case "description":
					return;
				//axis props are ignored. Or shouldnt they? :)
				case "defname":
					Match ma = TagMath.stringRE.Match(args);
					if (ma.Success) {
						args = String.Intern(ma.Groups["value"].Value);
					} else {
						args = String.Intern(args);
					}

					AbstractScript def;
					byDefname.TryGetValue(args, out def);

					if (def == null) {
						altdefname=args;
						byDefname[altdefname]=this;
					} else if (def == this) {
						throw new ScriptException("Defname redundantly specified for Def "+LogStr.Ident(args)+".");
					} else {
						throw new ScriptException("Def "+LogStr.Ident(args)+" defined multiple times.");
					}
					break;

				default:
					fieldValue = (FieldValue) fieldValues[param];
					if (fieldValue != null) {
						fieldValue.SetFromScripts(filename, line, args);
						return;
					}
					throw new ScriptException("Invalid data '"+LogStr.Ident(param)+"' = '"+LogStr.Number(args)+"'.");
			}
		}

		internal static void LoadSectionFromSaves(PropsSection input) {
			//todo: a way to load new defs (or just regions)

			string typeName = input.headerType.ToLower();
			string defname = input.headerName.ToLower();

			AbstractDef def = Get(defname);
			if (def == null) {
				Logger.WriteError(input.filename, input.headerLine, LogStr.Ident(typeName+" "+defname)
					+ " is in the world being loaded, but it was not defined in the scripts. Skipping.");
				return;
			}
			if (string.Compare(def.GetType().Name, typeName, true) != 0) {
				Logger.WriteWarning(input.filename, input.headerLine,
					LogStr.Ident(typeName+" "+defname)+" declared wrong class. It is in fact "+LogStr.Ident(def.GetType().Name)+".");
			}

			def.LoadFromSaves(input);
		}

		public virtual void LoadFromSaves(PropsSection input) {
			foreach (PropsLine pl in input.props.Values) {
				ObjectSaver.Load(pl.value, new LoadObjectParam(this.LoadField_Delayed), filename, pl.line,
					pl.name);
			}
		}

		private void LoadField_Delayed(object resolvedObject, string filename, int line, object args) {
			string fieldName = (string) args;
			FieldValue fv = (FieldValue) fieldValues[fieldName];

			if (fv == null) {//that means it's not in scripts
				Match m = TagHolder.tagRE.Match(fieldName);
				if (m.Success) {	//If the name begins with 'tag.'
					string tagName=m.Groups["name"].Value;
					TagKey td = TagKey.Get(tagName);
					tagName = "tag."+tagName;
					fv = new FieldValue(tagName, FieldValueType.Typeless, null, "", -1, "");
					fieldValues[td] = fv;
					fieldValues[tagName] = fv;
				}
			}

			if (fv != null) {
				try {
					fv.CurrentValue = resolvedObject;
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(filename, line, e);
				}
			}
		}

		protected FieldValue InitField_Typed(string name, string value, Type type) {
			FieldValue fieldValue = (FieldValue) fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.Typed, type, "<default>", -1, value);
				fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typed called , when a fieldvalue called "+LogStr.Ident(name)+" already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitField_ThingDef(string name, string value, Type type) {
			FieldValue fieldValue = (FieldValue) fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.ThingDefType, type, "<default>", -1, value);
				fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typed called , when a fieldvalue called "+LogStr.Ident(name)+" already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitField_Model(string name, string value) {
			FieldValue fieldValue = (FieldValue) fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.Model, null, "<default>", -1, value);
				fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Model called , when a fieldvalue called "+LogStr.Ident(name)+" already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitField_Typeless(string name, string value) {
			FieldValue fieldValue = (FieldValue) fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.Typeless, null, "<default>", -1, value);
				fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typeless called , when a fieldvalue called "+LogStr.Ident(name)+" already exists.");
			}
			return fieldValue;
		}

		public FieldValue GetFieldValue(string name) {
			return (FieldValue) fieldValues[name];
		}

		public object GetDefaultFieldValue(string name) {
			FieldValue fv = (FieldValue) fieldValues[name];
			if (fv != null) {
				return fv.DefaultValue;
			}
			return null;
		}

		public object GetCurrentFieldValue(string name) {
			FieldValue fv = (FieldValue) fieldValues[name];
			if (fv != null) {
				return fv.CurrentValue;
			}
			return null;
		}

		public override void Unload() {

			//ClearTags();

			foreach (FieldValue fv in this.fieldValues.Values) {
				fv.Unload();
			}

			base.Unload();

			//todo: clear the other various properties...
			//todo: not clear those tags/tgs/timers/whatever that were set dynamically (ie not in scripted defs)
		}

		[Summary("This method is called on startup when the resolveEverythingAtStart in steamengine.ini is set to True")]
		public static void ResolveAll() {
			int count = byDefname.Count;
			Logger.WriteDebug("Resolving "+count+" defs");

			DateTime before = DateTime.Now;
			int a = 0;
			foreach (AbstractScript script in byDefname.Values) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					if ((a%50)==0) {
						Logger.SetTitle("Resolving def field values: "+((a*100)/count)+" %");
					}
					if (!def.unloaded) {//those should have already stated what's the problem :)
						foreach (FieldValue fv in def.fieldValues.Values) {
							try {
								fv.ResolveTemporaryState();
							} catch (FatalException) {
								throw;
							} catch (Exception e) {
								Logger.WriteWarning(e);
							}
						}
					}
					a++;
				}
			}
			DateTime after = DateTime.Now;
			Logger.WriteDebug("...took "+(after-before));
			Logger.SetTitle("");
		}

		internal static void SaveAll(SaveStream output) {
			Logger.WriteDebug("Saving defs.");
			output.WriteComment("Defs");

			foreach (AbstractScript script in byDefname.Values) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					def.alreadySaved = false;
				}
			}
			int count = byDefname.Count;
			int a = 0;
			foreach (AbstractScript script in byDefname.Values) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					if ((a%50)==0) {
						Logger.SetTitle("Saving defs: "+((a*100)/count)+" %");
					}
					if ((!def.unloaded)&&(!def.alreadySaved)) {
						def.Save(output);
						def.alreadySaved = true;
					}
					a++;
				}
			}
			Logger.SetTitle("");
			output.WriteLine();
			ObjectSaver.FlushCache(output);
		}

		public void Save(SaveStream output) {
			bool headerWritten = false;
			foreach (DictionaryEntry entry in fieldValues) {
				if (entry.Key is TagKey) {//so that tags dont get written twice
					continue;
				}
				try {
					FieldValue fv = (FieldValue) entry.Value;
					if (fv.ShouldBeSaved()) {
						if (!headerWritten) {
							output.WriteLine();
							output.WriteSection(this.GetType().Name, this.PrettyDefname);
							headerWritten = true;
						}
						output.WriteValue(fv.Name, fv.CurrentValue);
					}
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(e);//this should not actually happen, I hope :)
				}
			}
			if (headerWritten) {
				ObjectSaver.FlushCache(output);
			}
		}

		public static void RegisterSubtype(Type type) {
			string typeName = type.Name;
			Type o;
			if (defTypesByName.TryGetValue(typeName, out o)) { //we have already a ThingDef type named like that
				throw new OverrideNotAllowedException("Trying to overwrite class "+LogStr.Ident(o)+" in the register of AbstractDef classes.");
			}
			defTypesByName[typeName] = type;
		}

		public static bool ExistsDefType(string name) {
			return defTypesByName.ContainsKey(name);
		}

		//public static void RegisterGetter(DefGetter deleg) {
		//	getterDelegates.Add(deleg);
		//}

		public object GetTag(TagKey td) {
			FieldValue fv = (FieldValue) fieldValues[td];
			if (fv != null) {
				return fv.CurrentValue;
			} else {
				return null;
			}
		}

		public void SetTag(TagKey tk, object value) {
			FieldValue fv = (FieldValue) fieldValues[tk];
			if (fv == null) {
				string tagName = "tag."+tk;
				fv = new FieldValue(tagName, FieldValueType.Typeless, null, "", -1, "");
				fieldValues[tk] = fv;
				fieldValues[tagName] = fv;
			}
			fv.CurrentValue = value;
		}

		public bool HasTag(TagKey td) {
			return (fieldValues.ContainsKey(td));
		}

		public void RemoveTag(TagKey td) {
			FieldValue fv = (FieldValue) fieldValues[td];
			if (fv != null) {
				fv.CurrentValue = null;
			}
		}

		public void ClearTags() {
			foreach (DictionaryEntry entry in fieldValues) {
				if (entry.Key is TagKey) {
					FieldValue fv = (FieldValue) entry.Value;
					fv.CurrentValue = null;
				}
			}
		}

		//unloads instances that come from scripts.
		internal static void UnloadScripts() {
			defTypesByName.Clear();//we assume that inside core there are no non-abstract defs
			byDefname.Clear();
		}

		public override bool Equals(object obj) {
			return obj == this;
		}

		public override int GetHashCode() {
			return uid;
		}
	}
}
