import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { Curve, TimeSeriesPoint } from '../models/app-models';
import { RenewAbleService } from '../services/renewable.service';

interface ChartPoint {
  x: number;
  y: number;
  label: string;
  value: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatTableModule
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  private readonly renewableService = inject(RenewAbleService);

  searchText = '';
  curves: Curve[] = [];
  selectedCurve: Curve | null = null;
  series: TimeSeriesPoint[] = [];
  chartPoints: ChartPoint[] = [];
  chartPath = '';
  start = this.toLocalInputValue(new Date(Date.now() - 24 * 60 * 60 * 1000));
  end = this.toLocalInputValue(new Date());
  asOf = '';
  isLoading = false;
  errorMessage = '';
  displayedColumns = ['timestamp', 'value', 'asOf'];

  searchCurves(): void {
    this.errorMessage = '';
    this.renewableService.searchCurves(this.searchText).subscribe({
      next: curves => {
        this.curves = curves;
        if (!this.selectedCurve && curves.length === 1) {
          this.selectCurve(curves[0]);
        }
      },
      error: () => this.errorMessage = 'Unable to search curves.'
    });
  }

  selectCurve(curve: Curve): void {
    this.selectedCurve = curve;
    this.loadSeries();
  }

  loadSeries(): void {
    if (!this.selectedCurve) {
      this.errorMessage = 'Select a curve first.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.renewableService.getCurveSeries(
      this.selectedCurve.meterPointId,
      this.toIso(this.start),
      this.toIso(this.end),
      this.asOf ? this.toIso(this.asOf) : undefined
    ).subscribe({
      next: points => {
        this.series = points;
        this.buildChart(points);
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load time-series data.';
        this.isLoading = false;
      }
    });
  }

  private buildChart(points: TimeSeriesPoint[]): void {
    if (points.length === 0) {
      this.chartPoints = [];
      this.chartPath = '';
      return;
    }

    const width = 760;
    const height = 260;
    const padding = 28;
    const values = points.map(point => point.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = Math.max(max - min, 1);

    this.chartPoints = points.map((point, index) => {
      const x = points.length === 1
        ? width / 2
        : padding + (index * (width - padding * 2)) / (points.length - 1);
      const y = height - padding - ((point.value - min) * (height - padding * 2)) / range;

      return {
        x,
        y,
        label: new Date(point.timestamp).toLocaleString(),
        value: point.value
      };
    });

    this.chartPath = this.chartPoints
      .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`)
      .join(' ');
  }

  private toIso(value: string): string {
    return new Date(value).toISOString();
  }

  private toLocalInputValue(date: Date): string {
    const offset = date.getTimezoneOffset();
    const local = new Date(date.getTime() - offset * 60 * 1000);
    return local.toISOString().slice(0, 16);
  }
}
