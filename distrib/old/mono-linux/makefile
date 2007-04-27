#
# General settings
#
MCS=mcs
MCS_OPTIONS=-d:MONO -warn:1
BIN_PATH=./../../bin

#
# Common library settings
#
COMMON_OPTIONS=-r:System.Drawing -r:$(BIN_PATH)/SteamDoc.dll -d:TRACE
DEBUG_COMMON_OPTIONS=$(COMMON_OPTIONS) -debug+ -d:DEBUG
SANE_COMMON_OPTIONS=$(COMMON_OPTIONS) -debug+ -d:TRACE -d:OPTIMIZED
COMMON_PATH=./../../Common
COMMON_OUTPUT=$(BIN_PATH)/Optimized_Common.dll
DEBUG_COMMON_OUTPUT=$(BIN_PATH)/Debug_Common.dll
SANE_COMMON_OUTPUT=$(BIN_PATH)/Sane_Common.dll
COMMON_SOURCES=$(COMMON_PATH)/common.cs $(COMMON_PATH)/ConvertTools.cs $(COMMON_PATH)/LogStr.cs \
$(COMMON_PATH)/IniHandler.cs $(COMMON_PATH)/Sanity.cs $(COMMON_PATH)/HighPerformanceTimer.cs \
$(COMMON_PATH)/ConAttrs.cs $(COMMON_PATH)/Exceptions.cs $(COMMON_PATH)/UberEnumerator.cs

