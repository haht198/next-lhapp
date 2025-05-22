import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CFAPIService } from '../services/cf-api.services';
import { AuthService } from '../services/auth.service';
import { CFAssetFile, InternalPostTask, internalPostWorkflow, ShootingType, TaskDetail, TaskList, Workflow, WorkflowStatus } from '../models/task.model';
import { CommonModule } from '@angular/common';
import { combineLatest, firstValueFrom, from, map, of, switchMap, tap } from 'rxjs';
import { SHOOTING_TYPE_CATEGORY_ID } from '../constants/task';
import { FormsModule } from '@angular/forms';
import { WorkflowStatusDisplayPipe, FileSizeDisplayPipe } from './workflow-status.pipe';
import { MatExpansionModule } from '@angular/material/expansion';
import { TaskUploader } from '../services/uploader/task-uploader';
import { GetFilesToUploadResponse } from '../services/uploader/config';
import { PresignedUrlErrorAssetFile, PresignedUrlAssetFile, GetPresignedUrlResult } from '../services/uploader/types';

    @Component({
  selector: 'app-internal-post',
  templateUrl: './internal-post.component.html',
  styleUrls: ['./internal-post.component.scss'],
  imports: [CommonModule, FormsModule, WorkflowStatusDisplayPipe, FileSizeDisplayPipe, MatExpansionModule,],
})
export class InternalPostComponent implements OnInit {
  cfApiService = inject(CFAPIService);
  authService = inject(AuthService);
  taskList = signal<TaskList[]>([]);
  shootingTypes = signal<ShootingType[]>([]);
  selectedProductionType = signal<number>(3);
  activeTask = signal<InternalPostTask | null>(null);
  workflow = signal<Workflow[]>(internalPostWorkflow);
  WorkflowStatus = WorkflowStatus;

  sessionData = signal<Map<string, any>>(new Map());

  filteredTaskList = computed(() => {
    switch (this.selectedProductionType()) {    
      case 1:
        return this.taskList();
      case 2:
        return this.taskList().filter((task) => task.isVideo);
      case 3:
        return this.taskList().filter((task) => !task.isVideo);
      default:
        return this.taskList();
    }
  });

  get objectKeys() {
    return Object.keys;
  }

  get sessionKeys(): string[] {
    return Array.from(this.sessionData().keys());
  }

  readonly productionTypes = [
    {
      id: 1,
      name: 'All    ',
    },
    {
      id: 2,
      name: 'Video',
    },
    {
      id: 3,
      name: 'Photo',
    },
  ];

  private _taskUploader: TaskUploader | null = null;

  ngOnInit() {
    this.authService.token$.subscribe((token) => {
      if (!token) { 
        this.taskList.set([]);
        return;
      }
      this.getListTasks();
       // Init task uploader
    this._taskUploader = new TaskUploader({
        clientId: 'hue',
        getFilesToUpload: (taskId: string) => this.getFilesToUpload(taskId),
        getPresignedUrl: (payload: any) => this.getPresignedUrl(payload),
        submitTask: () => {
          return Promise.resolve(null);
        },
      });
    });

    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    //@ts-ignore
    window.__internalPost = this;
   
  }

 

  startTask(taskId: string) {
    console.log('Start task', taskId);
    if (!taskId || taskId === this.activeTask()?.taskId) {
      return;
    }
    this.resetWorkflow();
    this.updateWorkflowStatus('GET_TASK_DETAIL', WorkflowStatus.DOING);
    this.cfApiService.getTaskDetail([taskId]).pipe(
        map((res) => {
            console.log('Get task detail', res);
            if (res.data) {
                this.activeTask.set(new InternalPostTask(res.data[0]));
                this.sessionData.update((sessionData) => {
                    sessionData.set('Task Detail', res.data[0]);
                    return sessionData;
                });
                this.updateWorkflowStatus('GET_TASK_DETAIL', WorkflowStatus.DONE);
                return  this.activeTask();
            } else {
                this.updateWorkflowStatus('GET_TASK_DETAIL', WorkflowStatus.ERROR);
                return null;
            }
        }),
        switchMap((task) => this.getAssetsInfo(task)),
        switchMap(() => this.selectAssets()),
        switchMap(() => this.uploadTask()),
    ).subscribe((res) => {
        if (res) {
            console.log('Finish', res);
        }
    });
  }

