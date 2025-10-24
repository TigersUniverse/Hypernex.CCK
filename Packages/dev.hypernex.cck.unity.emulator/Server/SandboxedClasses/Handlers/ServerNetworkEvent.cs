using System;
using System.Linq;
using Hypernex.Networking.Messages;
using Nexport;
using Nexport.BuiltinMessages;

namespace Hypernex.Networking.Server.SandboxedClasses.Handlers
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
                Data = data != null ? data.Select(x => new DynamicNetworkObject
                {
                    TypeFullName = x.GetType().FullName,
                    Data = x
                }).ToList() : Array.Empty<DynamicNetworkObject>().ToList()
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
                Data = data != null ? data.Select(x => new DynamicNetworkObject
                {
                    TypeFullName = x.GetType().FullName,
                    Data = x
                }).ToList() : Array.Empty<DynamicNetworkObject>().ToList()
            };
            _scriptHandler.Instance.BroadcastMessage(Msg.Serialize(networkedEvent), messageChannel);
        }
    }
}