# NamEcommerce Web E2E Tests

Standalone Playwright project for end-to-end testing NamEcommerce Web.

## Prerequisites

- Node.js (>= 18)
- NamEcommerce Web application running at `http://localhost:5132`
- Workflow tests require the web app to run with `ASPNETCORE_ENVIRONMENT=E2E`.

## Installation

```bash
# Install dependencies
npm install

# Install Playwright browsers
npx playwright install --with-deps
```

## Running Tests

```bash
# Run all tests in headless mode
npm run test:e2e

# Run order workflow tests only
npm run test:e2e:workflow

# Run tests in headed mode (visible browser)
npm run test:e2e:headed

# Show test report
npm run test:e2e:report
```

## Order Workflow Tests

`tests/specs/order-workflow.spec.ts` covers the first order workflow cases:

- Create one-product sales order -> create purchase order -> receive stock -> create delivery note -> deliver -> complete order.
- Create one-product sales order -> create purchase order -> receive as direct ship -> confirm customer received -> complete order.

The tests skip manual login through Playwright storage state. `global-setup.ts` logs in once with:

- `E2E_USERNAME`, default `admin12`
- `E2E_PASSWORD`, default `adminadmin`

Workflow data is created and cleaned through guarded endpoints under `/__e2e`. These endpoints are only available when:

- `ASPNETCORE_ENVIRONMENT=E2E`
- `E2E:Enabled=true`
- request header `X-E2E-Token` matches `E2E:Token`
- the connection string contains `E2E:RequiredDatabaseNameFragment`

Default local values are in `appsettings.E2E.json`:

- `E2E_TOKEN=local-e2e-token`
- database name fragment `E2E`
- connection string database `NamEcommerceDb_E2E`

Before running workflow tests, start the web app against an E2E database. Do not run these tests against a shared or production database.

## Project Structure

- `tests/pages/`: Page Object Models (POM).
- `tests/specs/`: Test specifications.
- `tests/fixtures/`: Test data and fixtures.
- `tests/support/`: E2E auth, reset, seed, state helpers.
- `playwright.config.ts`: Playwright configuration.

## Visual Regression

Visual regression tests are enabled. Snapshots are stored in the `tests/specs/homepage.spec.ts-snapshots` folder (created after the first run).
To update snapshots, run:
```bash
npx playwright test --update-snapshots
```
