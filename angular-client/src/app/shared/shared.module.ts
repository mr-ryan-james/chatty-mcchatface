import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
// UserNamesPipe is standalone and imported directly where needed

@NgModule({
  declarations: [], // Standalone pipes are not declared in NgModules
  imports: [CommonModule],
  exports: [], // Standalone pipes are not exported from NgModules
})
export class SharedModule {}
