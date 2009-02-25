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
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.LScript;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class PlayerDef {
	}

	[ViewableClass]
	public partial class Player : Character {

		public short Vit {
			get {
				return this.vitality;
			}
			set {
				if (value != this.vitality) {
					CharSyncQueue.AboutToChangeHitpoints(this);
					CharSyncQueue.AboutToChangeStamina(this);
					this.vitality = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Hits, this.vitality, this.HitsRegenSpeed);
					RegenerationPlugin.TryInstallPlugin(this, this.Stam, this.vitality, this.StamRegenSpeed);
				}
			}
		}

		public override short MaxHits {
			get {
				return this.vitality;
			}
			set {
				this.Vit = value; //or should we throw exception?
			}
		}

		public override short MaxMana {
			get {
				return this.Int;
			}
			set {
				this.Int = value; //or should we throw exception?
			}
		}

		public override short MaxStam {
			get {
				return this.vitality;
			}
			set {
				this.Vit = value; //or should we throw exception?
			}
		}

		public override short Int {
			get {
				return base.Int;
			}
			set {
				CharSyncQueue.AboutToChangeMana(this);

				base.Int = value;

				//regeneration...
				RegenerationPlugin.TryInstallPlugin(this, this.Mana, this.MaxMana, this.ManaRegenSpeed);

				//meditation finish
				if (this.Mana >= MaxMana) {
					this.DeletePlugin(MeditationPlugin.meditationPluginKey);
				}
			}
		}

		public Globals serv() {
			return Globals.Instance;
		}

		public void Where() {
			Message("You are at " + P());
			Message("You are in " + Region.HierarchyName);
		}

		public void Password(string newpass) {
			AbstractAccount acc = Account;
			if (acc != null) {
				acc.Password(newpass);
			}
		}

		[Button("Skills")]
		public void ShowSkills() {
			GameState state = Globals.SrcGameState;
			if (state != null) {
				this.ShowSkillsTo(state.Conn, state);
			}
		}

		public void AllShow() {
			GameState state = this.GameState;
			if (state != null) {
				if (state.AllShow) {
					state.AllShow = false;
					Globals.SrcWriteLine("AllShow off.");
				} else {
					state.AllShow = true;
					Globals.SrcWriteLine("AllShow on.");
				}
			}
		}

		public void AllShow(bool value) {
			GameState state = this.GameState;
			if (state != null) {
				state.AllShow = value;
				Globals.SrcWriteLine("AllShow " + (value ? "on" : "off"));
			}
		}

		[Summary("Display (or remove) a quest arrow pointing towards the specified coordinates")]
		public void QuestArrow(bool active, ushort xPos, ushort yPos) {
			QuestArrowOutPacket qaop = Pool<QuestArrowOutPacket>.Acquire();
			qaop.Prepare(active, xPos, yPos);
			this.GameState.Conn.SendSinglePacket(qaop);
		}

		[Summary("Display (or remove) a quest arrow pointing towards the specified location")]
		public void QuestArrow(bool active, IPoint2D position) {
			QuestArrowOutPacket qaop = Pool<QuestArrowOutPacket>.Acquire();
			qaop.Prepare(active, position);
			this.GameState.Conn.SendSinglePacket(qaop);
		}

		#region Profession
		public ProfessionPlugin Profession {
			get {
				return (ProfessionPlugin) GetPlugin(ProfessionPlugin.professionKey);
			}
		}

		[Remark("Trigger method called when the profession is assigned to the player. " +
				"It initializes all skills' min and max values.")]
		public virtual void On_ProfessionAssign(ProfessionDef profDef) {
			//this trigger is called after the profession has been assigned, so we can use it now
			Profession.Init();
		}

		public virtual void On_ProfessionUnAssign(ProfessionDef profDef) {

		}
		#endregion Profession

		[Summary("Every step is monitored by the ScriptSector system")]
		public override bool On_Step(byte direction, bool running) {
			ScriptSector.AddTrackingStep(this, direction);
			return base.On_Step(direction, running);
		}

		private static TimerKey charLingeringTimerTK = TimerKey.Get("_charLingeringTimer_");
		public override void On_LogOut() {
			//TODO: In safe/nonsafe areas, settings, etc.

			//this.AddTimer(charLingeringTimerTK, new CharLingeringTimer()).DueInSeconds = 5;

			//if (DbManager.Config.useDb) {
			//    DbMethods.loginLogs.GameLogout(this.Conn);
			//    Logger.WriteDebug(ScriptUtil.GetLogString(this.Conn, "Logged out"));
			//} else {
			//    Console.WriteLine(ScriptUtil.GetLogString(this.Conn, "Logged out"));
			//}

			base.On_LogOut();
		}

		public override bool On_LogIn() {
			bool stopLogin = base.On_LogIn();
			if (!stopLogin) {
				//if (DbManager.Config.useDb) {
				//    DbMethods.loginLogs.GameLogin(this.Conn);
				//    Logger.WriteDebug(ScriptUtil.GetLogString(this.Conn, "Logged in"));
				//} else {
				//    Console.WriteLine(ScriptUtil.GetLogString(this.Conn, "Logged in"));
				//}
			}
			return stopLogin;
		}

		//[Summary("Add a profession-powered skill checkings. Check if the skill value doesn't go "+
		//        "above the maximal limit")]
		//public override void On_SkillChange(Skill skill, ushort oldValue) {
		//    short skillMaxValue = (short)(profession.ProfessionDef.MaxSkill(skill.Id) + GetSkillMaxModifier(skill.Name));
		//    if(skill.RealValue > skillMaxValue) {
		//        skill.RealValue = (ushort)Math.Min((ushort)0, skillMaxValue); //don't allow to go over maximum or under 0
		//    }

		//    base.On_SkillChange(skill, oldValue);
		//}

		[Summary("@Death trigger - check if none of the skills goes above the maximal limit")]
		public override void On_Death(Character killedBy) {
			CheckSkillMaximums();
			base.On_Death(killedBy);
		}

		[Summary("Return the modifier of the skill's maximum value (it is normally determined by " +
				"selected profession, but can be altered e.g. by magic items etc...)")]
		internal short GetSkillMaxModifier(SkillName name) {
			if (maxSkillModifier != null) {
				short outVal = 0;
				if (maxSkillModifier.TryGetValue(name, out outVal)) {
					return outVal;//skill is modified somehow
				} else {
					return 0; //skill is not modified
				}
			}
			return 0;
		}

		[Summary("Set the special modifier to the given skill's maximum value (the player will be " +
				"allowed to go over his normal profession's maximum for this particular skill")]
		internal void SetSkillMaxModifier(SkillName name, short value) {
			if (maxSkillModifier == null) {
				maxSkillModifier = new Dictionary<SkillName, short>();
			}
			if (value == 0) {//remove the value from the dictionary
				maxSkillModifier.Remove(name);
				if (maxSkillModifier.Keys.Count == 0) {
					maxSkillModifier = null; //no modifiers left, release the reference
				}
			} else {
				maxSkillModifier[name] = value;
			}
		}

		private void CheckSkillMaximums() {
			//check all skills and fix any possible overlaps
			short skillMaxValue;
			foreach (ISkill skl in this.Skills) {
				skillMaxValue = (short) (profession.ProfessionDef.MaxSkill(skl.Id) + GetSkillMaxModifier(((Skill) skl).Name));
				if (skl.RealValue > skillMaxValue) {
					skl.RealValue = (ushort) Math.Min((ushort) 0, skillMaxValue); //don't allow to go over maximum or under 0
				}
			}
		}


		[SaveableClass]
		public class CharLingeringTimer : BoundTimer {
			[LoadingInitializer]
			public CharLingeringTimer() {
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Logger.WriteDebug("CharLingeringTimer OnTimeout on " + this.Cont);
				Character self = cont as Character;
				if (self != null) {
					if (self.IsLingering) {
						self.Disconnect();
					}
				}
			}
		}

		public void Target(AbstractTargetDef def) {
			def.Assign(this);
		}

		public void Target(AbstractTargetDef def, object parameter) {
			def.Assign(this, parameter);
		}

		#region Messaging
		public void DelayedMessage(string text) {
			DelayedMessage(null, text);
		}

		[Summary("Send a delayed message to the player. Specify the sender of the message (null means " +
				"that the message comes from the system)")]
		public void DelayedMessage(AbstractCharacter sender, string text) {
			if (sender == null) {
				//add a new message without the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(text));
				//send the message also to the client
				SysMessage("System: " + text);
			} else {
				//add a new message with the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(sender, text));
				//send the message also to the client
				SysMessage(sender.Name + ": " + text);
			}
			InfoMessage("Nova zprava, celkem neprectenych: " + MsgsBoard.CountUnread(this));
		}

		[Summary("Send a delayed message to the player. Specify the sender of the message (null means " +
				"that the message comes from the system), we can also specify a message custom color here")]
		public void DelayedMessage(AbstractCharacter sender, string text, Hues hue) {
			if (sender == null) {
				//add a new message without the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(null, text, hue));
			} else {
				//add a new message with the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(sender, text, hue));
			}
			//send the message also to the client
			SysMessage(text, (int) hue);
			InfoMessage("Nova zprava, celkem neprectenych: " + MsgsBoard.CountUnread(this));
		}

		public void DelayedRedMessage(string text) {
			DelayedRedMessage(null, text);
		}

		[Summary("Send a delayed red message to the player. Specify the sender of the message (null means " +
				"that the message comes from the system)")]
		public void DelayedRedMessage(AbstractCharacter sender, string text) {
			if (sender == null) {
				//add a new red message without the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(text, true));
			} else {
				//add a new red message with the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(sender, text, true));
			}
			//send the message also to the client
			RedMessage(text);
			InfoMessage("Nova vyznamna zprava, celkem neprectenych: " + MsgsBoard.CountUnread(this));
		}
		#endregion Messaging

		public void Add(int model) {
			this.Add(ThingDef.Get(model));
		}

		public void Add(ThingDef addedDef) {
			GameState state = this.GameState;
			if (state != null) {
				if (addedDef != null) {
					string name = addedDef.Name;
					this.SysMessage("Kam chceš umístit '" + name + "' ?");

					ItemDef idef = addedDef as ItemDef;
					if ((idef != null) && (idef.MultiData != null)) {
						state.TargetForMultis(idef.Model, this.Add_OnTargon, null, addedDef);
					} else {
						state.Target(true, this.Add_OnTargon, null, addedDef);
					}
				} else {
					this.SysMessage("Nenalezen odpovidajici ThingDef.");
				}
			}
		}

		private void Add_OnTargon(GameState state, IPoint3D getback, object parameter) {
			ThingDef addedDef = parameter as ThingDef;
			if (addedDef == null) {
				return;
			}
			Item targettedItem = getback as Item;
			if (targettedItem != null) {
				getback = targettedItem.TopObj();
			}
			addedDef.Create(getback.X, getback.Y, getback.Z, this.M);
		}

		[Summary("The crafting main method. Tries to create the given Item(Def) in a requested quantity")]
		public void Make(ItemDef what, int howMuch) {
			ResourcesList skillMake = what.SkillMake;
			ResourcesList resources = what.Resources;

			IResourceListItem missingRes;
			int cntr = 0;
			bool finishedOK = true;
			for (; cntr < howMuch; cntr++) {
				//check the requirements first
				if (!skillMake.HasResourcesPresent(this, ResourcesLocality.BackpackAndLayers, out missingRes)) {
					ResourcesList.SendResourceMissingMsg(this, missingRes);
					finishedOK = false;
					break;
				}

				//check the itemd making probability, count the success etc

				//now the resources
				if (!resources.ConsumeResourcesOnce(this, ResourcesLocality.BackpackAndLayers, out missingRes)) {
					ResourcesList.SendResourceMissingMsg(this, missingRes);
					finishedOK = false;
					break;
				}

				//get the greatest necessary skill to make this item
				SkillResource greatestSkill = ResourcesList.GetResource<SkillResource,IResourceListItemNonMultiplicable>(skillMake.NonMultiplicablesSublist, null);
				//vsechno tohleto musim vrazit samozrejme do skillu (carpentry, tinkering, tailoring, blacksmithy, bowcraft!!!!)
				//a zde podle vyse nejvyssiho skillu pak vybirat co hodlam prave vyrabet...
				//=>neni potreba ten sorting podle skillu bo vyroba pujde logicky z craftmenu prislusne sekce odkud se bude 
				//spoustet prislusny skill (pak se vybere opravdu ten GetResourceTyped toho ktereho skillu a podle nej se rozhodneme co dal)

				//finally create the item
				//zjistit vzdalenost od receiving containeru (a jestli je to ten samy mapplan) a podle toho to dat 
				//bud do nej nebo do backpacku
				Container targetCont = this.ReceivingContainer;
				if ((targetCont != this.BackpackAsContainer) && (Point2D.GetSimpleDistance(this, targetCont) > Globals.reachRange || this.M != targetCont.M)) {
					//the receivingContainer is not our backpack and we are too far or in another mapplane
					targetCont = this.BackpackAsContainer;
				}

			}
			if(finishedOK) {
				this.SysMessage("Výroba ukonèena, vyrobeno " + cntr + " " + ((cntr > 1) ? what.PluralName : what.Name));
			} else {
				this.RedMessage("Výroba ukonèena pøedèasnì, vyrobeno " + cntr + " z požadovaného poètu " + howMuch + " " + ((cntr > 1) ? what.PluralName : what.Name));
			}
		}

		public Container ReceivingContainer {
			get {
				if (receivingContainer == null) {//set the default
					receivingContainer = this.BackpackAsContainer;
				}
				return receivingContainer;
			}

			set {
				receivingContainer = value;
			}
		}
	}
}