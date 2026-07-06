/**
 * Environment configuration (DP-DEPLOY-001) — externalises the backend API base URL so it
 * is never hardcoded inline in a component or service. The backend's local launch profile
 * (src/SkyRoute.Api/Properties/launchSettings.json) serves on http://localhost:5094; CORS on
 * the backend (appsettings.json Cors:AllowedOrigins) is configured for the Angular dev
 * server's default origin, http://localhost:4200 (ASM-012).
 */
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5094/api',
};
