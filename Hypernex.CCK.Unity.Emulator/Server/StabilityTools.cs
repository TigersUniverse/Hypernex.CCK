using System.Linq;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;

namespace Hypernex.Networking.Server
{
    internal class StabilityTools
    {
        internal static bool CheckFloats(PlayerObjectUpdate playerObjectUpdate)
        {
            NetworkedObject[] netObjects = playerObjectUpdate.Objects.Values.ToArray();
            bool allowed = true;
            float[] vals = new float[10];
            for (int i = 0; i < netObjects.Length ; i++)
            {
                NetworkedObject networkedObject = netObjects[i];
                vals[0] = networkedObject.Position.x;
                vals[1] = networkedObject.Position.y;
                vals[2] = networkedObject.Position.z;
                vals[3] = networkedObject.Rotation.x;
                vals[4] = networkedObject.Rotation.y;
                vals[5] = networkedObject.Rotation.z;
                vals[6] = networkedObject.Rotation.w;
                vals[7] = networkedObject.Size.x;
                vals[8] = networkedObject.Size.y;
                vals[9] = networkedObject.Size.z;
                for (int j = 0; j < vals.Length; j++)
                {
                    float check = vals[j];
                    switch (check)
                    {
                        case float.NaN:
                        case float.NegativeInfinity:
                        case float.PositiveInfinity:
                        case float.MaxValue:
                        case float.MinValue:
                            allowed = false;
                            break;
                    }
                }
                if(!allowed) break;
            }
            return allowed;
        }
    }
}