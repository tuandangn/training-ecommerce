# Fast Sale Reuse Picker Components Plan

## Goal
Improve `Order/FastCreate` by reusing the existing `CustomerPicker` and `ProductBrowser` components instead of maintaining separate quick-sale search controls.

## Todo
- [x] Replace the fast-sale customer select with `CustomerPicker`.
- [x] Add quick customer creation from the picker plus button.
- [x] Replace the custom fast-sale product search/list with `ProductBrowser`.
- [x] Keep not-delivered sales able to add out-of-stock products.
- [x] Keep delivered sales constrained to products with available stock.
- [x] Verify JavaScript syntax and web build.

## Notes
- `CustomerPicker` searches through `/Customer/Search` and quick-created customers use the existing `Customer/QuickCreate` endpoint.
- `ProductBrowser` uses `/Product/Search`; that response now includes available warehouse quantities so the fast-sale cart can keep per-line warehouse selection.
- The previous fast-sale-only product search endpoint is removed to keep one product browsing path.
