import React, { useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

interface DatasetMetadata {
  id: string;
  source: string;
  endpoint: string;
  metric: string;
  unit: string;
  country: string;
  biddingZone: string;
  region: string;
  granularity: string;
  productionType: string;
  forecastType: string;
  neighbor: string;
  licenseInfo: string;
  deprecated: boolean;
  requestParameters: Record<string, string>;
  firstObservedAt: string;
  lastIngestedAt: string;
}

interface TimeSeriesPoint {
  timestamp: string;
  value: number | null;
  asOf: string;
}

interface IngestionSchedule {
  id: string;
  curveId: string;
  name: string;
  cronExpression: string;
  enabled: boolean;
  endpoint: string;
  parameters: Record<string, string>;
  lookbackHours: number;
  batchSize: number;
  lastQueuedAt?: string | null;
  updatedAt: string;
}

interface IngestionJob {
  id: string;
  scheduleId: string;
  curveId: string;
  status: string;
  queuedAt: string;
  startedAt?: string | null;
  finishedAt?: string | null;
  error: string;
}

interface IngestionExecution {
  id: string;
  jobId: string;
  scheduleId: string;
  curveId: string;
  status: string;
  createdAt: string;
  startedAt?: string | null;
  finishedAt?: string | null;
  inserted: number;
  skipped: number;
  error: string;
}

interface ChartPoint {
  x: number;
  y: number;
  point: TimeSeriesPoint;
}

const apiBase = import.meta.env.VITE_API_BASE_URL ?? '/api';
const refreshIntervalMs = 15_000;
const forecastDatasetId = 'energy-charts:public_power_forecast:forecast:dk:-:-:wind_offshore:day-ahead:-:quarter-hour';

function App() {
  const [datasets, setDatasets] = useState<DatasetMetadata[]>([]);
  const [selected, setSelected] = useState<DatasetMetadata | null>(null);
  const [series, setSeries] = useState<TimeSeriesPoint[]>([]);
  const [schedules, setSchedules] = useState<IngestionSchedule[]>([]);
  const [jobs, setJobs] = useState<IngestionJob[]>([]);
  const [executions, setExecutions] = useState<IngestionExecution[]>([]);
  const [search, setSearch] = useState('');
  const [endpoint, setEndpoint] = useState('');
  const [metric, setMetric] = useState('');
  const [start, setStart] = useState(toLocalInput(new Date(Date.now() - 24 * 60 * 60 * 1000)));
  const [end, setEnd] = useState(toLocalInput(new Date()));
  const [asOf, setAsOf] = useState('');
  const [loading, setLoading] = useState(false);
  const [live, setLive] = useState(true);
  const [lastLiveRefresh, setLastLiveRefresh] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    void loadDatasets();
    void loadIngestionStatus();
  }, []);

  const endpoints = useMemo(() => unique(datasets.map(x => x.endpoint)), [datasets]);
  const metrics = useMemo(() => unique(datasets.map(x => x.metric)), [datasets]);
  const chartPoints = useMemo(() => buildChartPoints(series), [series]);
  const chartPath = useMemo(() => buildPath(chartPoints), [chartPoints]);
  const latestExecution = executions[0];
  const forecastFocused = selected?.id === forecastDatasetId;

  useEffect(() => {
    if (!live) return;

    const id = window.setInterval(() => {
      void loadDatasets(true);
      void loadIngestionStatus(true);
      if (selected) {
        void loadSeries(selected, true);
      }
      setLastLiveRefresh(new Date().toISOString());
    }, refreshIntervalMs);

    return () => window.clearInterval(id);
  }, [live, selected, start, end, asOf, search, endpoint, metric]);

  async function loadDatasets(silent = false) {
    if (!silent) setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      if (endpoint) params.set('endpoint', endpoint);
      if (metric) params.set('metric', metric);
      const response = await fetch(`${apiBase}/datasets?${params}`);
      if (!response.ok) throw new Error('Dataset request failed');
      const result = await response.json() as DatasetMetadata[];
      setDatasets(result);
      if (!selected && result.length > 0) {
        setSelected(result[0]);
      } else if (selected) {
        setSelected(result.find(dataset => dataset.id === selected.id) ?? selected);
      }
    } catch {
      setError('Unable to load datasets.');
    } finally {
      if (!silent) setLoading(false);
    }
  }

  async function loadSeries(dataset = selected, silent = false) {
    if (!dataset) return;
    if (!silent) setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams({
        start: new Date(start).toISOString(),
        end: new Date(end).toISOString()
      });
      if (asOf) params.set('asOf', new Date(asOf).toISOString());
      const response = await fetch(`${apiBase}/datasets/${encodeURIComponent(dataset.id)}/series?${params}`);
      if (!response.ok) throw new Error('Series request failed');
      setSeries(await response.json() as TimeSeriesPoint[]);
    } catch {
      setError('Unable to load series.');
    } finally {
      if (!silent) setLoading(false);
    }
  }

  async function loadIngestionStatus(silent = false) {
    if (!silent) setError('');
    try {
      const [scheduleResponse, jobResponse, executionResponse] = await Promise.all([
        fetch(`${apiBase}/ingestion/schedules`),
        fetch(`${apiBase}/ingestion/jobs`),
        fetch(`${apiBase}/ingestion/executions`)
      ]);
      if (!scheduleResponse.ok || !jobResponse.ok || !executionResponse.ok) {
        throw new Error('Ingestion status request failed');
      }
      setSchedules(await scheduleResponse.json() as IngestionSchedule[]);
      setJobs(await jobResponse.json() as IngestionJob[]);
      setExecutions(await executionResponse.json() as IngestionExecution[]);
    } catch {
      if (!silent) setError('Unable to load ingestion status.');
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <strong>EcoAssetHub</strong>
          <span>Energy Charts explorer</span>
        </div>
        <div className="filter-stack">
          <input value={search} onChange={event => setSearch(event.target.value)} onKeyDown={event => event.key === 'Enter' && loadDatasets()} placeholder="Search datasets" />
          <select value={endpoint} onChange={event => setEndpoint(event.target.value)}>
            <option value="">All endpoints</option>
            {endpoints.map(value => <option key={value} value={value}>{value}</option>)}
          </select>
          <select value={metric} onChange={event => setMetric(event.target.value)}>
            <option value="">All metrics</option>
            {metrics.map(value => <option key={value} value={value}>{value}</option>)}
          </select>
          <button onClick={loadDatasets}>Refresh</button>
        </div>
        <div className="dataset-list">
          {datasets.map(dataset => (
            <button
              key={dataset.id}
              className={selected?.id === dataset.id ? 'dataset active' : 'dataset'}
              onClick={() => {
                setSelected(dataset);
                void loadSeries(dataset);
              }}>
              <span>{dataset.metric}</span>
              <small>{dataset.endpoint} · {dataset.country || dataset.biddingZone || dataset.region || 'global'}</small>
            </button>
          ))}
        </div>
      </aside>

      <section className="workspace">
        <section className="control-panel">
          <div className={forecastFocused ? 'title-block forecast-focus' : 'title-block'}>
            <h1>{selected?.metric ?? 'Dataset explorer'}</h1>
            <p title={selected?.id}>{selected ? selected.id : 'Select a dataset to inspect metadata and time-series values.'}</p>
          </div>

          <div className="range-toolbar">
            <label>Start<input type="datetime-local" value={start} onChange={event => setStart(event.target.value)} /></label>
            <label>End<input type="datetime-local" value={end} onChange={event => setEnd(event.target.value)} /></label>
            <label>As of<input type="datetime-local" value={asOf} onChange={event => setAsOf(event.target.value)} /></label>
            <label className="live-toggle">
              <input type="checkbox" checked={live} onChange={event => setLive(event.target.checked)} />
              Live
            </label>
            <button disabled={!selected || loading} onClick={() => loadSeries()}>{loading ? 'Loading' : 'Load series'}</button>
          </div>
        </section>

        {error && <div className="error">{error}</div>}

        <section className="chart-zone">
          <svg viewBox="0 0 900 320" role="img" aria-label="Dataset time series">
            <line x1="44" y1="276" x2="860" y2="276" />
            <line x1="44" y1="36" x2="44" y2="276" />
            {chartPath && <path d={chartPath} />}
            {chartPoints.map((chartPoint, index) => (
              <circle key={`${chartPoint.point.timestamp}-${chartPoint.point.asOf}-${index}`} cx={chartPoint.x} cy={chartPoint.y} r="4">
                <title>{formatDate(chartPoint.point.timestamp)} | {formatValue(chartPoint.point.value, selected?.unit ?? '')}</title>
              </circle>
            ))}
            {labelPoints(chartPoints).map((chartPoint, index) => (
              <g className="point-label" key={`${chartPoint.point.timestamp}-label-${index}`}>
                <line x1={chartPoint.x} y1={chartPoint.y - 7} x2={chartPoint.x} y2={chartPoint.y - 22} />
                <text x={chartPoint.x} y={chartPoint.y - 26} textAnchor="middle">
                  {formatCompactTime(chartPoint.point.timestamp)}
                  <tspan x={chartPoint.x} dy="13">{formatValue(chartPoint.point.value, selected?.unit ?? '')}</tspan>
                </text>
              </g>
            ))}
            {!chartPath && <text x="450" y="160" textAnchor="middle">No points loaded</text>}
          </svg>
          <div className="chart-footer">
            <span>{series.length} points</span>
            <span>{selected?.lastIngestedAt ? `Last ingested ${formatDate(selected.lastIngestedAt)}` : 'No ingestion timestamp'}</span>
            <span>{lastLiveRefresh ? `Live refresh ${formatDate(lastLiveRefresh)}` : `Live refresh every ${refreshIntervalMs / 1000}s`}</span>
          </div>
        </section>

        <PointTable points={series} unit={selected?.unit ?? ''} />

        <MetadataPanel dataset={selected} />

        <IngestionStatusPanel schedules={schedules} jobs={jobs} executions={executions} latestExecution={latestExecution} />
      </section>
    </main>
  );
}

