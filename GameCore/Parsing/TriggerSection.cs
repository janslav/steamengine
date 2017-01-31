using System.Text;

#warning format this
namespace SteamEngine.Parsing
{
    public class TriggerSection {
        public TriggerSection(string filename, int startline, string key, string name, string comment, string code) {
            this.Filename = filename;
            this.TriggerKey = key;
            this.TriggerName = name;
            this.StartLine = startline;
            this.Code = code;
            this.TriggerComment = comment;
        }

        public string TriggerComment { get; }

        //"on", "ontrigger", "onbutton", or just ""
        public string TriggerKey { get; }

        //"create", etc
        public string TriggerName { get; }

        public int StartLine { get; }

        public string Filename { get; }

        public string Code { get; }

        public override string ToString() {
            return string.Concat(this.TriggerKey, "=@", this.TriggerName);
        }
    }
}