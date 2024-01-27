import { Component, Input, inject } from '@angular/core';
import { SolarPanel } from '../models/app-models';
import { RenewAbleService } from '../services/renewable.service';
import { NotificationService } from '../services/notification.service';
import { catchError, tap } from 'rxjs/operators';
import { throwError } from 'rxjs';

@Component({
  selector: 'app-add-edit-solarpanel',
  templateUrl: './add-edit-solarpanel.component.html',
  styleUrl: './add-edit-solarpanel.component.css'
})
export class AddEditSolarpanelComponent {
  renewableService: RenewAbleService = inject(RenewAbleService);
  notificationService: NotificationService = inject(NotificationService);
  @Input() solarpanel!: SolarPanel;
  meterPointId = 0;
  capacity = 0;
  compassOrientation = '';
  id = '';

  ngOnInit(): void {

    this.meterPointId = this.solarpanel.meterPointId;
    this.capacity = this.solarpanel.capacity;
    this.compassOrientation = this.solarpanel.compassOrientation;
    this.id = this.solarpanel.id;

  }
  addsolarpanel() {
    var val = ({
      meterPointId: this.meterPointId,
      capacity: this.capacity,
      compassOrientation: this.compassOrientation
    } as SolarPanel);

    this.renewableService.addSolarPanel(val).pipe(
      tap(res => {
        this.notificationService.showSuccess('Solar panel added successfully!');
        // Other success logic...
      }),
      catchError(error => {
        this.notificationService.showError('Failed to add Solar panel.');
        // Other error logic...
        return throwError(() => error); // Rethrow the error if you need to handle it later.
      })
    ).subscribe();
  }

  updatesolarpanel() {
    var val = ({
      id: this.id,
      meterPointId: this.meterPointId,
      capacity: this.capacity,
      compassOrientation: this.compassOrientation
    } as SolarPanel);

    this.renewableService.updateSolarPanel(val).pipe(
      tap(res => {
        this.notificationService.showSuccess('Solar panel updated successfully!');
      }),
      catchError(error => {
        this.notificationService.showError('Failed to update Solar panel.');
        // Other error logic...
        return throwError(() => error); // Rethrow the error if you need to handle it later.
      })
    ).subscribe();
  }
}
