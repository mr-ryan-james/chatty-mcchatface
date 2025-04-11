import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ButtonsModule } from 'ngx-bootstrap/buttons';
// UserNamesPipe is standalone and imported directly where needed

@NgModule({
  declarations: [], // Standalone pipes are not declared in NgModules
  imports: [CommonModule, BsDropdownModule.forRoot(), ButtonsModule.forRoot()],
  exports: [BsDropdownModule, ButtonsModule], // Export the modules so components can use them
})
export class SharedModule {}
