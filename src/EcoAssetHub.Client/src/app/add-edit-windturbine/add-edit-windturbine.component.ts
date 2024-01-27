import { Component, Input, inject } from '@angular/core';
import { RenewAbleService } from '../services/renewable.service';
import { WindTurbine } from '../models/app-models';
import { NotificationService } from '../services/notification.service';
import { catchError, tap } from 'rxjs/operators';
import { throwError } from 'rxjs';

@Component({
  selector: 'app-add-edit-windturbine',
  templateUrl: './add-edit-windturbine.component.html',
  styleUrl: './add-edit-windturbine.component.css'
})
export class AddEditWindturbineComponent {
  renewableService: RenewAbleService = inject(RenewAbleService);
  notificationService: NotificationService = inject(NotificationService);
  @Input() windTurbine!: WindTurbine;
  meterPointId = 0;
  hubHeight = 0;
  rotorDiameter = 0;
  id = '';
  capacity = 0;
  ngOnInit(): void {

    this.meterPointId = this.windTurbine.meterPointId;
    this.hubHeight = this.windTurbine.hubHeight;
    this.rotorDiameter = this.windTurbine.rotorDiameter;
    this.id = this.windTurbine.id;
    this.capacity = this.windTurbine.capacity;
  }
  addWindTurbine() {
    var val = ({
      meterPointId: this.meterPointId,
      hubHeight: this.hubHeight,
      rotorDiameter: this.rotorDiameter,
      capacity: this.capacity
    } as WindTurbine);

    this.renewableService.addWindTurbine(val).pipe(
      tap(res => {
        this.notificationService.showSuccess('Wind turbine added successfully!');
        // Other success logic...
      }),
      catchError(error => {
        this.notificationService.showError('Failed to add wind turbine.');
        // Other error logic...
        return throwError(() => error);  // Rethrow the error if you need to handle it later.
      })
    ).subscribe();
  }

  updateWindTurbine() {
    var val = ({
      id: this.id,
      meterPointId: this.meterPointId,
      hubHeight: this.hubHeight,
      rotorDiameter: this.rotorDiameter,
      capacity: this.capacity
    } as WindTurbine);

    this.renewableService.updateWindTurbine(val).pipe(
      tap(res => {
        this.notificationService.showSuccess('Wind turbine updated successfully!');
        // Other success logic...
      }),
      catchError(error => {
        this.notificationService.showError('Failed to update wind turbine.');
        // Other error logic...
        return throwError(() => error);  // Rethrow the error if you need to handle it later.
      })
    ).subscribe();
  }
}
