# Order E2E Workflow Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a repeatable Playwright E2E test foundation for NamEcommerce workflows, starting with the order workflow from sale order to purchase order, receiving, delivery, direct-ship confirmation, and order completion.

**Architecture:** Keep E2E tests in the existing standalone Playwright project. Add a small E2E-only support surface in `NamEcommerce.Web` for authenticated session setup, deterministic seed/reset, and state assertions; all business workflow actions remain exercised through the real MVC UI.

**Tech Stack:** ASP.NET Core MVC, Cookie Authentication, EF Core SQL Server, Playwright, TypeScript.

---

## Scope Rules

- Do not add unit tests.
- Do not modify any xUnit `*.Test` project.
- Do not run EF migration commands or `Update-Database`.
- Do use the existing `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests` Playwright project.
- E2E support endpoints must be unavailable unless `ASPNETCORE_ENVIRONMENT=E2E`, `E2E:Enabled=true`, a valid token is provided, and the connection string targets an E2E database.
- Business workflow steps should go through UI. The test-support API is only for login/session preparation, reset/seed, and read-only state assertions.

## Assumptions

- The local app runs at `http://localhost:5132`, matching the current Playwright config.
- E2E login can use environment variables:
  - `E2E_USERNAME`, default `admin12`
  - `E2E_PASSWORD`, default `adminadmin`
  - `E2E_TOKEN`, required for reset/seed endpoints
- Tuấn will create/apply migrations to a separate E2E SQL Server database. Codex must not run migrations.
- The first reliable workflow suite should run Chromium only and serially. Multi-browser workflow tests can come after reset/seed is stable.

## File Map

### Existing E2E Project

- Modify: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/playwright.config.ts`
- Modify: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/package.json`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/global-setup.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/e2e-api.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/test-data.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/proof-image.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/LoginPage.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/OrderPage.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/OrderDetailsPage.ts`
- Modify: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/PurchaseOrderPage.ts`
- Modify: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/PurchaseOrderDetailsPage.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/DeliveryNotePage.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/DirectShipDeliveryPage.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/specs/order-workflow.spec.ts`

### Web App E2E Support

- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/E2ETestController.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Models/E2E/E2ETestModels.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/E2E/IE2ETestDataService.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/E2E/E2ETestDataService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.E2E.json`

### Stable UI Selectors

Add `data-testid` attributes only where current selectors are fragile:

- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/User/Login.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Create.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/Create.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/Details.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/Create.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/Details.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/DirectShipDelivery/Pending.cshtml`

---

## Test Case Matrix

Start with these two happy paths:

| ID | Workflow | Expected final state |
|---|---|---|
| `order-standard-full` | Create sale order for 1 product quantity `N` -> create PO quantity `N` -> approve PO -> receive to physical warehouse -> create delivery note -> confirm/deliver -> complete order | Order `Completed`, PO `Completed`, delivery note `Delivered`, no remaining shortage |
| `order-direct-ship-full` | Create sale order for 1 product quantity `N` -> create PO quantity `N` -> allocate PO item to order as direct-ship -> receive direct-ship -> confirm delivered in direct-ship pending screen -> complete order | Order `Completed`, PO `Completed`, direct-ship delivery removed from pending, delivery note `Delivered` |

Next cases after the foundation is stable:

| ID | Workflow |
|---|---|
| `order-standard-partial-receive` | Order `N`, PO `N`, receive `< N`, assert cannot complete order |
| `order-standard-partial-delivery` | Receive `N`, deliver `< N`, assert order remains pending |
| `order-oversupply-accept` | PO receive `> N`, choose accept oversupply to warehouse |
| `order-oversupply-reject` | PO receive `> N`, choose reject oversupply |
| `order-direct-ship-reject` | Direct-ship received, customer rejects, assert return warehouse path |
| `order-cancel-with-direct-ship-transit` | Cancel order with fully received direct-ship transit and selected return warehouse |

---

### Task 1: Configure Playwright Auth and Serial Workflow Execution

**Files:**
- Modify: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/playwright.config.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/global-setup.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/pages/LoginPage.ts`

