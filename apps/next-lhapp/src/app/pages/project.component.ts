import { Component } from "@angular/core";

@Component({
  selector: "app-project",
  template: `
    <div class="project-page">
      <h1>Project Page</h1>
    </div>
  `,
  styles: [
    `
      .project-page {
        padding: 20px;
        background-color: #f0f0f0;
        border-radius: 8px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }
      h1 {
        color: #333;
        font-size: 24px;
        margin-bottom: 10px;
      }
      p {
        color: #666;
        font-size: 16px;
        line-height: 1.5;
      }
    `
  ],
  standalone: true,
  imports: []
})
export class ProjectComponent {

}
