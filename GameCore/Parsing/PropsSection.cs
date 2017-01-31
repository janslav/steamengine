using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shielded;
using SteamEngine.Common;

#warning format this
namespace SteamEngine.Parsing
{
    public class PropsSection
    {
        private readonly ShieldedDictNc<string, PropsLine> props; //table of PropsLines
        private readonly ShieldedSeq<TriggerSection> triggerSections;//list of TriggerSections
        private readonly Shielded<string> headerName;

        internal PropsSection(string filename, string type, string name, int line, string comment,
            IEnumerable<KeyValuePair<string, PropsLine>> propLines, IEnumerable<TriggerSection> sections) {
            this.Filename = filename;
            this.HeaderType = type;
            this.headerName =new Shielded<string>(initial: name);
            this.HeaderLine = line;
            this.HeaderComment = comment;
            this.props = new ShieldedDictNc<string, PropsLine>(items: propLines, comparer: StringComparer.OrdinalIgnoreCase);
            this.triggerSections = new ShieldedSeq<TriggerSection>(items: sections.ToArray());
        }

        public string HeaderComment { get; }

        public string HeaderType { get; }

        public string HeaderName
        {
            get { return this.headerName.Value; }
            set { this.headerName.Value = value; }
        }

        public int HeaderLine { get; }

        public string Filename { get; }

        public TriggerSection GetTrigger(int index) {
            return this.triggerSections[index];
        }

        public TriggerSection GetTrigger(string name) {
            foreach (var s in this.triggerSections) {
                if (StringComparer.OrdinalIgnoreCase.Equals(name, s.TriggerName)) {
                    return s;
                }
            }
            return null;
        }

        public TriggerSection PopTrigger(string name) {
            int i = 0, n = this.triggerSections.Count;
            TriggerSection s = null;
            for (; i < n; i++) {
                s = this.triggerSections[i];
                if (StringComparer.OrdinalIgnoreCase.Equals(name, s.TriggerName)) {
                    n = -1;
                    break;
                }
            }
            if (n == -1) {
                this.triggerSections.RemoveAt(i);
                return s;
            }
            return null;
        }

        public int TriggerCount {
            get {
                return this.triggerSections.Count;
            }
        }

        public PropsLine TryPopPropsLine(string name) {
            PropsLine line;
            if (this.props.TryGetValue(name, out line)) {
                this.props.Remove(name);
            }
            return line;
        }

        public PropsLine PopPropsLine(string name) {
            PropsLine line;
            if (this.props.TryGetValue(name, out line)) {
                this.props.Remove(name);
            } else {
                throw new SEException(LogStr.FileLine(this.Filename, this.HeaderLine) + "There is no '" + name + "' line!");
            }
            return line;
        }

        public ICollection<PropsLine> PropsLines {
            get {
                return this.props.Values;
            }
        }

        public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, 
                "[{0} {1}]", this.HeaderType, this.HeaderName);
        }
    }
}