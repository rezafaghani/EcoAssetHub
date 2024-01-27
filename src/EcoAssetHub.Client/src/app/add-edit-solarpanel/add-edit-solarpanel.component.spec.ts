import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddEditSolarpanelComponent } from './add-edit-solarpanel.component';
import { RenewableAssetType, SolarPanel } from '../models/app-models';
import { RenewAbleService } from '../services/renewable.service';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

class MockRenewAbleService {
  addSolarPanel = jasmine.createSpy('addSolarPanel').and.returnValue(of({}));
  updateSolarPanel = jasmine.createSpy('updateSolarPanel').and.returnValue(of({}));
}

describe('AddEditSolarpanelComponent', () => {
  let component: AddEditSolarpanelComponent;
  let fixture: ComponentFixture<AddEditSolarpanelComponent>;
  let mockService: MockRenewAbleService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AddEditSolarpanelComponent],
      providers: [{ provide: RenewAbleService, useClass: MockRenewAbleService }],
      imports:[BrowserAnimationsModule]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AddEditSolarpanelComponent);
    component = fixture.componentInstance;
    mockService = TestBed.inject(RenewAbleService) as unknown as MockRenewAbleService;
  });

  // Test for addSolarPanel method
  it('should call addSolarPanel from RenewAbleService with correct arguments on addsolarpanel', () => {
    // Manually setting the solarpanel input
    component.solarpanel = {
      meterPointId: 123,
      capacity: 456,
      compassOrientation: 'North',
      id: '789',
      type:RenewableAssetType.SolarPanel
    };

    // Manually calling ngOnInit
    component.ngOnInit();

    // Call the method
    component.addsolarpanel();

    // Check if the service method was called with the correct arguments
    expect(mockService.addSolarPanel).toHaveBeenCalledWith(jasmine.objectContaining({
      meterPointId: component.meterPointId,
      capacity: component.capacity,
      compassOrientation: component.compassOrientation
    }));
  });


});
