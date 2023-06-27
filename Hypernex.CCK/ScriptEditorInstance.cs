using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using SimpleJSON;
using WebSocketSharp;

namespace Hypernex.CCK
{
    public class ScriptEditorInstance
    {
        private static Dictionary<ScriptEditorInstance, (NexboxScript, Action<string>)> Scripts =
            new Dictionary<ScriptEditorInstance, (NexboxScript, Action<string>)>();
        private static WebSocket _socket;
        private static Process app;

        public static bool IsOpen => _socket?.IsAlive ?? false;

        // https://stackoverflow.com/a/570461/12968919
        private static int GetHTTPPort()
        {
            int port = new Random().Next(80, 5000); //<--- This is your value
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port==port)
                {
                    isAvailable = false;
                    break;
                }
            }
            if (isAvailable)
                return port;
            return GetHTTPPort();
        }

        public static bool StartApp(string location, Action onOpen = null)
        {
            int hp = GetHTTPPort();
            if (!File.Exists(location))
                return false;
            app = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = location,
                    WorkingDirectory = Path.GetDirectoryName(location),
                    Arguments = "-port " + hp,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            bool b = app.Start();
            if (b)
            {
                WebClient webClient = new WebClient();
                int wp = Convert.ToInt32(webClient.DownloadString("http://localhost:" + hp + "/getWSPort"));
                InitSocket(wp, onOpen);
            }
            return b;
        }

        private static void InitSocket(int port, Action onOpen = null)
        {
            _socket = new WebSocket("ws://127.0.0.1:" + port + "/scripting");
            _socket.OnOpen += (sender, args) => onOpen?.Invoke();
            _socket.OnMessage += (sender, args) =>
            {
                try
                {
                    JSONNode node = JSONNode.Parse(args.Data);
                    switch (node["message"].Value.ToLower())
                    {
                        case "update":
                        {
                            foreach (KeyValuePair<ScriptEditorInstance, (NexboxScript, Action<string>)> keyValuePair in
                                     new Dictionary<ScriptEditorInstance, (NexboxScript, Action<string>)>(Scripts))
                            {
                                if (node["id"].Value == keyValuePair.Key.id)
                                {
                                    keyValuePair.Value.Item2.Invoke(node["Script"].Value);
                                    Scripts[keyValuePair.Key].Item1.Script = node["Script"].Value;
                                }
                            }
                            break;
                        }
                    }
                }
                catch (Exception){}
            };
            _socket.OnClose += (sender, args) => _socket = null;
            _socket.Connect();
        }

        public static ScriptEditorInstance GetInstanceFromId(string id)
        {
            foreach (KeyValuePair<ScriptEditorInstance,(NexboxScript, Action<string>)> keyValuePair in Scripts)
            {
                if (keyValuePair.Key.id == id)
                    return keyValuePair.Key;
            }
            return null;
        }
        
        public static ScriptEditorInstance GetInstanceFromScript(NexboxScript script)
        {
            foreach (KeyValuePair<ScriptEditorInstance,(NexboxScript, Action<string>)> keyValuePair in Scripts)
            {
                if (keyValuePair.Value.Item1 == script)
                    return keyValuePair.Key;
            }
            return null;
        }

        public static void Stop() => app?.Kill();
        
        private NexboxScript script;
        public readonly string id;

        public ScriptEditorInstance(NexboxScript script, Action<string> onScriptUpdate)
        {
            if (!IsOpen)
                throw new Exception("Attempt to Create ScriptEditorInstance before InitSocket!");
            string ide = Guid.NewGuid().ToString();
            while(Scripts.Count(x => x.Key.id == ide) > 0)
                ide = Guid.NewGuid().ToString();
            id = ide;
            this.script = script;
            Scripts.Add(this, (script, onScriptUpdate));
        }

        public void CreateScript()
        {
            JSONObject jsonObject = new JSONObject();
            jsonObject.Add("message", "createscript");
            jsonObject.Add("id", id);
            jsonObject.Add("Name", script.Name);
            jsonObject.Add("Language", (int) script.Language);
            jsonObject.Add("Script", script.Script);
            _socket.Send(jsonObject.ToString());
        }

        public void RemoveScript()
        {
            JSONObject jsonObject = new JSONObject();
            jsonObject.Add("message", "removescript");
            jsonObject.Add("id", id);
            _socket.Send(jsonObject.ToString());
        }
    }
}