- [ ] **Step 1: Add login page object**

Create `tests/pages/LoginPage.ts`:

```ts
import { Page, expect } from '@playwright/test';

export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto() {
    await this.page.goto('/User/Login');
  }

  async login(username: string, password: string) {
    await this.page.locator('input[name="Username"]').fill(username);
    await this.page.locator('input[name="Password"]').fill(password);
    await this.page.locator('button[type="submit"]').click();
    await expect(this.page).not.toHaveURL(/\/User\/Login/i);
  }
}
```

- [ ] **Step 2: Add global setup to save authenticated storage state**

Create `tests/support/global-setup.ts`:

```ts
import { chromium, FullConfig } from '@playwright/test';
import path from 'path';
import { LoginPage } from '../pages/LoginPage';

export default async function globalSetup(config: FullConfig) {
  const baseURL = config.projects[0].use.baseURL as string;
  const username = process.env.E2E_USERNAME || 'admin12';
  const password = process.env.E2E_PASSWORD || 'adminadmin';
  const storageStatePath = path.join(__dirname, '..', '.auth', 'admin.json');

  const browser = await chromium.launch();
  const page = await browser.newPage({ baseURL });

  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.login(username, password);

  await page.context().storageState({ path: storageStatePath });
  await browser.close();
}
```

- [ ] **Step 3: Wire storage state into Playwright config**

Modify `playwright.config.ts`:

```ts
import path from 'path';
import { defineConfig, devices } from '@playwright/test';

const authState = path.join(__dirname, 'tests', '.auth', 'admin.json');
const isWorkflowRun = process.env.E2E_WORKFLOW === 'true';

export default defineConfig({
  testDir: './tests/specs',
  globalSetup: require.resolve('./tests/support/global-setup'),
  timeout: 60 * 1000,
  expect: { timeout: 10000 },
  fullyParallel: !isWorkflowRun,
  workers: isWorkflowRun ? 1 : undefined,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [
    ['html', { open: 'never', outputFolder: 'playwright-report' }],
    ['list']
  ],
  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:5132',
    storageState: authState,
    headless: true,
    viewport: { width: 1280, height: 720 },
    ignoreHTTPSErrors: true,
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure'
  },
  projects: isWorkflowRun
    ? [{ name: 'workflow-chromium', use: { ...devices['Desktop Chrome'] } }]
    : [
        { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
        { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
        { name: 'webkit', use: { ...devices['Desktop Safari'] } }
      ]
});
```

- [ ] **Step 4: Add npm scripts**

Modify `package.json`:

```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:workflow": "cross-env E2E_WORKFLOW=true playwright test tests/specs/order-workflow.spec.ts",
    "test:e2e:headed": "playwright test --headed",
    "test:e2e:report": "playwright show-report"
  }
}
```

If `cross-env` is not already installed, avoid adding it in the first pass and use PowerShell env assignment in docs:

```powershell
$env:E2E_WORKFLOW='true'; rtk npm run test:e2e -- tests/specs/order-workflow.spec.ts --project=workflow-chromium
```

### Task 2: Add Safe E2E Reset and Seed API

