using System;
using System.Linq;
using Hypernex.Networking.Messages;
using Nexport;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public class ServerNetworkEvent
    {
        private ScriptHandler _scriptHandler;

        public ServerNetworkEvent() => throw new Exception("Cannot Instantiate ServerNetworkEvent");
        internal ServerNetworkEvent(ScriptHandler scriptHandler) => _scriptHandler = scriptHandler;

        public void SendToClient(string userid, string eventName, object[] data = null,
            MessageChannel messageChannel = MessageChannel.Reliable)
        {
            NetworkedEvent networkedEvent = new NetworkedEvent
            {
                EventName = eventName,
                Data = data?.ToList() ?? Array.Empty<object>().ToList()
            };
            _scriptHandler.Instance.SendMessageToClient(userid, Msg.Serialize(networkedEvent),
                messageChannel);
        }

        public void SendToAllClients(string eventName, object[] data = null,
            MessageChannel messageChannel = MessageChannel.Reliable)
        {
            NetworkedEvent networkedEvent = new NetworkedEvent
            {
                EventName = eventName,
                Data = data?.ToList() ?? Array.Empty<object>().ToList()
            };
            _scriptHandler.Instance.BroadcastMessage(Msg.Serialize(networkedEvent), messageChannel);
        }
    }
}