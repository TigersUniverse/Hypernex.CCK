using System;
using Hypernex.Networking.Messages.Data;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public class OfflineNetworkedObject
    {
        public OfflineNetworkedObject()
        {
            throw new Exception("Cannot instance OfflineNetworkedObject");
        }

        private NetworkedObject networkedObject;

        internal OfflineNetworkedObject(NetworkedObject networkedObject) => this.networkedObject = networkedObject;
    
        public string ObjectLocation => networkedObject.ObjectLocation;
        public bool IgnoreObjectLocation => networkedObject.IgnoreObjectLocation;
        public float3 Position => networkedObject.Position;
        public float4 Rotation => networkedObject.Rotation;
        public float3 Size => networkedObject.Size;
    }
}