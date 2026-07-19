namespace EcoAssetHub.Infrastructure;

using ClickHouse.Client.ADO;
using Npgsql;

public sealed class EcoAssetHubContext
{
    private readonly NpgsqlDataSource? _postgres;

    public EcoAssetHubContext(string? postgresConnectionString, string? clickHouseConnectionString)
    {
        _postgres = string.IsNullOrWhiteSpace(postgresConnectionString) ? null : NpgsqlDataSource.Create(postgresConnectionString);
        ClickHouseConnectionString = clickHouseConnectionString ?? string.Empty;
    }

    public NpgsqlDataSource Postgres => _postgres ?? throw new InvalidOperationException("Postgres connection string is not configured.");
    public string ClickHouseConnectionString { get; }

    public ClickHouseConnection CreateClickHouseConnection()
    {
        if (string.IsNullOrWhiteSpace(ClickHouseConnectionString))
        {
            throw new InvalidOperationException("ClickHouse connection string is not configured.");
        }

        return new ClickHouseConnection(ClickHouseConnectionString);
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (_postgres is not null)
        {
            await EnsurePostgresSchemaAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(ClickHouseConnectionString))
        {
            await EnsureClickHouseSchemaAsync(cancellationToken);
        }
    }

    public async Task EnsurePostgresSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS renewable_assets (
                id text PRIMARY KEY,
                type integer NOT NULL,
                name text NOT NULL,
                capacity numeric NOT NULL,
                meter_point_id bigint NOT NULL UNIQUE,
                hub_height numeric NULL,
                rotor_diameter numeric NULL,
                compass_orientation text NULL
            );

