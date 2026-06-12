# UI/UX Design System Overhaul Implementation

## Current Checkpoint: Phase 2

### Phase 0 TodoList
- [x] Add Bootstrap-first `theme.css` and load it after Bootstrap.
- [x] Self-host Inter font in `wwwroot/fonts/inter/`.
- [x] Move loading mask CSS out of `_Styles.cshtml` into `loading.css`.
- [x] Split shared responsive CSS into `wwwroot/css/responsive/` by Bootstrap breakpoint boundary.
- [x] Rewrite `DESIGN.md` as Bootstrap-first guidance.
- [x] Capture baseline screenshots for FastSale, Order list/details, CustomerDebt, DeliveryNote details, Inventory list.

### Phase 1 TodoList
- [x] Add shared component models.
- [x] Add shared component partials.
- [x] Add `components.css`.
- [x] Add Development-only `/design`.
- [x] Update `DESIGN.md` with component names and rules.

### Phase 4 TodoList
- [x] Add Playwright screenshot script.
- [x] Add `tools/ui-lint.ps1`.
- [x] Add `tools/ui-lint-baseline.json`.
- [x] Wire `ui-lint` into build/CI.
- [x] Update agent workflow docs after screenshot script exists.

### Phase 2 TodoList
- [x] Slice 1: migrate `Order/List.cshtml` CSS/action hygiene.
- [x] Slice 2: migrate `Order/QuickCreate.cshtml` CSS/action hygiene.
- [x] Slice 3: migrate `Order/Create.cshtml` CSS/action hygiene.
- [x] Slice 4: migrate `Order/Details.cshtml` workflow/offcanvas CSS/action hygiene.
- [x] Slice 5: extract remaining Razor `<style>` blocks to CSS files and reduce inline styles below target.
- [x] Slice 6: replace remaining `btn-light` / `btn-link` action variants with outline button variants.
- [x] Complete batch 1: FastSale + Order list/details/create.
- [x] Slice 7: migrate CustomerDebt/VendorDebt list/details CSS/action hygiene and fix VendorDebt mobile grid.
- [x] Complete batch 2: CustomerDebt + VendorDebt list/details.
- [x] Slice 8: migrate DeliveryNote/GoodsReceipt CSS/action hygiene and remove remaining batch inline styles.
- [x] Complete batch 3: DeliveryNote + GoodsReceipt list/details/create.
- [x] Slice 9: migrate Inventory/PurchaseOrder/Returns CSS/action hygiene and progress/modal cleanup.
- [x] Complete batch 4: Inventory + PurchaseOrder + Returns.
- [x] Slice 10: remove final inline styles and legacy modal/action shells from remaining pages.
- [x] Complete batch 5: remaining Accounting/Expense/Home/Preparation/Delivery progress cleanup.

## Files Changed

