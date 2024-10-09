namespace Hypernex.CCK
{
    public abstract class HypernexPlugin
    {
        public abstract string PluginName { get; }
        public abstract string PluginCreator { get; }
        public abstract string PluginVersion { get; }
        
        public Logger Logger { get; private set; }
        
        public virtual void OnPluginLoaded(){}
        public virtual void Start(){}
        public virtual void FixedUpdate(){}
        public virtual void Update(){}
        public virtual void LateUpdate(){}
        public virtual void OnApplicationExit(){}
    }
}