#
# Core settings
#
CORE_OPTIONS=-r:$(COMMON_OUTPUT) -r:System.Drawing -r:$(BIN_PATH)/SteamDoc.dll -d:TRACE
DEBUG_CORE_OPTIONS=-r $(DEBUG_COMMON_OUTPUT) -r:$(BIN_PATH)/SteamDoc.dll -r:System.Drawing -d:TRACE -d:DEBUG -debug+
SANE_CORE_OPTIONS=-r $(SANE_COMMON_OUTPUT) -r:$(BIN_PATH)/SteamDoc.dll -r:System.Drawing -d:TRACE -d:OPTIMIZED
CORE_PATH=./../../src
CORE_OUTPUT=$(BIN_PATH)/Optimized_Steamengine.exe
DEBUG_CORE_OUTPUT=$(BIN_PATH)/Debug_Steamengine.exe
SANE_CORE_OUTPUT=$(BIN_PATH)/Sane_Steamengine.exe
# files in '/src/'
MAIN_SOURCES=$(CORE_PATH)/AbstractKey.cs $(CORE_PATH)/AssemblyInfo.cs \
$(CORE_PATH)/Commands.cs $(CORE_PATH)/Conn.cs $(CORE_PATH)/ConsConn.cs $(CORE_PATH)/Constant.cs \
$(CORE_PATH)/DelayedResolver.cs $(CORE_PATH)/DispidInfo.cs $(CORE_PATH)/Enums.cs \
$(CORE_PATH)/FastWalk.cs $(CORE_PATH)/FieldValue.cs $(CORE_PATH)/GameAccount.cs $(CORE_PATH)/GameConn.cs \
$(CORE_PATH)/GenericMulReader.cs $(CORE_PATH)/Globals.cs $(CORE_PATH)/AbstractCharacter.cs $(CORE_PATH)/CharDef.cs \
$(CORE_PATH)/Item.cs $(CORE_PATH)/ItemDef.cs $(CORE_PATH)/LayerDefs.cs $(CORE_PATH)/Logger.cs \
$(CORE_PATH)/Main.cs $(CORE_PATH)/Map.cs $(CORE_PATH)/MemberWrapper.cs $(CORE_PATH)/MemoryFile.cs \
$(CORE_PATH)/MIScriptHolder.cs $(CORE_PATH)/Names.cs $(CORE_PATH)/Point.cs $(CORE_PATH)/PropsFileLoader.cs \
$(CORE_PATH)/Rectangle.cs $(CORE_PATH)/Region.cs $(CORE_PATH)/ScriptArgs.cs $(CORE_PATH)/Script.cs \
$(CORE_PATH)/ScriptFilesCollection.cs $(CORE_PATH)/ScriptHolder.cs $(CORE_PATH)/ScriptLoader.cs $(CORE_PATH)/Sector.cs \
$(CORE_PATH)/SEIniHandler.cs $(CORE_PATH)/Server.cs $(CORE_PATH)/SimulConn.cs \
$(CORE_PATH)/Skills.cs $(CORE_PATH)/Static.cs $(CORE_PATH)/StaticSector.cs \
$(CORE_PATH)/TagHolder.cs $(CORE_PATH)/TagKey.cs $(CORE_PATH)/TagMath.cs $(CORE_PATH)/Temporary.cs \
$(CORE_PATH)/Thing.cs $(CORE_PATH)/ThingDef.cs $(CORE_PATH)/ThingDefs.cs $(CORE_PATH)/tiledata.cs \
$(CORE_PATH)/TriggerGroup.cs $(CORE_PATH)/TriggerKey.cs $(CORE_PATH)/TypeInfo.cs $(CORE_PATH)/UIDArray.cs \
$(CORE_PATH)/WorldLoader.cs $(CORE_PATH)/WorldSaver.cs $(CORE_PATH)/GumpBuilder.cs $(CORE_PATH)/GumpDef.cs \
$(CORE_PATH)/CompiledGumpDef.cs $(CORE_PATH)/NetState.cs $(CORE_PATH)/Statistics.cs \
$(CORE_PATH)/UberSectorEnumerator.cs $(CORE_PATH)/TemplateDef.cs $(CORE_PATH)/Sounds.cs $(CORE_PATH)/TestSuite.cs \
$(CORE_PATH)/ClassTemplate.cs $(CORE_PATH)/SoundMul.cs $(CORE_PATH)/CharData.cs $(CORE_PATH)/FastDLL.cs
# files in /src/Timers
TIMERS_SOURCES=$(CORE_PATH)/Timers/FunctionTimer.cs $(CORE_PATH)/Timers/MethodTimer.cs \
$(CORE_PATH)/Timers/PriorityQueue.cs $(CORE_PATH)/Timers/SimpleQueue.cs $(CORE_PATH)/Timers/Timer.cs \
$(CORE_PATH)/Timers/TimerKey.cs $(CORE_PATH)/Timers/TriggerTimer.cs
# files in /src/Ai
AI_SOURCES=$(CORE_PATH)/AI/AI.cs $(CORE_PATH)/AI/PathFinder.cs
# files in /src/Packets
PACKETS_SOURCES=$(CORE_PATH)/Packets/InPackets.cs $(CORE_PATH)/Packets/HighLevelOut.cs $(CORE_PATH)/Packets/Prepared.cs\
$(CORE_PATH)/Packets/LowLevelOut.cs $(CORE_PATH)/Packets/Compression.cs $(CORE_PATH)/Packets/PacketGroup.cs \
$(CORE_PATH)/Packets/PacketBase.cs $(CORE_PATH)/Packets/PacketHandler.cs \
$(CORE_PATH)/Packets/PacketSender.cs $(CORE_PATH)/Packets/PacketStats.cs $(CORE_PATH)/Packets/MediumLevelOut.cs
# files in /src/LScript
LSCRIPT_SOURCES=$(CORE_PATH)/LScript/Exceptions.cs $(CORE_PATH)/LScript/IntrinsicMethods.cs \
$(CORE_PATH)/LScript/LocalVarKey.cs $(CORE_PATH)/LScript/LScriptAnalyzer.cs $(CORE_PATH)/LScript/LScript.cs \
$(CORE_PATH)/LScript/LScriptHolder.cs $(CORE_PATH)/LScript/LScriptParser.cs $(CORE_PATH)/LScript/OpNode_Code.cs \
$(CORE_PATH)/LScript/OpNode.cs $(CORE_PATH)/LScript/OpNode_Final_AddTimer.cs $(CORE_PATH)/LScript/OpNode_Final_ARG.cs \
$(CORE_PATH)/LScript/OpNode_Final_Arguments.cs $(CORE_PATH)/LScript/OpNode_Final_ArrayListIndexer.cs \
$(CORE_PATH)/LScript/OpNode_Final_BinMathOperators.cs $(CORE_PATH)/LScript/OpNode_Final_BinOperators.cs \
$(CORE_PATH)/LScript/OpNode_Final_ComparOperators.cs $(CORE_PATH)/LScript/OpNode_Final_Constructor.cs \
$(CORE_PATH)/LScript/OpNode_Final_Field.cs $(CORE_PATH)/LScript/OpNode_Final_Function.cs \
$(CORE_PATH)/LScript/OpNode_Final_LogicalOp.cs $(CORE_PATH)/LScript/OpNode_Final_Method.cs \
$(CORE_PATH)/LScript/OpNode_Final_Return.cs $(CORE_PATH)/LScript/OpNode_Final_TAG.cs \
$(CORE_PATH)/LScript/OpNode_Final_UnOperators.cs $(CORE_PATH)/LScript/OpNode_Final_VAR.cs \
$(CORE_PATH)/LScript/OpNode_If.cs $(CORE_PATH)/LScript/OpNode_Lazy_AddTimer.cs \
$(CORE_PATH)/LScript/OpNode_Lazy_Arguments.cs $(CORE_PATH)/LScript/OpNode_Lazy_BinOperator.cs \
$(CORE_PATH)/LScript/OpNode_Lazy_EvalExpression.cs $(CORE_PATH)/LScript/OpNode_Lazy_Expression.cs \
$(CORE_PATH)/LScript/OpNode_Lazy_ExpressionChain.cs $(CORE_PATH)/LScript/OpNode_Lazy_Indexer.cs \
$(CORE_PATH)/LScript/OpNode_Lazy_QuotedString.cs $(CORE_PATH)/LScript/OpNode_Lazy_UnOperator.cs \
$(CORE_PATH)/LScript/OpNode_Lazy_VarExpression.cs $(CORE_PATH)/LScript/OpNode_Script.cs \
$(CORE_PATH)/LScript/OpNode_While.cs $(CORE_PATH)/LScript/StrictAnalyzer.cs \
$(CORE_PATH)/LScript/StrictConstants.cs $(CORE_PATH)/LScript/StrictParser.cs $(CORE_PATH)/LScript/StrictTokenizer.cs \
$(CORE_PATH)/LScript/ScriptedGumpDef.cs $(CORE_PATH)/LScript/OpNode_Foreach.cs \
$(CORE_PATH)/LScript/OpNode_Switch.cs
# files in /src/3rdParty/Grammatica/PerCederberg.Grammatica.Parser/
GRAMMATICA_PATH=$(CORE_PATH)/3rdParty/Grammatica/PerCederberg.Grammatica.Parser
GRAMMATICARE_PATH=$(CORE_PATH)/3rdParty/Grammatica/PerCederberg.Grammatica.Parser.RE
GRAMMATICA_SOURCES=$(GRAMMATICA_PATH)/Analyzer.cs $(GRAMMATICA_PATH)/LookAheadSet.cs $(GRAMMATICA_PATH)/Node.cs \
$(GRAMMATICA_PATH)/ParseException.cs $(GRAMMATICA_PATH)/ParserCreationException.cs $(GRAMMATICA_PATH)/Parser.cs \
$(GRAMMATICA_PATH)/ParserLogException.cs $(GRAMMATICA_PATH)/Production.cs \
$(GRAMMATICA_PATH)/ProductionPatternAlternative.cs $(GRAMMATICA_PATH)/ProductionPattern.cs \
$(GRAMMATICA_PATH)/ProductionPatternElement.cs $(GRAMMATICA_PATH)/RecursiveDescentParser.cs \
$(GRAMMATICA_PATH)/Token.cs $(GRAMMATICA_PATH)/Tokenizer.cs $(GRAMMATICA_PATH)/TokenPattern.cs \
$(GRAMMATICARE_PATH)/AlternativeElement.cs $(GRAMMATICARE_PATH)/CombineElement.cs $(GRAMMATICARE_PATH)/Element.cs \
$(GRAMMATICARE_PATH)/CharacterSetElement.cs $(GRAMMATICARE_PATH)/Matcher.cs $(GRAMMATICARE_PATH)/RegExp.cs \
$(GRAMMATICARE_PATH)/RegExpException.cs $(GRAMMATICARE_PATH)/RepeatElement.cs $(GRAMMATICARE_PATH)/StringElement.cs
# other files in src/3rdParty
OTHER_SOURCES=$(CORE_PATH)/JMBucknall.Containers/PriorityQueue.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/ \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/Collection2Base.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/HashSet.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/ICollection2.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/IList2.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/ISet.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/LinkedList.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/LinkedSet.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/List2Base.cs \
$(CORE_PATH)/3rdParty/Foley.Utilities.Collections/SetBase.cs
# files in src/Importer
IMPORTER_SOURCES=$(CORE_PATH)/Importer/ImportedCharDef.cs $(CORE_PATH)/Importer/ImportedRegion.cs \
$(CORE_PATH)/Importer/Imports.cs $(CORE_PATH)/Importer/ImportedDef.cs $(CORE_PATH)/Importer/ImportedItemDef.cs $(CORE_PATH)/Importer/ImportedTemplateDef.cs

