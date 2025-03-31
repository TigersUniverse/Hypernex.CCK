using Nexbox;
using WebSocketSharp;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public class ServerWebSocket
    {
        private WebSocket webSocket;

        public bool IsOpen => webSocket?.IsAlive ?? false;
    
        public void Create(string url, object OnOpen = null, object OnMessage = null, object OnClose = null, object OnError = null)
        {
            webSocket = new WebSocket(url);
            if (OnOpen != null)
            {
                SandboxFunc onOpen = SandboxFuncTools.TryConvert(OnOpen);
                webSocket.OnOpen += (sender, args) => SandboxFuncTools.InvokeSandboxFunc(onOpen);
            }
            if (OnMessage != null)
            {
                SandboxFunc onMessage = SandboxFuncTools.TryConvert(OnMessage);
                webSocket.OnMessage += (sender, args) => SandboxFuncTools.InvokeSandboxFunc(onMessage, args.Data);
            }
            if (OnClose != null)
            {
                SandboxFunc onClose = SandboxFuncTools.TryConvert(OnClose);
                webSocket.OnClose += (sender, args) =>
                    SandboxFuncTools.InvokeSandboxFunc(onClose, args.Code, args.Reason, args.WasClean);
            }
            if (OnError != null)
            {
                SandboxFunc onError = SandboxFuncTools.TryConvert(OnError);
                webSocket.OnError += (sender, args) => SandboxFuncTools.InvokeSandboxFunc(onError, args.Message);
            }
        }

        public void Open() => webSocket.Connect();
        public void Send(string message) => webSocket.Send(message);
        public void Close() => webSocket.Close();
    }
}