import { RenewAbleService } from '../services/renewable.service';
import { Component, Inject, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators ,ReactiveFormsModule} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { WindTurbine } from '../models/app-models';

@Component({
  selector: 'app-add-edit-windturbine',
  templateUrl: './add-edit-windturbine.component.html',
  styleUrl: './add-edit-windturbine.component.css'
})
export class AddEditWindturbineComponent {
  windForm: FormGroup;
  constructor(
    private renewableService: RenewAbleService,
    public dialogRef: MatDialogRef<AddEditWindturbineComponent>,
    @Inject(MAT_DIALOG_DATA) public editData: WindTurbine // Use your wind turbin model type here
  ) {
    // Initialize the form group
    this.windForm = new FormGroup({
      meterPointId: new FormControl('', Validators.required),
      capacity: new FormControl('', Validators.required),
      hubHeight: new FormControl('', Validators.required),
      rotorDiameter: new FormControl('', Validators.required),
      // ... other form controls ...
    });
  }

  ngOnInit(): void {
    if (this.editData && this.editData) {
      this.windForm.patchValue(this.editData);

    }
  }

  onSubmit() {
    if (this.windForm.valid) {
      if (this.editData && this.editData.id) {
        // Update existing solar panel
        this.renewableService.updateWindTurbine(this.windForm.value).subscribe(() => {
          this.dialogRef.close(true);
        });
      } else {
        // Add new solar panel
        this.renewableService.addWindTurbine(this.windForm.value).subscribe(() => {
          this.dialogRef.close(true);
        });
      }
    }
  }

  onCancel() {
    this.dialogRef.close();
  }
}
