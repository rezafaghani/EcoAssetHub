import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddEditWindturbineComponent } from './add-edit-windturbine.component';
import { RenewableAssetType, WindTurbine } from '../models/app-models';
import { RenewAbleService } from '../services/renewable.service';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

class MockRenewAbleService {
  addWindTurbine = jasmine.createSpy('addWindTurbine').and.returnValue(of({}));
  updateWindTurbine = jasmine.createSpy('updateWindTurbine').and.returnValue(of({}));
}

describe('AddEditWindturbineComponent', () => {
  let component: AddEditWindturbineComponent;
  let fixture: ComponentFixture<AddEditWindturbineComponent>;
  let mockService: MockRenewAbleService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AddEditWindturbineComponent],
      providers: [{ provide: RenewAbleService, useClass: MockRenewAbleService }],
      imports:[BrowserAnimationsModule]
    })
      .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AddEditWindturbineComponent);
    component = fixture.componentInstance;
    mockService = TestBed.inject(RenewAbleService) as unknown as MockRenewAbleService;
  });

  // Test for addWindTurbine method
  it('should call addWindTurbine from RenewAbleService with correct arguments on addWindTurbine', () => {
    // Manually setting the WindTurbine input
    component.windTurbine = {
      meterPointId: 123,
      capacity: 456,
      id: '789',
      type: RenewableAssetType.WindTurbine,
      hubHeight: 100,
      rotorDiameter: 100
    };

    // Manually calling ngOnInit
    component.ngOnInit();

    // Call the method
    component.addWindTurbine();

    // Check if the service method was called with the correct arguments
    expect(mockService.addWindTurbine).toHaveBeenCalledWith(jasmine.objectContaining({
      meterPointId: component.meterPointId,
      capacity: component.capacity,
      hubHeight: component.hubHeight,
      rotorDiameter: component.rotorDiameter
    }));
  });


});
