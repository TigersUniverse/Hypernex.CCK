using System;

namespace Hypernex.CCK
{
    public abstract class Logger
    {
        public static Logger CurrentLogger { get; private set; }

        public abstract void Debug(object o);
        public abstract void Log(object o);
        public abstract void Warn(object o);
        public abstract void Error(object o);
        public abstract void Critical(Exception e);
    
        public void SetLogger() => CurrentLogger = this;
    }
}
