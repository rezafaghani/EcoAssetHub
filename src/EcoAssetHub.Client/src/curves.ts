export interface CurveDataset {
  id: string;
  curveId: string;
  metric: string;
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
      lastIngestedAt: ''
    };

    curve.datasets.push(dataset);
    if (!curve.lastIngestedAt || dataset.lastIngestedAt > curve.lastIngestedAt) {
      curve.lastIngestedAt = dataset.lastIngestedAt;
    }
    curves.set(id, curve);
  }

  return Array.from(curves.values()).sort((a, b) => a.label.localeCompare(b.label));
}