  getListTasks() {
    combineLatest([
      this.cfApiService.getMyTasks(),
      this.cfApiService.getAllShootingType(),
    ])
      .pipe(
        map(([tasks, shootingTypes]) => {
          return {
            tasks: tasks.data.pageData,
            shootingTypes: shootingTypes.data.pageData,
          };
        })
      )
      .subscribe(({ tasks, shootingTypes }) => {
        console.log('Fetch data success', { tasks, shootingTypes });
        this.shootingTypes.set(shootingTypes);
        this.taskList.set(
          tasks.map((task: TaskList) => ({
            ...task,
            isVideo: this.checkIsVideo(task.shootingTypeId),
          }))
        );
      });
    // this.cfApiService.getMyTasks().subscribe((res) => {
    //     console.log(res);
    //     if (!res || !res.data || res.metadata.code !== 'GEN-0') {
    //         console.error('Error get list tasks', res);
    //         return;
    //     }
    //    this.taskList.set(res.data.pageData || []);
    // });
  }

  changeProductionType(productionTypeId: number) {
    this.selectedProductionType.set(Number(productionTypeId));
  }

  resetWorkflow() {
    this.workflow.update((workflow) => {
      return workflow.map((w) => ({ ...w, status: WorkflowStatus.PENDING }));
    });
    this.sessionData.set(new Map());
  }

  updateWorkflowStatus(workflowId: string, status: WorkflowStatus) {
    this.workflow.update((workflow) => {
      const workflowIndex = workflow.findIndex((w) => w.id === workflowId);
      if (workflowIndex >= 0) {
        workflow[workflowIndex].status = status;
      }
      return workflow;
    });
  }

  private checkIsVideo(shootingTypeId: number) {
    return (
      this.shootingTypes()
        .filter(
          (shootingType) =>
            shootingType.shootingTypeCategoryId ===
            SHOOTING_TYPE_CATEGORY_ID.VIDEO
        )
        .findIndex(
          (shootingType) => shootingType.shootingTypeId === shootingTypeId
        ) >= 0
    );
  }

  private getAssetsInfo(task: InternalPostTask | null) {
    if (!task) {
      return of(null);
    }
    // Update status of workflow
    this.updateWorkflowStatus('DOWNLOAD_ASSETS', WorkflowStatus.DOING);
    const assets = task.assetLifeCycles.flatMap((asset) => {
        return asset.assets.filter(a => asset.inputAssetIds.includes(a.assetId));   
    });
    return this.cfApiService.getAssetFiles(assets.map((a) => a.assetId), [6,8]).pipe(
        map((res) => {
            if (!res || !res.data || res.metadata.code !== 'GEN-0') {
                console.error('Error get asset files', res);
                return null;
            }
            const cfAssetFiles = res.data as CFAssetFile[];
            this.activeTask.update((task) => {
                if (!task) {
                    return null;
                }
                for (const assetLifeCycle of task.assetLifeCycles) {
                    // add cfAssetFiles info to assetLifeCycle
                    for (const asset of assetLifeCycle.assets) {
                       const cfAssetFile = cfAssetFiles.find((a) => a.assetId === asset.assetId);
                       if (!cfAssetFile) {
                           continue;
                       }
                       if (!assetLifeCycle.cfAssetFile) {
                        assetLifeCycle.cfAssetFile = [];
                       }
                       assetLifeCycle.cfAssetFile.push(cfAssetFile);
                    }
                }
                return task;
            });
            
            // Update status of workflow
            this.updateWorkflowStatus('DOWNLOAD_ASSETS', WorkflowStatus.DONE);
            console.log('Assets Life Cycles', task.assetLifeCycles);
            // update session data
            this.sessionData.update((sessionData) => {
                sessionData.set('Assets', task.assetLifeCycles.map(a => {
                    return {
                        assetLifeCycleId: a.assetLifeCycleId,
                        cfAssetFiles: a.cfAssetFile?.map((cfAssetFile) => {
                            return {
                                fileName: cfAssetFile.fileName,
                                fileId: cfAssetFile.fileId,
                                fileLength: cfAssetFile.length,
                                assetId: cfAssetFile.assetId,
                                fileUrl: cfAssetFile.fileUrl,
                                smallThumbnailUrl: cfAssetFile.smallThumbnailUrl,
                            }
                        })
                    }
                }));
                return sessionData;
            });
            return task.assetLifeCycles;
        })
    )
  
  }

