import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { AddEditSolarpanelComponent } from './add-edit-solarpanel/add-edit-solarpanel.component';
import { AddEditWindturbineComponent } from './add-edit-windturbine/add-edit-windturbine.component';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RenewAbleService } from './services/renewable.service';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AddEditSolarpanelComponent,
    AddEditWindturbineComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    
  ],
  providers: [RenewAbleService],
  bootstrap: [AppComponent]
})
export class AppModule { }
