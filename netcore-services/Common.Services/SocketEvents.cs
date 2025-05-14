namespace Common.Services
{
    public static class SocketEvents
    {
        public static string RegisterChanel => "register";

        public static string ChangeHostApplicationState => "change-host-application-state";
        public static string RequestHostApplicationState => "request-host-application-state";
        public static string ChangeUserSettings => "change-user-settings";
        public static string RequestUserSettings => "request-user-settings";

        // UPLOAD EVENTS
        public static string UploadBatchFiles => "upload-batch-files";
        public static string StopUpload => "upload-stop";
        public static string ResumeUpload => "upload-resume";

        public static string FinishUpload => "upload-finished";

    }
}
