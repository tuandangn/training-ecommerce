# Customer Portal Design

Date: 2026-05-20

## Goal

Build a separate customer-facing portal where a customer can scan a QR code on a delivery note, view delivery details, verify with OTP, then manage their own orders, delivery notes, return requests, debts, and online payment flow.

The portal must be deliberately defensive against abuse. Anonymous access is limited, authentication is session-only, OTP requests are rate-limited, and admin approval remains required for customer-created orders, return requests, and mock payment reconciliation.

## Current Context

The system is a .NET 10 NamEcommerce solution using Clean Architecture and DDD:

- Domain entities and managers live under `Domain`.
- Application contracts and services live under `Application`.
- SQL Server EF Core infrastructure lives under `Infrastructure`.
- The current internal/admin MVC web app lives under `Presentation/NamEcommerce.Web`.
- Existing public REST API project `Presentation/NamEcommerce.Api.Restful` is currently only a thin sample host.

Relevant existing modules:

- `Customer` already stores full name, phone, email, address, and note.
- `Order` is linked to `CustomerId` and already has order items, status, totals, and completion semantics.
- `DeliveryNote` is linked to `OrderId` and `CustomerId`, has delivery status, direct-ship fields, and `DeliveryConfirmationStatus`.
- `CustomerDebt` and `CustomerPayment` already model customer debt and payments.
- `CustomerReturn` already exists for the internal/admin return workflow.
- `ISecurityService` already hashes and verifies passwords with BCrypt.

Project rules:

- Do not add or edit unit tests in any `*.Test` project.
- Do not run migration commands or update database commands. Tuấn will run migrations.
- Before UI work, follow `DESIGN.md`.

## Approved Decisions

- Use a separate customer API and a separate React frontend.
- Do not reuse `NamEcommerce.Web.Contracts` or `NamEcommerce.Web.Framework` directly for the customer portal.
- Reuse lower layers: Application contracts/services, Domain services/entities, EF Core infrastructure, and shared security utilities.
- Use React + TypeScript + Vite for the frontend.
- Use a client-only React SPA, not Next.js or React Server Components.
- Authenticate with SMS OTP as the primary channel and email as fallback.
- Use mock SMS, mock email, and mock payment providers in the first phase.
- Customer sessions are strict session-only. Closing the browser/session requires authentication again.
- QR anonymous access only shows the related delivery note/order essentials.
- After authentication, authorization scope is based on the `CustomerId` from the delivery note, not by matching phone or email across customers.
- New orders from customers are requests that require admin approval before becoming real orders.
- Return requests from customers require admin review before becoming real customer returns.
- Mock payment success creates a pending reconciliation record. Admin approval is required before applying the payment to debt.

## Architecture

Add three presentation-level projects:

- `Presentation/Customer/NamEcommerce.Customer.Contracts`
  - Customer API request/response models and MediatR command/query contracts.
  - Contains only customer-safe contracts.
  - Does not depend on Application or Domain projects.

- `Presentation/Customer/NamEcommerce.Customer.Framework`
  - Customer-specific MediatR handlers and mapping logic.
  - Calls Application services or new customer application services.
  - Enforces customer-safe model shaping.

- `Presentation/Customer/NamEcommerce.Customer.Api`
  - ASP.NET Core REST API for the React customer portal.
  - Registers Customer Framework handlers only.
  - Does not register `NamEcommerce.Web.Framework`.
  - Owns public API middleware, CORS, session cookie auth, CSRF policy, rate limiting, and ProblemDetails responses.

Add one frontend project:

- `Presentation/Customer/NamEcommerce.Customer.Client`
  - React + TypeScript + Vite SPA.
  - Calls `NamEcommerce.Customer.Api` through REST.
  - Uses customer-safe API models only.

The internal/admin `NamEcommerce.Web` app remains the place for approval workflows, reconciliation, and blocking/unblocking customers.

## Why Not Reuse Admin Web Contracts Directly

`NamEcommerce.Web.Contracts` and `NamEcommerce.Web.Framework` are built for the internal MVC/admin surface. They contain commands and handlers that are too powerful for a public customer API, such as order deletion, order completion, cancellation, delivery note creation, and order item updates.

The customer API needs stricter boundaries:

- Anonymous QR access must expose minimal delivery data.
- Authenticated data must be scoped by customer session.
- Customer-created order, return, and payment flows must create requests or intents, not mutate internal business records directly.
- Public API responses must avoid leaking internal admin details.

The customer portal should reuse the Application and Domain layers underneath, but have its own contracts and handlers at the presentation boundary.

## Access Model

### Public QR Access

Each printed delivery note can include a QR code URL:

```text
/d/{token}
```

The token is an opaque random token, not a raw `DeliveryNoteId`.

Anonymous users can view only:

