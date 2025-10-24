using System;
using System.Threading;
using Hypernex.Sandboxing.SandboxedTypes;
using Nexbox;

namespace Hypernex.Networking.Server.SandboxedClasses.Handlers
{
    public class ScriptEvents
    {
        private ScriptHandler ScriptHandler;
        
        /// <summary>
        /// UserId when someone joins
        /// </summary>
        internal Action<string> OnUserJoin = userId => { };
        /// <summary>
        /// UserId when someone leaves
        /// </summary>
        internal Action<string> OnUserLeave = userId => { };
        /// <summary>
        /// When a client invokes a NetworkedEvent, the data is passed here
        /// </summary>
        internal Action<string, string, object[]> OnUserNetworkEvent = (userId, EventName, EventArgs) => { };

        /// <summary>
        /// When a client updates their Player Information
        /// </summary>
        internal Action<string, bool, string, bool, bool, string> OnPlayerUpdate =
            (userId, isVR, avatarId, isSpeaking, isFBT, vrikJson) => { };

        /// <summary>
        /// When a client updates their Player Object information
        /// </summary>
        internal Action<string, int, OfflineNetworkedObject> OnPlayerObject = (userId, coreBone, obj) => { };

        /// <summary>
        /// When a client updates a weight on their avatar
        /// </summary>
        internal Action<string, string, float> OnWeightedObject = (userId, weightName, weight) => { };

        /// <summary>
        /// When a client updates their self-assigned tags
        /// </summary>
        internal Action<string, string[]> OnPlayerTags = (userId, tags) => { };

        /// <summary>
        /// When a client updates their extraneous object
        /// </summary>
        internal Action<string, string, object> OnExtraneousObject = (userId, key, value) => { };

        public ScriptEvents() => throw new Exception("Cannot instantiate ScriptEvents!");
        internal ScriptEvents(ScriptHandler s) => ScriptHandler = s;

        public void Subscribe(ScriptEvent scriptEvent, object o)
        {
            SandboxFunc callback = SandboxFuncTools.TryConvert(o);
            switch (scriptEvent)
            {
                case ScriptEvent.OnUserJoin:
                    /*OnUserJoin += userId => SandboxFuncTools.InvokeSandboxFunc(callback, userId);*/
                    OnUserJoin += userId =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if(ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() => SandboxFuncTools.InvokeSandboxFunc(callback, userId));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnUserLeave:
                    /*OnUserLeave += userId => SandboxFuncTools.InvokeSandboxFunc(callback, userId);*/
                    OnUserLeave += userId =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if(ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() => SandboxFuncTools.InvokeSandboxFunc(callback, userId));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnUserNetworkEvent:
                    /*OnUserNetworkEvent += (userId, eventName, eventArgs) =>
                        SandboxFuncTools.InvokeSandboxFunc(callback, userId, eventName, eventArgs);*/
                    OnUserNetworkEvent += (userId, eventName, eventArgs) =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if (ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() =>
                                        SandboxFuncTools.InvokeSandboxFunc(callback, userId, eventName, eventArgs));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnPlayerUpdate:
                    OnPlayerUpdate += (userId, isVR, avatarId, isSpeaking, isFBT, vrikJson) =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if (ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() =>
                                    SandboxFuncTools.InvokeSandboxFunc(callback, userId, isVR, avatarId, isSpeaking, isFBT,
                                        vrikJson));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnNetworkedObject:
                    OnPlayerObject += (userId, coreBone, obj) =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if (ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() =>
                                    SandboxFuncTools.InvokeSandboxFunc(callback, userId, coreBone, obj));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnWeight:
                    OnWeightedObject += (userId, weightName, weight) =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if (ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() =>
                                    SandboxFuncTools.InvokeSandboxFunc(callback, userId, weightName, weight));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnPlayerTags:
                    OnPlayerTags += (userId, tags) =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if (ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() =>
                                    SandboxFuncTools.InvokeSandboxFunc(callback, userId, tags));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
                case ScriptEvent.OnPlayerExtraneousObject:
                    OnExtraneousObject += (userId, key, value) =>
                    {
                        if (ScriptHandler.disposed) return;
                        new Thread(() =>
                        {
                            if (ScriptHandler.m.WaitOne())
                            {
                                ScriptHandler.AwaitingTasks.Enqueue(() =>
                                    SandboxFuncTools.InvokeSandboxFunc(callback, userId, key, value));
                                ScriptHandler.m.ReleaseMutex();
                            }
                        }).Start();
                    };
                    break;
            }
        }
    }
}