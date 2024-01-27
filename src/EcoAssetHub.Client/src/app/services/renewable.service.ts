// solar-panel.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Renewable, SolarPanel, WindTurbine } from '../models/app-models';

@Injectable({
  providedIn: 'root'
})
export class RenewAbleService {
  public apiUrl = 'https://localhost:7115/api';
  //https://localhost:7115/api/Renewables

  constructor(private http: HttpClient) { }


  //share
  getRenewAbleList(): Observable<Renewable[]> {
    return this.http.get<Renewable[]>(this.apiUrl + '/Renewables');
  }


  deleteRenewable(deptId: string): Observable<HttpResponse<any>> {
    return this.http.delete(this.apiUrl + '/Renewables/' + deptId, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
      observe: 'response'
    });
  }
  //solar panel metods


  addSolarPanel(solarPanelData: SolarPanel): Observable<HttpResponse<any>> {
    return this.http.post<any>(this.apiUrl + '/SolarPanels', solarPanelData, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
      observe:'response'
    });
  }

  updateSolarPanel(solarPanel: SolarPanel): Observable<HttpResponse<any>> {
    return this.http.put(this.apiUrl + '/SolarPanels/' + solarPanel.id, solarPanel, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
      observe: 'response'
    });
  }

  //windturbine methods

  addWindTurbine(windturbineData: WindTurbine): Observable<HttpResponse<any>> {
    const httpOptions = { headers: new HttpHeaders({ 'Content-Type': 'application/json' }) };
    return this.http.post<any>(this.apiUrl + '/WindTurbines', windturbineData, httpOptions);
  }

  updateWindTurbine(windturbineData: WindTurbine): Observable<HttpResponse<any>> {
    console.log(windturbineData);
    return this.http.put<any>(this.apiUrl + '/WindTurbines/'+ windturbineData.id, windturbineData, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
      observe: 'response'
    });
  }

}
