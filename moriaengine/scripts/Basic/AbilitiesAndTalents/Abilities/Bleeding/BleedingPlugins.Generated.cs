//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4927
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SteamEngine.CompiledScripts {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using SteamEngine;
	using SteamEngine.Timers;
	using SteamEngine.Persistence;
	using SteamEngine.Common;
	
	
	public partial class BleedingStrikePluginDef : EffectDurationPluginDef {
		
		public BleedingStrikePluginDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
		}
		
		protected override SteamEngine.Plugin CreateImpl() {
			return new BleedingStrikePlugin();
		}
		
		public new static void Bootstrap() {
			SteamEngine.PluginDef.RegisterPluginDef(typeof(BleedingStrikePluginDef), typeof(BleedingStrikePlugin));
		}
	}
	
	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class BleedingStrikePlugin : EffectDurationPlugin {
		
		[SteamEngine.DeepCopyImplementationAttribute()]
		public BleedingStrikePlugin(BleedingStrikePlugin copyFrom) : 
				base(copyFrom) {
		}
		
		[SteamEngine.Persistence.LoadingInitializerAttribute()]
		public BleedingStrikePlugin() {
		}
		
		public new BleedingStrikePluginDef TypeDef {
			get {
				return ((BleedingStrikePluginDef)(base.Def));
			}
		}
	}
	
	public partial class BleedingEffectPluginDef : FadingEffectDurationPluginDef {
		
		public BleedingEffectPluginDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
		}
		
		protected override SteamEngine.Plugin CreateImpl() {
			return new BleedingEffectPlugin();
		}
		
		public new static void Bootstrap() {
			SteamEngine.PluginDef.RegisterPluginDef(typeof(BleedingEffectPluginDef), typeof(BleedingEffectPlugin));
		}
	}
	
	[SteamEngine.DeepCopyableClassAttribute()]
	[SteamEngine.Persistence.SaveableClassAttribute()]
	public partial class BleedingEffectPlugin : FadingEffectDurationPlugin {
		
		[SteamEngine.DeepCopyImplementationAttribute()]
		public BleedingEffectPlugin(BleedingEffectPlugin copyFrom) : 
				base(copyFrom) {
		}
		
		[SteamEngine.Persistence.LoadingInitializerAttribute()]
		public BleedingEffectPlugin() {
		}
		
		public new BleedingEffectPluginDef TypeDef {
			get {
				return ((BleedingEffectPluginDef)(base.Def));
			}
		}
	}
}
