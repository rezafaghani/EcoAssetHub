import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeComponent } from './home.component';
import { RenewAbleService } from '../services/renewable.service';
import { of } from 'rxjs';

class MockRenewAbleService {
  getRenewAbleList = jasmine.createSpy('getRenewAbleList').and.returnValue(of([]));
  deleteRenewable = jasmine.createSpy('deleteRenewable').and.returnValue(of({}));
  // Other methods as needed
}

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let mockService: MockRenewAbleService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HomeComponent],
      providers: [{ provide: RenewAbleService, useClass: MockRenewAbleService }]
    })
      .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    mockService = TestBed.inject(RenewAbleService) as unknown as MockRenewAbleService;
    fixture.detectChanges();
  });

  // Tests go here

  it('should fetch renewable list on initialization', () => {
    expect(mockService.getRenewAbleList).toHaveBeenCalled();
    expect(component.renewAbleList).toEqual([]);
  });
  it('should set properties correctly for addWindTurbineClick', () => {
    component.addWindTurbineClick();
    expect(component.ModalTitle).toEqual('Add Wind Turbine');
    expect(component.ActivateAddEditWindturbin).toBeTrue();
    expect(component.windTurbine).toBeDefined();
  });

  it('should set properties correctly for editWindTurbineClick', () => {
    const mockItem = { /* mock data */ };
    component.editWindTurbineClick(mockItem);
    expect(component.ModalTitle).toEqual('Edit Wind Turbine');
    expect(component.ActivateAddEditWindturbin).toBeTrue();
    expect(component.windTurbine).toEqual(mockItem);
  });

  // Similar tests for addSolarPanelClick and editSolarPanelClick
  it('should reset properties on closeClick', () => {
    component.closeClick();
    expect(component.ActivateAddEditSolar).toBeFalse();
    expect(component.ActivateAddEditWindturbin).toBeFalse();
    // Check if refreshRenewAbleList is called
  });
  it('should call deleteRenewable on deleteClick', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    const mockItem = { id: 123 };
    component.deleteClick(mockItem);
    expect(mockService.deleteRenewable).toHaveBeenCalledWith(123);
    // Check if refreshRenewAbleList is called
  });

});
