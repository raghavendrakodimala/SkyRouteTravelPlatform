/**
 * Environment configuration (DP-DEPLOY-001) — production build variant. Selected via
 * `angular.json`'s `build.configurations.production.fileReplacements` in place of
 * `environment.ts` for `defaultConfiguration: "production"` builds (CR-004). Kept in the
 * same shape as `environment.ts`; `apiBaseUrl` should be updated if/when a non-local
 * production backend target is introduced (`docs/requirements.md` Section 1.3 — this MVP
 * is local-only, so it currently mirrors the dev backend origin).
 */
export const environment = {
  production: true,
  apiBaseUrl: 'http://localhost:5094/api',
};
