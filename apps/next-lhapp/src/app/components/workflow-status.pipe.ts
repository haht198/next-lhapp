import { Pipe, PipeTransform } from "@angular/core";
import { WorkflowStatus } from "../models/task.model";

@Pipe({
    name: 'workflowStatusDisplay'
})
export class WorkflowStatusDisplayPipe implements PipeTransform {
  transform(value: number): string {
    switch (value) {
      case WorkflowStatus.PENDING:
        return 'Pending';
      case WorkflowStatus.DOING:
        return 'Doing';
      case WorkflowStatus.DONE:
        return 'Done';
      case WorkflowStatus.ERROR:
        return 'Error';
      default:
        return 'Unknown';
    }
  }
}


@Pipe({
    name: 'fileSizeDisplay'
})
export class FileSizeDisplayPipe implements PipeTransform {
  transform(value: number): string {
    return (value / 1024 / 1024).toFixed(2) + ' MB';
  }
}   