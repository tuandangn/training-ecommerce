# Casso Bank Transfer Verification Design

## Goal

Phase 3 integrates Casso as the real bank-transfer verification source for Fast Sale VietQR payments.

The system will confirm bank-transfer payment intents through two paths:

- Casso webhook events when new bank transactions arrive.
- Casso transaction reconciliation through both a scheduled job and a manual trigger.

Phase 2 already owns the normalized payment confirmation boundary:

- `ProcessProviderTransactionAsync(...)`
- `BankTransferVerificationLog`
- `BankTransferPaymentIntent`
- `api/bank-transfer/webhook`
- Fast Sale UI polling

Phase 3 must not redesign the sale flow. It adds a Casso-specific adapter that maps Casso transactions into the existing normalized provider transaction processing.

## References

- Casso webhook setup: https://developer.casso.vn/english-v2-new/webhook/thiet-lap-webhook-thu-cong
- Casso transactions API: https://developer.casso.vn/casso-api/api/lay-giao-dich

Relevant Casso behaviors from the docs:

- Casso sends webhook notifications for deposit or withdrawal transactions on linked bank accounts.
- The webhook security key is attached to an HTTP header configured in Casso.
- Casso webhook URLs must be public HTTPS endpoints.
- Casso can retry failed webhook delivery up to 17 times.
- Casso recommends anti-duplicate handling by transaction `id`.
- Casso webhook payload stores new transactions in a `data` array, even when there is one transaction.
- The transactions API supports fetching transaction records and requires `Authorization: Apikey <key>` or OAuth bearer token.

## Scope

### In Scope

- Add a Casso webhook endpoint that receives the real Casso webhook payload.
- Validate the configured Casso security header before processing.
- Map Casso transactions to the existing normalized provider transaction DTO.
- Process only incoming transfers that can represent customer payments.
- Add a Casso transaction API client for reconciliation.
- Add a scheduled reconciliation worker with a conservative interval.
- Add a manual reconciliation endpoint or command for admin/accounting operators.
- Store reconciliation run metadata so operators can see what was processed.
- Keep idempotency by using Casso transaction `id` as the provider transaction id.

### Out Of Scope

- Changing the Fast Sale order/accounting flow.
- Changing VietQR QR generation.
- Supporting virtual account allocation as a separate feature.
- Integrating payOS.
- Building a full accounting reconciliation dashboard.
- OAuth token flow for Casso. Phase 3 uses API key configuration first.

## Configuration

Add a dedicated Casso config section:

```json
"Payments": {
  "BankTransfer": {
    "Verification": {
      "Provider": "Casso"
    },
    "Casso": {
      "Enabled": true,
      "ApiBaseUrl": "https://oauth.casso.vn",
      "ApiKey": "",
      "WebhookEnabled": true,
      "WebhookSecurityHeaderName": "X-NamEcommerce-Casso-Token",
      "WebhookSecurityKey": "",
      "ReconciliationEnabled": true,
      "ReconciliationIntervalMinutes": 15,
      "ReconciliationLookbackMinutes": 180,
      "ReconciliationPageSize": 50
    }
  }
}
```

`ApiKey` and `WebhookSecurityKey` must be empty in committed config files. Production values must be supplied by environment or deployment secrets.

The system should accept a configurable header name because the Casso dashboard lets the webhook security key be attached through HTTP headers. The default should not reuse the Phase 2 normalized webhook header to keep Casso-specific and internal normalized routes separate.

## Public Endpoints

### Casso Webhook

Add:

```http
POST /api/casso/webhook
```

This endpoint accepts Casso's real webhook body. It should not expose the normalized `ProcessBankTransferProviderTransactionCommand` shape directly.

Processing rules:

- If Casso integration is disabled, return `404`.
- If webhook is enabled but the configured security key is empty, return `503`.
- If the configured security header is missing or does not exactly match the configured key, return `401`.
- If the payload is malformed, return `400`.
- If valid, process every transaction in `data`.
- Return `200` with `success = true` when the webhook was accepted and each valid transaction was either processed, ignored, or recorded as failed in the response summary.

For Casso strict mode compatibility, successful processing should return JSON with `success: true`.

### Manual Reconciliation

Add an authorized internal endpoint or command handler:

```http
POST /api/casso/reconciliation/run
```

Input:

```json
{
  "fromDate": "2026-06-05",
  "toDate": "2026-06-05"
}
```

Rules:

- Only authorized users can run it.
- If dates are omitted, use configured lookback window.
- It should page through Casso transactions using `pageSize`.
- It should process transactions through the same mapper and normalized app service as webhook processing.
- It should return run summary: total records, processed, matched, duplicate, rejected, ignored, failed.

## Casso Payload Mapping

Casso v2 webhook payload:

```json
{
  "error": 0,
  "data": [
    {
      "id": 6785,
      "tid": "BANK_REF_ID",
      "description": "QS260605000001",
      "amount": 79000,
      "when": "2020-10-14 00:34:57",
      "bank_sub_acc_id": "123456789",
      "subAccId": "123456789",
      "bankName": "VPBank",
      "bankAbbreviation": "VPB"
    }
  ]
}
```

Casso transactions API returns records with similar fields, using some camelCase names such as `bankSubAccId` and `bankCodeName`.

Map to normalized provider transaction:

