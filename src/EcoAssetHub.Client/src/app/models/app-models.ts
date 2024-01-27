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