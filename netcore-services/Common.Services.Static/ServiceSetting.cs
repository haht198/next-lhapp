namespace Common.Services.Static
{
    public static class ServiceSetting
    {
        private static readonly bool _isProduction = ProgramArguments.Env.Equals("prod");
        private static readonly bool _isUat = ProgramArguments.Env.Equals("uat");
        private static readonly bool _isDev = ProgramArguments.Env.Equals("dev");
        private static readonly string _apiUrl = _isProduction ? "https://api.creativeforce.io" : (_isUat ? "https://api.creativeforce-uat.io" : "https://api.creativeforce-dev.io");

        public static string Logging_ElasticSearch_ApiKey
        {
            get
            {
                if (_isProduction)
                {
                    return "c30VH2Rg4XaA3q06L5lCC11neSa0FiIAaH0ir6Y1";
                }

                if (_isUat)
                {
                    return "4jZXDU4GL77MyXpVH6bZn23hnlX9ngeP4UEbSYzg";
                }

                if (_isDev)
                {
                    return "4jZXDU4GL77MyXpVH6bZn23hnlX9ngeP4UEbSYzg";
                }

                return "4jZXDU4GL77MyXpVH6bZn23hnlX9ngeP4UEbSYzg";
            }
        }
        public static string Logging_ElasticSearch_BulkInsertEndpoint
        {
            get
            {
                if (_isProduction)
                {
                    return "https://app-log.creativeforce.io/v1/aws/es/_bulk";
                }

                if (_isUat)
                {
                    return "https://app-log.creativeforce-dev.io/v1/aws/es/_bulk";
                }

                if (_isDev)
                {
                    return "https://app-log.creativeforce-dev.io/v1/aws/es/_bulk";
                }

                return "https://app-log.creativeforce-dev.io/v1/aws/es/_bulk";
            }
        }
        public static ServiceUploaderSettingModel Service_Uploader => new ServiceUploaderSettingModel();
        public static ServiceThumbnailSettingModel Service_Thumbnail => new ServiceThumbnailSettingModel();
        public static ServiceThumbnailPreparationSettingModel Service_ThumbnailPreparation => new ServiceThumbnailPreparationSettingModel();
        public static ServiceAutoCleanupSettingModel Service_AutoCleanup => new ServiceAutoCleanupSettingModel();

        public static bool IsValid()
        {
            return
                Service_Uploader != null &&
                Service_Thumbnail != null &&
                Service_ThumbnailPreparation != null &&
                Service_AutoCleanup != null;
        }

        public class ServiceUploaderSettingModel
        {
            //public int ServiceWaitForRestartTimeInMillisecond { get; set; }
            //public int WaitForInternetTimeInMillisecond { get; set; }
            //public int IntervalTimeInMillisecond { get; set; }
            //public int UploadParallelCount { get; set; }
            //public int ThumbnailMaxsize { get; set; }
            //public string ApiEndpoint_RefreshToken { get; set; }
            //public string ApiEndpoint_RequestSubmitPreSelection { get; set; }
            //public string ApiEndpoint_RequestSubmitPreSelectionByPosition { get; set; }
            //public string ApiEndpoint_RequestSubmitFinalSelection { get; set; }
            //public string ApiEndpoint_SubmitPreSelection { get; set; }
            //public string ApiEndpoint_SubmitPreSelectionByPosition { get; set; }
            //public string ApiEndpoint_SubmitFinalSelection { get; set; }
            //public string ApiEndpoint_RenewTransactionPresignUrl { get; set; }
            //public string ApiEndpoint_GenerateTempPresignUrl { get; set; }
            //public string ApiEndpoint_CreateFileV3 { get; set; }
            //public string ApiEndpoint_CreateAnnotation { get; set; }
            //public string ApiEndpoint_GetTempPresignUrl { get; set; }
            //public string ApiEndpoint_SubmitEditorial { get; set; }

            public int ServiceWaitForRestartTimeInMillisecond
            {
                get
                {
                    if (_isProduction)
                    {
                        return 2000;
                    }

                    if (_isUat)
                    {
                        return 2000;
                    }
                    
                    if (_isDev)
                    {
                        return 2000;
                    }

                    return 2000;
                }
            }
            public int WaitForInternetTimeInMillisecond
            {
                get
                {
                    if (_isProduction)
                    {
                        return 30000;
                    }

                    if (_isUat)
                    {
                        return 30000;
                    }
                    
                    if (_isDev)
                    {
                        return 30000;
                    }

                    return 30000;
                }
            }
            public int IntervalTimeInMillisecond
            {
                get
                {
                    if (_isProduction)
                    {
                        return 5000;
                    }
                    

                    if (_isUat)
                    {
                        return 5000;
                    }
                    
                    if (_isDev)
                    {
                        return 5000;
                    }

                    return 5000;
                }
            }
            public int UploadParallelCount
            {
                get
                {
                    if (_isProduction)
                    {
                        return 3;
                    }

                    if (_isUat)
                    {
                        return 3;
                    }
                    
                    if (_isDev)
                    {
                        return 3;
                    }

                    return 3;
                }
            }
            public int ThumbnailMaxsize
            {
                get
                {
                    if (_isProduction)
                    {
                        return 1600;
                    }

                    if (_isUat)
                    {
                        return 1600;
                    }

                    if (_isDev)
                    {
                        return 1600;
                    }

                    return 1600;
                }
            }
            public string ApiEndpoint_RefreshToken
            {
                get
                {
                    return $"{_apiUrl}/contact/v2/oauth/refreshtoken";
                }
            }
            public string ApiEndpoint_RequestSubmitPreSelection
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/takephoto/requestsubmitpreselectionv2";
                }
            }
            public string ApiEndpoint_RequestSubmitPreSelectionByPosition
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/takephoto/requestsubmitpreselectionbyposition";
                }
            }
            public string ApiEndpoint_RequestSubmitFinalSelection
            {
                get
                {

                    return $"{_apiUrl}/workflow/v2/takephoto/requestsubmitfinalselectionv2";
                }
            }
            public string ApiEndpoint_SubmitPreSelection
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/takephoto/submitpreselectionv2";
                }
            }
            public string ApiEndpoint_SubmitPreSelectionByPosition
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/takephoto/submitpreselectionbyposition";
                }
            }
            public string ApiEndpoint_SubmitFinalSelection
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/takephoto/submitfinalselectionv2";
                }
            }
            public string ApiEndpoint_RenewTransactionPresignUrl
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/takephoto/renewtransactionpresignurlv2";
                }
            }
            public string ApiEndpoint_GenerateTempPresignUrl
            {
                get
                {
                   return $"{_apiUrl}/workflow/v2/files/gettemppresignurl";
                }
            }
            public string ApiEndpoint_CreateFileV3
            {
                get
                {
                    return $"{_apiUrl}/workflow/v2/files/createfilev3";
                }
            }
            public string ApiEndpoint_CreateAnnotation
            {
                get
                {
                    return $"{_apiUrl}/communication/v2/annotations";
                }
            }
            public string ApiEndpoint_CreateAnnotationEditorial
            {
                get
                {
                    return $"{_apiUrl}/editorial/v2/annotations/createannotation";
                }
            }
            public string ApiEndpoint_SubmitEditorial
            {
                get
                {
                    return $"{_apiUrl}/editorial/v2/assets/createeditorialasset";
                }
            }
            public string ApiEndpoint_SubmitEditorialV2
            {
                get
                {
                    return $"{_apiUrl}/editorial/v2/assets/createeditorialassetv2";
                }
            }

            public string ApiEndpoint_SubmitEditorialV3
            {
                get
                {
                    return $"{_apiUrl}/editorial/v2/assets/createeditorialassetv3";
                }
            }
        }
        public class ServiceThumbnailSettingModel
        {
            public int ServiceWaitForRestartTime
            {
                get
                {
                    if (_isProduction)
                    {
                        return 2000;
                    }

                    if (_isUat)
                    {
                        return 2000;
                    }

                    if (_isDev)
                    {
                        return 2000;
                    }

                    return 2000;
                }
            }
        }
        public class ServiceThumbnailPreparationSettingModel
        {
            public int ServiceWaitForRestartTime
            {
                get
                {
                    if (_isProduction)
                    {
                        return 2000;
                    }

                    if(_isUat)
                    {
                        return 2000;
                    }

                    if(_isDev)
                    {
                        return 2000;
                    }

                    return 2000;
                }
            }
        }
        public class ServiceAutoCleanupSettingModel
        {
            public int DefaultTimeToCleanUpDataInDay
            {
                get
                {
                    if (_isProduction)
                    {
                        return 7;
                    }

                    if (_isUat)
                    {
                        return 7;
                    }

                    if (_isDev)
                    {
                        return 7;
                    }

                    return 7;
                }
            }
            public string DefaultCleanUpThumbnailType
            {
                get
                {
                    if (_isProduction)
                    {
                        return "month";
                    }

                    if (_isUat)
                    {
                        return "month";
                    }

                    if (_isDev)
                    {
                        return "month";
                    }

                    return "month";
                }
            }
            public int DefaultTaskSelectionCachingDay
            {
                get
                {
                    if (_isProduction)
                    {
                        return 7;
                    }

                    if (_isUat)
                    {
                        return 7;
                    }

                    if (_isDev)
                    {
                        return 7;
                    }

                    return 7;
                }
            }
        }
    }
}