- `DESIGN.md`: converted Tailwind-oriented guidance to Bootstrap-first rules.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/_Styles.cshtml`: removed Google Font preconnects and inline loader `<style>`, added theme/responsive/loading CSS load order.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/theme.css`: added Bootstrap token map, Inter `@font-face`, and shared Bootstrap component overrides.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/loading.css`: added shared loading mask styles.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/responsive/sm.css`: moved shared rules around the Bootstrap `sm` boundary.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/responsive/md.css`: moved shared rules around the Bootstrap `md` boundary.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/responsive/lg.css`: reserved breakpoint file for shared `lg` rules.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/responsive/xl.css`: reserved breakpoint file for shared `xl` rules.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/responsive/xxl.css`: reserved breakpoint file for shared `xxl` rules.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/fonts/inter/InterVariable.woff2`: self-hosted Inter font.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/fonts/inter/LICENSE.txt`: Inter font license.
- `NamEcommerce/Presentation/NamEcommerce.Web/Models/DesignSystem/DesignSystemModels.cs`: typed models for shared design-system partials.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_PageHeader.cshtml`: shared page header.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_FilterToolbar.cshtml`: shared filter toolbar.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_DataTable.cshtml`: shared data table wrapper.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_StatusBadge.cshtml`: shared status badge.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_FormSection.cshtml`: shared form section.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_FormRow.cshtml`: shared form row.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_EmptyState.cshtml`: shared empty state.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_ConfirmModal.cshtml`: shared confirm modal.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_MoneyDisplay.cshtml`: shared money display.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/_QuantityDisplay.cshtml`: shared quantity display.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/components.css`: shared component styling.
- `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DesignController.cs`: Development-only `/design` controller.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Design/Index.cshtml`: design-system reference page.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/site.css`: removed Google Font import and moved shared responsive rules to breakpoint files.
- `tools/ui-lint.ps1`: checks that Razor `<style>` and inline `style=""` counts do not increase.
- `tools/ui-lint-baseline.json`: current baseline for UI lint.
- `tools/ui-screenshot.ps1`: starts the web app with seed/background workers disabled and captures screenshots through Playwright CLI.
- `.github/workflows/dotnet.yml`: runs `tools/ui-lint.ps1` with `pwsh` before build/test.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/List.cshtml`: moved order item popover CSS out of Razor, removed inline progress/status width styles, and aligned list actions with outline/primary button rules.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/QuickCreate.cshtml`: moved FastSale CSS out of Razor and replaced `btn-light` actions with outline variants.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Create.cshtml`: loads Order page CSS, removes inline row/thumb/empty-state/price-history styles, and replaces `btn-light`/`btn-link` secondary actions with outline variants.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml` and Order workflow/offcanvas partials: load Order page CSS, remove inline thumbnail/column/progress styles, and replace `btn-light` secondary actions with outline variants.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/OrderController.js`: renders Create order rows and price-history rows with CSS classes instead of inline style mutations.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/order.css`: added Order page CSS for item popovers, status/quantity column widths, native progress display, Create/Details order item rows, thumbnails, and price-history rows.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/debt.css`: added shared CustomerDebt/VendorDebt summary, ledger, modal, and mobile action styles.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/CustomerDebt/List.cshtml`: loads Debt page CSS for the debt list surface.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/CustomerDebt/Details.cshtml`: uses shared Debt summary/ledger/modal classes and keeps submit behavior unchanged.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/VendorDebt/Index.cshtml`: adds the missing responsive grid panel so mobile default grid view renders cards instead of a blank content area.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/VendorDebt/Details.cshtml`: uses shared Debt summary/ledger/modal classes and changes the page-level money-out action to `btn-outline-danger`.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/List.cshtml`: changes delivery status actions to outline button variants.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/Details.cshtml`: uses DeliveryNote page CSS for the financial summary and modal shell, and keeps visible destructive/success actions in outline variants.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/deliverynote-details.css`: adds DeliveryNote details summary and modal shell styles.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/GoodsReceipt/Create.cshtml`: uses GoodsReceipt create page CSS, removes the inline empty-state display style, and changes the add-item confirm action to `btn-primary`.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/GoodsReceipt/Details.cshtml`: uses GoodsReceipt modal classes, replaces inline score/status sizing with shared utilities, and avoids Razor-unsafe JavaScript bracket/template parsing in the quick-create PO form payload.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/goodsreceipt-create.css`: adds GoodsReceipt create modal shell styles.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/goodsreceipt-details.css`: adds GoodsReceipt details modal shell styling.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/GoodsReceiptCreateController.js`: toggles the empty-state visibility with Bootstrap `d-none` instead of mutating inline display style.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Inventory/StockList.cshtml`: removes inline progress widths and uses the Inventory modal shell class.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/List.cshtml`, `Create.cshtml`, `Details.cshtml`, `ShortageAggregation.cshtml`, and workflow partials: remove inline progress/modal/action styling and keep Razor-safe status option rendering.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/CustomerReturn/Create.cshtml` and `Details.cshtml`: remove inline empty-state/modal styling and keep details option lists Razor-safe.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/VendorReturn/Create.cshtml` and `Details.cshtml`: remove inline empty-state/modal styling and keep details option lists Razor-safe.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/StockAdjustment/Create.cshtml` and `Views/StockTransfer/Create.cshtml`: switch empty-state toggles and modal shells to shared classes.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/js/site.js`: applies `data-progress-width` values to Bootstrap progress bars.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/CreatePurchaseOrderController.js`: renders rows, thumbnails, empty states, and price-history rows with CSS classes instead of inline style mutations.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/purchaseorder-create.css`, `purchaseorder-details.css`, `customerreturn-details.css`, `vendorreturn-create.css`, `vendorreturn-details.css`, and `inventory-stocklist.css`: add page modal shell styles.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/components.css`: adds the shared `ui-modal-content` utility.
- `tools/ui-lint-baseline.json`: lowers the inline-style baseline after Batch 4 cleanup.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Accounting/CashFlow.cshtml` and `IncomeStatement.cshtml`: replace conditional inline report widths with max-width utilities.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/Run.cshtml`, `Views/DeliveryRun/Details.cshtml`, `Views/Expense/Budgets.cshtml`, `Views/Expense/List.cshtml`, and `Views/Home/Index.cshtml`: replace final inline progress widths with `data-progress-width`.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Preparation/List.cshtml`: replaces the final inline gradient and legacy modal shells with CSS classes.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Accounting/BankAccounts.cshtml`, Order modal views/partials, `Views/Product/Create.cshtml`, and shared modal partials: replace remaining legacy modal shell class chains with `ui-modal-content`.
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/GoodsReceipt/Create.cshtml`: replaces the last `bg-light text-primary` action with `btn-outline-primary`.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/components.css`: adds `ui-max-w-700`, `ui-max-w-900`, and `preparation-payment-summary-card`.
- `tools/ui-lint-baseline.json`: lowers the inline-style baseline after Batch 5 cleanup.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/pages/*.css`: extracted remaining page-level Razor `<style>` blocks.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/print/*.css`: extracted print/receipt page styles from Razor.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/delivery-mobile.css`: extracted delivery mobile layout styles from Razor layout.
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/components.css`: added shared picker styles and small reusable UI utilities for fixed widths, thumbnails, font sizes, and action cleanup.
- `AGENTS.md`: adds UI workflow guardrails for shared components, CSS ownership, lint, and screenshots.
- `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`: adds default-on `SeedData:RunOnStartup` and `HostedServices:RunOnStartup` flags so UI verification can run without startup seed/background DB work.
- `plans/UI/plan_ui_design_system_overhaul.md`: updated and checked off completed Phase 0/1 items.

## Verification

- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`: passed with 0 errors, 0 warnings.
- `/design` HTTP smoke with `SeedData__RunOnStartup=false` and `HostedServices__RunOnStartup=false`: returned `200 18900`.
- Browser smoke screenshots captured under `artifacts/ui-screenshots/`: `design-system.png`, `login.png`, `order-list.png`, `order-list-mobile.png`, `order-details.png`, `order-create.png`, `quick-create.png`, `inventory-stock-list.png`, `customer-debt-list.png`, `customer-debt-details.png`, `delivery-note-list.png`, `delivery-note-details.png`.
- Browser smoke pages passed with console warnings/errors `0`: `/design`, `/User/Login`, `/Order/List`, `/Order/Details/f2773f73-9118-46f4-86df-ec5a21141bdf`, `/Order/Create`, `/Order/QuickCreate`, `/Inventory/StockList`, `/CustomerDebt/List`, `/CustomerDebt/Details/052e18e0-87a5-4034-a746-79a024a23de1`, `/DeliveryNote/List`, `/DeliveryNote/Details/e0d68be5-8886-40f1-9f28-cb1f780777ce`.
- Mobile smoke for `/Order/List` at narrow viewport: no document-level horizontal overflow, responsive card list rendered, console warnings/errors `0`.
- Reproduced VendorDebt mobile issue before fix: `/VendorDebt/Index` at narrow viewport had `activeView=grid`, `panelCount=1`, `visiblePanels=[]`, so the list content was hidden.
- Browser smoke after VendorDebt fix: `/VendorDebt/Index` at narrow viewport has `activeView=grid`, `panelCount=2`, `visiblePanels=["grid"]`, `cardCount=1`, no horizontal overflow, console warnings/errors `0`; screenshots `vendor-debt-mobile-before.png` and `vendor-debt-mobile-after.png` captured under `artifacts/ui-screenshots/`.
- Browser smoke for Debt details after shared CSS: `/CustomerDebt/Details/052e18e0-87a5-4034-a746-79a024a23de1` and `/VendorDebt/Details?vendorId=19de6689-545c-4a42-9814-fa5001452565` each render 3 `.debt-summary-card`, 1 `.debt-ledger-card`, and 1 `.debt-modal-content`, with console warnings/errors `0`.
- Modal smoke after shared CSS: CustomerDebt `Thu tiền` and VendorDebt `Chi tiền NCC` modals open without submitting; screenshot `vendor-debt-payment-modal.png` captured under `artifacts/ui-screenshots/`.
- Browser smoke after Batch 3: `/DeliveryNote/List`, `/DeliveryNote/Details/e0d68be5-8886-40f1-9f28-cb1f780777ce`, `/GoodsReceipt/List`, `/GoodsReceipt/Details/c80743ec-5769-4359-ab1a-eb8a710b03e7`, and `/GoodsReceipt/Create` render with console warnings/errors `0` and no document-level horizontal overflow.
- GoodsReceipt Details runtime regression fixed by restarting the worktree dev server after Razor-safe JavaScript cleanup; fresh render title is `Phiếu nhập - 12/06/2026 - VLXD Tuấn Khôi`.
- Mobile smoke after Batch 3: `/GoodsReceipt/List` at narrow viewport renders card view with visible tables `0`, visible cards `7`, and no horizontal overflow.
- Batch 3 screenshots captured under `artifacts/ui-screenshots/`: `delivery-note-list-batch3.png`, `delivery-note-details-batch3.png`, `delivery-note-details-batch3-after-restart.png`, `goodsreceipt-list-batch3.png`, `goodsreceipt-details-batch3.png`, `goodsreceipt-create-batch3.png`, `goodsreceipt-list-mobile-batch3.png`.
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1 -UpdateBaseline`: lowered baseline from `0/22` to `0/18` after DeliveryNote/GoodsReceipt inline-style cleanup.
- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`: passed with 0 errors, 0 warnings after Batch 3 Razor/CSS changes.
- Browser smoke after Batch 4: `/PurchaseOrder/Create`, `/PurchaseOrder/List`, `/PurchaseOrder/Details/12f1a3ad-7d71-4528-bdb7-c5e746be90bc`, `/Inventory/StockList`, `/CustomerReturn/Create`, `/CustomerReturn/Details/a40d727b-b7ac-4a47-b287-a64792bf0658`, `/VendorReturn/Create`, `/StockAdjustment/Create`, `/StockTransfer/Create`, and `/PurchaseOrder/ShortageAggregation` render with console warnings/errors `0` and no document-level horizontal overflow.
- Mobile smoke after Batch 4: `/PurchaseOrder/List` at narrow viewport renders card view with visible tables `0`, visible cards `2`, progress bars applied, and no horizontal overflow.
- Batch 4 screenshots captured under `artifacts/ui-screenshots/`: `purchaseorder-create-batch4.png`, `purchaseorder-list-batch4.png`, `purchaseorder-details-batch4.png`, `inventory-stocklist-batch4.png`, `customerreturn-create-batch4.png`, `customerreturn-details-batch4.png`, `vendorreturn-create-batch4.png`, `stockadjustment-create-batch4.png`, `stocktransfer-create-batch4.png`, `purchaseorder-shortageaggregation-batch4.png`, and `purchaseorder-list-mobile-batch4.png`.
- VendorReturn Details was not browser-smoked in Batch 4 because the current seed data had no detail link in `/VendorReturn/List`; Razor compile is covered by build verification.
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1 -UpdateBaseline`: lowered baseline from `0/18` to `0/8` after Inventory/PurchaseOrder/Returns inline-style cleanup.
- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj -v:minimal`: passed with 0 errors, 0 warnings after Batch 4 Razor/CSS changes.
- Browser smoke after Batch 5: `/Accounting/CashFlow`, `/Accounting/IncomeStatement`, `/Expense/Budgets`, `/Expense/List`, `/Home/Index`, and `/Preparation/List` render after local dev login with console warnings/errors `0`, old modal shell count `0`, and no document-level horizontal overflow.
- Batch 5 screenshots captured under `artifacts/ui-screenshots/`: `accounting-cashflow-batch5.png`, `accounting-incomestatement-batch5.png`, `expense-budgets-batch5.png`, `expense-list-batch5.png`, `home-index-batch5.png`, and `preparation-list-batch5.png`.
- DeliveryRun Details and DeliveryMobile Run had no current seed detail/run link to browser-smoke; Razor compile is covered by build verification.
- Static sweep after Batch 5: no Razor `<style>`, inline `style=""`, `btn-light`, `btn-link`, `bg-light text-primary`, or legacy modal shell class chains remain under `Views/**/*.cshtml`.
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1 -UpdateBaseline`: lowered baseline from `0/8` to `0/0` after final inline-style cleanup.
- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj -v:minimal`: passed with 0 errors, 0 warnings after Batch 5 Razor/CSS changes.
- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`: passed with 0 errors, 0 warnings after Batch 2 Razor/CSS changes.
- Final `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1`: passed with `styleBlockCount=0/0 inlineStyleCount=22/22`.
- Browser/Playwright rendered smoke for `http://localhost:5148/design`: title `Design System - VLXD Tuấn Khôi`, `h1=Design System`, `.ui-page-header=1`, `.ui-data-table=2`, `.ui-form-section=1`, modal trigger count `1`, console warnings/errors `0`.
- Playwright MCP interaction proof: clicked `Mở xác nhận`; active dialog `Xác nhận huỷ đơn` was visible; viewport screenshot captured as `ui-design-system-modal.png` in the MCP output workspace.
- Static check: no `fonts.googleapis`, `fonts.gstatic`, or `@import url` remains in `_Styles.cshtml` or top-level CSS files.
- Static check: `_Styles.cshtml` no longer contains an inline `<style>` block.
- Static check: new Razor views/partials do not add `<style>` blocks or inline `style=""`.
- Static check: all Razor views now have `styleBlockCount=0`, `inlineStyleCount=22`, `btn-light=0`, and `btn-link=0`.
- Static check: touched `OrderController.js` paths no longer use the removed inline row/thumb/price-history styles.
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1`: passed with `styleBlockCount=0/0 inlineStyleCount=22/22`.
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1 -UpdateBaseline`: lowered baseline from `29/317` to `0/22` after CSS extraction, inline-style cleanup, and action button cleanup.
- `rtk dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`: passed with 0 errors, 0 warnings after the broad Razor/CSS migration.
- `.github/workflows/dotnet.yml` static check: `UI lint` step uses `shell: pwsh` and runs `./tools/ui-lint.ps1`.

## Screenshot Notes

The repo does not currently include Playwright CLI (`playwright.cmd` was not found). `tools/ui-screenshot.ps1` fails fast with the install command instead of hanging in raw Chrome/Edge headless mode. Visual QA for this checkpoint was completed through the in-app browser/DevTools fallback and screenshots were saved locally under `artifacts/ui-screenshots/`.
