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
using SteamEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Dialogs {

	[Remark("Dialog listing all characters of the account")]
	public class D_Acc_Characters : CompiledGump {
		internal static readonly TagKey accountTK = TagKey.Get("__account_with_chars_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			AbstractAccount acc = (AbstractAccount)args.GetTag(D_Acc_Characters.accountTK);
			List<AbstractCharacter> chars = acc.Characters;
			
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(600);
			dlg.SetLocation(50, 600);

			dlg.AddTable(new GUTATable(1, 0, ButtonFactory.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = TextFactory.CreateHeadline("Seznam postav na accountu "+acc.Name);
			dlg.LastTable[0, 1] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonCross, 0);
			dlg.MakeLastTableTransparent();

			//seznam charu s tlacitkem pro info
			dlg.AddTable(new GUTATable(1, ButtonFactory.D_BUTTON_WIDTH, 160, 130, 110, 40, 0));
			dlg.LastTable[0, 0] = TextFactory.CreateLabel("Info");
			dlg.LastTable[0, 1] = TextFactory.CreateLabel("Jméno");
			dlg.LastTable[0, 2] = TextFactory.CreateLabel("Profese");
			dlg.LastTable[0, 3] = TextFactory.CreateLabel("Rasa");
			dlg.LastTable[0, 4] = TextFactory.CreateLabel("Level");
			dlg.LastTable[0, 5] = TextFactory.CreateLabel("Pozice");
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable((int)AbstractAccount.maxCharactersPerGameAccount));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			foreach(AbstractCharacter oneChar in chars) {
				if(oneChar == null) {
					continue;
				}
				Player castChar = (Player)oneChar;
				///TODO poresit barvy podle prislusnosti ke strane!
				Hues color = Hues.WriteColor;
				dlg.LastTable[rowCntr, 0] = ButtonFactory.CreateButton(LeafComponentTypes.ButtonPaper, rowCntr + 10); //char info
				dlg.LastTable[rowCntr, 1] = TextFactory.CreateText(color, castChar.Name); //plr name
				dlg.LastTable[rowCntr, 2] = TextFactory.CreateText(color, "TODO-profese"); //profese
				dlg.LastTable[rowCntr, 3] = TextFactory.CreateText(color, "TODO-rasa"); //rasa
				dlg.LastTable[rowCntr, 4] = TextFactory.CreateText(color, "TODO-level"); //level
				dlg.LastTable[rowCntr, 5] = TextFactory.CreateText(color, castChar.P().ToNormalString()); //pozice

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			dlg.WriteOut();
		}

		public override void OnResponse(GumpInstance gi, GumpResponse gr, DialogArgs args) {
			AbstractAccount acc = (AbstractAccount)args.GetTag(D_Acc_Characters.accountTK);
			
			if(gr.pressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch(gr.pressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;					
				}
			} else { //skutecna tlacitka z radku
				//zjistime kterej cudlik z kteryho radku byl zmacknut
				int row = (int)(gr.pressedButton - 10);
				List<AbstractCharacter> chars = acc.Characters;
				Character oneChar = (Character)chars[row];
				DialogArgs newArgs = new DialogArgs(0, 0); //button, fields paging
				newArgs.SetTag(D_Info.infoizedTargTK, oneChar);
				GumpInstance newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, newArgs);
				//ulozime dialog pro navrat
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}

		[Remark("Display a list of characters on the account list. "+
				"Usage - .x AccChars. or .AccChars('accname')")]
		[SteamFunction]
		public static void AccChars(AbstractCharacter target, ScriptArgs text) {
			DialogArgs newArgs = new DialogArgs();				
			if(text.argv == null || text.argv.Length == 0) {
				newArgs.SetTag(D_Acc_Characters.accountTK, target.Account);
				Globals.SrcCharacter.Dialog(SingletonScript<D_Acc_Characters>.Instance, newArgs);
			} else {
				string accName = (String)text.argv[0];
				AbstractAccount acc = AbstractAccount.Get(accName);
				if(acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem "+accName+" neexistuje!",(int)Hues.Red);
					return;
				}
				newArgs.SetTag(D_Acc_Characters.accountTK, acc);
				Globals.SrcCharacter.Dialog(SingletonScript<D_Acc_Characters>.Instance, newArgs);
			}
		}
	}	
}