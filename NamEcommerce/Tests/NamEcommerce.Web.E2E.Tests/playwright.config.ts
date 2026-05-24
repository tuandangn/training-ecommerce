import * as path from 'path';
import { defineConfig, devices } from '@playwright/test';

const authState = path.join(__dirname, 'tests', '.auth', 'admin.json');
const isWorkflowRun = process.env.E2E_WORKFLOW === 'true';

export default defineConfig({
  testDir: './tests/specs',
  globalSetup: './tests/support/global-setup',
  webServer: {
    command: 'dotnet run --launch-profile E2E --project ../../Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj',
    url: 'http://localhost:4132',
    reuseExistingServer: true,
    timeout: 120_000,
    ignoreHTTPSErrors: true,
  },
  timeout: 60 * 1000,
  expect: {
    timeout: 10000
  },
  fullyParallel: !isWorkflowRun,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: isWorkflowRun || process.env.CI ? 1 : undefined,
  reporter: [
    ['html', { open: 'never', outputFolder: 'playwright-report' }],
    ['list']
  ],
  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:4132',
    storageState: authState,
    headless: true,
    viewport: { width: 1280, height: 720 },
    ignoreHTTPSErrors: true,
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure'
  },
  projects: isWorkflowRun
    ? [
      {
        name: 'workflow-chromium',
        use: { ...devices['Desktop Chrome'] },
      },
    ]
    : [
      {
        name: 'chromium',
        use: { ...devices['Desktop Chrome'] },
      },
      {
        name: 'firefox',
        use: { ...devices['Desktop Firefox'] },
      },
      {
        name: 'webkit',
        use: { ...devices['Desktop Safari'] },
      },
    ],
});
