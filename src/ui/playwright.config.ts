import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env['PLAYWRIGHT_BASE_URL'] ?? 'http://127.0.0.1:4201';

export default defineConfig({
  testDir: './tests/e2e',
  outputDir: '../../build/playwright/test-results',
  fullyParallel: true,
  reporter: [
    ['list'],
    [
      'html',
      {
        open: 'never',
        outputFolder: '../../build/playwright/html-report',
      },
    ],
  ],
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'npm run start:e2e',
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000,
    url: baseURL,
  },
  projects: [
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
