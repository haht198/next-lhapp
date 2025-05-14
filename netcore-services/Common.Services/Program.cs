using MihaZupan;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Common.Services.Static;

namespace Common.Services
{
    internal static class Program
    {
        private static string[] _args;
        private static Startup app;

        public static void Stop()
        {
            app.Stop().Wait();
            Environment.Exit(-1);
        }

        internal static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandleExceptionEvent;
            _args = args;
            ParsingArguments(_args);
            if (!ProgramArguments.IsValid())
            {
                Console.WriteLine("Service arguments invalid");
                Environment.Exit(-1);
                return;
            }
            try
            {
                app = new Startup();
                await app.Run();
            }
            catch (Exception ex)
            {
                // Logger.Error(ex, $"Unnhandle exception {ex.Message}");
                Console.WriteLine($"Unnhandle exception {ex.Message}");
                Environment.Exit(-1);
            }
        }


        private static void UnhandleExceptionEvent(object sender, UnhandledExceptionEventArgs ex)
        {
            // Logger.Error((Exception)ex.ExceptionObject, "UnhandleExceptionEvent");
            Console.WriteLine($"UnhandleExceptionEvent");
            Environment.Exit(-1);
        }

        private static void ParsingArguments(string[] args)
        {
            ProgramArguments.Env = "debug";
            var customArgs = new List<string>();
            foreach (var arg in args)
            {
                customArgs.AddRange(arg.Split('|'));
            }
            foreach (var arg in customArgs)
            {
                if (arg.StartsWith("--service="))
                {
                    ProgramArguments.Service = arg.Replace("--service=", "");
                }
                if (arg.StartsWith("--env="))
                {
                    ProgramArguments.Env = arg.Replace("--env=", "");
                }
                if (arg.StartsWith("--instanceId="))
                {
                    ProgramArguments.InstanceId = arg.Replace("--instanceId=", "");
                }
                //if (arg.StartsWith("--appDataFolder="))
                //{
                //    ProgramArguments.AppDataFolder = arg.Replace("--appDataFolder=", "");
                //}
                //if (arg.StartsWith("--logFolder="))
                //{
                //    ProgramArguments.LogFolder = arg.Replace("--logFolder=", "");
                //}
                if (arg.StartsWith("--servicesFolder="))
                {
                    ProgramArguments.ServicesFolder = arg.Replace("--servicesFolder=", "");
                }
                if (arg.StartsWith("--appversion="))
                {
                    ProgramArguments.AppVersion = arg.Replace("--appversion=", "");
                }
                if (arg.StartsWith("--socket-server-host="))
                {
                    ProgramArguments.SocketServerHost = arg.Replace("--socket-server-host=", "");
                }
                if (arg.StartsWith("--socket-server-port="))
                {
                    ProgramArguments.SocketServerPort = int.Parse(arg.Replace("--socket-server-port=", ""));
                }
                if (arg.StartsWith("--proxy_protocol="))
                {
                    ProgramArguments.ProxyProtocol = arg.Replace("--proxy_protocol=", "");
                }
                if (arg.StartsWith("--proxy_host="))
                {
                    ProgramArguments.ProxyHost = arg.Replace("--proxy_host=", "");
                }
                if (arg.StartsWith("--proxy_port="))
                {
                    var argValue = arg.Replace("--proxy_port=", "");
                    if (string.IsNullOrEmpty(argValue))
                    {
                        argValue = "80";
                    }
                    ProgramArguments.ProxyPort = int.Parse(arg.Replace("--proxy_port=", ""));
                }
                if (arg.StartsWith("--proxy_usr="))
                {
                    ProgramArguments.ProxyUser = arg.Replace("--proxy_usr=", "");
                }
                if (arg.StartsWith("--proxy_psw="))
                {
                    ProgramArguments.ProxyPassword = arg.Replace("--proxy_psw=", "");
                }
            }
            if (!string.IsNullOrEmpty(ProgramArguments.ProxyProtocol) && !string.IsNullOrEmpty(ProgramArguments.ProxyHost) && ProgramArguments.ProxyPort > 0)
            {
                if (ProgramArguments.ProxyProtocol.StartsWith("http"))
                {
                    NetworkCredential credential = null;
                    if (!string.IsNullOrEmpty(ProgramArguments.ProxyUser) && !string.IsNullOrEmpty(ProgramArguments.ProxyPassword))
                    {
                        credential = new NetworkCredential(ProgramArguments.ProxyUser, ProgramArguments.ProxyPassword);
                    }
                    ProgramArguments.Proxy = new WebProxy($"{ProgramArguments.ProxyProtocol}://{ProgramArguments.ProxyHost}:{ProgramArguments.ProxyPort}", true, new string[] { }, credential);
                }
                else if (ProgramArguments.ProxyProtocol.StartsWith("socks"))
                {
                    ProgramArguments.Proxy = new HttpToSocks5Proxy(ProgramArguments.ProxyHost, ProgramArguments.ProxyPort, ProgramArguments.ProxyUser, ProgramArguments.ProxyPassword);
                }
            }
            else
            {
                ProgramArguments.Proxy = null;
            }
        }
    }
}
