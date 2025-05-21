export interface TaskList {
   taskId: string,
   assignedDate: number,
   clientId: string,
   clientName: string,
   coverAssetId: string,
   jobCode: string,
   jobId: string,
   jobName: string,
   productCode: string,
   productId: string,
   productName: string,
   shootingTypeId: number,
   stepId: number,
   taskStatusId: number,
   totalAssets: number,

   isVideo?: boolean,
}

export interface ShootingType {
    createdDatetimeUtc: number,
    isDisabled: boolean,
    isShootingTypeCategoryDisable: boolean,
    isSystemDefined: boolean,
    shootingTypeCategoryId: number,
    shootingTypeId: number,
    shootingTypeName: string,
}

export interface TaskDetail {
  taskId: string;
  stepId: number;
  isNotAllowAutoQa: boolean;
  assetLifeCycles: AssetLifeCycle[];
  colorReferenceFiles: any[];
  retouchingBrief: {
    fileIds: string[];
    goodExamplesIds: string[];
    badExamplesIds: string[];
  };
  videoBrief: {
    fileIds: string[];
  };
  otherWorkUnitOutputs: any[];
  allowVerifyFlowControl: boolean;
  allowReject: boolean;
  allowBypass: boolean;
  transitionOptions: any[];
}

export interface AssetLifeCycle {
  assetLifeCycleId: string;
  finalSelectionAssetId: string;
  positionId: number;
  positionName: string;
  outputSpecs: any[];
  assets: Asset[];
  inclipAssetIds: string[];
  resourceAssetIds: string[];
  inputAssetIds: string[];
  styleGuideGroupId: number;
  shootingTypeId: number;
  styleGuideId: string;
  cfAssetFile?: CFAssetFile[];
}

export interface Asset extends CFAssetFile {
  assetId: string;
  stepId: number;
  isRejected: boolean;
  assetAttribute: number;
}

export class InternalPostTask {
    taskId: string;
    stepId: number;
    assetLifeCycles: AssetLifeCycle[];
    
    constructor(taskDetail: TaskDetail) {
      this.taskId = taskDetail.taskId;
      this.stepId = taskDetail.stepId;
      this.assetLifeCycles = taskDetail.assetLifeCycles;
    }
}

export interface Workflow {
    id: string;
    name: string;
    description: string;
    status: WorkflowStatus;
}

export enum WorkflowStatus {
    PENDING = 1000,
    DOING = 2000,
    DONE = 3000,
    ERROR = 4000,
}

export const internalPostWorkflow: Workflow[]   = [
   {
    id: 'GET_TASK_DETAIL',
    name: 'Get Task Detail',
    description: 'Get Task Detail',
    status: WorkflowStatus.PENDING,
   },
   {
    id: 'DOWNLOAD_ASSETS',
    name: 'Download Assets',
    description: 'Download Assets',
    status: WorkflowStatus.PENDING,
   },
   {
    id: 'SELECT_ASSETS',
    name: 'Select Assets',
    description: 'Select Assets',
    status: WorkflowStatus.PENDING,
   },
   {
    id: 'UPLOAD_ASSETS',
    name: 'Upload Assets',
    description: 'Upload Assets',
    status: WorkflowStatus.PENDING,
   },
]

export interface CFAssetFile {
    assetId: string;
    fileId: string;
    fileCreatedDatetimeUtc: number;
    fileName: string;
    fileUrl: string;
    length: number;
    mediaTypeId: number;
    smallThumbnailUrl: string;
}
