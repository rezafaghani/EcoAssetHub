# Validation Platform v2 Notes

## Missing Data Investigation

Pipeline checked: provider response normalization, ingestion write, ClickHouse storage, API reads, validation runtime, and UI display.

Findings:

- Provider-side missing values are represented as `null` values in Energy Charts payloads, normalized into `TimeSeriesWritePoint.Value = null`, stored as nullable ClickHouse values, and returned by the API. Validation now classifies these as `provider_missing_data` instead of application failures.
- Absent timestamps are still reported as completeness warnings, not provider failures. A missing row can be caused by provider coverage, ingestion, mapping, storage, API range/version filters, or time-zone window selection, so it should remain auditable.
- The concrete system bug found was in validation execution: `ExecutionRuntime` read at most `10000` points from storage. Long historical/backfill windows could therefore look incomplete even when ClickHouse had more rows. The cap was removed for validation/execution reads.
- Storage versioning uses latest `as_of` per dataset/timestamp and stores actual and forecast datasets in separate tables based on `energy_datasets.data_kind`; no mapping bug was found in that read path.
- UI range selection and API date resolution both use `DateTimeExpression` with the selected time zone. No time-zone conversion loss was found, but arbitrary local windows can still expose real provider/data coverage gaps.

Remaining TODO:

- Add a diagnostics endpoint that compares expected timestamps, stored rows, null-valued provider rows, and API-returned rows for one dataset/range.
- Persist execution result payload details in the UI when users need to inspect exact missing timestamp samples.
