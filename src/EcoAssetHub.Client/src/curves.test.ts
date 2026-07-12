import { describe, expect, it } from 'vitest';
import { groupDatasetsByCurve } from './curves';

describe('groupDatasetsByCurve', () => {
  it('groups datasets by scheduler curve id', () => {
    const curves = groupDatasetsByCurve([
      dataset('solar', 'dk.public_power', '2026-01-01T00:00:00Z'),
      dataset('wind', 'dk.public_power', '2026-01-02T00:00:00Z'),
      dataset('price', 'DK1.price', '2026-01-01T00:00:00Z')
    ]);

    expect(curves.find(curve => curve.id === 'dk.public_power')).toMatchObject({
      datasets: [expect.any(Object), expect.any(Object)],
      lastIngestedAt: '2026-01-02T00:00:00Z'
    });
    expect(curves.find(curve => curve.id === 'DK1.price')).toMatchObject({
      datasets: [expect.any(Object)],
      lastIngestedAt: '2026-01-01T00:00:00Z'
    });
  });
});

function dataset(metric: string, curveId: string, lastIngestedAt: string) {
  return {
    id: `${curveId}:${metric}`,
    curveId,
    metric,
    endpoint: 'public_power',
    country: 'dk',
    biddingZone: '',
    region: '',
    lastIngestedAt
  };
}
