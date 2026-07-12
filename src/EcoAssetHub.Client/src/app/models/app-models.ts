export interface Renewable {
    id: string;
    capacity: number;
    meterPointId: number;
    hubHeight: number | null;
    rotorDiameter: number | null;
    compassOrientation: string;
    type: number;
  }

export enum RenewableAssetType {
    RenewableAsset,
    WindTurbine,
    SolarPanel    
  }
  export interface WindTurbine{
    id: string;
    type: RenewableAssetType;
    capacity: number;
    meterPointId: number;
    hubHeight: number;
    rotorDiameter: number
  }

  export interface SolarPanel{
    id: string;
    type: RenewableAssetType;
    capacity: number;
    meterPointId: number;
    compassOrientation: string;
  }

export interface Curve {
  id: string;
  name: string;
  meterPointId: number;
  capacity: number;
  type: RenewableAssetType;
}

export interface TimeSeriesPoint {
  timestamp: string;
  value: number;
  asOf: string;
}