CORE_SOURCES=$(PACKETS_SOURCES) $(MAIN_SOURCES) $(TIMERS_SOURCES) $(AI_SOURCES) $(LSCRIPT_SOURCES) \
$(GRAMMATICA_SOURCES) $(IMPORTER_SOURCES)

#
# Console settings
# (NOTE: WinConsole doesn't work under MONO 1.0)
#
WINCONSOLE_OPTIONS=-r:System.Drawing -r:System.Windows.Forms -r:$(BIN_PATH)/SteamDoc.dll -r:$(COMMON_OUTPUT)
WINCONSOLE_PATH=./../../WinConsole
WINCONSOLE_OUTPUT=$(BIN_PATH)/GUI.exe
WINCONSOLE_SOURCES=$(WINCONSOLE_PATH)/AssemblyInfo.cs $(WINCONSOLE_PATH)/SteamConsole.cs \
$(WINCONSOLE_PATH)/WinConsoleForm.cs $(WINCONSOLE_PATH)/ConnectionForm.cs $(WINCONSOLE_PATH)/FileViewForm.cs \
$(WINCONSOLE_PATH)/StyleForm.cs


all: $(CORE_OUTPUT) $(DEBUG_CORE_OUTPUT) $(SANE_CORE_OUTPUT)

common: $(COMMON_OUTPUT) $(DEBUG_COMMON_OUTPUT)

