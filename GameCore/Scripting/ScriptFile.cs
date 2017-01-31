using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SteamEngine.Scripting
{
#warning format this
    internal class ScriptFile
    {
        private readonly FileInfo file;
        private readonly List<IUnloadable> scripts = new List<IUnloadable>();

        private FileAttributes attribs;
        private DateTime time;
        private long length;

        internal ScriptFile(FileInfo file)
        {
            this.file = file;
            this.attribs = file.Attributes;
            this.time = file.LastWriteTime;
            this.length = file.Length;
        }

        internal long Length => this.length;

        internal bool Exists => this.file.Exists;

        internal string FullName => this.file.FullName;

        internal string Name => this.file.Name;

        internal void Add(IUnloadable script)
        {
            SeShield.AssertNotInTransaction();
            this.scripts.Add(script);
        }

        internal void Unload()
        {
            SeShield.AssertNotInTransaction();
            foreach (var script in this.scripts)
            {
                script.Unload();
            }
            this.scripts.Clear();
        }

        internal bool CheckChanged()
        {
            SeShield.AssertNotInTransaction();
            this.file.Refresh();
            if (this.file.Exists)
            {
                if (this.attribs == this.file.Attributes
                    && this.time == this.file.LastWriteTime
                    && this.length == this.file.Length)
                    return false;

                this.attribs = this.file.Attributes;
                this.time = this.file.LastWriteTime;
                this.length = this.file.Length;
            }

            return true;
        }

        internal StreamReader OpenText()
        {
            return new StreamReader(this.file.FullName, Encoding.Default);

            //var bytes = File.ReadAllBytes(file.FullName);

            //return new StreamReader(new MemoryStream(bytes), Encoding.Default);
        }
    }
}