function IngestionStatusPanel({
  schedules,
  jobs,
  executions,
  latestExecution
}: {
  schedules: IngestionSchedule[];
  jobs: IngestionJob[];
  executions: IngestionExecution[];
  latestExecution?: IngestionExecution;
}) {
  return (
    <section className="ingestion-panel">
      <header className="section-heading">
        <div>
          <h2>Ingestion status</h2>
          <p>{schedules.length} schedules · {jobs.length} jobs · {executions.length} executions</p>
        </div>
        {latestExecution && <StatusBadge status={latestExecution.status} />}
      </header>

      <div className="status-summary">
        <MetricTile className="metric-tile-compact" label="Enabled schedules" value={schedules.filter(x => x.enabled).length.toString()} />
        <MetricTile label="Running jobs" value={jobs.filter(x => equalsStatus(x.status, 'running')).length.toString()} />
        <MetricTile label="Failed executions" value={executions.filter(x => equalsStatus(x.status, 'failed')).length.toString()} />
        <MetricTile label="Latest inserted" value={(latestExecution?.inserted ?? 0).toLocaleString()} />
      </div>

      <div className="status-grid">
        <StatusTable title="Schedules" rows={schedules.slice(0, 8).map(schedule => ({
          id: schedule.id,
          primary: schedule.name || schedule.curveId,
          secondary: `${schedule.endpoint} · ${schedule.cronExpression}`,
          status: schedule.enabled ? 'enabled' : 'disabled',
          time: schedule.lastQueuedAt ? formatDate(schedule.lastQueuedAt) : ''
        }))} />
        <StatusTable title="Jobs" rows={jobs.slice(0, 8).map(job => ({
          id: job.id,
          primary: job.curveId,
          secondary: job.scheduleId,
          status: job.status,
          time: formatDate(job.queuedAt),
          error: job.error
        }))} />
        <StatusTable title="Executions" rows={executions.slice(0, 8).map(execution => ({
          id: execution.id,
          primary: execution.curveId,
          secondary: `${execution.inserted.toLocaleString()} inserted · ${execution.skipped.toLocaleString()} skipped`,
          status: execution.status,
          time: formatDate(execution.createdAt),
          error: execution.error
        }))} />
      </div>
    </section>
  );
}

