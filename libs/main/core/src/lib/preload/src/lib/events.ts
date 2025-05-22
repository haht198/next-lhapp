export const API_EVENTS = {
  GET_APPLICATION_STATE: 'getApplicationState',
  CHANGE_APPLICATION_STATE: 'changeApplicationState',

  NET_SERVICE: {
    INSTALL: 'netServiceInstall',
    UNINSTALL: 'netServiceUninstall',
    CHECK_VERSION: 'netServiceCheckVersion',
    DOWNLOAD: 'netServiceDownload',
  // UNZIP: 'netServiceUnzip',
    START: 'netServiceStart',
    STOP: 'netServiceStop',

    TEST_UPLOAD: 'netServiceTestUpload',
  },
  FILE_SYSTEM: {
    GET_FILE_INFO: 'fileSystemGetFileInfo',
    GET_FILE_SIZE: 'fileSystemGetFileSize',
  },
};