- Delivery note code.
- Order code.
- Delivery status.
- Delivery items: product name and quantity.
- Basic shipping information.
- Whether authentication is available.

Anonymous users cannot view:

- Full order history.
- Customer debt.
- Payment history.
- Other delivery notes.
- Other orders for the customer.
- Internal notes or admin-only fields.

### OTP Verified Session

From a valid public delivery note page, the customer can request OTP.

The API resolves:

- Delivery token.
- Delivery note.
- CustomerId from the delivery note.
- Customer phone and fallback email from the customer record.

SMS is the primary OTP channel. Email is fallback when SMS cannot be sent in the current implementation. The first phase uses mock providers.

After successful OTP verification:

- The API creates a session-only cookie.
- The session is scoped to exactly one `CustomerId`.
- The session may also remember the source delivery note for audit and first navigation.
- The React app can navigate into `/app`.

### Password Login

After OTP verification, the customer can set a password.

Later logins can use phone/email plus password. The session remains session-only even after password login. No persistent refresh token is used.

Password hashing uses the existing `ISecurityService` BCrypt implementation or a customer-account-specific wrapper over the same service.

## Session Policy

Customer portal authentication uses a cookie with:

- `HttpOnly`.
- `Secure`.
- `SameSite=Lax` or `SameSite=Strict` depending on deployment and QR flow.
- No persistent expiration.
- Idle timeout configured server-side.

Closing the browser session requires a new OTP verification or password login.

The preferred implementation stores sessions in the database through `CustomerPortalSession` so admin blocking or security review can revoke sessions when needed.

## Abuse Protection

The portal must protect the business phone/email channels from harassment.

Rate limits apply across multiple dimensions:

- IP address.
- Delivery access token.
- CustomerId.
- Phone number.
- Email address.
- OTP challenge id.

Recommended first-phase limits:

- At least 60 seconds between OTP sends for the same customer or token.
- At most 5 OTP sends per 15 minutes per customer.
- At most 10 OTP sends per day per customer.
- At most 5 wrong OTP attempts per challenge.
- OTP expires after 5 minutes.
- Blocked customers cannot request OTP, login, create order requests, create return requests, send feedback, or create payment intents.

The API should return generic messages for sensitive operations. For example, OTP request responses should not reveal whether a phone number, email, token, or customer exists.

All important auth and abuse events are written to `CustomerSecurityEvent`.

## Customer Account State

Add `CustomerPortalAccount` linked one-to-one with `CustomerId`.

Status values:

- `Active`
- `Blocked`

Blocked behavior:

- Public QR page can still show minimal delivery note data if the token is valid.
- OTP request is refused with a generic response.
- Password login is refused.
- Existing customer sessions are revoked or treated as invalid.
- Customer cannot create order requests, return requests, feedback, or payment intents.
- Security events are still recorded.

Admin can block or unblock the portal account from the internal web app.

## Data Model Additions

### CustomerPortalAccount

Purpose: stores portal authentication and status for an existing customer.

Fields:

- `Id`
- `CustomerId`
- `PasswordHash`
- `PasswordSalt`
- `Status`
- `PasswordSetOnUtc`
- `LastLoginOnUtc`
- `CreatedOnUtc`
- `UpdatedOnUtc`

### CustomerOtpChallenge

Purpose: tracks OTP generation and verification attempts.

Fields:

- `Id`
- `CustomerId`
- `DeliveryNoteId`
- `Channel`
- `OtpHash`
- `ExpiresOnUtc`
- `AttemptCount`
- `Status`
- `RequestedIp`
- `RequestedUserAgent`
- `SentToMasked`
- `CreatedOnUtc`
- `VerifiedOnUtc`

Status values:

- `Pending`
- `Verified`
- `Expired`
- `Locked`
- `Cancelled`

### CustomerPortalSession

Purpose: stores revocable session records.

Fields:

- `Id`
- `CustomerId`
- `SessionTokenHash`
- `CreatedOnUtc`
- `LastSeenOnUtc`
- `ExpiresOnUtc`
- `RevokedOnUtc`
- `CreatedIp`
- `UserAgent`

### CustomerSecurityEvent

Purpose: audit sensitive portal operations.

Fields:

- `Id`
- `CustomerId`
- `DeliveryNoteId`
- `EventType`
- `Outcome`
- `IpAddress`
- `UserAgent`
- `MetadataJson`
- `CreatedOnUtc`

Event types include:

- `PublicDeliveryViewed`
- `OtpRequested`
- `OtpSendBlocked`
- `OtpVerified`
- `OtpVerifyFailed`
- `PasswordSet`
- `PasswordLoginSucceeded`
- `PasswordLoginFailed`
- `SessionRevoked`
- `BlockedActionAttempted`
- `OrderRequestCreated`
- `ReturnRequestCreated`
- `PaymentIntentCreated`
- `PaymentMockCompleted`

