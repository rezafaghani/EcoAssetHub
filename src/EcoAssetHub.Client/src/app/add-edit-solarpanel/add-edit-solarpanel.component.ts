import { Component, Inject, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { RenewAbleService } from '../services/renewable.service';
import { SolarPanel } from '../models/app-models';

@Component({
  selector: 'app-add-edit-solarpanel',
  templateUrl: './add-edit-solarpanel.component.html',
  // styleUrls: ['./add-edit-solarpanel.component.css']
})
export class AddEditSolarpanelComponent implements OnInit {
  solarForm: FormGroup;
  constructor(
    private renewableService: RenewAbleService,
    public dialogRef: MatDialogRef<AddEditSolarpanelComponent>,
    @Inject(MAT_DIALOG_DATA) public editData: SolarPanel // Use your solar panel model type here
  ) {
    // Initialize the form group
    this.solarForm = new FormGroup({
      meterPointId: new FormControl('', Validators.required),
      capacity: new FormControl('', Validators.required),
      compassOrientation: new FormControl('', Validators.required),
      // ... other form controls ...
    });
  }

  ngOnInit(): void {
    // If editing, initialize form with existing data
    if (this.editData && this.editData) {
      this.solarForm.patchValue(this.editData);
    }
  }

  onSubmit() {
    if (this.solarForm.valid) {
      if (this.editData && this.editData) {
        // Update existing solar panel
        this.renewableService.updateSolarPanel(this.solarForm.value).subscribe(() => {
          this.dialogRef.close(true);
        });
      } else {
        // Add new solar panel
        this.renewableService.addSolarPanel(this.solarForm.value).subscribe(() => {
          this.dialogRef.close(true);
        });
      }
    }
  }

  onCancel() {
    this.dialogRef.close();
  }
}
