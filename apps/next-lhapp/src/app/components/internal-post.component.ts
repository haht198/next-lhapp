import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CFAPIService } from '../services/cf-api.services';
import { AuthService } from '../services/auth.service';
import { CFAssetFile, InternalPostTask, internalPostWorkflow, ShootingType, TaskDetail, TaskList, Workflow, WorkflowStatus } from '../models/task.model';
import { CommonModule } from '@angular/common';
import { combineLatest, map, of, switchMap, tap } from 'rxjs';
import { SHOOTING_TYPE_CATEGORY_ID } from '../constants/task';
import { FormsModule } from '@angular/forms';
import { WorkflowStatusDisplayPipe, FileSizeDisplayPipe } from './workflow-status.pipe';
import { MatExpansionModule } from '@angular/material/expansion';

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

  ngOnInit() {
    this.authService.token$.subscribe((token) => {
      if (!token) { 
        this.taskList.set([]);
        return;
      }
      this.getListTasks();
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
    return of(null);
  }


  
  get objectKeys() {
    return Object.keys;
  }

  get sessionKeys(): string[] {
    return Array.from(this.sessionData().keys());
  }
}
