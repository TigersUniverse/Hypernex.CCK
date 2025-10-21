using System;
using System.Collections.Generic;
using Hypernex.Networking.Server.SandboxedClasses.Handlers;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public class Instance
    {
        private HypernexInstance _instance;
        private Dictionary<string, object> _handlers = new Dictionary<string, object>
        {
            ["http"] = new HTTP()
        };

        public Instance()
        {
            throw new Exception("Cannot instantiate Instance!");
        }

        public object GetHandler(string name)
        {
            string l = name.ToLower();
            if (!_handlers.ContainsKey(l)) return null;
            return _handlers[l];
        }

        internal Instance(HypernexInstance instance, ScriptEvents s, ServerNetworkEvent sne)
        {
            _instance = instance;
            _handlers.Add("events", s);
            _handlers.Add("network", sne);
        }

        public string[] UserIds => _instance.ConnectedClients.ToArray();
        public string InstanceCreatorId => _instance.InstanceCreatorId;
        public string HostId => _instance.HostId;
    }
}