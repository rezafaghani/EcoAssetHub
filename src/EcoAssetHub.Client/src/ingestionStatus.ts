export interface IngestionSchedule {
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

export interface IngestionJob {
  id: string;
  scheduleId: string;
  curveId: string;
  status: string;
  queuedAt: string;
  startedAt?: string | null;
  finishedAt?: string | null;
  error: string;
}

export interface IngestionExecution {
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

export interface ScheduleStatusRow {
  id: string;
  name: string;
  detail: string;
  enabled: boolean;
  scheduleStatus: string;
  displayStatus: string;
  isRecovering: boolean;
  latestJob?: IngestionJob;
  latestExecution?: IngestionExecution;
  displayExecution?: IngestionExecution;
}

export interface JobHistoryRow {
  id: string;
  job: IngestionJob;
  latestExecution?: IngestionExecution;
}

export function buildScheduleStatusRows(
  schedules: IngestionSchedule[],
  jobs: IngestionJob[],
  executions: IngestionExecution[]
): ScheduleStatusRow[] {
  const latestJobByScheduleId = latestBy(jobs, job => job.scheduleId, job => job.queuedAt);
  const jobHistoryByScheduleId = new Map<string, JobHistoryRow[]>();
  for (const schedule of schedules) {
    jobHistoryByScheduleId.set(schedule.id, buildScheduleJobHistoryRows(schedule.id, jobs, executions));
  }

  return schedules.map(schedule => {
    const latestJob = latestJobByScheduleId.get(schedule.id);
    const history = jobHistoryByScheduleId.get(schedule.id) ?? [];
    const latestExecution = history.find(row => row.job.id === latestJob?.id)?.latestExecution;
    const previousFinishedExecution = history
      .filter(row => row.job.id !== latestJob?.id)
      .map(row => row.latestExecution)
      .find(execution => execution?.status === 'completed' || execution?.status === 'failed');
    const currentStatus = latestExecution?.status ?? latestJob?.status ?? (schedule.enabled ? 'enabled' : 'disabled');
    const isRunning = currentStatus === 'running' || currentStatus === 'queued';
    const displayExecution = isRunning && previousFinishedExecution ? previousFinishedExecution : latestExecution;
    const displayStatus = isRunning && previousFinishedExecution
      ? (previousFinishedExecution.status === 'failed' ? 'retrying' : 'working')
      : currentStatus;

    return {
      id: schedule.id,
      name: schedule.name || schedule.curveId,
      detail: `${schedule.endpoint} · ${schedule.cronExpression}`,
      enabled: schedule.enabled,
      scheduleStatus: schedule.enabled ? 'enabled' : 'disabled',
      displayStatus,
      isRecovering: isRunning && !!previousFinishedExecution,
      latestJob,
      latestExecution,
      displayExecution
    };
  });
}

export function buildJobHistoryRows(jobs: IngestionJob[], executions: IngestionExecution[]): JobHistoryRow[] {
  const latestExecutionByJobId = latestBy(executions, execution => execution.jobId, execution => execution.createdAt);

  return [...jobs]
    .sort((a, b) => b.queuedAt.localeCompare(a.queuedAt))
    .map(job => ({
      id: job.id,
      job,
      latestExecution: latestExecutionByJobId.get(job.id)
    }));
}

export function buildScheduleJobHistoryRows(scheduleId: string, jobs: IngestionJob[], executions: IngestionExecution[]) {
  return buildJobHistoryRows(
    jobs.filter(job => job.scheduleId === scheduleId),
    executions.filter(execution => execution.scheduleId === scheduleId)
  );
}

function latestBy<T>(items: T[], key: (item: T) => string, time: (item: T) => string) {
  const latest = new Map<string, T>();

  for (const item of items) {
    const itemKey = key(item);
    const existing = latest.get(itemKey);
    if (!existing || time(item) > time(existing)) {
      latest.set(itemKey, item);
    }
  }

  return latest;
}
