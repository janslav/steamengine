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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Configuration;
using SteamEngine.Common;
using SteamEngine.LScript;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	//it's not a typical def, but then again, who cares :P
	public sealed class TemplateDef : AbstractDef, IThingFactory {
		public static bool TemplateTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Template Trace Messages"]);

		private FieldValue container;
		private LScriptHolder holder;
		private static ItemDef defaultContainer;

		public static new TemplateDef Get(string defname) {
			AbstractScript script;
			AllScriptsByDefname.TryGetValue(defname, out script);
			return script as TemplateDef;
		}

		internal TemplateDef(string defname, string filename, int headerLine)
			:
				base(defname, filename, headerLine) {
			container = InitThingDefField("container", null, typeof(ItemDef));
		}

		public ItemDef Container {
			get {
				return (ItemDef) container.CurrentValue;
			}
			set {
				container.CurrentValue = value;
			}
		}

		private static void UnRegisterTemplateDef(TemplateDef td) {
			AllScriptsByDefname.Remove(td.Defname);
			if (td.Altdefname != null) {
				AllScriptsByDefname.Remove(td.Altdefname);
			}
		}

		private static void RegisterTemplateDef(TemplateDef td) {
			AllScriptsByDefname[td.Defname] = td;
			if (td.Altdefname != null) {
				AllScriptsByDefname[td.Altdefname] = td;
			}
		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			string typeName = input.headerType.ToLower();
			string defname = input.headerName.ToLower();
			//Console.WriteLine("loading section "+input.HeadToString());
			//[typeName defname]

			//Attempt to convert defname to a uint, so that we can "normalize" it
			int defnum;
			if (TagMath.TryParseInt32(defname, out defnum)) {
				defname = "td_0x" + defnum.ToString("x");
			}

			AbstractScript def;
			AllScriptsByDefname.TryGetValue(defname, out def);
			TemplateDef td = def as TemplateDef;
			if (td == null) {
				if (def != null) {//it isnt TemplateDef
					throw new OverrideNotAllowedException("TemplateDef " + LogStr.Ident(defname) + " has the same name as " + LogStr.Ident(def) + ". Ignoring.");
				} else {
					td = new TemplateDef(defname, input.filename, input.headerLine);
				}
			} else if (td.IsUnloaded) {
				td.IsUnloaded = false;
				UnRegisterTemplateDef(td);//will be re-registered again
			} else {
				throw new OverrideNotAllowedException("TemplateDef " + LogStr.Ident(defname) + " defined multiple times.");
			}

			TriggerSection trigger = input.GetTrigger(0);
			//try to read container and/or second defname from the start of the text
			bool altdefnamedefined = false;
			bool containerdefined = false;
			StringBuilder newCode = new StringBuilder();
			StringReader reader = new StringReader(trigger.code.ToString());
			int linenumber = input.headerLine;
			while (true) {
				string line = reader.ReadLine();
				linenumber++;
				if (line != null) {
					line = line.Trim();
					if ((line.Length == 0) || (line.StartsWith("//"))) {//it is a comment or a blank line
						newCode.Append(Environment.NewLine);
						continue;
					}

					Match m = Loc.valueRE.Match(line);
					if (m.Success) {
						GroupCollection gc = m.Groups;
						string name = gc["name"].Value;
						if (StringComparer.OrdinalIgnoreCase.Equals(name, "defname")) {//set altdefname
							if (altdefnamedefined) {
								Logger.WriteWarning(input.filename, linenumber, "Alternative defname already defined, ignoring.");
							} else {
								string altdefname = gc["value"].Value;
								Match ma = TagMath.stringRE.Match(altdefname);
								if (ma.Success) {
									altdefname = String.Intern(ma.Groups["value"].Value);
								} else {
									altdefname = String.Intern(altdefname);
								}
								def = Get(altdefname);
								TemplateDef t = def as TemplateDef;
								if (t == null) {
									if (def != null) {//it isnt TemplateDef
										throw new OverrideNotAllowedException(
											"TemplateDef " + LogStr.Ident(defname) + " has the same name as " + LogStr.Ident(def) + ". Ignoring.");
									} else {
										td.Altdefname = altdefname;
									}
								} else if (t == td) {
									Logger.WriteWarning(input.filename, linenumber,
										"Defname redundantly specified for TemplateDef " + LogStr.Ident(altdefname) + ".");
								} else {
									throw new OverrideNotAllowedException("TemplateDef " + LogStr.Ident(altdefname) + " defined multiple times.");
								}
								altdefnamedefined = true;
							}
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "container")) {//set container itemdef
							if (containerdefined) {
								Logger.WriteWarning(input.filename, linenumber, "Container id already defined, ignoring.");
							} else {
								td.container.SetFromScripts(input.filename, linenumber, gc["value"].Value);
								containerdefined = true;
							}
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "CATEGORY")) {//ignoring axis thingies
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "SUBSECTION")) {//ignoring axis thingies
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "DESCRIPTION")) {//ignoring axis thingies
						} else {
							newCode.Append(line);
						}
					} else {
						newCode.Append(line);
					}
					newCode.Append(Environment.NewLine);
				} else {//end of section code
					break;
				}
			}

			//so we have read the important fields, now read the rest as lscript function

			trigger.code = newCode;
			td.holder = new LScriptHolder(trigger);
			if (input.TriggerCount > 1) {
				Logger.WriteWarning(input.filename, input.headerLine, "Triggers in a template are nonsensual (and ignored).");
			}

			//that's all, we're done ;)
			RegisterTemplateDef(td);

			return td;
		}

		public static new void Bootstrap() {
			ScriptLoader.RegisterScriptType(new string[] { "templatedef", "template" },
				new LoadSection(LoadFromScripts), true);
		}

		public ItemDef DefaultContainer {
			get {
				if (defaultContainer == null) {
					defaultContainer = (ItemDef) ThingDef.Get("i_bag");
					if (defaultContainer == null) {
						throw new ScriptException("Missing TemplateDef's default container itemdef (i_bag)");
					}
				}
				return defaultContainer;
			}
		}

		public Thing Create(IPoint4D p) {
			return Create(p.X, p.Y, p.Z, p.M);
		}

		public Thing Create(int x, int y, int z, byte m) {
			ThrowIfUnloaded();
			ItemDef contDef = this.Container;
			if (contDef == null) {
				contDef = DefaultContainer;
			}
			Thing cont = contDef.Create(x, y, z, m);
			holder.Run(cont, (ScriptArgs) null);
			return cont;
		}

		public Thing Create(Thing cont) {
			ThrowIfUnloaded();
			ItemDef contDef = this.Container;
			if (cont.IsChar) {
				cont = ((Character) cont).Backpack;
			}
			if (contDef != null) {
				cont = contDef.Create(cont);
			} //else we put the item(s) in the given container
			holder.Run(cont, (ScriptArgs) null);
			return cont;//we return the container even in case we didn't create it
		}
	}
}
