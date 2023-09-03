using System;
using System.Collections.Generic;
using Avalonia.Threading;
using SimpleJSON;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Hypernex.CCK.ScriptEditor;

public class WebSocketServerManager
{
    public static int Port { get; private set; }
    private static WebSocketServer? _socket;
    
    internal static readonly Dictionary<string, NexboxScript> Scripts = new();

    public static void OpenServer()
    {
        Port = new Random().Next(5001, 9999);
        _socket = new WebSocketServer(Port);
        _socket.AddWebSocketService<ScriptEditorSocketEndpoint>("/scripting");
        _socket.Start();
    }

    public static void Stop() => _socket?.Stop();

    private static string? GetMonacoLanguage(NexboxLanguage l)
    {
        switch (l)
        {
            case NexboxLanguage.JavaScript:
                return "javascript";
            case NexboxLanguage.Lua:
                return "lua";
        }
        return null;
    }

    private class ScriptEditorSocketEndpoint : WebSocketBehavior
    {
        private static List<ScriptEditorSocketEndpoint> g = new();
        
        protected override void OnOpen()
        {
            g.Add(this);
            base.OnOpen();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            g.Remove(this);
            base.OnError(e);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                JSONNode node = JSONNode.Parse(e.Data);
                switch (node["message"].Value.ToLower())
                {
                    case "request":
                    {
                        NexboxScript s = Scripts[node["id"].Value];
                        JSONObject o = new JSONObject();
                        o.Add("message", "openscript");
                        o.Add("script", s.Script);
                        o.Add("language", GetMonacoLanguage(s.Language)!);
                        o.Add("theme", "vs-dark");
                        Send(o.ToString());
                        break;
                    }
                    case "savecode":
                    {
                        NexboxScript s = Scripts[node["id"].Value];
                        s.Script = node["text"].Value;
                        JSONObject jsonObject = new JSONObject();
                        jsonObject.Add("message", "update");
                        jsonObject.Add("id", node["id"].Value);
                        jsonObject.Add("Script", node["text"].Value);
                        // just why cant we have a broadcast
                        foreach (ScriptEditorSocketEndpoint scriptEditorSocketEndpoint in new List<ScriptEditorSocketEndpoint>(g))
                            scriptEditorSocketEndpoint.Send(jsonObject.ToString());
                        break;
                    }
                    case "createscript":
                    {
                        string id = node["id"].Value;
                        if (Scripts.ContainsKey(id))
                            break;
                        NexboxScript script = new NexboxScript
                        {
                            Name = node["Name"].Value,
                            Language = (NexboxLanguage) node["Language"].AsInt,
                            Script = node["Script"].Value
                        };
                        Scripts.Add(id, script);
                        Dispatcher.UIThread.Post(() =>
                            MainWindow.Instance!.CreateScript(id, script.Name, script.Language));
                        break;
                    }
                    case "removescript":
                    {
                        string id = node["id"].Value;
                        if (!Scripts.ContainsKey(id))
                            break;
                        Scripts.Remove(id);
                        Dispatcher.UIThread.Post(() => MainWindow.Instance!.RemoveScript(id));
                        break;
                    }
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee);
            }
        }
    }
}