function MetricTile({ label, value, className = '' }: { label: string; value: string; className?: string }) {
  return (
    <div className={`metric-tile ${className}`.trim()}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function StatusTable({
  title,
  rows
}: {
  title: string;
  rows: Array<{ id: string; primary: string; secondary: string; status: string; time: string; error?: string }>;
}) {
  return (
    <section className="status-table">
      <h3>{title}</h3>
      <div className="table-scroll compact">
        <table>
          <thead><tr><th>Name</th><th>Status</th><th>Time</th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.id} title={row.error || row.id}>
                <td>
                  <strong>{row.primary}</strong>
                  <small>{row.error || row.secondary}</small>
                </td>
                <td><StatusBadge status={row.status} /></td>
                <td>{row.time}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function StatusBadge({ status }: { status: string }) {
  return <span className={`status-badge ${status.toLowerCase()}`}>{status}</span>;
}

function MetadataPanel({ dataset }: { dataset: DatasetMetadata | null }) {
  if (!dataset) return <section className="metadata-panel"><h2>Metadata</h2><p>No dataset selected.</p></section>;
  const rows = [
    ['Source', dataset.source],
    ['Endpoint', dataset.endpoint],
    ['Metric', dataset.metric],
    ['Unit', dataset.unit],
    ['Country', dataset.country],
    ['Bidding zone', dataset.biddingZone],
    ['Region', dataset.region],
    ['Granularity', dataset.granularity],
    ['Production type', dataset.productionType],
    ['Forecast type', dataset.forecastType],
    ['Neighbor', dataset.neighbor],
    ['Last ingested', formatDate(dataset.lastIngestedAt)]
  ].filter(([, value]) => value);

  return (
    <section className="metadata-panel">
      <h2>Metadata</h2>
      <dl>{rows.map(([label, value]) => <React.Fragment key={label}><dt>{label}</dt><dd>{value}</dd></React.Fragment>)}</dl>
    </section>
  );
}

function PointTable({ points, unit }: { points: TimeSeriesPoint[]; unit: string }) {
  return (
    <section className="table-panel">
      <h2>{points.length} points</h2>
      <div className="table-scroll">
        <table>
          <thead><tr><th>Timestamp</th><th>Value</th><th>As of</th></tr></thead>
          <tbody>
            {points.slice(0, 200).map(point => (
              <tr key={`${point.timestamp}-${point.asOf}`}>
                <td>{formatDate(point.timestamp)}</td>
                <td>{formatValue(point.value, unit)}</td>
                <td>{formatDate(point.asOf)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function buildChartPoints(points: TimeSeriesPoint[]): ChartPoint[] {
  const numeric = points.filter(point => point.value !== null);
  if (numeric.length === 0) return [];
  const values = numeric.map(point => point.value as number);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(max - min, 1);
  return numeric.map((point, index) => {
    const x = numeric.length === 1 ? 450 : 44 + (index * 816) / (numeric.length - 1);
    const y = 276 - (((point.value as number) - min) * 240) / range;
    return { x, y, point };
  });
}

function buildPath(points: ChartPoint[]) {
  return points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' ');
}

function labelPoints(points: ChartPoint[]) {
  if (points.length <= 8) return points;
  const step = Math.ceil(points.length / 8);
  return points.filter((_, index) => index % step === 0 || index === points.length - 1);
}

function unique(values: string[]) {
  return Array.from(new Set(values.filter(Boolean))).sort();
}

function formatDate(value: string) {
  return value ? new Date(value).toLocaleString() : '';
}

function formatCompactTime(value: string) {
  return value ? new Date(value).toLocaleString([], { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }) : '';
}

function formatValue(value: number | null, unit: string) {
  if (value === null) return 'null';
  const formatted = Math.abs(value) >= 100 ? value.toFixed(0) : value.toFixed(2);
  return unit ? `${formatted} ${unit}` : formatted;
}

function equalsStatus(value: string, expected: string) {
  return value.localeCompare(expected, undefined, { sensitivity: 'accent' }) === 0;
}

function toLocalInput(date: Date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

createRoot(document.getElementById('root')!).render(<App />);
