import { Pipe, PipeTransform } from '@angular/core';
import * as moment from 'moment';

@Pipe({name: 'moment'})
export class MomentPipe implements PipeTransform {
  transform(dateString: String): String {
    return moment(dateString).format("MMMM Do YYYY, h:mm:ss a");
  }
}