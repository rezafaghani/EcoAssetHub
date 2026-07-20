export interface CurveDataset {
  id: string;
  curveId: string;
  source: string;
  metric: string;
  dataKind: string;
  category: string;
  endpoint: string;
  country: string;
  biddingZone: string;
  region: string;
  lastIngestedAt: string;
}

export interface CurveSummary {
  id: string;
  label: string;
  datasets: CurveDataset[];
  categories: string[];
  dataKinds: string[];
  providers: string[];
  lastIngestedAt: string;
}

export function getDatasetCurveId(dataset: Pick<CurveDataset, 'id' | 'curveId'>) {
  return dataset.curveId || dataset.id;
}

export function groupDatasetsByCurve(datasets: CurveDataset[]): CurveSummary[] {
  const curves = new Map<string, CurveSummary>();

  for (const dataset of datasets) {
    const id = getDatasetCurveId(dataset);
    const curve = curves.get(id) ?? {
      id,
      label: id,
      datasets: [],
      categories: [],
      dataKinds: [],
      providers: [],
      lastIngestedAt: ''
    };

    curve.datasets.push(dataset);
    addUnique(curve.categories, dataset.category);
    addUnique(curve.dataKinds, dataset.dataKind);
    addUnique(curve.providers, dataset.source);
    if (!curve.lastIngestedAt || dataset.lastIngestedAt > curve.lastIngestedAt) {
      curve.lastIngestedAt = dataset.lastIngestedAt;
    }
    curves.set(id, curve);
  }

  return Array.from(curves.values()).sort((a, b) => a.label.localeCompare(b.label));
}

function addUnique(values: string[], value: string) {
  if (value && !values.includes(value)) {
    values.push(value);
    values.sort();
  }
}