            CREATE TABLE IF NOT EXISTS ingestion_schedules (
                id text PRIMARY KEY,
                curve_id text NOT NULL,
                name text NOT NULL,
                cron_expression text NOT NULL,
                default_cron_expression text NOT NULL DEFAULT '',
                enabled boolean NOT NULL,
                source text NOT NULL DEFAULT 'energy-charts',
                endpoint text NOT NULL,
                parameters jsonb NOT NULL,
                lookback_hours integer NOT NULL,
                window_start_expression text NOT NULL DEFAULT 'now-48h',
                window_end_expression text NOT NULL DEFAULT 'now',
                default_window_start_expression text NOT NULL DEFAULT 'now-48h',
                default_window_end_expression text NOT NULL DEFAULT 'now',
                batch_size integer NOT NULL,
                last_queued_at timestamptz NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_ingestion_schedules_enabled_curve
                ON ingestion_schedules(enabled, curve_id);

            CREATE TABLE IF NOT EXISTS ingestion_jobs (
                id text PRIMARY KEY,
                schedule_id text NOT NULL,
                curve_id text NOT NULL,
                status text NOT NULL,
                queued_at timestamptz NOT NULL,
                started_at timestamptz NULL,
                finished_at timestamptz NULL,
                error text NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_ingestion_jobs_schedule_curve_queued
                ON ingestion_jobs(schedule_id, curve_id, queued_at DESC);

            CREATE TABLE IF NOT EXISTS ingestion_executions (
                id text PRIMARY KEY,
                job_id text NOT NULL,
                schedule_id text NOT NULL,
                curve_id text NOT NULL,
                status text NOT NULL,
                created_at timestamptz NOT NULL,
                started_at timestamptz NULL,
                finished_at timestamptz NULL,
                inserted integer NOT NULL,
                skipped integer NOT NULL,
                error text NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_ingestion_executions_job_schedule_curve_created
                ON ingestion_executions(job_id, schedule_id, curve_id, created_at DESC);

            CREATE TABLE IF NOT EXISTS energy_datasets (
                id text PRIMARY KEY,
                curve_id text NOT NULL,
                source text NOT NULL,
                endpoint text NOT NULL,
                metric text NOT NULL,
                data_kind text NOT NULL DEFAULT 'actual',
                category text NOT NULL DEFAULT 'unknown',
                unit text NOT NULL,
                country text NOT NULL,
                bidding_zone text NOT NULL,
                region text NOT NULL,
                granularity text NOT NULL,
                production_type text NOT NULL,
                forecast_type text NOT NULL,
                neighbor text NOT NULL,
                license_info text NOT NULL,
                deprecated boolean NOT NULL,
                request_parameters jsonb NOT NULL,
                first_observed_at timestamptz NOT NULL,
                last_ingested_at timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_energy_datasets_filters
                ON energy_datasets(endpoint, curve_id, metric, data_kind, category, country, bidding_zone, region, granularity);

            CREATE TABLE IF NOT EXISTS quality_curve_groups (
                id text PRIMARY KEY,
                name text NOT NULL,
                description text NOT NULL,
                group_type text NOT NULL,
                enabled boolean NOT NULL,
                rule jsonb NOT NULL,
                tags jsonb NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_curve_groups_enabled_type
                ON quality_curve_groups(enabled, group_type);

            CREATE TABLE IF NOT EXISTS quality_curve_group_members (
                group_id text NOT NULL REFERENCES quality_curve_groups(id) ON DELETE CASCADE,
                dataset_id text NOT NULL,
                curve_id text NOT NULL,
                created_at timestamptz NOT NULL,
                PRIMARY KEY (group_id, dataset_id)
            );

            CREATE INDEX IF NOT EXISTS ix_quality_curve_group_members_dataset
                ON quality_curve_group_members(dataset_id, curve_id);

            CREATE TABLE IF NOT EXISTS quality_validation_jobs (
                id text PRIMARY KEY,
                name text NOT NULL,
                description text NOT NULL,
                enabled boolean NOT NULL,
                cron_expression text NOT NULL,
                time_zone text NOT NULL,
                window_start_expression text NOT NULL,
                window_end_expression text NOT NULL,
                max_parallelism integer NOT NULL,
                timeout_seconds integer NOT NULL,
                tags jsonb NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_validation_jobs_enabled
                ON quality_validation_jobs(enabled, updated_at DESC);

            CREATE TABLE IF NOT EXISTS quality_validation_job_targets (
                job_id text NOT NULL REFERENCES quality_validation_jobs(id) ON DELETE CASCADE,
                target_type text NOT NULL,
                target_id text NOT NULL,
                rule jsonb NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_validation_job_targets_job
                ON quality_validation_job_targets(job_id);

            CREATE TABLE IF NOT EXISTS quality_validation_job_checks (
                id text PRIMARY KEY,
                job_id text NOT NULL REFERENCES quality_validation_jobs(id) ON DELETE CASCADE,
                validator_id text NOT NULL,
                validator_version integer NOT NULL,
                enabled boolean NOT NULL,
                configuration jsonb NOT NULL,
                severity jsonb NOT NULL,
                sort_order integer NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_validation_job_checks_job
                ON quality_validation_job_checks(job_id, enabled, sort_order);

            CREATE TABLE IF NOT EXISTS quality_validation_executions (
                id text PRIMARY KEY,
                job_id text NOT NULL,
                trigger_type text NOT NULL,
                status text NOT NULL,
                queued_at timestamptz NOT NULL,
                started_at timestamptz NULL,
                finished_at timestamptz NULL,
                evaluated_start timestamptz NULL,
                evaluated_end timestamptz NULL,
                config_snapshot jsonb NOT NULL,
                target_snapshot jsonb NOT NULL,
                target_count integer NOT NULL,
                completed_count integer NOT NULL,
                warning_count integer NOT NULL,
                critical_count integer NOT NULL,
                technical_failure_count integer NOT NULL,
                error text NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_validation_executions_job_queued
                ON quality_validation_executions(job_id, queued_at DESC);

            CREATE TABLE IF NOT EXISTS quality_validation_target_executions (
                id text PRIMARY KEY,
                execution_id text NOT NULL REFERENCES quality_validation_executions(id) ON DELETE CASCADE,
                dataset_id text NOT NULL,
                curve_id text NOT NULL,
                status text NOT NULL,
                started_at timestamptz NULL,
                finished_at timestamptz NULL,
                evaluated_start timestamptz NULL,
                evaluated_end timestamptz NULL,
                point_count integer NOT NULL,
                error text NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_validation_target_executions_curve
                ON quality_validation_target_executions(dataset_id, curve_id, started_at DESC);

            CREATE TABLE IF NOT EXISTS quality_validator_executions (
                id text PRIMARY KEY,
                target_execution_id text NOT NULL REFERENCES quality_validation_target_executions(id) ON DELETE CASCADE,
                validator_id text NOT NULL,
                status text NOT NULL,
                severity text NOT NULL,
                duration_ms integer NOT NULL,
                metrics jsonb NOT NULL,
                error text NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_validator_executions_target
                ON quality_validator_executions(target_execution_id, validator_id);

            CREATE TABLE IF NOT EXISTS quality_findings (
                id text PRIMARY KEY,
                execution_id text NOT NULL,
                target_execution_id text NULL,
                validator_execution_id text NULL,
                dataset_id text NOT NULL,
                curve_id text NOT NULL,
                validator_id text NOT NULL,
                category text NOT NULL,
                severity text NOT NULL,
                quality_status text NOT NULL,
                trading_impact text NOT NULL,
                title text NOT NULL,
                message text NOT NULL,
                affected_start timestamptz NULL,
                affected_end timestamptz NULL,
                expected_count integer NULL,
                actual_count integer NULL,
                affected_count integer NULL,
                sample_timestamps jsonb NOT NULL,
                details jsonb NOT NULL,
                fingerprint text NOT NULL,
                active boolean NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_quality_findings_active_curve
                ON quality_findings(active, dataset_id, curve_id, severity, updated_at DESC);

            CREATE INDEX IF NOT EXISTS ix_quality_findings_fingerprint
                ON quality_findings(fingerprint, active, updated_at DESC);

            CREATE TABLE IF NOT EXISTS quality_status_snapshots (
                dataset_id text NOT NULL,
                curve_id text NOT NULL,
                overall_status text NOT NULL,
                category_statuses jsonb NOT NULL,
                latest_execution_id text NOT NULL,
                as_of timestamptz NOT NULL,
                PRIMARY KEY (dataset_id, as_of)
            );

            CREATE INDEX IF NOT EXISTS ix_quality_status_snapshots_curve_latest
                ON quality_status_snapshots(dataset_id, curve_id, as_of DESC);

            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS default_cron_expression text NOT NULL DEFAULT '';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS window_start_expression text NOT NULL DEFAULT 'now-48h';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS window_end_expression text NOT NULL DEFAULT 'now';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS default_window_start_expression text NOT NULL DEFAULT 'now-48h';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS default_window_end_expression text NOT NULL DEFAULT 'now';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS source text NOT NULL DEFAULT 'energy-charts';
            ALTER TABLE energy_datasets ADD COLUMN IF NOT EXISTS data_kind text NOT NULL DEFAULT 'actual';
            ALTER TABLE energy_datasets ADD COLUMN IF NOT EXISTS category text NOT NULL DEFAULT 'unknown';

            UPDATE energy_datasets
            SET data_kind = CASE
                    WHEN forecast_type <> '' OR metric LIKE '%forecast%' THEN 'forecast'
                    WHEN endpoint = 'installed_power' THEN 'reference'
                    ELSE data_kind
                END,
                category = CASE
                    WHEN endpoint IN ('public_power', 'total_power', 'public_power_forecast') THEN 'power'
                    WHEN endpoint = 'installed_power' THEN 'capacity'
                    WHEN endpoint = 'price' THEN 'price'
                    WHEN endpoint IN ('cbet', 'cbpf') THEN 'exchange'
                    WHEN endpoint LIKE '%share%' THEN 'share'
                    WHEN endpoint = 'frequency' THEN 'frequency'
                    WHEN endpoint = 'signal' THEN 'signal'
                    ELSE category
                END
            WHERE data_kind = 'actual' OR category = 'unknown';
            """;

        await using var command = Postgres.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task EnsureClickHouseSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateClickHouseConnection();
        await connection.OpenAsync(cancellationToken);

        await ExecuteClickHouseAsync(connection, """
            CREATE TABLE IF NOT EXISTS actual_energy_time_series_points (
                dataset_id String,
                timestamp DateTime64(3, 'UTC'),
                value Nullable(Float64),
                as_of DateTime64(3, 'UTC'),
                inserted_at DateTime64(3, 'UTC'),
                source_metadata_version String
            )
            ENGINE = MergeTree
            ORDER BY (dataset_id, timestamp, as_of)
            """, cancellationToken);

        await ExecuteClickHouseAsync(connection, """
            CREATE TABLE IF NOT EXISTS forecast_energy_time_series_points (
                dataset_id String,
                timestamp DateTime64(3, 'UTC'),
                value Nullable(Float64),
                as_of DateTime64(3, 'UTC'),
                inserted_at DateTime64(3, 'UTC'),
                source_metadata_version String
            )
            ENGINE = MergeTree
            ORDER BY (dataset_id, timestamp, as_of)
            """, cancellationToken);

        await ExecuteClickHouseAsync(connection, """
            CREATE TABLE IF NOT EXISTS power_productions (
                meter_point_id String,
                production_datetime DateTime64(3, 'UTC'),
                production Int32,
                as_of DateTime64(3, 'UTC'),
                inserted_at DateTime64(3, 'UTC')
            )
            ENGINE = MergeTree
            ORDER BY (meter_point_id, production_datetime, as_of)
            """, cancellationToken);
    }

    private static async Task ExecuteClickHouseAsync(ClickHouseConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
