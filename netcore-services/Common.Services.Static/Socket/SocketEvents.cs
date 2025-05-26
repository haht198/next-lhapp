namespace Common.Services.Static.Socket
{
    public static class SocketEvents
    {
      public static string RegisterChanel => "register";

      public static string ChangeUserSettings => "change-user-settings";
      public static string RequestUserSettings => "request-user-settings";

      // UPLOAD EVENTS
      public static string UploadBatchFiles => "upload-batch-files";
      public static string StopUpload => "upload-stop";
      public static string ResumeUpload => "upload-resume";

      public static string FinishUpload => "upload-finished";

      public static string UploadProgress => "upload-progress";
      public static string ChangeInternetConnectionStatus => "change-internet-connection-status";
      public static string RequestRefreshToken => "auth-request-refresh-token";
      public static string ChangeAccessToken => "auth-change-access-token";
    }
}
