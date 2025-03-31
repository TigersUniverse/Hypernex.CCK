using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hypernex.CCK;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Networking.Server.SandboxedClasses;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Nexbox;
using Nexbox.Interpreters;
using Nexport;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;
using Time = Hypernex.Sandboxing.SandboxedTypes.Time;

namespace Hypernex.Networking.Server
{
    public class ScriptHandler : IDisposable
    {
        internal static readonly List<ScriptHandler> Instances = new();
        
        internal HypernexInstance Instance;
        internal readonly ScriptEvents Events;
        internal readonly Queue<Action> AwaitingTasks = new();
        internal readonly Mutex m = new ();
        private readonly CancellationTokenSource cts = new();
        internal bool disposed;

        private static readonly Dictionary<string, object> GlobalsToForward = new Dictionary<string, object>
        {

        };

        private static readonly Dictionary<string, Type> TypesToForward = new Dictionary<string, Type>
        {
            ["float2"] = typeof(float2),
            ["float3"] = typeof(float3),
            ["float4"] = typeof(float4),
            ["MathF"] = typeof(ServerMathF),
            ["SinCos"] = typeof(SinCos),
            ["MidpointRounding"] = typeof(MidpointRounding),
            ["Http"] = typeof(HTTP),
            ["WebSocket"] = typeof(ServerWebSocket),
            ["ServerNetworkEvent"] = typeof(ServerNetworkEvent),
            ["MessageChannel"] = typeof(MessageChannel),
            ["ScriptEvent"] = typeof(ScriptEvent),
            ["HttpMediaType"] = typeof(HttpMediaType),
            ["OfflineNetworkedObject"] = typeof(OfflineNetworkedObject),
            ["CoreBone"] = typeof(CoreBone),
            ["Instance"] = typeof(Instance),
            ["Time"] = typeof(Time),
            ["UtcTime"] = typeof(UtcTime),
            ["ScriptEvents"] = typeof(ScriptEvents)
        };

        internal static ScriptHandler GetScriptHandlerFromInstance(HypernexInstance instance)
        {
            foreach (ScriptHandler scriptHandler in Instances)
            {
                if(scriptHandler.Instance != instance) continue;
                return scriptHandler;
            }
            return null;
        }
        
        private Dictionary<NexboxScript, IInterpreter> RunningScripts = new();

        internal ScriptHandler(HypernexInstance instance)
        {
            Events = new ScriptEvents(this);
            Instance = instance;
            Instance.OnClientConnect += Events.OnUserJoin;
            Instance.OnMessage += (userId, meta, channel) =>
            {
                if (meta.TypeOfData == typeof(NetworkedEvent))
                {
                    NetworkedEvent networkedEvent = (NetworkedEvent) Convert.ChangeType(meta.Data, typeof(NetworkedEvent))!;
                    Events.OnUserNetworkEvent.Invoke(userId, networkedEvent.EventName, (object[])networkedEvent.Data.ToArray()[0]);
                }
            };
            Instance.OnClientDisconnect += Events.OnUserLeave;
            Instances.Add(this);
        }

        private void CreateGlobalsForInterpreter(IInterpreter interpreter)
        {
            interpreter.CreateGlobal("Events", Events);
            interpreter.CreateGlobal("NetworkEvent", new ServerNetworkEvent(this));
            interpreter.CreateGlobal("Instance", new Instance(Instance));
            foreach (KeyValuePair<string,object> keyValuePair in GlobalsToForward)
                interpreter.CreateGlobal(keyValuePair.Key, keyValuePair.Value);
        }

        private void ForwardTypesToInterpreter(IInterpreter interpreter)
        {
            foreach (KeyValuePair<string,Type> keyValuePair in TypesToForward)
                interpreter.ForwardType(keyValuePair.Key, keyValuePair.Value);
        }

        private IEnumerator e(IInterpreter interpreter, string script)
        {
            interpreter.RunScript(script, Logger.CurrentLogger.Log);
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    if (AwaitingTasks.Count > 0)
                    {
                        if (m.WaitOne(5))
                        {
                            Action a = AwaitingTasks.Dequeue();
                            a.Invoke();
                            m.ReleaseMutex();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Critical(e);
                    Dispose();
                }

                //Thread.Sleep(10);
                yield return new WaitForSeconds(0.01f);
            }
        }

        private void execute(IInterpreter interpreter, string script, bool multithread)
        {
            if (multithread)
                new Thread(() => e(interpreter, script)).Start();
            else
                CoroutineRunner.Instance.StartCoroutine(e(interpreter, script));
        }

        private void OnPrint(string scriptName, object o)
        {
            Logger.CurrentLogger.Log($"[SERVER] [{scriptName}] {o}");
            List<string> sentTo = new List<string>();
            foreach (string userid in Instance.ConnectedClients)
                if (userid == Instance.WorldOwnerId)
                {
                    // Send the world owner the server log
                    ServerConsoleLog serverConsoleLog = new ServerConsoleLog
                    {
                        ScriptName = scriptName,
                        Log = o.ToString()
                    };
                    byte[] msg = Msg.Serialize(serverConsoleLog);
                    Instance.SendMessageToClient(userid, msg);
                    sentTo.Add(userid);
                    // We can also send all moderators of the instance this message, IF the instance owner is also the world owner
                    if (userid != Instance.InstanceCreatorId) continue;
                    foreach (string moderator in Instance.Moderators.Where(x => !sentTo.Contains(x)))
                        Instance.SendMessageToClient(moderator, msg);
                }
        }

        internal void LoadAndExecuteScript(NexboxScript script, bool multithread = true)
        {
            try
            {
                if (disposed)
                    return;
                switch (script.Language)
                {
                    case NexboxLanguage.JavaScript:
                        JavaScriptInterpreter javaScriptInterpreter = new JavaScriptInterpreter();
                        javaScriptInterpreter.StartSandbox(o => OnPrint(script.Name + script.GetExtensionFromLanguage(), o));
                        ForwardTypesToInterpreter(javaScriptInterpreter);
                        CreateGlobalsForInterpreter(javaScriptInterpreter);
                        RunningScripts.Add(script, javaScriptInterpreter);
                        execute(javaScriptInterpreter, script.Script, multithread);
                        break;
                    case NexboxLanguage.Lua:
                        LuaInterpreter luaInterpreter = new LuaInterpreter();
                        luaInterpreter.StartSandbox(o => OnPrint(script.Name + script.GetExtensionFromLanguage(), o));
                        ForwardTypesToInterpreter(luaInterpreter);
                        CreateGlobalsForInterpreter(luaInterpreter);
                        RunningScripts.Add(script, luaInterpreter);
                        execute(luaInterpreter, script.Script, multithread);
                        break;
                }
            }
            catch(Exception e){Logger.CurrentLogger.Critical(e);}
        }

        internal bool Compare(HypernexInstance instance) => Instance.Equals(instance);

        public void Dispose()
        {
            if (disposed)
                return;
            foreach (KeyValuePair<NexboxScript, IInterpreter> keyValuePair in new Dictionary<NexboxScript, IInterpreter>(
                         RunningScripts))
            {
                keyValuePair.Value.Stop();
                RunningScripts.Remove(keyValuePair.Key);
            }
            Instances.Remove(this);
            m?.Dispose();
            cts?.Dispose();
            disposed = true;
        }
    }
}