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