  private selectAssets() {
    if (!this.activeTask()) {
      return of(null);
    }
    // Update status of workflow
    this.updateWorkflowStatus('SELECT_ASSETS', WorkflowStatus.DOING);
    // add session data
    const selectedAssetsData = this.activeTask()?.assetLifeCycles.map(assetLifeCycle => {
        return {
            assetLifeCycleId: assetLifeCycle.assetLifeCycleId,
            cfAssetFiles: assetLifeCycle.cfAssetFile,
            positionName: assetLifeCycle.positionName,
        }   
    });
    console.log('Selected Assets Data', selectedAssetsData);
    this.sessionData.update((sessionData) => {
        sessionData.set('Selected Assets', selectedAssetsData);
        return sessionData;
    });
    // Update status of workflow
    this.updateWorkflowStatus('SELECT_ASSETS', WorkflowStatus.DONE);
    return of(true);
  }

  private uploadTask() {
  
   console.log('Upload task', this._taskUploader);
   // Update status of workflow
   this.updateWorkflowStatus('UPLOAD_ASSETS', WorkflowStatus.DOING);
   if (!this._taskUploader) {
    console.error('Task uploader not found');
    this.updateWorkflowStatus('UPLOAD_ASSETS', WorkflowStatus.ERROR);
    return of(null);
    }
   return from(this._taskUploader.start(this.activeTask()?.taskId || ''));
  }

  private async getFilesToUpload(taskId: string) {
    // find task in taskList
    const task = this.taskList().find((t) => t.taskId === taskId);
    if (!task) {
      console.error('Task not found', taskId);
      return Promise.resolve(null);
    }
    const result: GetFilesToUploadResponse = {
      taskId: taskId,
      clientId: task.clientId,
      files: [],
    };
    // check has task detail
    let taskDetail: InternalPostTask | null = this.activeTask();
    if (taskDetail?.taskId !== taskId) {
        // get task detail
        const res = await firstValueFrom(this.cfApiService.getTaskDetail([taskId]));
        if (!res || !res.data || res.metadata.code !== 'GEN-0') {
            console.error('Task detail not found', taskId);
            return Promise.resolve(null);
        }
        taskDetail = new InternalPostTask(res.data[0]);
    }

    // Get expected output name
    const res = await firstValueFrom(this.cfApiService.getTaskExpectedOutputName(taskId));
    if (!res || !res.data || res.metadata.code !== 'GEN-0') {
        console.error('Expected output name not found', taskId);
        return Promise.resolve(null);
    }
    const expectedOutputNameResult = res.data as Record<string, Array<{
        expectedOutputFilename: string;
        specId?: string;
    }>>;
    console.log('Expected output name', expectedOutputNameResult);

    console.warn('[Uploader] Mock upload file local path', `/Volumes/DATA/haht/Images/Auto transfer 2/Transfer203.jpg` );
    for (const assetLifeCycle of taskDetail?.assetLifeCycles || []) {
        const expectedOutputNameData = expectedOutputNameResult[assetLifeCycle.assetLifeCycleId];

        if (!assetLifeCycle.cfAssetFile) {
            continue;
        }
        // No output spec
        if(assetLifeCycle.outputSpecs.length === 0) {
            for (const asset of assetLifeCycle.cfAssetFile) {
                result.files.push({
                    localId: asset.assetId,
                localPath: `/Volumes/DATA/haht/Images/Auto transfer 2/Transfer203.jpg`,
                expectedOutputName: expectedOutputNameData[0].expectedOutputFilename,
            }) ;}
        }
        // Has output spec
        for (const spec of assetLifeCycle.outputSpecs) {
            const expectedOutputFilename = expectedOutputNameData.find((e) => e.specId === spec.specId);
            
            result.files.push({
                localId: spec.specId,
                localPath: `/Volumes/DATA/haht/Images/Auto transfer 2/Transfer203.jpg`,
                expectedOutputName: expectedOutputFilename?.expectedOutputFilename + '.jpg',
            });
        }
    }
    return Promise.resolve(result);
  }


  private async getPresignedUrl(payload: any) {
    console.log('[Uploader] Get presigned url', payload);
    const res = await firstValueFrom(this.cfApiService.getAssetPresignedUrl(payload));
    if (!res || !res.data || res.metadata.code !== 'GEN-0') {
        console.error('Error get asset presigned url', res);
        return Promise.resolve(null);
    }
    return Promise.resolve(res.data as GetPresignedUrlResult<PresignedUrlAssetFile, PresignedUrlErrorAssetFile>);
  }




  

}
