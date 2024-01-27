import { Component, inject } from '@angular/core';
import { RenewAbleService } from '../services/renewable.service';
import { Renewable, RenewableAssetType } from '../models/app-models';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { AddEditSolarpanelComponent } from '../add-edit-solarpanel/add-edit-solarpanel.component';
import { AddEditWindturbineComponent } from '../add-edit-windturbine/add-edit-windturbine.component';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  renewAbleList: Renewable[] = [];
  renewableService: RenewAbleService = inject(RenewAbleService);
  dialog: MatDialog = inject(MatDialog);
  ModalTitle = '';
  ActivateAddEditSolar: boolean = false;
  ActivateAddEditWindturbin: boolean = false;
  windTurbine: any;
  SolarPanel: any;
  RenewableAssetType = RenewableAssetType;
  displayedColumns: string[] = ['index','meterPointId', 'capacity', 'compassOrientation','hubHeight','type','actions'];
  constructor() {
    this.renewableService.getRenewAbleList().subscribe((data) => {
      this.renewAbleList = data;
    });
  }
  addWindTurbineClick() {
    const dialogRef = this.dialog.open(AddEditWindturbineComponent, {
      width: '250px',
      data: {
        hubHeight: 0,
        rotorDiameter: 0,
        capacity: 0,
        meterPointId: 0,
        id:''
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if(result){
        this.refreshRenewAbleList();
      }
    });
  }
  addSolarPanelClick() {
    const dialogRef = this.dialog.open(AddEditSolarpanelComponent, {
      width: '250px',
      data: {
        compassOrientation: '',
        capacity: 0,
        meterPointId: 0,
        id: '',
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.refreshRenewAbleList();
      }
    });
  }
  openEditDialog(element: Renewable) {
    const dialogRef =
      element.type == RenewableAssetType.SolarPanel
        ? this.dialog.open(AddEditSolarpanelComponent, {
            width: '250px',
            data: element,
          })
        : this.dialog.open(AddEditWindturbineComponent, {
            width: '250px',
            data: element,
          });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.refreshRenewAbleList();
      }
    });
  }

  openDeleteDialog(id: string) {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '250px',
      data: { message: 'Are you sure you want to delete this item?' }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.renewableService.deleteRenewable(id).subscribe(() => {
          this.refreshRenewAbleList();
        });
      }
    });
  }
  refreshRenewAbleList() {
    this.renewableService.getRenewAbleList().subscribe((data) => {
      this.renewAbleList = data;
    });
  }
}
