import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RenewAbleService } from './renewable.service';
import { Renewable } from '../models/app-models';
describe('RenewAbleService', () => {
  let service: RenewAbleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [RenewAbleService]
    });
    service = TestBed.inject(RenewAbleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // Example test for getRenewAbleList method
  it('should retrieve renewable list', () => {
    const dummyData: Renewable[] = [
        {
          id: 'r1',
          capacity: 100,
          meterPointId: 101,
          hubHeight: 50,
          rotorDiameter: 30,
          compassOrientation: 'N',
          type: 1
        },
        {
          id: 'r2',
          capacity: 200,
          meterPointId: 102,
          hubHeight: 60,
          rotorDiameter: 35,
          compassOrientation: 'E',
          type: 2
        },
        {
          id: 'r3',
          capacity: 150,
          meterPointId: 103,
          hubHeight: null,
          rotorDiameter: null,
          compassOrientation: 'S',
          type: 1
        }
        // Add more objects as needed
      ];

    service.getRenewAbleList().subscribe(data => {
      expect(data.length).toBe(dummyData.length);
      expect(data).toEqual(dummyData);
    });

    const req = httpMock.expectOne(`${service.apiUrl}/Renewables`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyData);
  });

  // Additional tests for other methods...

});
