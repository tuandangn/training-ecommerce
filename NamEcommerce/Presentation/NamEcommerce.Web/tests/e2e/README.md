# End‑to‑End Tests for NamEcommerce.Web

This folder contains Playwright end‑to‑end (E2E) tests.

## Prerequisites
- Node.js (>= 18) installed.
- Run `npm install` in the **NamEcommerce.Web** project root to install Playwright and its browsers.

## Project structure
```
tests/e2e/
├─ pages/      # Page Object Model classes (e.g., HomePage.ts)
├─ specs/      # Test specifications (e.g., homepage.spec.ts)
├─ fixtures/   # Shared test data (e.g., data.ts)
└─ README.md   # This file
```

## Running the tests locally
```bash
# Install dependencies (if not already done)
npm install

# Run the full suite (headless)
npm run test:e2e

# Run with a visible browser (debugging)
npm run test:e2e:headed
```

## Generating a report
After a run, an HTML report is generated in `playwright-report/`. Open it with:
```bash
npm run test:e2e:report
```

## Visual regression
Screenshots are captured with `expect(page).toHaveScreenshot()` and stored under `tests/e2e/screenshots/`. Baseline images are committed to the repo; subsequent runs compare against them.

## CI integration
The repository includes a GitHub Actions workflow (`.github/workflows/e2e-tests.yml`) that runs the suite on every push and pull request, uploading the HTML report as an artifact.

---
*If you need to add more test scenarios, follow the same pattern: create a page object in `pages/`, write a spec in `specs/`, and optionally add fixtures.
```