### DeliveryNoteAccessToken

Purpose: maps printed QR tokens to delivery notes.

Fields:

- `Id`
- `DeliveryNoteId`
- `TokenHash`
- `ExpiresOnUtc`
- `RevokedOnUtc`
- `CreatedOnUtc`
- `LastViewedOnUtc`

Tokens are random opaque values. Store only hashes in the database.

### CustomerDeliveryFeedback

Purpose: records customer feedback for a delivery note.

Fields:

- `Id`
- `CustomerId`
- `DeliveryNoteId`
- `Rating`
- `Message`
- `Status`
- `CreatedOnUtc`
- `ReviewedOnUtc`

### CustomerOrderRequest

Purpose: lets a customer request a new order without creating a real `Order` immediately.

Fields:

- `Id`
- `CustomerId`
- `Code`
- `Status`
- `ExpectedShippingDateUtc`
- `ShippingAddress`
- `Note`
- `AdminNote`
- `CreatedOnUtc`
- `ReviewedOnUtc`
- `ReviewedByUserId`
- `ConvertedOrderId`

Status values:

- `PendingApproval`
- `Approved`
- `Rejected`
- `ConvertedToOrder`
- `Cancelled`

### CustomerOrderRequestItem

Fields:

- `Id`
- `CustomerOrderRequestId`
- `ProductId`
- `ProductName`
- `Quantity`
- `UnitPriceSnapshot`

### CustomerReturnRequest

Purpose: lets a customer request return handling without creating a real `CustomerReturn` immediately.

Fields:

- `Id`
- `CustomerId`
- `DeliveryNoteId`
- `Status`
- `Reason`
- `AdminNote`
- `CreatedOnUtc`
- `ReviewedOnUtc`
- `ReviewedByUserId`
- `ConvertedCustomerReturnId`

Status values:

- `PendingReview`
- `Accepted`
- `Rejected`
- `ConvertedToReturn`
- `Cancelled`

### CustomerReturnRequestItem

Fields:

- `Id`
- `CustomerReturnRequestId`
- `DeliveryNoteItemId`
- `ProductId`
- `ProductName`
- `RequestedQuantity`
- `Reason`

### CustomerPaymentIntent

Purpose: supports online payment flow through a provider abstraction. First phase uses mock provider.

Fields:

- `Id`
- `CustomerId`
- `CustomerDebtId`
- `Amount`
- `Provider`
- `ProviderIntentId`
- `Status`
- `FailureReason`
- `CreatedOnUtc`
- `CompletedOnUtc`
- `ReconciledOnUtc`
- `ReconciledByUserId`
- `CustomerPaymentId`

Status values:

- `Created`
- `Processing`
- `SucceededPendingReconciliation`
- `Failed`
- `Cancelled`
- `Reconciled`

## Provider Abstractions

### OTP Sender

Use interfaces so providers can be replaced later:

- `ICustomerOtpSender`
- `MockSmsOtpSender`
- `MockEmailOtpSender`

The OTP application service decides channel priority:

1. Try SMS.
2. Fall back to email if SMS send fails or is disabled.

Mock provider behavior:

- Writes the generated OTP to logs or a development-safe store.
- Does not call external services.
- Supports deterministic local testing and manual verification.

### Payment Provider

Use:

- `ICustomerPaymentProvider`
- `MockCustomerPaymentProvider`

The provider abstraction returns a provider intent id and current status. Later real providers can use webhook callbacks.

First phase mock flow:

1. Customer creates a payment intent.
2. React shows a mock payment page.
3. Customer clicks mock success or mock failure.
4. API marks success as `SucceededPendingReconciliation`.
5. Admin reviews and reconciles.
6. Only after reconciliation does the system create or apply a `CustomerPayment`.

## Customer REST API

All authenticated endpoints must derive `CustomerId` from the session, never from request body.

### Public

- `GET /api/public/delivery-notes/{token}`
  - Returns public delivery note details.

### Auth

- `POST /api/auth/otp/request`
  - Input: delivery token.
  - Output: generic send result and challenge id when safe.

- `POST /api/auth/otp/verify`
  - Input: challenge id and OTP.
  - Output: current customer session model.
  - Side effect: creates session-only cookie.

- `POST /api/auth/password/login`
  - Input: phone/email and password.
  - Output: current customer session model.

- `POST /api/auth/password/set`
  - Auth required.
  - Input: password.
  - Output: success.

- `POST /api/auth/logout`
  - Revokes current customer session and clears cookie.

- `GET /api/me`
  - Returns current customer session and basic customer profile.

### Orders

- `GET /api/orders`
  - Returns orders for the current `CustomerId`.

- `GET /api/orders/{id}`
  - Returns order details only if the order belongs to current `CustomerId`.