steamengine: $(CORE_OUTPUT) $(DEBUG_CORE_OUTPUT) $(SANE_CORE_OUTPUT)

debug: $(DEBUG_CORE_OUTPUT)

sane: $(SANE_CORE_OUTPUT)

optimized: $(CORE_OUTPUT)

$(SANE_CORE_OUTPUT): $(SANE_COMMON_OUTPUT) $(CORE_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(SANE_CORE_OPTIONS) -target:exe -out:"$(SANE_CORE_OUTPUT)" $(CORE_SOURCES)

$(DEBUG_CORE_OUTPUT): $(DEBUG_COMMON_OUTPUT) $(CORE_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(DEBUG_CORE_OPTIONS) -target:exe -out:"$(DEBUG_CORE_OUTPUT)" $(CORE_SOURCES)

$(CORE_OUTPUT): $(COMMON_OUTPUT) $(CORE_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(CORE_OPTIONS) -target:exe -out:"$(CORE_OUTPUT)" $(CORE_SOURCES)

$(WINCONSOLE_OUTPUT): $(COMMON_OUTPUT) $(WINCONSOLE_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(WINCONSOLE_OPTIONS) -target:exe -out:"$(WINCONSOLE_OUTPUT)" $(WINCONSOLE_SOURCES)
	

$(COMMON_OUTPUT): $(COMMON_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(COMMON_OPTIONS) -target:library -out:"$(COMMON_OUTPUT)" $(COMMON_SOURCES)

$(DEBUG_COMMON_OUTPUT): $(DEBUG_COMMON_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(DEBUG_COMMON_OPTIONS) -target:library -out:"$(DEBUG_COMMON_OUTPUT)" $(COMMON_SOURCES)

$(SANE_COMMON_OUTPUT): $(SANE_COMMON_SOURCES)
	$(MCS) $(MCS_OPTIONS) $(SANE_COMMON_OPTIONS) -target:library -out:"$(SANE_COMMON_OUTPUT)" $(COMMON_SOURCES)
	
clean:
	rm -f $(COMMON_OUTPUT) $(DEBUG_COMMON_OUTPUT) $(CORE_OUTPUT) $(DEBUG_CORE_OUTPUT) $(WINCONSOLE_OUTPUT) \
	$(SANE_COMMON_OUTPUT) $(SANE_CORE_OUTPUT)
