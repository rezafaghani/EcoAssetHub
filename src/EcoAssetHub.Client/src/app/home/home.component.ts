import { Component, inject } from '@angular/core';
import { RenewAbleService } from '../services/renewable.service';
import { Renewable, RenewableAssetType } from '../models/app-models';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})

export class HomeComponent {
  renewAbleList: Renewable[] = [];
  renewableService: RenewAbleService = inject(RenewAbleService);
  ModalTitle = "";
  ActivateAddEditSolar: boolean = false;
  ActivateAddEditWindturbin: boolean = false;
  windTurbine: any;
  SolarPanel: any;
  RenewableAssetType = RenewableAssetType;

  constructor() {
    this.renewableService.getRenewAbleList().subscribe(data => {
      this.renewAbleList = data;
    });
  }
  //wind turbin functions

  addWindTurbineClick() {
    this.windTurbine = {
      hubHeight: 0,
      rotorDiameter: 0,
      capacity: 0,
      meterPointId: 0
    }
    this.ModalTitle = "Add Wind Turbine";
    this.ActivateAddEditWindturbin = true;
  }
  editWindTurbineClick(item: any) {
    this.windTurbine = item;
    this.ModalTitle = "Edit Wind Turbine";
    this.ActivateAddEditWindturbin = true;
  }

  //solar panel

  addSolarPanelClick() {
    this.SolarPanel = {
      compassOrientation: "",
      capacity: 0,
      meterPointId: 0,
      id: ''
    }
    this.ModalTitle = "Add Wind Turbine";
    this.ActivateAddEditSolar = true;
  }
  editSolarPanelClick(item: any) {
    console.log(item);
    this.SolarPanel = item;
    this.ModalTitle = "Edit Solar Panel";
    this.ActivateAddEditSolar = true;
  }
  //share functions
  closeClick() {
    this.ActivateAddEditSolar = false;
    this.ActivateAddEditWindturbin = false;
    this.refreshRenewAbleList();
  }

  deleteClick(item: any) {
    if (confirm('Are you sure??')) {
      this.renewableService.deleteRenewable(item.id).subscribe(() => {
         this.refreshRenewAbleList();
      })
    }
  }
  refreshRenewAbleList() {
    this.renewableService.getRenewAbleList().subscribe(data => {
      this.renewAbleList = data;
    });
  }
}
