namespace Common.Services.Static
{

    public static class HostApplication
    {
       public static string AppName { get; set; }
       public static string AppVersion { get; set; }
    }
    public static class UserSetting
    {
        public static string UserId { get; set; }
        public static string UserEmail { get; set; }
        public static string StudioId { get; set; }
        public static string StudioName { get; set; }
        public static string WorkspaceFolder { get; set; }
        public static string ThumbnailSavingType { get; set; }
        public static string ExifTool { get; set; }
        public static string MetadataRootFolderPath { get; set; }
        public static int ScreenMaxDimention { get; set; }
        public static string UserAccessToken { get; set; }
        public static long? UserAccessTokenExpiredIn { get; set; }
    }

}