**Files:**
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/E2ETestController.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Models/E2E/E2ETestModels.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/E2E/IE2ETestDataService.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Services/E2E/E2ETestDataService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.E2E.json`

- [ ] **Step 1: Add E2E config**

Create `appsettings.E2E.json`:

```json
{
  "E2E": {
    "Enabled": true,
    "Token": "local-e2e-token",
    "RequiredDatabaseNameFragment": "E2E"
  },
  "ConnectionStrings": {
    "NamEcommerceEfDbContext": "Data Source=.\\SQLEXPRESS;Database=NamEcommerceDb_E2E;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  }
}
```

- [ ] **Step 2: Add request/response models**

Create `Models/E2E/E2ETestModels.cs`:

```csharp
namespace NamEcommerce.Web.Models.E2E;

public sealed record E2EResetRequest(string? ScenarioId);

public sealed record E2ESeedOrderWorkflowRequest
{
    public required string ScenarioId { get; init; }
    public required decimal Quantity { get; init; }
    public required bool DirectShip { get; init; }
}

public sealed record E2ESeedOrderWorkflowResult
{
    public required string ScenarioId { get; init; }
    public required decimal Quantity { get; init; }
    public required string CustomerName { get; init; }
    public required string CustomerPhone { get; init; }
    public required string ShippingAddress { get; init; }
    public required string VendorName { get; init; }
    public required string WarehouseName { get; init; }
    public required string ProductName { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal UnitCost { get; init; }
}

public sealed record E2EOrderWorkflowState
{
    public required string ScenarioId { get; init; }
    public string? OrderCode { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public required string OrderStatus { get; init; }
    public required string PurchaseOrderStatus { get; init; }
    public required string DeliveryStatus { get; init; }
    public required decimal OrderedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal DeliveredQuantity { get; init; }
}
```

- [ ] **Step 3: Add service interface**

Create `Services/E2E/IE2ETestDataService.cs`:

```csharp
using NamEcommerce.Web.Models.E2E;

namespace NamEcommerce.Web.Services.E2E;

public interface IE2ETestDataService
{
    Task ResetAsync(string? scenarioId, CancellationToken cancellationToken = default);
    Task<E2ESeedOrderWorkflowResult> SeedOrderWorkflowAsync(E2ESeedOrderWorkflowRequest request, CancellationToken cancellationToken = default);
    Task<E2EOrderWorkflowState> GetOrderWorkflowStateAsync(string scenarioId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement reset and seed service**

Create `Services/E2E/E2ETestDataService.cs`.

Rules for implementation:

- Build a marker: `var marker = $"E2E-{request.ScenarioId}"`.
- Seed names:
  - Customer: `${marker} Customer`
  - Vendor: `${marker} Vendor`
  - Warehouse: `${marker} Warehouse`
  - Product: `${marker} Product`
- Use existing application/domain services where practical for create flows.
- Use EF Core direct cleanup only in this service, only for rows containing the E2E marker.
- Cleanup transactional records before master data.
- Do not delete any row that does not contain the marker.

Cleanup order:

```csharp
// Delete by marker and relationship IDs, in dependency order:
// 1. Delivery note items, delivery notes
// 2. Goods receipt items, goods receipts
// 3. Purchase order allocations, purchase order items, purchase orders
// 4. Order items, orders
// 5. Customer debts/payments linked to E2E orders/customers
// 6. Inventory stock/movement/reservation rows for E2E products
// 7. Product vendor/category rows, products
// 8. Customers, vendors, warehouses
```

The first implementation may seed master data through UI if service-level creation is faster to wire later, but reset must stay inside `E2ETestDataService` so each scenario starts clean.

- [ ] **Step 5: Add guarded E2E controller**

Create `Controllers/E2ETestController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Models.E2E;
using NamEcommerce.Web.Services.E2E;

namespace NamEcommerce.Web.Controllers;

[ApiController]
[Route("__e2e")]
public sealed class E2ETestController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IE2ETestDataService _testDataService;

    public E2ETestController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IE2ETestDataService testDataService)
    {
        _environment = environment;
        _configuration = configuration;
        _testDataService = testDataService;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(E2EResetRequest request, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        await _testDataService.ResetAsync(request.ScenarioId, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    [HttpPost("seed/order-workflow")]
    public async Task<IActionResult> SeedOrderWorkflow(E2ESeedOrderWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        var result = await _testDataService.SeedOrderWorkflowAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("state/order-workflow/{scenarioId}")]
    public async Task<IActionResult> GetOrderWorkflowState(string scenarioId, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        var result = await _testDataService.GetOrderWorkflowStateAsync(scenarioId, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    private bool IsAllowed()
    {
        if (!_environment.IsEnvironment("E2E"))
            return false;

        if (!_configuration.GetValue<bool>("E2E:Enabled"))
            return false;

        var expectedToken = _configuration["E2E:Token"];
        if (string.IsNullOrWhiteSpace(expectedToken))
            return false;

        if (!Request.Headers.TryGetValue("X-E2E-Token", out var actualToken) || actualToken != expectedToken)
            return false;

        var requiredDbFragment = _configuration["E2E:RequiredDatabaseNameFragment"];
        var connectionString = _configuration.GetConnectionString(nameof(NamEcommerce.Data.SqlServer.NamEcommerceEfDbContext));
        return string.IsNullOrWhiteSpace(requiredDbFragment)
            || (connectionString?.Contains(requiredDbFragment, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
```

- [ ] **Step 6: Register service in DI**

Modify `Program.cs` inside `ConfigureServices`:

```csharp
if (builder.Environment.IsEnvironment("E2E"))
{
    services.AddScoped<IE2ETestDataService, E2ETestDataService>();
}
```

Add required `using`:

```csharp
using NamEcommerce.Web.Services.E2E;
```

### Task 3: Add Stable Selectors for Workflow Controls

**Files:**
- Modify the Razor files listed in "Stable UI Selectors".

- [ ] **Step 1: Login selectors**

In `Views/User/Login.cshtml`, add:

```html
<input asp-for="Username" data-testid="login-username" ... />
<input asp-for="Password" data-testid="login-password" ... />
<button type="submit" data-testid="login-submit" ...>
```

- [ ] **Step 2: Order create selectors**

In `Views/Order/Create.cshtml`, add:

```html
<div id="customerPicker" data-testid="order-customer-picker" ...></div>
<button type="button" data-testid="order-add-product-open" ...>
<input id="itemQuantity" data-testid="order-item-quantity" ... />
<input id="itemUnitPrice" data-testid="order-item-unit-price" ... />
<button type="submit" id="addItemToTable" data-testid="order-add-product-submit" ...>
<button type="submit" id="btnSubmitOrder" data-testid="order-submit" ...>
```

- [ ] **Step 3: Purchase order selectors**

In `Views/PurchaseOrder/Create.cshtml` and `Views/PurchaseOrder/Details.cshtml`, add:

```html
<div id="vendorPicker" data-testid="po-vendor-picker" ...></div>
<button id="btnSubmitPurchaseOrder" data-testid="po-submit" ...>
<button data-testid="po-submit-for-approval" ...>
<button data-testid="po-approve" ...>
<button data-testid="po-receive-item-open" ...>
<input id="modalReceivedQty" data-testid="po-receive-quantity" ... />
<select id="modalWarehouseId" data-testid="po-receive-warehouse" ...>
<button type="submit" data-testid="po-receive-submit" ...>
<button class="btn-allocate-to-order" data-testid="po-allocate-open" ...>
<input id="allocateIsDirectShip" data-testid="po-allocate-direct-ship" ...>
<button id="btnAllocateToOrder" data-testid="po-allocate-submit" ...>
```

- [ ] **Step 4: Delivery note selectors**

In `Views/DeliveryNote/Create.cshtml` and `Views/DeliveryNote/Details.cshtml`, add:

```html
<button type="submit" data-testid="delivery-create-submit" ...>
<button data-testid="delivery-confirm" ...>
<button data-testid="delivery-mark-delivering" ...>
<input type="file" data-testid="delivery-proof-file" ...>
<button data-testid="delivery-mark-delivered" ...>
```

- [ ] **Step 5: Direct ship selectors**

In `Views/DirectShipDelivery/Pending.cshtml`, add:

```html
<button class="btn-confirm-delivery" data-testid="directship-confirm-open" ...>
<button id="btnConfirmSubmit" data-testid="directship-confirm-submit" ...>
```

### Task 4: Add E2E Test API Client and Proof Image Helper

**Files:**
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/e2e-api.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/test-data.ts`
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/support/proof-image.ts`

- [ ] **Step 1: Add E2E API client**

Create `tests/support/e2e-api.ts`:

```ts
import { APIRequestContext, expect } from '@playwright/test';

export type OrderWorkflowSeed = {
  scenarioId: string;
  quantity: number;
  customerName: string;
  customerPhone: string;
  shippingAddress: string;
  vendorName: string;
  warehouseName: string;
  productName: string;
  unitPrice: number;
  unitCost: number;
};

export class E2EApi {
  constructor(private readonly request: APIRequestContext) {}

  private headers() {
    const token = process.env.E2E_TOKEN;
    if (!token) throw new Error('E2E_TOKEN is required.');
    return { 'X-E2E-Token': token };
  }

  async reset(scenarioId: string) {
    const response = await this.request.post('/__e2e/reset', {
      headers: this.headers(),
      data: { scenarioId }
    });
    expect(response.ok()).toBeTruthy();
  }

  async seedOrderWorkflow(scenarioId: string, quantity: number, directShip: boolean): Promise<OrderWorkflowSeed> {
    const response = await this.request.post('/__e2e/seed/order-workflow', {
      headers: this.headers(),
      data: { scenarioId, quantity, directShip }
    });
    expect(response.ok()).toBeTruthy();
    return response.json();
  }

  async getOrderWorkflowState(scenarioId: string) {
    const response = await this.request.get(`/__e2e/state/order-workflow/${scenarioId}`, {
      headers: this.headers()
    });
    expect(response.ok()).toBeTruthy();
    return response.json();
  }
}
```

- [ ] **Step 2: Add scenario ID helper**

Create `tests/support/test-data.ts`:

```ts
export function scenarioId(name: string) {
  return `${name}-${Date.now()}`;
}
```

- [ ] **Step 3: Add proof image helper**

Create `tests/support/proof-image.ts`:

```ts
import fs from 'fs';
import os from 'os';
import path from 'path';

export function createProofImage(): string {
  const filePath = path.join(os.tmpdir(), `delivery-proof-${Date.now()}.png`);
  const onePixelPng = Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=',
    'base64'
  );
  fs.writeFileSync(filePath, onePixelPng);
  return filePath;
}
```

### Task 5: Build Page Objects for Order Workflow

**Files:**
- Create: `tests/pages/OrderPage.ts`
- Create: `tests/pages/OrderDetailsPage.ts`
- Modify: `tests/pages/PurchaseOrderPage.ts`
- Modify: `tests/pages/PurchaseOrderDetailsPage.ts`
- Create: `tests/pages/DeliveryNotePage.ts`
- Create: `tests/pages/DirectShipDeliveryPage.ts`

- [ ] **Step 1: Order page object**

Create `tests/pages/OrderPage.ts`:

```ts
import { Page, expect } from '@playwright/test';
import { OrderWorkflowSeed } from '../support/e2e-api';

export class OrderPage {
  constructor(private readonly page: Page) {}

  async gotoCreate() {
    await this.page.goto('/Order/Create');
  }

  async createSingleProductOrder(seed: OrderWorkflowSeed) {
    await this.gotoCreate();

    await this.page.locator('[data-testid="order-customer-picker"] .customerSearch').fill(seed.customerName);
    await this.page.getByText(seed.customerName).click();

    await this.page.locator('[data-testid="order-add-product-open"]').click();
    await this.page.locator('#productPicker .productSearch').fill(seed.productName);
    await this.page.getByText(seed.productName).click();
    await this.page.locator('[data-testid="order-item-quantity"]').fill(String(seed.quantity));
    await this.page.locator('[data-testid="order-item-unit-price"]').fill(String(seed.unitPrice));
    await this.page.locator('[data-testid="order-add-product-submit"]').click();

    await this.page.locator('[data-testid="order-submit"]').click();
    await expect(this.page).toHaveURL(/\/Order\/Details\//);
  }
}
```

- [ ] **Step 2: Order details page object**

Create `tests/pages/OrderDetailsPage.ts`:

```ts
import { Page, expect } from '@playwright/test';

export class OrderDetailsPage {
  constructor(private readonly page: Page) {}

  async getOrderIdFromUrl() {
    const match = this.page.url().match(/\/Order\/Details\/([^/?#]+)/i);
    if (!match) throw new Error(`Cannot read order id from URL: ${this.page.url()}`);
    return match[1];
  }

  async expectPending() {
    await expect(this.page.locator('.topbar-page-title')).toContainText(/Đang xử lý|Pending/i);
  }

  async createDeliveryNoteFromOrder() {
    const orderId = await this.getOrderIdFromUrl();
    await this.page.goto(`/DeliveryNote/Create?orderId=${orderId}`);
  }

  async completeOrder() {
    await this.page.locator('#btnCompleteOrder,[data-testid="order-complete"]').click();
    await expect(this.page.locator('.topbar-page-title')).toContainText(/Hoàn thành|Completed/i);
  }
}
```

- [ ] **Step 3: Extend purchase order page object**

Modify `PurchaseOrderPage.ts` with a workflow helper:

```ts
async createSingleProductPurchaseOrder(seed: OrderWorkflowSeed) {
  await this.page.goto('/PurchaseOrder/Create');
  await this.page.locator('[data-testid="po-vendor-picker"] .vendorSearch').fill(seed.vendorName);
  await this.page.getByText(seed.vendorName).click();
  await this.page.locator('[data-bs-target="#addProductModal"]').click();
  await this.page.locator('#productPicker .productSearch').fill(seed.productName);
  await this.page.getByText(seed.productName).click();
  await this.page.locator('#itemQuantity').fill(String(seed.quantity));
  await this.page.locator('#itemUnitPrice').fill(String(seed.unitCost));
  await this.page.locator('#addItemToTable').click();
  await this.page.locator('[data-testid="po-submit"]').click();
  await expect(this.page).toHaveURL(/\/PurchaseOrder\/Details\//);
}
```

- [ ] **Step 4: Extend purchase order details page object**

Modify `PurchaseOrderDetailsPage.ts` with workflow helpers:

```ts
async submitAndApprove() {
  await this.page.locator('[data-testid="po-submit-for-approval"]').click();
  await this.page.locator('[data-testid="po-approve"]').click();
  await this.verifyStatus('Phê duyệt');
}

async allocateFirstItemToOrder(directShip: boolean) {
  await this.page.locator('[data-testid="po-allocate-open"]').first().click();
  await this.page.locator('#allocateOrderItemsBody tr').first().click();
  if (directShip) {
    await this.page.locator('[data-testid="po-allocate-direct-ship"]').check();
  }
  await this.page.locator('[data-testid="po-allocate-submit"]').click();
  await this.page.waitForLoadState('networkidle');
}

async receiveFirstItem(quantity: number, warehouseName?: string) {
  await this.page.locator('[data-testid="po-receive-item-open"]').first().click();
  await this.page.locator('[data-testid="po-receive-quantity"]').fill(String(quantity));
  if (warehouseName) {
    await this.page.locator('[data-testid="po-receive-warehouse"]').selectOption({ label: warehouseName });
  }
  await this.page.locator('[data-testid="po-receive-submit"]').click();
  await this.page.waitForLoadState('networkidle');
}
```

- [ ] **Step 5: Delivery note page object**

Create `tests/pages/DeliveryNotePage.ts`:

```ts
import { Page, expect } from '@playwright/test';
import { createProofImage } from '../support/proof-image';

export class DeliveryNotePage {
  constructor(private readonly page: Page) {}

  async createFromCurrentOrder() {
    await this.page.locator('[data-testid="delivery-create-submit"]').click();
    await expect(this.page).toHaveURL(/\/DeliveryNote\/List|\/DeliveryNote\/Details/i);
  }

  async openLatestFromList() {
    await this.page.goto('/DeliveryNote/List');
    await this.page.locator('table tbody tr').first().locator('a[href*="/DeliveryNote/Details"]').click();
  }

  async confirmAndDeliver(receiverName: string) {
    await this.page.locator('[data-testid="delivery-confirm"]').click();
    await this.page.locator('[data-testid="delivery-mark-delivering"]').click();
    await this.page.locator('[data-testid="delivery-proof-file"]').setInputFiles(createProofImage());
    await this.page.locator('input[name="receiverName"], input[name="ReceiverName"]').fill(receiverName);
    await this.page.locator('[data-testid="delivery-mark-delivered"]').click();
    await expect(this.page.locator('.topbar-page-title, body')).toContainText(/Đã giao|Delivered/i);
  }
}
```

- [ ] **Step 6: Direct ship page object**

Create `tests/pages/DirectShipDeliveryPage.ts`:

```ts
import { Page, expect } from '@playwright/test';

export class DirectShipDeliveryPage {
  constructor(private readonly page: Page) {}

  async gotoPending() {
    await this.page.goto('/DirectShipDelivery/Pending');
  }

  async confirmFirstPending() {
    await this.page.locator('[data-testid="directship-confirm-open"]').first().click();
    await this.page.locator('[data-testid="directship-confirm-submit"]').click();
    await expect(this.page.locator('body')).not.toContainText('Danh sách phiếu giao thẳng chờ xác nhận');
  }
}
```

### Task 6: Implement First Two Order Workflow Specs

**Files:**
- Create: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/tests/specs/order-workflow.spec.ts`

- [ ] **Step 1: Add standard delivery workflow test**

Create `tests/specs/order-workflow.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { E2EApi } from '../support/e2e-api';
import { scenarioId } from '../support/test-data';
import { OrderPage } from '../pages/OrderPage';
import { OrderDetailsPage } from '../pages/OrderDetailsPage';
import { PurchaseOrderPage } from '../pages/PurchaseOrderPage';
import { PurchaseOrderDetailsPage } from '../pages/PurchaseOrderDetailsPage';
import { DeliveryNotePage } from '../pages/DeliveryNotePage';

test.describe.configure({ mode: 'serial' });

test('order-standard-full: order -> purchase order -> receive -> deliver -> complete', async ({ page, request }) => {
  const api = new E2EApi(request);
  const id = scenarioId('order-standard-full');
  const quantity = 7;

  await api.reset(id);
  const seed = await api.seedOrderWorkflow(id, quantity, false);

  const orderPage = new OrderPage(page);
  await orderPage.createSingleProductOrder(seed);

  const orderDetails = new OrderDetailsPage(page);
  await orderDetails.expectPending();

  const orderDetailsUrl = page.url();

  const poPage = new PurchaseOrderPage(page);
  const poDetails = new PurchaseOrderDetailsPage(page);
  await poPage.createSingleProductPurchaseOrder(seed);
  await poDetails.submitAndApprove();
  await poDetails.allocateFirstItemToOrder(false);
  await poDetails.receiveFirstItem(quantity, seed.warehouseName);

  await page.goto(orderDetailsUrl);
  await orderDetails.createDeliveryNoteFromOrder();

  const deliveryNote = new DeliveryNotePage(page);
  await deliveryNote.createFromCurrentOrder();
  await deliveryNote.openLatestFromList();
  await deliveryNote.confirmAndDeliver(seed.customerName);

  await page.goto(orderDetailsUrl);
  await orderDetails.completeOrder();

  const state = await api.getOrderWorkflowState(id);
  expect(state.orderStatus).toBe('Completed');
  expect(state.purchaseOrderStatus).toBe('Completed');
  expect(state.deliveryStatus).toBe('Delivered');
  expect(state.orderedQuantity).toBe(quantity);
  expect(state.receivedQuantity).toBe(quantity);
  expect(state.deliveredQuantity).toBe(quantity);
});
```

- [ ] **Step 2: Add direct-ship workflow test**

Append:

```ts
import { DirectShipDeliveryPage } from '../pages/DirectShipDeliveryPage';

test('order-direct-ship-full: order -> purchase order direct ship -> confirm delivered -> complete', async ({ page, request }) => {
  const api = new E2EApi(request);
  const id = scenarioId('order-direct-ship-full');
  const quantity = 5;

  await api.reset(id);
  const seed = await api.seedOrderWorkflow(id, quantity, true);

  const orderPage = new OrderPage(page);
  await orderPage.createSingleProductOrder(seed);

  const orderDetails = new OrderDetailsPage(page);
  const orderDetailsUrl = page.url();

  const poPage = new PurchaseOrderPage(page);
  const poDetails = new PurchaseOrderDetailsPage(page);
  await poPage.createSingleProductPurchaseOrder(seed);
  await poDetails.submitAndApprove();
  await poDetails.allocateFirstItemToOrder(true);
  await poDetails.receiveFirstItem(quantity);

  const directShipPage = new DirectShipDeliveryPage(page);
  await directShipPage.gotoPending();
  await directShipPage.confirmFirstPending();

  await page.goto(orderDetailsUrl);
  await orderDetails.completeOrder();

  const state = await api.getOrderWorkflowState(id);
  expect(state.orderStatus).toBe('Completed');
  expect(state.purchaseOrderStatus).toBe('Completed');
  expect(state.deliveryStatus).toBe('Delivered');
  expect(state.orderedQuantity).toBe(quantity);
  expect(state.receivedQuantity).toBe(quantity);
  expect(state.deliveredQuantity).toBe(quantity);
});
```

### Task 7: Verification and Operating Guide

**Files:**
- Modify: `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests/README.md`

- [ ] **Step 1: Build web app**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

- [ ] **Step 2: Start app in E2E environment**

Tuấn must create/update the E2E database first. Do not run migration commands from Codex.

Run app:

```powershell
$env:ASPNETCORE_ENVIRONMENT='E2E'
$env:E2E__Enabled='true'
$env:E2E__Token='local-e2e-token'
rtk dotnet run --project NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj --urls http://localhost:5132
```

- [ ] **Step 3: Run order workflow E2E**

From `NamEcommerce/Tests/NamEcommerce.Web.E2E.Tests`:

```powershell
$env:E2E_TOKEN='local-e2e-token'
$env:E2E_WORKFLOW='true'
rtk npm run test:e2e -- tests/specs/order-workflow.spec.ts --project=workflow-chromium --workers=1
```

Expected:

- `order-standard-full` passes.
- `order-direct-ship-full` passes.
- Playwright report has trace/video only for failures.

- [ ] **Step 4: Document normal commands**

Update README with:

```markdown
## Workflow E2E

Workflow tests require the web app to run with `ASPNETCORE_ENVIRONMENT=E2E` against an E2E database. Codex must not run EF migrations; prepare the E2E database manually before running tests.

```powershell
$env:E2E_TOKEN='local-e2e-token'
$env:E2E_WORKFLOW='true'
rtk npm run test:e2e -- tests/specs/order-workflow.spec.ts --project=workflow-chromium --workers=1
```
```

### Task 8: Expand Workflow Coverage After the First Two Tests Pass

**Files:**
- Modify: `tests/specs/order-workflow.spec.ts`
- Modify page objects as needed.

- [ ] Add `order-standard-partial-receive`.
- [ ] Add `order-standard-partial-delivery`.
- [ ] Add `order-oversupply-accept`.
- [ ] Add `order-oversupply-reject`.
- [ ] Add `order-direct-ship-reject`.
- [ ] Add `order-cancel-with-direct-ship-transit`.

Each new test follows the same shape:

```ts
await api.reset(id);
const seed = await api.seedOrderWorkflow(id, quantity, directShip);
// Execute only real UI workflow actions.
const state = await api.getOrderWorkflowState(id);
expect(state.orderStatus).toBe(expectedOrderStatus);
```

---

## Self-Review

- Spec coverage: Covers login bypass, fresh data, standard order workflow, direct-ship order workflow, and a path for future similar test cases.
- Safety: Reset/seed is gated by environment, config flag, token, and E2E database name fragment.
- Project rules: No unit tests, no xUnit `*.Test` changes, no migrations.
- Risk: The exact UI selectors for product/customer/vendor pickers need one browser pass during implementation. If picker markup differs, adjust page objects while keeping `data-testid` attributes stable.
