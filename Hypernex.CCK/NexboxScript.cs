using System;

namespace Hypernex.CCK
{
    [Serializable]
    public class NexboxScript
    {
        public string Name;
        public NexboxLanguage Language;
        public string Script;

        public NexboxScript(){}
        public NexboxScript(NexboxLanguage language, string script)
        {
            Language = language;
            Script = script;
        }
        
        public string GetExtensionFromLanguage()
        {
            switch (Language)
            {
                case NexboxLanguage.JavaScript:
                    return ".js";
                case NexboxLanguage.Lua:
                    return ".lua";
            }
            return String.Empty;
        }
        
        public static string GetExtensionFromLanguage(NexboxLanguage language)
        {
            switch (language)
            {
                case NexboxLanguage.JavaScript:
                    return ".js";
                case NexboxLanguage.Lua:
                    return ".lua";
            }
            return String.Empty;
        }
    }
}