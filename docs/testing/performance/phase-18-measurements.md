# Phase 18 Performance Measurements — Raw Data

| Field | Value |
|---|---|
| Date | 2026-07-07 |
| Branch | `sdlc/18-performance-review-skyroute-mvp` |
| Commit basis | `3d44d99` |
| Agent | performance-tester |
| Environment | Windows 11 Enterprise 10.0.26200, single developer machine, localhost, no artificial network latency |
| Backend | `dotnet run --project src/SkyRoute.Api --urls http://localhost:5094` (.NET SDK 10.0.301, Development environment) |
| Measurement tool | `curl -s -o /dev/null -w "%{time_total}"`, 20 sequential warm iterations per endpoint (per NFR-PERF-001 methodology: ≥20 sequential requests, single-user local load) |
| Frontend build | `npm run build` (Angular production configuration) in `frontend/` |

Both mock providers (GlobalAir, BudgetWings) were registered and responding. Neither provider contains `Task.Delay`/`Thread.Sleep` (verified by grep) — mock provider latency is effectively zero, so these numbers measure the API pipeline + aggregation + serialization overhead.

## 1. POST /api/search — LHR→JFK, Economy, 1 passenger, OneWay (2-result route)

Cold (first request after fresh process start, readiness confirmed via "Now listening" log line, not an HTTP probe): **253.3 ms** (HTTP 200)

Warm, 20 iterations (seconds):

```
0.009928 0.008800 0.002935 0.004261 0.005113 0.003897 0.003511 0.002911 0.003166 0.003223
0.002501 0.004187 0.002983 0.003724 0.002637 0.002712 0.003364 0.002377 0.002599 0.002720
```

p50 = **3.2 ms**, p95 = **8.8 ms**, max = **9.9 ms**

Response payload: **517 bytes** (`application/json`), 2 flight results (GA101 GlobalAir + BW210 BudgetWings).

## 2. POST /api/bookings — 1 passenger (GlobalAir GA101 LHR→JFK Economy snapshot)

Cold (first booking request in a fresh process): **83.0 ms** (HTTP 201)

Warm, 20 iterations (seconds):

```
0.080083 0.004428 0.005400 0.003350 0.002977 0.003570 0.003103 0.002561 0.002418 0.003060
0.002558 0.014983 0.003119 0.002429 0.002540 0.002700 0.018174 0.002552 0.002192 0.002803
```

p50 = **3.0 ms**, p95 = **18.2 ms**, max = **80.1 ms** (max is the first-in-loop cold-ish sample; all others ≤18.2 ms)

## 3. POST /api/bookings — 9 passengers (boundary per BR-005)

Warm, 20 iterations (seconds):

```
0.002352 0.002397 0.002005 0.003510 0.002730 0.002734 0.002395 0.011046 0.002421 0.002210
0.002873 0.003016 0.002455 0.003617 0.002048 0.003076 0.001994 0.003012 0.002113 0.002458
```

p50 = **2.5 ms**, p95 = **3.6 ms**, max = **11.0 ms** — 9-passenger bookings are not measurably slower than 1-passenger (HTTP 201 confirmed for both).

## 4. Frontend production bundle (`npm run build`)

```
Initial chunk files | Names         |  Raw size | Estimated transfer size
main-WRG3QOO2.js    | main          | 365.31 kB |                91.05 kB
styles-WBOTNNBZ.css | styles        |   2.84 kB |               948 bytes
                    | Initial total | 368.15 kB |                92.00 kB
```

`angular.json` budgets: initial maximumWarning 500 kB / maximumError 1 MB — build completed with no budget warning.

## 5. Process hygiene

Two transient API server processes were started for evidence capture (PIDs 18036, 44492). Both were terminated (`taskkill //PID <pid> //F`) and port 5094 was confirmed free (`netstat`) after each run. No processes remain running.
