using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Services.Static;
using Common.Services.Static.Logger;
using WebSocket4Net;

namespace Common.Services
{
    public class SocketEventModel
    {
        public string chanel { get; set; }
        public string @event { get; set; }
        public object body { get; set; }

        public string sessionId { get; set; }
    }

    public class RegisteredEventHandler
    {
        public string Event { get; set; }
        public Action<object> Handler { get; set; }
    }
    public static class SocketIntegration
    {
        private static string chanelIdentifier;
        private static string sessionId;
        private static WebSocket socket;
        private static List<RegisteredEventHandler> registeredHandlers;
        private static ConcurrentQueue<MessageReceivedEventArgs> pendingMessagesQueue;
        public static bool IsConnected => socket.State == WebSocketState.Open;

        public static bool Init(string _chanelIdentifier)
        {
            if (string.IsNullOrEmpty(_chanelIdentifier))
            {
                throw new Exception("chanelIdentifier is invalid");
            }
            if (string.IsNullOrEmpty(ProgramArguments.SocketServerHost) || ProgramArguments.SocketServerPort <= 0)
            {
                throw new Exception("Program arguments need to have socket setting");
            }
            if (socket != null && (socket.State == WebSocketState.Connecting || socket.State == WebSocketState.Open))
            {
                throw new Exception("Socket is already initialized");
            }
            chanelIdentifier = _chanelIdentifier;
            sessionId = Guid.NewGuid().ToString();
            socket = new WebSocket($"ws://{ProgramArguments.SocketServerHost}:{ProgramArguments.SocketServerPort}/");
            registeredHandlers = new List<RegisteredEventHandler>();
            pendingMessagesQueue = new ConcurrentQueue<MessageReceivedEventArgs>();

            var result = socket.OpenAsync().GetAwaiter().GetResult();
            if (!result)
            {
                Logger.Warning($"{LogPrefix()} - Cannot connect socket chanel {_chanelIdentifier} to main socket, application will exit {result}");
                Program.Stop();
                return result;
            }
            Logger.Info($"{LogPrefix()} - Socket chanel {_chanelIdentifier} connected to main socket - sessionId: {sessionId}");
            socket.MessageReceived += HandleAllSocketEvent;
            OnClosed(() =>
            {
                Logger.Warning($"{LogPrefix()} - Socket closed, application will exit");
                Program.Stop();
            });

            var assembly = Assembly.GetEntryAssembly();
            Send(SocketEvents.RegisterChanel, new
            {
                processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                processName = assembly.FullName,
                netServiceVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                sessionId
            });



            On(SocketEvents.ChangeHostApplicationState, (hostAppState) =>
            {
                Logger.Info($"{LogPrefix()} - Handle SocketEvents.ChangeHostApplicationState", hostAppState);
                try
                {
                    var setting = (dynamic)hostAppState;
                    if (setting != null)
                    {
                        HostApplicationState.AppName = setting.appName;
                        HostApplicationState.AppVersion = setting.appVersion;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"{LogPrefix()} - Handle socket event SocketEvents.ChangeHostApplicationState Exception");
                }
            });
            Send(SocketEvents.RequestHostApplicationState, null);

            On(SocketEvents.ChangeUserSettings, (userSetting) =>
            {
                Logger.Info($"{LogPrefix()} - Handle SocketEvents.ChangeUserSettings", userSetting);
                try
                {
                    var setting = (dynamic)userSetting;
                    if (setting != null)
                    {
                        UserSetting.UserId = setting.userId;
                        UserSetting.UserEmail = setting.userEmail;
                        UserSetting.StudioId = setting.studioId;
                        UserSetting.StudioName = setting.studioName;
                        UserSetting.WorkspaceFolder = setting.workspace;

                        while (pendingMessagesQueue.TryDequeue(out var message))
                            HandleAllSocketEvent(null, message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"{LogPrefix()} - SocketEvents.ChangeUserSettings Exception");
                }
            });
            Send(SocketEvents.RequestUserSettings, null);

            return result;

        }



        public static void On(string @event, Action<object> handler)
        {
            if (registeredHandlers.Any(t => t.Event == @event))
            {
                registeredHandlers.First(e => e.Event == @event).Handler = handler;
            }
            else
            {
                Logger.Info($"{LogPrefix()} - Register socket event {@event}");
                registeredHandlers.Add(new RegisteredEventHandler
                {
                    Event = @event,
                    Handler = handler
                });
            }
        }

        public static void OnClosed(Action callback)
        {
            socket.Closed += (object sender, EventArgs e) => callback?.Invoke();
        }

        public static void Send(string @event, dynamic body)
        {
            if (IsConnected)
            {
                var sendData = new SocketEventModel
                {
                    @event = @event,
                    chanel = chanelIdentifier,
                    sessionId = sessionId,
                    body = body
                };
                socket.Send(Newtonsoft.Json.JsonConvert.SerializeObject(sendData));
            }
        }

        private static void HandleAllSocketEvent(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                var socketData = Newtonsoft.Json.JsonConvert.DeserializeObject<SocketEventModel>(e.Message);
                if (socketData == null || string.IsNullOrEmpty(socketData.@event))
                {
                    Logger.Warning($"{LogPrefix()} - Received invalid socket message", e.Message);
                    return;
                }
                if (socketData.@event != SocketEvents.ChangeHostApplicationState && socketData.@event != SocketEvents.ChangeUserSettings && (string.IsNullOrEmpty(UserSetting.UserId) || string.IsNullOrEmpty(UserSetting.WorkspaceFolder)))
                {
                    pendingMessagesQueue.Enqueue(e);
                    Send(SocketEvents.RequestUserSettings, null);
                    return;
                }

                var eventRegistered = registeredHandlers.FirstOrDefault(re => re.Event == socketData.@event);
                if (eventRegistered != null && eventRegistered.Handler != null)
                {
                    eventRegistered.Handler.Invoke(socketData.body);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{LogPrefix()} - Handle socket message exception");
            }
        }

        private static string LogPrefix()
        {
            return $"[SOCKET] [{chanelIdentifier}__{sessionId}]";
        }
    }
}
