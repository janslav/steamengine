//------------------------------------------------------------------------------
// <auto-generated>
//     Tento kód byl generován nástrojem.
//     Verze modulu runtime:2.0.50727.1433
//
//     Změny tohoto souboru mohou způsobit nesprávné chování a budou ztraceny,
//     dojde-li k novému generování kódu.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SteamEngine.CompiledScripts {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using SteamEngine;
	using SteamEngine.Timers;
	using SteamEngine.Packets;
	using SteamEngine.Persistence;
	using SteamEngine.Common;
	
	
	public partial class SnoopingPluginDef : TimerPluginDef {
		
		public SnoopingPluginDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
		}
		
		protected override SteamEngine.Plugin CreateImpl() {
			return new SnoopingPlugin();
		}
		
		public new static void Bootstrap() {
			SteamEngine.PluginDef.RegisterPluginDef(typeof(SnoopingPluginDef), typeof(SnoopingPlugin));
		}
	}
	
	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class SnoopingPlugin : TimerPlugin {
		
		public LinkedList<Container> snoopedBackpacks = null;
		
		[SteamEngine.DeepCopyImplementationAttribute()]
		public SnoopingPlugin(SnoopingPlugin copyFrom) : 
				base(copyFrom) {
			this.snoopedBackpacks = copyFrom.snoopedBackpacks;
		}
		
		[SteamEngine.Persistence.LoadingInitializerAttribute()]
		public SnoopingPlugin() {
		}
		
		private new SnoopingPluginDef Def {
			get {
				return ((SnoopingPluginDef)(this.Def));
			}
		}
		
		[SteamEngine.Persistence.SaveAttribute()]
		public override void Save(SaveStream output) {
			if ((this.snoopedBackpacks != null)) {
				output.WriteValue("snoopedBackpacks", this.snoopedBackpacks);
			}
			base.Save(output);
		}
		
		private void DelayedLoad_SnoopedBackpacks(object resolvedObject, string filename, int line) {
			this.snoopedBackpacks = ((LinkedList<Container>)(resolvedObject));
		}
		
		[SteamEngine.Persistence.LoadLineAttribute()]
		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {

				case "snoopedbackpacks":
			SteamEngine.Persistence.ObjectSaver.Load(valueString, new SteamEngine.Persistence.LoadObject(this.DelayedLoad_SnoopedBackpacks), filename, line);
					break;

				default:

			base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}
	}
}