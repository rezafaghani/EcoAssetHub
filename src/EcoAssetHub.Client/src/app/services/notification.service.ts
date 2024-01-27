import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  constructor(private snackBar: MatSnackBar) {}

  showSuccess(message: string): void {
    this.snackBar.open(message, 'OK', {
      duration: 3000, // Duration of the snackbar
      panelClass: ['success-snackbar'] // Add custom CSS class for styling
    });
  }

  showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000, // Duration of the snackbar
      panelClass: ['error-snackbar'] // Add custom CSS class for styling
    });
  }
}
