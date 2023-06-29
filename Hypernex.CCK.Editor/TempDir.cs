using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hypernex.CCK.Editor
{
    public class TempDir : IDisposable
    {
        private const string TEMP_FOLDER = "Hypernex.CCK.Temp";
        private const string ASSETS_TEMP_FOLDER = "Assets/Hypernex.CCK/Temp";

        private static readonly Dictionary<string, TempDir> TempDirInstances = new Dictionary<string, TempDir>();

        public static byte[] GetByteFromString(string s) => Encoding.UTF8.GetBytes(s);

        private bool inAssets;
        private string guid;
        private string path;

        public TempDir(bool inAssets = false)
        {
            this.inAssets = inAssets;
            if (inAssets)
            {
                if (!Directory.Exists(ASSETS_TEMP_FOLDER))
                    Directory.CreateDirectory(ASSETS_TEMP_FOLDER);
                guid = Guid.NewGuid().ToString();
                while(TempDirInstances.ContainsKey(guid))
                    guid = Guid.NewGuid().ToString();
                path = Path.Combine(ASSETS_TEMP_FOLDER, guid);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            else
            {
                if (!Directory.Exists(TEMP_FOLDER))
                    Directory.CreateDirectory(TEMP_FOLDER);
                guid = Guid.NewGuid().ToString();
                while(TempDirInstances.ContainsKey(guid))
                    guid = Guid.NewGuid().ToString();
                path = Path.Combine(TEMP_FOLDER, guid);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            /*if (!Directory.Exists(TEMP_FOLDER))
                Directory.CreateDirectory(TEMP_FOLDER);
            guid = Guid.NewGuid().ToString();
            while(TempDirInstances.ContainsKey(guid))
                guid = Guid.NewGuid().ToString();
            path = Path.Combine(TEMP_FOLDER, guid);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            TempDirInstances.Add(guid, this);*/
        }

        public string GetPath() => path;

        public void CreateChildDirectory(string name)
        {
            string p = Path.Combine(path, name);
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);
        }

        public FileStream CreateFile(string file, byte[] data)
        {
            File.WriteAllBytes(Path.Combine(path, file), data);
            FileStream s = new FileStream(Path.Combine(path, file), FileMode.Open, FileAccess.ReadWrite,
                FileShare.Delete | FileShare.ReadWrite);
            return s;
        }

        public void Dispose()
        {
            if (inAssets)
            {
                string n = Path.Combine(ASSETS_TEMP_FOLDER, guid + ".meta");
                if(File.Exists(n))
                    File.Delete(n);
            }
            if(Directory.Exists(path))
                Directory.Delete(path, true);
            TempDirInstances.Remove(guid);
        }
    }
}