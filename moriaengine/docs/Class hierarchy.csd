<ClassProject>
  <Language>CSharp</Language>
  <Entities>
    <Entity type="CSharpInterface">
      <Name>ITriggerGroupHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">void AddEvent()</Operation>
      <Operation type="CSharpMethod">void RemoveTriggerGroup()</Operation>
      <Operation type="CSharpMethod">void Trigger()</Operation>
      <Operation type="CSharpMethod">void CancellableTrigger()</Operation>
    </Entity>
    <Entity type="CSharpInterface">
      <Name>ITagHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">void SetTag()</Operation>
      <Operation type="CSharpMethod">void GetTag()</Operation>
    </Entity>
    <Entity type="CSharpClass">
      <Name>TagHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public Timer AddTimer()</Operation>
      <Operation type="CSharpMethod">public Timer RemoveTimer()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>GameAccount</Name>
      <Access>Public</Access>
      <Operation type="Property">public string Name { get; }</Operation>
      <Operation type="CSharpMethod">public static void Create()</Operation>
      <Operation type="CSharpMethod">public void Delete()</Operation>
      <Operation type="CSharpMethod">public void Block()</Operation>
      <Operation type="CSharpMethod">public void UnBlock()</Operation>
      <Operation type="Property">public int Plevel { get; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Globals</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public static void Resync()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Region</Name>
      <Access>Public</Access>
      <Operation type="Property">public string Defname { get; }</Operation>
      <Operation type="Property">public Point P { get; set; }</Operation>
      <Operation type="Property">public Region Parent { get; }</Operation>
      <Operation type="CSharpMethod">public void On_Exit()</Operation>
      <Operation type="CSharpMethod">public void On_Enter()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>GumpInstance</Name>
      <Access>Public</Access>
      <Operation type="Property">public int X { get; set; }</Operation>
      <Operation type="Property">public int Y { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Conn</Name>
      <Access>Public</Access>
      <Operation type="Property">public bool IsConnected { get; set; }</Operation>
      <Operation type="Property">public IPAddress IP { get; set; }</Operation>
      <Operation type="CSharpMethod">public void WriteLine()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>FlaggedRegion</Name>
      <Access>Public</Access>
      <Operation type="Property">public int Flags { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Thing</Name>
      <Access>Public</Access>
      <Operation type="Property">public int Uid { get; set; }</Operation>
      <Operation type="Property">public Point4D P { get; set; }</Operation>
      <Operation type="Property">public ThingDef def { get; set; }</Operation>
      <Operation type="Property">public bool IsChar { get; set; }</Operation>
      <Operation type="Property">public bool IsItem { get; set; }</Operation>
      <Operation type="CSharpMethod">public Thing Dupe()</Operation>
      <Operation type="CSharpMethod">public void Delete()</Operation>
      <Modifier>None</Modifier>
    </Entity>
  </Entities>
  <Relations />
  <Positions>
    <Shape>
      <Location left="315" top="61" />
      <Size width="162" height="121" />
    </Shape>
    <Shape>
      <Location left="115" top="61" />
      <Size width="162" height="92" />
    </Shape>
    <Shape>
      <Location left="186" top="212" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="439" top="346" />
      <Size width="162" height="148" />
    </Shape>
    <Shape>
      <Location left="264" top="346" />
      <Size width="162" height="77" />
    </Shape>
    <Shape>
      <Location left="611" top="346" />
      <Size width="162" height="134" />
    </Shape>
    <Shape>
      <Location left="786" top="346" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="964" top="346" />
      <Size width="162" height="105" />
    </Shape>
    <Shape>
      <Location left="611" top="511" />
      <Size width="162" height="77" />
    </Shape>
    <Shape>
      <Location left="50" top="346" />
      <Size width="162" height="216" />
    </Shape>
  </Positions>
</ClassProject>