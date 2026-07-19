import React, { useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { getDatasetCurveId, groupDatasetsByCurve } from './curves';
import { buildScheduleJobHistoryRows, buildScheduleStatusRows, suggestCronFromGranularity, type IngestionExecution, type IngestionJob, type IngestionSchedule, type ScheduleStatusRow } from './ingestionStatus';
import './styles.css';

interface DatasetMetadata {
  id: string;
  curveId: string;
  source: string;
  endpoint: string;
  metric: string;
  dataKind: string;
  category: string;
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

interface ChartPoint {
  x: number;
  y: number;
  point: TimeSeriesPoint;
}

interface ChartTick {
  x: number;
  label: string;
  anchor: 'start' | 'middle' | 'end';
}

const chartBounds = {
  left: 72,
  right: 872,
  top: 28,
  bottom: 318
};
const chartViewBox = { width: 900, height: 380 };
const chartHorizontalPadding = 18;
const apiBase = import.meta.env.VITE_API_BASE_URL ?? '/api';
const refreshIntervalMs = 15_000;
const defaultTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
const timeZones = unique([defaultTimeZone, 'UTC', 'Europe/Copenhagen', 'Europe/London', 'America/New_York', 'Asia/Tokyo']);

function App() {
  const [datasets, setDatasets] = useState<DatasetMetadata[]>([]);
  const [selectedCurveId, setSelectedCurveId] = useState('');
  const [selected, setSelected] = useState<DatasetMetadata | null>(null);
  const [series, setSeries] = useState<TimeSeriesPoint[]>([]);
  const [schedules, setSchedules] = useState<IngestionSchedule[]>([]);
  const [jobs, setJobs] = useState<IngestionJob[]>([]);
  const [executions, setExecutions] = useState<IngestionExecution[]>([]);
  const [search, setSearch] = useState('');
  const [endpoint, setEndpoint] = useState('');
  const [metric, setMetric] = useState('');
  const [dataKind, setDataKind] = useState('');
  const [category, setCategory] = useState('');
  const [start, setStart] = useState(toLocalInput(new Date(Date.now() - 24 * 60 * 60 * 1000)));
  const [end, setEnd] = useState(toLocalInput(new Date()));
  const [asOf, setAsOf] = useState('');
  const [timeZone, setTimeZone] = useState(defaultTimeZone);
  const [loading, setLoading] = useState(false);
  const [live, setLive] = useState(true);
  const [lastLiveRefresh, setLastLiveRefresh] = useState('');
  const [error, setError] = useState('');
  const [hoveredPoint, setHoveredPoint] = useState<ChartPoint | null>(null);

  useEffect(() => {
    void loadDatasets();
  }, []);

  const endpoints = useMemo(() => unique(datasets.map(x => x.endpoint)), [datasets]);
  const metrics = useMemo(() => unique(datasets.map(x => x.metric)), [datasets]);
  const dataKinds = useMemo(() => unique(datasets.map(x => x.dataKind)), [datasets]);
  const categories = useMemo(() => unique(datasets.map(x => x.category)), [datasets]);
  const curves = useMemo(() => groupDatasetsByCurve(datasets), [datasets]);
  const selectedCurve = curves.find(curve => curve.id === selectedCurveId);
  const selectedCurveDatasets = (selectedCurve?.datasets ?? []) as DatasetMetadata[];
  const chartData = useMemo(() => buildChartData(series, selected?.unit ?? '', timeZone), [series, selected?.unit, timeZone]);
  const latestExecution = executions[0];

  useEffect(() => {
    setHoveredPoint(null);
  }, [selected?.id, series]);

  useEffect(() => {
    if (!live) return;

    const id = window.setInterval(() => {
      void loadDatasets(true);
      if (selectedCurveId) {
        void loadIngestionStatus(selectedCurveId, true);
      }
      if (selected) {
        void loadSeries(selected, true);
      }
      setLastLiveRefresh(new Date().toISOString());
    }, refreshIntervalMs);

    return () => window.clearInterval(id);
  }, [live, selected, selectedCurveId, start, end, asOf, timeZone, search, endpoint, metric, dataKind, category]);

  async function loadDatasets(silent = false) {
    if (!silent) setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      if (endpoint) params.set('endpoint', endpoint);
      if (metric) params.set('metric', metric);
      if (dataKind) params.set('dataKind', dataKind);
      if (category) params.set('category', category);
      const response = await fetch(`${apiBase}/datasets?${params}`);
      if (!response.ok) throw new Error('Dataset request failed');
      const result = await response.json() as DatasetMetadata[];
      const resultCurves = groupDatasetsByCurve(result);
      const nextCurveId = selectedCurveId && resultCurves.some(curve => curve.id === selectedCurveId)
        ? selectedCurveId
        : resultCurves[0]?.id ?? '';
      const nextCurveDatasets = result.filter(dataset => getDatasetCurveId(dataset) === nextCurveId);
      const nextSelected = nextCurveDatasets.find(dataset => dataset.id === selected?.id) ?? nextCurveDatasets[0] ?? null;

      setDatasets(result);
      setSelectedCurveId(nextCurveId);
      setSelected(nextSelected);
      if (nextSelected?.id !== selected?.id) {
        setSeries([]);
      }
      if (nextCurveId) {
        void loadIngestionStatus(nextCurveId, silent);
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
        start: start.trim(),
        end: end.trim(),
        timeZone
      });
      if (asOf) params.set('asOf', asOf.trim());
      const response = await fetch(`${apiBase}/datasets/${encodeURIComponent(dataset.id)}/series?${params}`);
      if (!response.ok) throw new Error('Series request failed');
      setSeries(await response.json() as TimeSeriesPoint[]);
    } catch {
      setError('Unable to load series.');
    } finally {
      if (!silent) setLoading(false);
    }
  }

  async function loadIngestionStatus(curveId = selectedCurveId, silent = false) {
    if (!curveId) {
      setSchedules([]);
      setJobs([]);
      setExecutions([]);
      return;
    }

    if (!silent) setError('');
    try {
      const encoded = encodeURIComponent(curveId);
      const [scheduleResponse, jobResponse, executionResponse] = await Promise.all([
        fetch(`${apiBase}/ingestion/curves/${encoded}/schedules`),
        fetch(`${apiBase}/ingestion/curves/${encoded}/jobs`),
        fetch(`${apiBase}/ingestion/curves/${encoded}/executions`)
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

  async function saveSchedule(schedule: IngestionSchedule) {
    setError('');
    const response = await fetch(`${apiBase}/ingestion/schedules/${encodeURIComponent(schedule.id)}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        cronExpression: schedule.cronExpression,
        enabled: schedule.enabled,
        windowStartExpression: schedule.windowStartExpression,
        windowEndExpression: schedule.windowEndExpression,
        batchSize: schedule.batchSize
      })
    });
    if (!response.ok) {
      setError(await response.text() || 'Unable to save schedule.');
      return false;
    }
    await loadIngestionStatus(schedule.curveId);
    return true;
  }

  async function resetSchedule(scheduleId: string) {
    setError('');
    const response = await fetch(`${apiBase}/ingestion/schedules/${encodeURIComponent(scheduleId)}/reset`, { method: 'POST' });
    if (!response.ok) {
      setError('Unable to reset schedule.');
      return false;
    }
    await loadIngestionStatus(selectedCurveId);
    return true;
  }

  async function createBackload(scheduleId: string, datasetId: string, windowStartExpression: string, windowEndExpression: string, batchSize: number) {
    setError('');
    const response = await fetch(`${apiBase}/ingestion/schedules/${encodeURIComponent(scheduleId)}/backloads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ datasetId, windowStartExpression, windowEndExpression, batchSize })
    });
    if (!response.ok) {
      setError(await response.text() || 'Unable to queue backload.');
      return false;
    }
    await loadIngestionStatus(selectedCurveId);
    return true;
  }

  function selectCurve(curveId: string) {
    const curveDatasets = datasets.filter(dataset => getDatasetCurveId(dataset) === curveId);
    const nextSelected = curveDatasets[0] ?? null;
    setSelectedCurveId(curveId);
    setSelected(nextSelected);
    setSeries([]);
    void loadIngestionStatus(curveId);
    if (nextSelected) {
      void loadSeries(nextSelected);
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <strong>EcoAssetHub</strong>
          <span>Energy Charts explorer</span>
        </div>
        <div className="filter-panel">
          <label>
            <span>Search</span>
            <input value={search} onChange={event => setSearch(event.target.value)} onKeyDown={event => event.key === 'Enter' && void loadDatasets()} placeholder="Dataset name or id" />
          </label>
          <label>
            <span>Endpoint</span>
            <select value={endpoint} onChange={event => setEndpoint(event.target.value)}>
              <option value="">All endpoints</option>
              {endpoints.map(value => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
          <label>
            <span>Metric</span>
            <select value={metric} onChange={event => setMetric(event.target.value)}>
              <option value="">All metrics</option>
              {metrics.map(value => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
          <label>
            <span>Kind</span>
            <select value={dataKind} onChange={event => setDataKind(event.target.value)}>
              <option value="">All kinds</option>
              {dataKinds.map(value => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
          <label>
            <span>Category</span>
            <select value={category} onChange={event => setCategory(event.target.value)}>
              <option value="">All categories</option>
              {categories.map(value => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
          <button onClick={() => void loadDatasets()}>Search</button>
        </div>
        <div className="sidebar-heading">Curves</div>
        <div className="dataset-list">
          {curves.map(curve => (
            <button
              key={curve.id}
              className={selectedCurveId === curve.id ? 'dataset active' : 'dataset'}
              onClick={() => selectCurve(curve.id)}>
              <span>{curve.label}</span>
              <small>{curve.datasets.length} datasets · {curve.lastIngestedAt ? formatDate(curve.lastIngestedAt, timeZone) : 'not ingested'}</small>
            </button>
          ))}
        </div>

        {selectedCurveId && (
          <>
            <div className="sidebar-heading">Datasets</div>
            <div className="dataset-list">
              {selectedCurveDatasets.map(dataset => (
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
          </>
        )}

        {!curves.length && <p className="empty-state">No curves found.</p>}
      </aside>

      <section className="workspace">
        <section className="control-panel">
          <div className="title-block">
            <h1>{selectedCurveId || 'Curve explorer'}</h1>
            <p title={selected?.id}>{selected ? `${selected.metric} · ${selected.id}` : 'Select a curve to inspect details, schedules, jobs, and executions.'}</p>
          </div>

          <div className="range-toolbar">
            <DateExpressionInput label="Start" value={start} onChange={setStart} placeholder="today-1 or exact time" />
            <DateExpressionInput label="End" value={end} onChange={setEnd} placeholder="now, today+1, or exact time" />
            <DateExpressionInput label="Version time" value={asOf} onChange={setAsOf} placeholder="empty for latest, now, or exact time" />
            <label>
              <span>Time zone</span>
              <select value={timeZone} onChange={event => setTimeZone(event.target.value)}>
                {timeZones.map(value => <option key={value} value={value}>{formatTimeZoneLabel(value)}</option>)}
              </select>
            </label>
            <label className="live-toggle">
              <input type="checkbox" checked={live} onChange={event => setLive(event.target.checked)} />
              Live
            </label>
            <button disabled={!selected || loading} onClick={() => loadSeries()}>{loading ? 'Loading' : 'Load series'}</button>
          </div>
        </section>

        {error && <div className="error">{error}</div>}

        <MetadataPanel dataset={selected} curveId={selectedCurveId} datasetCount={selectedCurveDatasets.length} timeZone={timeZone} />

        <IngestionStatusPanel
          curveId={selectedCurveId}
          schedules={schedules}
          jobs={jobs}
          executions={executions}
          latestExecution={latestExecution}
          datasets={selectedCurveDatasets}
          timeZone={timeZone}
          onSaveSchedule={saveSchedule}
          onResetSchedule={resetSchedule}
          onCreateBackload={createBackload}
        />

        <section className="chart-zone">
          <svg viewBox={`0 0 ${chartViewBox.width} ${chartViewBox.height}`} role="img" aria-label="Dataset time series">
            {selected?.unit && <text className="chart-y-title" x={chartBounds.left} y={chartBounds.top - 12}>{selected.unit}</text>}
            {chartData.yTicks.map(tick => (
              <g className="chart-grid" key={`y-${tick.label}`}>
                <line x1={chartBounds.left} y1={tick.y} x2={chartBounds.right} y2={tick.y} />
                <text className="chart-y-tick" x={chartBounds.left - 12} y={tick.y + 4} textAnchor="end">{tick.label}</text>
              </g>
            ))}
            {chartData.xTicks.map(tick => (
              <g className="chart-grid" key={`x-${tick.label}-${tick.x}`}>
                <line x1={tick.x} y1={chartBounds.top} x2={tick.x} y2={chartBounds.bottom} />
                <text x={tick.x} y={chartBounds.bottom + 30} textAnchor={tick.anchor}>{tick.label}</text>
              </g>
            ))}
            <line className="chart-axis" x1={chartBounds.left} y1={chartBounds.bottom} x2={chartBounds.right} y2={chartBounds.bottom} />
            <line className="chart-axis" x1={chartBounds.left} y1={chartBounds.top} x2={chartBounds.left} y2={chartBounds.bottom} />
            {hoveredPoint && <line className="chart-hover-line" x1={hoveredPoint.x} y1={chartBounds.top} x2={hoveredPoint.x} y2={chartBounds.bottom} />}
            {chartData.path && <path d={chartData.path} />}
            {chartData.points.map((chartPoint, index) => (
              <circle
                key={`${chartPoint.point.timestamp}-${chartPoint.point.asOf}-${index}`}
                cx={chartPoint.x}
                cy={chartPoint.y}
                r="4"
                tabIndex={0}
                aria-label={`${formatDate(chartPoint.point.timestamp, timeZone)} ${formatValue(chartPoint.point.value, selected?.unit ?? '')}`}
                onMouseEnter={() => setHoveredPoint(chartPoint)}
                onMouseLeave={() => setHoveredPoint(null)}
                onFocus={() => setHoveredPoint(chartPoint)}
                onBlur={() => setHoveredPoint(null)}
              />
            ))}
            {hoveredPoint && (
              <g className="chart-tooltip" transform={tooltipTransform(hoveredPoint)}>
                <rect width="238" height="44" rx="6" />
                <text x="10" y="17">
                  {formatDate(hoveredPoint.point.timestamp, timeZone)}
                  <tspan x="10" dy="17">{formatValue(hoveredPoint.point.value, selected?.unit ?? '')}</tspan>
                </text>
              </g>
            )}
            {!chartData.path && <text x={chartViewBox.width / 2} y={chartViewBox.height / 2} textAnchor="middle">No points loaded</text>}
          </svg>
          <div className="chart-footer">
            <span>{series.length} points</span>
            <span>{selected?.lastIngestedAt ? `Last ingested ${formatDate(selected.lastIngestedAt, timeZone)}` : 'No ingestion timestamp'}</span>
            <span>{lastLiveRefresh ? `Live refresh ${formatDate(lastLiveRefresh, timeZone)}` : `Live refresh every ${refreshIntervalMs / 1000}s`}</span>
          </div>
        </section>

        <PointTable points={series} unit={selected?.unit ?? ''} timeZone={timeZone} />
      </section>
    </main>
  );
}

function IngestionStatusPanel({
  curveId,
  schedules,
  jobs,
  executions,
  latestExecution,
  datasets,
  timeZone,
  onSaveSchedule,
  onResetSchedule,
  onCreateBackload
}: {
  curveId: string;
  schedules: IngestionSchedule[];
  jobs: IngestionJob[];
  executions: IngestionExecution[];
  latestExecution?: IngestionExecution;
  datasets: DatasetMetadata[];
  timeZone: string;
  onSaveSchedule: (schedule: IngestionSchedule) => Promise<boolean>;
  onResetSchedule: (scheduleId: string) => Promise<boolean>;
  onCreateBackload: (scheduleId: string, datasetId: string, windowStartExpression: string, windowEndExpression: string, batchSize: number) => Promise<boolean>;
}) {
  const [historyScheduleId, setHistoryScheduleId] = useState('');
  const [editingSchedule, setEditingSchedule] = useState<IngestionSchedule | null>(null);
  const [backloadSchedule, setBackloadSchedule] = useState<IngestionSchedule | null>(null);
  const scheduleRows = buildScheduleStatusRows(schedules, jobs, executions);
  const historySchedule = scheduleRows.find(row => row.id === historyScheduleId);
  const historyRows = historyScheduleId ? buildScheduleJobHistoryRows(historyScheduleId, jobs, executions).slice(0, 20) : [];
  const healthySchedules = scheduleRows.filter(row => row.displayStatus === 'completed' || row.displayStatus === 'working').length;
  const failedSchedules = scheduleRows.filter(row => row.displayStatus === 'failed' || row.displayStatus === 'retrying').length;

  return (
    <section className="ingestion-panel">
      <header className="section-heading">
        <div>
          <h2>Curve ingestion</h2>
          <p>{curveId ? `${curveId} · ` : ''}{schedules.length} schedules · latest result by schedule</p>
        </div>
        {latestExecution && <StatusBadge status={latestExecution.status} />}
      </header>

      <div className="status-summary">
        <MetricTile className="metric-tile-compact" label="Enabled schedules" value={schedules.filter(x => x.enabled).length.toString()} />
        <MetricTile label="Healthy schedules" value={healthySchedules.toString()} />
        <MetricTile label="Needs attention" value={failedSchedules.toString()} />
        <MetricTile label="Latest inserted" value={(latestExecution?.inserted ?? 0).toLocaleString()} />
      </div>

      <ScheduleStatusTable
        rows={scheduleRows}
        timeZone={timeZone}
        onOpenHistory={setHistoryScheduleId}
        onEdit={scheduleId => setEditingSchedule(schedules.find(schedule => schedule.id === scheduleId) ?? null)}
        onBackload={scheduleId => setBackloadSchedule(schedules.find(schedule => schedule.id === scheduleId) ?? null)}
      />
      {editingSchedule && (
        <ScheduleEditModal
          schedule={editingSchedule}
          datasets={datasets}
          onClose={() => setEditingSchedule(null)}
          onSave={async schedule => {
            if (await onSaveSchedule(schedule)) {
              setEditingSchedule(null);
            }
          }}
          onReset={async scheduleId => {
            if (await onResetSchedule(scheduleId)) {
              setEditingSchedule(null);
            }
          }}
        />
      )}
      {backloadSchedule && (
        <BackloadModal
          schedule={backloadSchedule}
          datasets={datasets}
          onClose={() => setBackloadSchedule(null)}
          onCreate={async (datasetId, windowStartExpression, windowEndExpression, batchSize) => {
            if (await onCreateBackload(backloadSchedule.id, datasetId, windowStartExpression, windowEndExpression, batchSize)) {
              setBackloadSchedule(null);
            }
          }}
        />
      )}
      {historySchedule && (
        <ScheduleHistoryModal
          schedule={historySchedule}
          rows={historyRows}
          timeZone={timeZone}
          onClose={() => setHistoryScheduleId('')}
        />
      )}
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

function DateExpressionInput({
  label,
  value,
  placeholder,
  onChange
}: {
  label: string;
  value: string;
  placeholder: string;
  onChange: (value: string) => void;
}) {
  return (
    <label>
      <span>{label}</span>
      <div className="date-expression">
        <input value={value} onChange={event => onChange(event.target.value)} placeholder={placeholder} />
        <input
          aria-label={`${label} picker`}
          type="datetime-local"
          value={toPickerInput(value)}
          onChange={event => onChange(event.target.value)}
        />
      </div>
    </label>
  );
}

function ScheduleStatusTable({
  rows,
  timeZone,
  onOpenHistory,
  onEdit,
  onBackload
}: {
  rows: ReturnType<typeof buildScheduleStatusRows>;
  timeZone: string;
  onOpenHistory: (scheduleId: string) => void;
  onEdit: (scheduleId: string) => void;
  onBackload: (scheduleId: string) => void;
}) {
  return (
    <section className="status-table">
      <h3>Schedule status</h3>
      <div className="table-scroll compact">
        <table>
          <thead><tr><th>Schedule</th><th>Latest result</th><th>Last queued</th><th>Rows</th><th></th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.id} title={row.latestExecution?.error || row.latestJob?.error || row.id}>
                <td>
                  <strong>{row.name}</strong>
                  <small>{row.detail}</small>
                </td>
                <td>
                  <StatusBadge status={row.displayStatus} />
                  <small>{row.displayExecution?.error || row.latestExecution?.error || row.latestJob?.error || row.latestJob?.id || 'No jobs yet'}</small>
                </td>
                <td>{row.latestJob?.queuedAt ? formatDate(row.latestJob.queuedAt, timeZone) : (row.latestJob ? '' : 'Never')}</td>
                <td>{row.displayExecution ? `${row.displayExecution.inserted.toLocaleString()} inserted · ${row.displayExecution.skipped.toLocaleString()} skipped` : ''}</td>
                <td>
                  <button className="table-action" type="button" onClick={() => onEdit(row.id)}>
                    Edit
                  </button>
                  <button className="table-action" type="button" onClick={() => onBackload(row.id)}>
                    Backload
                  </button>
                  <button className="table-action" type="button" onClick={() => onOpenHistory(row.id)}>
                    History
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function ScheduleEditModal({
  schedule,
  datasets,
  onClose,
  onSave,
  onReset
}: {
  schedule: IngestionSchedule;
  datasets: DatasetMetadata[];
  onClose: () => void;
  onSave: (schedule: IngestionSchedule) => Promise<void>;
  onReset: (scheduleId: string) => Promise<void>;
}) {
  const [draft, setDraft] = useState(schedule);
  const cronChanged = draft.cronExpression !== draft.defaultCronExpression;
  const granularity = datasets.find(dataset => dataset.endpoint === schedule.endpoint)?.granularity ?? '';
  const suggestedCron = suggestCronFromGranularity(granularity);

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div className="modal-panel form-modal" role="dialog" aria-modal="true" aria-labelledby="schedule-edit-title" onClick={event => event.stopPropagation()}>
        <header className="modal-header">
          <div>
            <h3 id="schedule-edit-title">Edit schedule</h3>
            <p>{schedule.name} · {schedule.endpoint}</p>
          </div>
          <button className="icon-button" type="button" onClick={onClose} aria-label="Close schedule editor">×</button>
        </header>

        <div className="form-grid">
          <label className="switch-row">
            <input type="checkbox" checked={draft.enabled} onChange={event => setDraft({ ...draft, enabled: event.target.checked })} />
            <span>{draft.enabled ? 'Enabled' : 'Disabled'}</span>
          </label>
          <label>
            <span>Cron</span>
            <input value={draft.cronExpression} onChange={event => setDraft({ ...draft, cronExpression: event.target.value })} />
            <small>{cronChanged ? `Default: ${draft.defaultCronExpression}` : 'Using default cadence'}</small>
            {suggestedCron && <small>Granularity suggests {suggestedCron}</small>}
          </label>
          <label>
            <span>Window start</span>
            <input value={draft.windowStartExpression} onChange={event => setDraft({ ...draft, windowStartExpression: event.target.value })} />
          </label>
          <label>
            <span>Window end</span>
            <input value={draft.windowEndExpression} onChange={event => setDraft({ ...draft, windowEndExpression: event.target.value })} />
          </label>
          <label>
            <span>Batch size</span>
            <input type="number" min="1" value={draft.batchSize} onChange={event => setDraft({ ...draft, batchSize: Number(event.target.value) })} />
          </label>
        </div>

        <PresetButtons onPick={(startValue, endValue) => setDraft({ ...draft, windowStartExpression: startValue, windowEndExpression: endValue })} />

        <footer className="modal-actions">
          <button className="secondary" type="button" onClick={() => onReset(schedule.id)}>Reset to default</button>
          <span className="muted">Default cron follows the seeded curve cadence.</span>
          <button type="button" onClick={() => onSave(draft)}>Save</button>
        </footer>
      </div>
    </div>
  );
}

function BackloadModal({
  schedule,
  datasets,
  onClose,
  onCreate
}: {
  schedule: IngestionSchedule;
  datasets: DatasetMetadata[];
  onClose: () => void;
  onCreate: (datasetId: string, windowStartExpression: string, windowEndExpression: string, batchSize: number) => Promise<void>;
}) {
  const defaultDataset = datasets.find(dataset => dataset.endpoint === schedule.endpoint) ?? datasets[0];
  const [datasetId, setDatasetId] = useState(defaultDataset?.id ?? '');
  const [windowStartExpression, setWindowStartExpression] = useState('today-1');
  const [windowEndExpression, setWindowEndExpression] = useState('today');
  const [batchSize, setBatchSize] = useState(schedule.batchSize);

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div className="modal-panel form-modal" role="dialog" aria-modal="true" aria-labelledby="backload-title" onClick={event => event.stopPropagation()}>
        <header className="modal-header">
          <div>
            <h3 id="backload-title">Backload missed data</h3>
            <p>{schedule.name} · queued once, then normal schedule continues</p>
          </div>
          <button className="icon-button" type="button" onClick={onClose} aria-label="Close backload">×</button>
        </header>

        <div className="form-grid">
          <label className="wide">
            <span>Dataset</span>
            <select value={datasetId} onChange={event => setDatasetId(event.target.value)}>
              {datasets.map(dataset => (
                <option key={dataset.id} value={dataset.id}>
                  {dataset.metric} · {dataset.endpoint} · {dataset.granularity || 'unknown cadence'}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Start</span>
            <input value={windowStartExpression} onChange={event => setWindowStartExpression(event.target.value)} />
          </label>
          <label>
            <span>End</span>
            <input value={windowEndExpression} onChange={event => setWindowEndExpression(event.target.value)} />
          </label>
          <label>
            <span>Batch size</span>
            <input type="number" min="1" value={batchSize} onChange={event => setBatchSize(Number(event.target.value))} />
          </label>
        </div>

        <PresetButtons onPick={(startValue, endValue) => {
          setWindowStartExpression(startValue);
          setWindowEndExpression(endValue);
        }} />

        <footer className="modal-actions">
          <span className="muted">Use relative windows like today-1 to today or exact ISO timestamps.</span>
          <button type="button" disabled={!datasetId} onClick={() => onCreate(datasetId, windowStartExpression, windowEndExpression, batchSize)}>Queue backload</button>
        </footer>
      </div>
    </div>
  );
}

function PresetButtons({ onPick }: { onPick: (startValue: string, endValue: string) => void }) {
  const presets = [
    ['Previous day', 'today-1', 'today'],
    ['Last 48h', 'now-48h', 'now'],
    ['Today + forecast', 'today', 'today+1']
  ];

  return (
    <div className="preset-row">
      {presets.map(([label, startValue, endValue]) => (
        <button className="secondary" type="button" key={label} onClick={() => onPick(startValue, endValue)}>
          {label}
        </button>
      ))}
    </div>
  );
}

function ScheduleHistoryModal({
  schedule,
  rows,
  timeZone,
  onClose
}: {
  schedule: ScheduleStatusRow;
  rows: ReturnType<typeof buildScheduleJobHistoryRows>;
  timeZone: string;
  onClose: () => void;
}) {
  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="schedule-history-title" onClick={event => event.stopPropagation()}>
        <header className="modal-header">
          <div>
            <h3 id="schedule-history-title">Schedule history</h3>
            <p>{schedule.name} · {schedule.detail}</p>
          </div>
          <button className="icon-button" type="button" onClick={onClose} aria-label="Close history">×</button>
        </header>

        <div className="table-scroll compact">
          <table>
            <thead><tr><th>Job</th><th>Job status</th><th>Latest execution</th><th>Result</th></tr></thead>
            <tbody>
              {rows.map(row => (
                <tr key={row.id} title={row.latestExecution?.error || row.job.error || row.id}>
                  <td>
                    <strong>{formatDate(row.job.queuedAt, timeZone)}</strong>
                    <small>{row.job.id}</small>
                  </td>
                  <td><StatusBadge status={row.job.status} /></td>
                  <td>
                    {row.latestExecution ? <StatusBadge status={row.latestExecution.status} /> : <span className="muted">No execution</span>}
                    <small>{row.latestExecution?.error || row.job.error || row.job.scheduleId}</small>
                  </td>
                  <td>{row.latestExecution ? `${row.latestExecution.inserted.toLocaleString()} inserted · ${row.latestExecution.skipped.toLocaleString()} skipped` : ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {!rows.length && <p className="empty-state">No jobs for this schedule yet.</p>}
        </div>
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  return <span className={`status-badge ${status.toLowerCase()}`}>{status || 'unknown'}</span>;
}

function MetadataPanel({ dataset, curveId, datasetCount, timeZone }: { dataset: DatasetMetadata | null; curveId: string; datasetCount: number; timeZone: string }) {
  if (!dataset) return <section className="metadata-panel"><h2>Curve details</h2><p>No curve selected.</p></section>;
  const rows = [
    ['Curve', curveId],
    ['Datasets', datasetCount.toLocaleString()],
    ['Selected dataset', dataset.id],
    ['Source', dataset.source],
    ['Endpoint', dataset.endpoint],
    ['Metric', dataset.metric],
    ['Kind', dataset.dataKind],
    ['Category', dataset.category],
    ['Unit', dataset.unit],
    ['Country', dataset.country],
    ['Bidding zone', dataset.biddingZone],
    ['Region', dataset.region],
    ['Granularity', dataset.granularity],
    ['Production type', dataset.productionType],
    ['Forecast type', dataset.forecastType],
    ['Neighbor', dataset.neighbor],
    ['Last ingested', formatDate(dataset.lastIngestedAt, timeZone)]
  ].filter(([, value]) => value);

  return (
    <section className="metadata-panel">
      <h2>Curve details</h2>
      <dl>{rows.map(([label, value]) => <React.Fragment key={label}><dt>{label}</dt><dd>{value}</dd></React.Fragment>)}</dl>
    </section>
  );
}

function PointTable({ points, unit, timeZone }: { points: TimeSeriesPoint[]; unit: string; timeZone: string }) {
  return (
    <section className="table-panel">
      <h2>{points.length} points</h2>
      <div className="table-scroll">
        <table>
          <thead><tr><th>Timestamp</th><th>Value</th><th>As of</th></tr></thead>
          <tbody>
            {points.slice(0, 200).map(point => (
              <tr key={`${point.timestamp}-${point.asOf}`}>
                <td>{formatDate(point.timestamp, timeZone)}</td>
                <td>{formatValue(point.value, unit)}</td>
                <td>{formatDate(point.asOf, timeZone)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function buildChartData(points: TimeSeriesPoint[], unit: string, timeZone: string) {
  const numeric = points.filter(point => point.value !== null);
  if (numeric.length === 0) return { points: [], path: '', xTicks: [], yTicks: [] };

  const values = numeric.map(point => point.value as number);
  const rawMin = Math.min(...values);
  const rawMax = Math.max(...values);
  const padding = rawMin === rawMax ? Math.max(Math.abs(rawMin) * 0.1, 1) : 0;
  const min = rawMin - padding;
  const max = rawMax + padding;
  const plotLeft = chartBounds.left + chartHorizontalPadding;
  const plotRight = chartBounds.right - chartHorizontalPadding;
  const width = plotRight - plotLeft;
  const height = chartBounds.bottom - chartBounds.top;
  const range = Math.max(max - min, 1);
  const chartPoints = numeric.map((point, index) => {
    const x = numeric.length === 1 ? plotLeft + width / 2 : plotLeft + (index * width) / (numeric.length - 1);
    const y = chartBounds.bottom - (((point.value as number) - min) * height) / range;
    return { x, y, point };
  });

  const yTicks = Array.from({ length: 5 }, (_, index) => {
    const value = min + (range * index) / 4;
    const y = chartBounds.bottom - ((value - min) * height) / range;
    return { y, label: formatTickValue(value) };
  }).reverse();
  const xTicks = buildXTicks(chartPoints, timeZone);
  const path = chartPoints.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' ');

  return { points: chartPoints, path, xTicks, yTicks };
}

function buildXTicks(points: ChartPoint[], timeZone: string): ChartTick[] {
  const ticks = pickTicks(points, 4);
  const first = new Date(points[0].point.timestamp).getTime();
  const last = new Date(points[points.length - 1].point.timestamp).getTime();
  return ticks.map((point, index) => ({
    x: point.x,
    label: formatChartTime(point.point.timestamp, last - first, timeZone),
    anchor: index === 0 ? 'start' : index === ticks.length - 1 ? 'end' : 'middle'
  }));
}

function pickTicks(points: ChartPoint[], maxTicks: number) {
  if (points.length <= maxTicks) return points;
  const step = (points.length - 1) / (maxTicks - 1);
  return Array.from({ length: maxTicks }, (_, index) => points[Math.round(index * step)]);
}

function tooltipPosition(point: ChartPoint) {
  const width = 238;
  const height = 44;
  const x = Math.min(Math.max(point.x + 12, 8), chartViewBox.width - width - 8);
  const y = Math.min(Math.max(point.y - height - 12, 8), chartViewBox.height - height - 8);
  return { x, y };
}

function tooltipTransform(point: ChartPoint) {
  const position = tooltipPosition(point);
  return `translate(${position.x} ${position.y})`;
}

function unique(values: string[]) {
  return Array.from(new Set(values.filter(Boolean))).sort();
}

function formatDate(value: string, timeZone: string) {
  return value ? new Intl.DateTimeFormat([], dateTimeFormat(timeZone)).format(new Date(value)) : '';
}

function formatChartTime(value: string, rangeMs: number, timeZone: string) {
  if (!value) return '';
  const date = new Date(value);
  const day = 24 * 60 * 60 * 1000;
  if (rangeMs <= 2 * day) return new Intl.DateTimeFormat([], { timeZone, hour: '2-digit', minute: '2-digit' }).format(date);
  if (rangeMs <= 14 * day) return new Intl.DateTimeFormat([], { timeZone, weekday: 'short', day: '2-digit' }).format(date);
  if (rangeMs <= 120 * day) return new Intl.DateTimeFormat([], { timeZone, month: 'short', day: '2-digit' }).format(date);
  return new Intl.DateTimeFormat([], { timeZone, month: 'short', year: '2-digit' }).format(date);
}

function formatTickValue(value: number) {
  if (Number.isInteger(value)) return value.toFixed(0);
  if (Number.isInteger(value * 10)) return value.toFixed(1);
  return value.toFixed(2);
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

function dateTimeFormat(timeZone: string): Intl.DateTimeFormatOptions {
  return {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  };
}

function toPickerInput(value: string) {
  return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value) ? value : '';
}

function formatTimeZoneLabel(value: string) {
  if (value === 'Europe/Copenhagen') return 'CET/CEST (Europe/Copenhagen)';
  return value === defaultTimeZone ? `${value} (local)` : value;
}

createRoot(document.getElementById('root')!).render(<App />);
