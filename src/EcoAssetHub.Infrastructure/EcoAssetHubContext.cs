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
                ON energy_datasets(endpoint, curve_id, metric, country, bidding_zone, region, granularity);

            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS default_cron_expression text NOT NULL DEFAULT '';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS window_start_expression text NOT NULL DEFAULT 'now-48h';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS window_end_expression text NOT NULL DEFAULT 'now';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS default_window_start_expression text NOT NULL DEFAULT 'now-48h';
            ALTER TABLE ingestion_schedules ADD COLUMN IF NOT EXISTS default_window_end_expression text NOT NULL DEFAULT 'now';
            """;

        await using var command = Postgres.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task EnsureClickHouseSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateClickHouseConnection();
        await connection.OpenAsync(cancellationToken);

        await ExecuteClickHouseAsync(connection, """
            CREATE TABLE IF NOT EXISTS energy_time_series_points (
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
