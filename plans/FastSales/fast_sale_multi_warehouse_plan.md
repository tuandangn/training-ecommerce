# Fast Sale Multi Warehouse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow `Order/FastCreate` to sell items from multiple warehouses in one quick sale.

**Architecture:** Keep one sales order and one delivery note for fast sale. Store warehouse selection on each quick-sale line; delivery note item warehouse drives reservation/dispatch. When fulfillment mode is `NotDelivered`, do not require or persist line warehouse choices for sale creation.

**Tech Stack:** ASP.NET Core MVC/Razor, MediatR commands, application service DTOs, existing DDD managers, vanilla JS module.

---

## Tasks
- [x] Add `WarehouseId` to quick sale item DTOs and commands.
- [x] Validate stock by `(ProductId, WarehouseId)` only when fulfillment mode is `DeliverNow`.
- [x] Build delivery note items using each line's warehouse and use the first line warehouse as delivery note header fallback.
- [x] Return per-warehouse stock data from product search.
- [x] Update FastCreate cart UI to select warehouse per line only when delivering now.
- [x] Run `dotnet build` for the web project and `node --check` for `FastSale.js`.
