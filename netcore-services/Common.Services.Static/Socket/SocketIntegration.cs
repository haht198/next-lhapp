using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WebSocket4Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Common.Services.Static.Socket
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
        private static string _chanelIdentifier;
        private static string _sessionId;
        private static WebSocket socket;
        private static List<RegisteredEventHandler> registeredHandlers;
        private static ConcurrentQueue<MessageReceivedEventArgs> pendingMessagesQueue;
        public static bool IsConnected => socket.State == WebSocketState.Open;

        private static readonly int _ivSize = 16;
        private static readonly string _password = "iPmjRTZyf%YYMtLd!zax";

        private static string Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length <= _ivSize)
            {
                throw new ArgumentException("Invalid encrypted data.  Must contain IV and ciphertext.");
            }

            byte[] iv = new byte[_ivSize];
            Array.Copy(encryptedData, 0, iv, 0, _ivSize);

            byte[] cipherText = new byte[encryptedData.Length - _ivSize];
            Array.Copy(encryptedData, _ivSize, cipherText, 0, encryptedData.Length - _ivSize);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes(_password));

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = iv;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        public static void Init(string chanelIdentifier)
        {
            if (string.IsNullOrEmpty(chanelIdentifier))
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
            _chanelIdentifier = chanelIdentifier;
            _sessionId = Guid.NewGuid().ToString();
            socket = new WebSocket($"ws://{ProgramArguments.SocketServerHost}:{ProgramArguments.SocketServerPort}/");
            registeredHandlers = new List<RegisteredEventHandler>();
            pendingMessagesQueue = new ConcurrentQueue<MessageReceivedEventArgs>();
        }


        public static bool Connect()
        {
               var result = socket.OpenAsync().GetAwaiter().GetResult();
            if (!result)
            {
                Logger.Logger.Warning($"[SOCKET] - Cannot connect to main socket, application will exit {result}");
                return false;
            }

            socket.MessageReceived += HandleAllSocketEvent;

            Send(SocketEvents.RegisterChanel, new
            {
                processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                processName = Assembly.GetEntryAssembly()?.FullName,
                appversion = ProgramArguments.AppVersion
            });


            On(SocketEvents.ChangeUserSettings, (userSetting) =>
            {
                Logger.Logger.Info($"[SOCKET] - SocketEvents.ChangeUserSettings", userSetting);
                byte[] encryptedSettinng = Convert.FromBase64String((string)userSetting);
                var decryptedSetting = Decrypt(encryptedSettinng);
                try
                {
                    dynamic setting = JObject.Parse(decryptedSetting);
                    if (setting != null)
                    {
                        UserSetting.UserId = setting.userId;
                        UserSetting.UserEmail = setting.userEmail;
                        UserSetting.StudioId = setting.studioId;
                        UserSetting.StudioName = setting.studioName;
                        UserSetting.ThumbnailSavingType = setting.thumbnailSavingType;
                        UserSetting.WorkspaceFolder = setting.workspace;
                        UserSetting.ExifTool = setting.exifTool;
                        UserSetting.MetadataRootFolderPath = string.IsNullOrEmpty(UserSetting.WorkspaceFolder) ? null : System.IO.Path.Combine(UserSetting.WorkspaceFolder, "metadata");
                        UserSetting.UserAccessToken = setting.userAccessToken;
                        UserSetting.UserAccessTokenExpiredIn = setting.userAccessTokenExpiredIn != null ? setting.userAccessTokenExpiredIn : 0;

                        while (pendingMessagesQueue.TryDequeue(out var message))
                            HandleAllSocketEvent(null, message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Logger.Error(ex, "[SOCKET] - SocketEvents.ChangeUserSettings Exception");
                }
            });

            On(SocketEvents.ChangeInternetConnectionStatus, value =>
            {
                Utils.HasInternetConnection = Convert.ToBoolean(value);
                Logger.Logger.Info($"[SOCKET] - SocketEvents.ChangeInternetConnectionStatus received {Utils.HasInternetConnection}");
            });

            On(SocketEvents.ChangeAccessToken, token =>
            {
                Logger.Logger.Info($"[SOCKET] - SocketEvents.ChangeAccessToken", token);
                byte[] encryptedToken = Convert.FromBase64String((string)token);
                var decryptedToken = Decrypt(encryptedToken);
                try
                {
                    dynamic userToken = JObject.Parse(decryptedToken);
                    UserSetting.UserAccessToken = userToken.accessToken;
                    UserSetting.UserAccessTokenExpiredIn = userToken.accessTokenExpiredIn;
                }
                catch (Exception ex)
                {
                    Logger.Logger.Error($"[SOCKET] - SocketEvents.ChangeAccessToken exception", ex);
                    throw;
                }
            });

            return true;
        }


        public static void On(string @event, Action<object> handler)
        {
            if (registeredHandlers.Any(t => t.Event == @event))
            {
                registeredHandlers.First(e => e.Event == @event).Handler = handler;
            }
            else
            {
                Logger.Logger.Info($"[SOCKET] - Register socket event {@event}");
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
                    chanel = _chanelIdentifier,
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
                    Logger.Logger.Warning("[SOCKET] - Received invalid socket message", e.Message);
                    return;
                }
                if (socketData.@event != SocketEvents.ChangeUserSettings && (string.IsNullOrEmpty(UserSetting.UserId) || string.IsNullOrEmpty(UserSetting.WorkspaceFolder)))
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
                Logger.Logger.Error(ex, "[SOCKET] - Handle socket message exception");
            }
        }
        private static string LogPrefix()
        {
          return $"[SOCKET] [{_chanelIdentifier}__{_sessionId}]";
        }
    }
}
