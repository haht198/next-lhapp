using System.Net;

namespace Common.Services.Static
{
    public static class ProgramArguments
    {
        public static string Service { get; set; }
        public static string Env { get; set; }
        public static string InstanceId { get; set; }
        //public static string AppDataFolder { get; set; }
        //public static string LogFolder { get; set; }
        public static string ServicesFolder { get; set; }
        public static string AppVersion { get; set; }
        public static string SocketServerHost { get; set; }
        public static int SocketServerPort { get; set; }
        public static IWebProxy Proxy { get; set; }
        public static string ProxyProtocol { get; set; }
        public static string ProxyHost { get; set; }
        public static int ProxyPort { get; set; }
        public static string ProxyUser { get; set; }
        public static string ProxyPassword { get; set; }
        public static bool IsValid()
        {
            return
                !string.IsNullOrEmpty(Service) &&
                !string.IsNullOrEmpty(Env) &&
                //!string.IsNullOrEmpty(AppDataFolder) &&
                //!string.IsNullOrEmpty(LogFolder) &&
                !string.IsNullOrEmpty(AppVersion) &&
                !string.IsNullOrEmpty(SocketServerHost) &&
                SocketServerPort > 0;
        }
    }
}