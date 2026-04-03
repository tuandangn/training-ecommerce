# NamEcommerce Web E2E Tests

This is a standalone Playwright project for end-to-end testing of the NamEcommerce Web application.

## Prerequisites

- Node.js (>= 18)
- NamEcommerce Web application running at `http://localhost:5132`

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

# Run tests in headed mode (visible browser)
npm run test:e2e:headed

# Show test report
npm run test:e2e:report
```

## Project Structure

- `tests/pages/`: Page Object Models (POM).
- `tests/specs/`: Test specifications.
- `tests/fixtures/`: Test data and fixtures.
- `playwright.config.ts`: Playwright configuration.

## Visual Regression

Visual regression tests are enabled. Snapshots are stored in the `tests/specs/homepage.spec.ts-snapshots` folder (created after the first run).
To update snapshots, run:
```bash
npx playwright test --update-snapshots
```