- `POST /api/order-requests`
  - Creates a pending customer order request.
  - Does not create an internal `Order`.

### Delivery Notes

- `GET /api/delivery-notes`
  - Returns delivery notes for current `CustomerId`.

- `GET /api/delivery-notes/{id}`
  - Returns delivery note details only if it belongs to current `CustomerId`.

- `POST /api/delivery-notes/{id}/confirm`
  - Confirms customer receipt when business rules allow it.

- `POST /api/delivery-notes/{id}/feedback`
  - Creates customer feedback.

### Return Requests

- `POST /api/return-requests`
  - Creates a pending return request.
  - Does not create an internal `CustomerReturn`.

### Debts And Payments

- `GET /api/debts`
  - Returns summary, debt rows, and payment history for current `CustomerId`.

- `POST /api/payment-intents`
  - Creates a mock payment intent.

- `POST /api/payment-intents/{id}/mock-complete`
  - Marks mock success or failure.
  - Success becomes pending reconciliation, not applied debt payment.

## React Client

Use React + TypeScript + Vite.

Routes:

- `/d/:token`
  - Public QR delivery note page.

- `/verify`
  - OTP verification.

- `/login`
  - Password login.

- `/set-password`
  - Set password after OTP-authenticated session.

- `/app`
  - Customer dashboard.

- `/app/orders`
  - Order list.

- `/app/orders/:id`
  - Order detail.

- `/app/orders/new`
  - New order request form.

- `/app/delivery-notes`
  - Delivery note list.

- `/app/delivery-notes/:id`
  - Delivery note detail, confirm receipt, feedback, return request entry.

- `/app/debts`
  - Debt summary and payment entry.

- `/app/payments/:intentId`
  - Mock payment screen.

Frontend state:

- Use a small API client wrapping `fetch`.
- Use `credentials: "include"` for cookie-authenticated API calls.
- Keep auth state in memory by calling `/api/me` on app load.
- Do not store session tokens in localStorage.
- Do not store OTP or sensitive data in localStorage.

UI principles:

- Mobile-first because QR scanning usually happens on a phone.
- Keep the first screen practical, not marketing-oriented.
- Follow `DESIGN.md` colors and typography.
- Use clear badges for delivery, order, debt, and request statuses.
- Prefer simple tables/lists and compact cards.
- Keep money formatting consistent with existing Vietnamese `đ` formatting.

## Admin Web Additions

Internal/admin `NamEcommerce.Web` needs later screens or sections for:

- Customer portal account status on customer detail.
- Block/unblock portal account.
- View customer security events.
- Review customer order requests.
- Convert approved order requests into real `Order` records.
- Review customer return requests.
- Convert accepted return requests into real `CustomerReturn` records.
- Review payment intents pending reconciliation.
- Apply reconciled payments to `CustomerPayment` and debt records.

These admin flows remain internal and are not exposed through the customer API.

## Error Handling

Customer API responses should use API-safe errors:

- Validation failures return HTTP 400 with ProblemDetails.
- Unauthenticated calls return HTTP 401.
- Authenticated but cross-customer access returns HTTP 404 or 403 consistently, without leaking record existence.
- Blocked customer operations return a generic blocked-action result and write a security event.
- OTP request returns generic success-style messaging where needed to avoid enumeration.

## Verification

After implementation, verify:

- `dotnet build NamEcommerce/NamEcommerce.sln` succeeds.
- Customer API starts and exposes only customer endpoints.
- `Customer.Api` does not reference or register `NamEcommerce.Web.Framework`.
- Public QR page shows only minimal delivery note data.
- OTP request uses mock SMS first and mock email fallback.
- OTP cooldown and attempt limits work.
- Session cookie is session-only and `HttpOnly`.
- Closing the browser session requires login again.
- Authenticated endpoints are scoped to the session `CustomerId`.
- Blocked customer cannot request OTP, login, create order requests, return requests, feedback, or payment intents.
- Payment mock success creates pending reconciliation and does not immediately reduce debt.
- React app does not store session tokens in localStorage.

Do not add or edit unit tests in any `*.Test` project unless project instructions change. Do not run migrations or database updates.

## Out Of Scope

- Real SMS provider integration.
- Real email provider integration.
- Real payment gateway integration.
- Native mobile application.
- Replacing admin MVC workflows.
- Exposing admin Web contracts or handlers through the customer API.
- Running EF migrations.
- Writing or editing test projects.

## References

- React docs currently document React 19.x and list React 19.2 as the latest documented version: https://react.dev/versions
- React Server Components had recent security advisories, reinforcing the decision to keep the portal as a client-only Vite SPA for now: https://react.dev/blog/2025/12/11/denial-of-service-and-source-code-exposure-in-react-server-components
