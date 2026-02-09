import { Component, Inject } from '@angular/core';
import { FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { RenewAbleService } from '../services/renewable.service';
import { SolarPanel } from '../models/app-models';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-add-edit-solarpanel',
  standalone: true,
  imports:[ReactiveFormsModule,MatDialogModule,MatFormFieldModule,MatInputModule,MatButtonModule],
  templateUrl: './add-edit-solarpanel.component.html',
  // styleUrls: ['./add-edit-solarpanel.component.css']
})
export class AddEditSolarpanelComponent {
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

  // lifecycle hook not needed; data is available in ctor

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
