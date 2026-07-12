# EcoAssetHub Client

React 19 + Vite frontend for exploring Energy Charts curves, datasets, and curve-scoped ingestion status.

The app starts from a Curve list. Selecting a curve shows only that curve's datasets, schedules, jobs, and job executions.

## Development server

Run the dev server on `127.0.0.1`:

```bash
npm install
npm start
```

## Build

```bash
npm run build
```

## Tests

```bash
npm test
```

## Notes

Set `VITE_API_BASE_URL` if the API is not available at `/api`.