| Normalized Field | Casso Source |
|---|---|
| `ReferenceCode` | Extract from `description` |
| `Amount` | `amount` |
| `BankId` | Prefer `bankAbbreviation`, fallback configured bank id |
| `AccountNo` | `bank_sub_acc_id`, `subAccId`, or `bankSubAccId` |
| `ProviderTransactionId` | `id` as string |
| `Source` | `BankWebhook` for webhook, `BankStatement` for API reconciliation |
| `RawPayload` | Original transaction JSON |
| `ConfirmedAtUtc` | Parsed `when`, fallback current UTC |

## Reference Extraction

The existing intent reference code is alphanumeric and starts with the configured transfer content prefix, currently `QS`.

Reference extraction should:

- Normalize `description` by uppercasing and keeping only ASCII letters and digits as token characters.
- Search for candidate tokens that start with configured `TransferContentPrefix`.
- Prefer a candidate with max length 25 because existing reference codes are capped at 25.
- Reject the transaction as ignored or rejected if no reference candidate is found.

Do not scan the database to guess by amount alone. Amount-only matching is unsafe and can confirm the wrong customer's sale.

## Transaction Eligibility

Process only transactions that can be customer incoming payments:

- `amount > 0`
- account number exists
- provider transaction id exists
- reference code can be extracted

Withdrawals, missing account numbers, missing references, and zero/negative amounts should be ignored by the Casso adapter and counted in summaries. They should not call the normalized provider processing boundary because they are not payment candidates.

If the transaction looks like a payment candidate but fails intent matching, the normalized boundary records the verification log as rejected.

## Idempotency

Casso transaction `id` is the primary idempotency key.

Phase 2 already rejects duplicate provider transaction ids in `BankTransferPaymentIntentManager.ConfirmFromProviderAsync(...)`. Phase 3 should also use `id` for reconciliation run dedupe and summaries.

Expected duplicate behavior:

- If Casso retries the same webhook, the duplicate should not confirm another intent.
- The webhook endpoint should still return a successful response if the duplicate was handled as a known duplicate.
- The verification log should mark duplicate where the normalized processing boundary detects duplicate transaction id.

## Reconciliation Worker

Add a hosted service that runs when:

- `Payments:BankTransfer:Casso:Enabled = true`
- `Payments:BankTransfer:Casso:ReconciliationEnabled = true`
- `Payments:BankTransfer:Verification:Provider = "Casso"`

Default interval: 15 minutes.

Each run:

1. Compute `fromDate/toDate` from `ReconciliationLookbackMinutes`.
2. Call Casso transactions API with pagination.
3. Map each record to the same internal transaction model used by webhook handling.
4. Process eligible records through `ProcessProviderTransactionAsync(...)`.
5. Save a reconciliation run summary.

Concurrency:

- Only one reconciliation run can execute at a time in the current process.
- A simple in-process lock is acceptable for v1.
- If the app is deployed with multiple instances, the design needs a database lock later. That is out of scope for Phase 3 unless the current deployment is already multi-instance.

## Run Metadata

Add a small entity such as `CassoReconciliationRun`:

- `Id`
- `StartedAtUtc`
- `FinishedAtUtc`
- `FromDate`
- `ToDate`
- `Trigger`: `Scheduled` or `Manual`
- `TotalRecords`
- `Processed`
- `Matched`
- `Duplicate`
- `Rejected`
- `Ignored`
- `Failed`
- `ErrorMessage`

This is run-level operational metadata, not accounting data.

Do not store full Casso API payloads here; raw transaction payloads belong in `BankTransferVerificationLog.RawPayload` for payment candidates.

## Error Handling

Webhook endpoint:

- Auth/config errors return `401`, `404`, or `503` before parsing business data.
- Malformed payload returns `400`.
- Per-transaction processing errors should be captured in the response summary.
- Return `200` with `success = true` when the endpoint accepted the Casso webhook and all per-transaction failures were recorded, so Casso does not retry forever for already-recorded business mismatches.
- Return `500` only for infrastructure failures that prevent recording the webhook outcome.

Reconciliation:

- API authentication or connectivity failure marks the run as failed.
- A failed run must not change existing confirmed intents unless individual transaction processing already succeeded before the failure.
- A later manual run can replay the date range.

## Security

- Never commit API keys or webhook security keys.
- Compare webhook security key exactly.
- Keep Casso webhook route separate from the normalized Phase 2 route.
- The manual reconciliation endpoint must require normal app authorization.
- Log raw payload only for payment candidates and keep existing `RawPayload` length limits in mind.

## Testing Strategy

Unit tests:

- Casso transaction mapper extracts reference code from common descriptions.
- Mapper ignores withdrawals and missing references.
- Mapper supports `bank_sub_acc_id`, `subAccId`, and `bankSubAccId`.
- Duplicate provider transaction id is surfaced as duplicate.

Application tests:

- Casso webhook payload with one matching transaction confirms the intent.
- Casso webhook retry for the same `id` does not confirm a second intent.
- Manual reconciliation processes records and writes a run summary.
- Scheduled reconciliation skips execution when disabled.

Manual smoke tests:

- Configure webhook security key and post a Casso sample payload.
- Verify Fast Sale UI polling moves from pending to confirmed.
- Run manual reconciliation for a known date range.
- Verify wrong amount/reference does not confirm the intent and is visible in verification logs.

## Rollout

1. Deploy Phase 3 with Casso integration disabled.
2. Configure Casso API key and webhook security key through secrets.
3. Enable Casso webhook only.
4. Use Casso test call or a safe test transaction.
5. Enable scheduled reconciliation after webhook behavior is verified.
6. Keep manual reconciliation available for operational recovery.

## Open Assumption

This design assumes the deployment is a single application instance. If production runs multiple web instances, scheduled reconciliation should use a database-backed distributed lock before enabling the worker.

