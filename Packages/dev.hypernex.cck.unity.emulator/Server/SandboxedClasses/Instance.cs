using System;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public class Instance
    {
        private HypernexInstance _instance;

        public Instance()
        {
            throw new Exception("Cannot instantiate NetPlayers!");
        }

        internal Instance(HypernexInstance instance) => _instance = instance;

        public string[] UserIds => _instance.ConnectedClients.ToArray();
        public string InstanceCreatorId => _instance.InstanceCreatorId;
        public string HostId => _instance.HostId;
    }
}