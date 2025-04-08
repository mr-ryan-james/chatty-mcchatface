import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserNamesPipe } from './pipes/user-names.pipe';

@NgModule({
  declarations: [UserNamesPipe],
  imports: [CommonModule],
  exports: [UserNamesPipe],
})
export class SharedModule {}
