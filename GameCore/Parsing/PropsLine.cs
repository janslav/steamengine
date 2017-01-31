#warning format this
namespace SteamEngine.Parsing
{
    public class PropsLine {
        public PropsLine(string name, string value, int line, string comment) {
            this.Name = name;
            this.Value = value;
            this.Line = line;
            this.Comment = comment;
        }

        public string Comment { get; }

        public string Name { get; }

        public string Value { get; }

        public int Line { get; }

        public override string ToString() {
            return this.Name + " = " + this.Value;
        }
    }
}