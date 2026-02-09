import { RenewAbleService } from '../services/renewable.service';
import { Component, Inject } from '@angular/core';
import { FormGroup, FormControl, Validators ,ReactiveFormsModule} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { WindTurbine } from '../models/app-models';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-add-edit-windturbine',
  standalone:true,
  imports:[ReactiveFormsModule,MatDialogModule,MatFormFieldModule,MatInputModule,MatButtonModule],
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

  // data injected; patch in ctor not needed

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
