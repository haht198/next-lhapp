import { Component, inject, signal } from '@angular/core';
import { CFAPIService } from '../services/cf-api.services';
import { AuthService } from '../services/auth.service';
import { ShootingType, TaskList } from '../models/task.model';
import { CommonModule } from '@angular/common';
import { combineLatest, map } from 'rxjs';
import { SHOOTING_TYPE_CATEGORY_ID } from '../constants/task';
@Component({
  selector: 'app-internal-post',
  templateUrl: './internal-post.component.html',
  styleUrls: ['./internal-post.component.scss'],
  imports: [CommonModule],
})
export class InternalPostComponent {
  cfApiService = inject(CFAPIService);
  authService = inject(AuthService);
  taskList = signal<TaskList[]>([]);
  shootingTypes = signal<ShootingType[]>([]);

  ngOnInit() {
    this.authService.token$.subscribe((token) => {
      if (!token) { 
        this.taskList.set([]);
        return;
      }
      this.getListTasks();
    });
  }

  startTask(taskId: string) {
    console.log('Start task', taskId);
    this.cfApiService.getTaskDetail([taskId]).subscribe((res) => {
      console.log('Get task detail', res);
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
}
