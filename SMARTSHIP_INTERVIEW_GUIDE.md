# SmartShip – Technical Interview Revision & Cheat Sheet Guide

An ultra-concise, high-yield revision manual designed to prepare you for technical interviews on the **SmartShip Logistics Management System**.

---

## 1. Fast Interview Pitches

### Project in 30 Seconds
> "SmartShip is a distributed logistics management platform built with C# and .NET 10 microservices. It features four domain services—Identity, Shipment, Payment, and Admin—behind an Ocelot API Gateway running on port 5000. It implements database-per-service using SQL Server, JWT bearer security with role-based claims, Razorpay and COD payment integration, and RabbitMQ via MassTransit for real-time event-driven metrics aggregation."

### Project in 2 Minutes
> "SmartShip decouples core logistics operations into four autonomous microservices. Identity Service manages credentials, BCrypt password hashing, and issues signed JWT tokens containing role claims (`CUSTOMER` vs `ADMIN`). Shipment Service manages the parcel state machine (`Draft` -> `Booked` -> `PickedUp` -> `InTransit` -> `OutForDelivery` -> `Delivered`), calculates shipping quotes dynamically based on weight and service tier, and handles pickup scheduling. Payment Service integrates with Razorpay for online order creation and HMAC-SHA256 signature verification, calculates itemized taxes (Fuel Surcharge, Handling Fee, Fragile Surcharge, 18% GST), and supports Cash on Delivery. Admin Service provisions logistics hubs, generates business performance reports, and consumes MassTransit domain events over RabbitMQ to maintain real-time operational analytics without querying operational databases. An Ocelot API Gateway front-ends the system, handling upstream path rewriting and centralized JWT validation."

---

## 2. Architecture Quick Reference

### System Layout
* **Gateway**: `SmartShip.Gateway` (Port `5000`)
* **Identity Service**: `SmartShip.Identity.API` (Port `5002`) | Database: `SmartShip_IdentityDb`
* **Shipment Service**: `SmartShip.Shipment.API` (Port `5004`) | Database: `SmartShip_ShipmentDb`
* **Payment Service**: `SmartShip.Payment.API` (Port `5003`) | Database: `SmartShip_PaymentDb`
* **Admin Service**: `SmartShip.Admin.API` (Port `5001`) | Database: `SmartShip_AdminDb`
* **Messaging Broker**: RabbitMQ using MassTransit on default port `5672`

### Service Summaries in One Sentence
* **Identity Service**: "Security authority issuing JWT tokens and managing user accounts."
* **Shipment Service**: "Core domain engine controlling parcel rate calculation, pickup scheduling, tracking, and status transitions."
* **Payment Service**: "Financial gateway executing Razorpay HMAC signature checks, itemized tax math, and COD registrations."
* **Admin Service**: "Analytics authority providing real-time dashboard metrics and logistics hub administration."

---

## 3. High-Yield Topic Breakdown

### Most Important APIs
1. `POST /gateway/auth/signup` & `login`: Account registration and JWT retrieval.
2. `POST /gateway/shipments/create`: Book parcel in `Draft` status.
3. `POST /gateway/shipments/{id}/schedule-pickup`: Schedule pickup and advance status to `Booked`.
4. `GET /gateway/shipments/by-tracking/{trackingNumber}`: Parcel tracking by tracking number.
5. `POST /gateway/payment/create-order`: Calculate taxes and generate Razorpay order.
6. `POST /gateway/payment/verify`: Verify Razorpay HMAC-SHA256 signature and mark `Paid`.
7. `GET /gateway/admin/dashboard`: Fetch real-time system metrics.

### Most Important Events
1. `ShipmentCreatedEvent`: Published by Shipment Service -> Consumed by Admin Service to increment shipment counts.
2. `PaymentCompletedEvent`: Published by Payment Service -> Consumed by Admin Service to increment total revenue.
3. `ShipmentCancelledEvent`: Published by Shipment Service -> Consumed by Payment & Admin Services.
4. `UserDeletedEvent`: Published by Identity Service -> Consumed by Shipment, Payment & Admin Services to purge data.
5. `PaymentFailedEvent`: Published by Payment Service -> Consumed by Shipment Service to set status `PaymentFailed`.

### Database Architecture
* **Pattern**: Database-per-Service.
* **Technology**: Microsoft SQL Server with EF Core 10.0 Code-First migrations.
* **Why**: Enforces domain isolation, prevents cross-service database locks, allows independent database scaling.

### Authentication & Security
* **Pattern**: JWT Bearer Tokens signed with HMAC-SHA256 (`SymmetricSecurityKey`).
* **Password Security**: BCrypt password hashing (`BCrypt.Net`).
* **Payment Security**: Razorpay HMAC-SHA256 signature verification over `orderId + "|" + paymentId`.
* **Authorization**: Roles `CUSTOMER` and `ADMIN` using `[Authorize(Roles = "...")]`.

### RabbitMQ & MassTransit
* **RabbitMQ**: Message broker providing exchanges and queues.
* **MassTransit**: High-level .NET bus library managing RabbitMQ connections, serialization, and consumer endpoints.
* **Why**: Provides loose coupling, fault isolation, and eventual consistency across domain boundaries.

---

## 4. Pattern Rationale Cheat Sheet

* **Why Microservices?**: Decouples domain failure domains so payment or analytics issues don't crash shipment booking.
* **Why API Gateway (Ocelot)?**: Provides a single port (`5000`) for clients, hides internal microservice ports (`5001`-`5004`), handles path rewriting and central token validation.
* **Why Repository Pattern?**: Abstracts EF Core LINQ queries behind interfaces (`IShipmentRepository`), making application code mockable and unit-testable with Moq.
* **Why Unit of Work Pattern?**: Ensures atomic database commits (`SaveChangesAsync()`) across multiple repository modifications within a single request.
* **Why Event-Driven Architecture?**: Eliminates slow synchronous HTTP calls for side-effects, achieving eventual consistency without direct database dependencies.
* **How Errors Are Handled?**: Global `ExceptionMiddleware` catches exceptions centrally, mapping `KeyNotFoundException` to HTTP 404, `UnauthorizedAccessException` to 401, etc.
* **How Testing Is Done?**: Unit tests built with xUnit and Moq. In-memory database used for repository testing. Over 54+ tests actively passing across Admin and Identity suites.

---

## 5. Top 30 Technical Interview Q&A

#### Q1: What is SmartShip and what architecture does it use?
**A**: SmartShip is an enterprise logistics management platform built on .NET 10 microservices using Clean Architecture, Database-per-Service (SQL Server), Ocelot API Gateway, and RabbitMQ via MassTransit for asynchronous event messaging.

#### Q2: What services exist in the project and what ports do they use?
**A**: Gateway (Port `5000`), Admin Service (Port `5001`), Identity Service (Port `5002`), Payment Service (Port `5003`), and Shipment Service (Port `5004`).

#### Q3: Why did you use an API Gateway?
**A**: Ocelot acts as a reverse proxy, presenting a single entry point (`http://localhost:5000`) to clients, hiding internal service topology, handling upstream path rewriting (`/gateway/*` -> `/api/*`), and performing JWT authentication pass-through.

#### Q4: How is user authentication implemented?
**A**: Identity Service validates credentials, hashes passwords using BCrypt, and issues signed JWT bearer tokens containing standard claims (`NameIdentifier`, `Email`, `Name`, `Role`).

#### Q5: How is authorization enforced across microservices?
**A**: Controllers use ASP.NET Core `[Authorize]` attributes specifying required roles (`CUSTOMER` or `ADMIN`). Ocelot validates token signatures, and downstream services inspect claims.

#### Q6: How does Database-per-Service work in SmartShip?
**A**: Each microservice owns a dedicated SQL Server database (`IdentityDb`, `ShipmentDb`, `PaymentDb`, `AdminDb`). Services never execute SQL queries or joins against another service's database.

#### Q7: How do services reference data in other microservices without foreign keys?
**A**: They store logical integer keys (e.g., `Shipments.CustomerId` referencing `User.Id`). These references are validated either synchronously via internal REST HTTP calls or synchronized asynchronously via events.

#### Q8: How does rate calculation work in Shipment Service?
**A**: `CalculateRateAsync` multiplies parcel weight by tier multipliers (Domestic ₹80/kg, Express ₹150/kg, Freight ₹50/kg, International ₹300/kg) with a minimum charge floor of ₹99.

#### Q9: Explain the shipment lifecycle statuses.
**A**: `Draft` -> `Booked` -> `PickedUp` -> `InTransit` -> `OutForDelivery` -> `Delivered`. Additional terminal or exception states include `Cancelled`, `PaymentFailed`, `Delayed`, `Failed`, and `Returned`.

#### Q10: How does pickup scheduling work?
**A**: Customer calls `POST /gateway/shipments/{id}/schedule-pickup`. The service verifies shipment status is `Draft` or `PaymentFailed`, sets `PickupScheduledAt`, advances status to `Booked`, and publishes `ShipmentStatusUpdatedEvent`.

#### Q11: How does Payment Service calculate the total shipment charge?
**A**: Base shipping rate + Fuel Surcharge (5%) + Handling Fee (₹120 International / ₹50 others) + Fragile Surcharge (₹80 if fragile) + COD Fee (1.5% if COD) + 18% GST on the subtotal.

#### Q12: How does Razorpay payment verification work?
**A**: The service receives `razorpayOrderId`, `razorpayPaymentId`, and `signature`. It computes an HMAC-SHA256 hash of `orderId + "|" + paymentId` using the server-side Razorpay Secret Key and verifies that it matches the provided signature.

#### Q13: What happens if Razorpay signature verification fails?
**A**: Payment status is set to `Failed`, a `PaymentFailedEvent` is published to RabbitMQ, and `ShipmentService` updates the shipment status to `PaymentFailed`.

#### Q14: How does Cash on Delivery (COD) work?
**A**: When requested, Payment Service registers a payment record with `PaymentMethod = COD` and `PaymentStatus = Pending`, calculating a 1.5% COD handling fee without invoking Razorpay API orders.

#### Q15: What is the purpose of the Demo Payment endpoint?
**A**: `POST /api/payment/demo-payment/{orderId}` generates a mock `razorpayPaymentId` and valid HMAC signature for testing payment verification without requiring live Razorpay checkout UI integration.

#### Q16: Why did you use MassTransit with RabbitMQ?
**A**: MassTransit abstracts RabbitMQ broker mechanics, handling exchange/queue bindings, message serialization, and consumer lifecycle management in C# code.

#### Q17: Name three domain events and their subscribers.
**A**:
1. `ShipmentCreatedEvent` -> Consumed by Admin Service (`ShipmentCreatedMetricsConsumer`).
2. `PaymentCompletedEvent` -> Consumed by Admin Service (`PaymentCompletedConsumer`).
3. `UserDeletedEvent` -> Consumed by Shipment, Payment, and Admin Services.

#### Q18: How does Admin Service aggregate metrics in real time?
**A**: Admin Service consumes MassTransit domain events (`ShipmentCreatedEvent`, `PaymentCompletedEvent`, `ShipmentDeliveredEvent`) and incrementally updates its own `DashboardMetrics` table in `AdminDb`.

#### Q19: What is the advantage of event-driven metrics over direct SQL queries?
**A**: It avoids cross-database SQL queries and expensive runtime joins, allowing the admin dashboard API to respond instantly (< 10ms) by reading pre-aggregated rows.

#### Q20: What happens when an admin deletes a user?
**A**: Identity Service deletes the user from `IdentityDb` and publishes `UserDeletedEvent`. Shipment Service purges customer shipments, Payment Service purges payment records, and Admin Service decrements registered customer metrics.

#### Q21: What design patterns are used in SmartShip?
**A**: Clean/Onion Architecture, Repository Pattern, Unit of Work, DTO Pattern, Dependency Injection, Global Exception Handling Middleware, and Event-Driven Architecture.

#### Q22: What is the Repository Pattern and where is it used?
**A**: It abstracts database operations behind interfaces like `IShipmentRepository` and `IUserRepository`, decoupling data access from business logic and enabling unit testing with Moq.

#### Q23: What is the Unit of Work Pattern and why is it useful?
**A**: It coordinates multiple repositories under a single EF Core `DbContext` transaction, ensuring atomic commits (`SaveChangesAsync()`) so related entity changes succeed or fail together.

#### Q24: How are exceptions handled globally across services?
**A**: `ExceptionMiddleware` in `SmartShip.Shared` catches unhandled exceptions centrally, translating domain exceptions like `KeyNotFoundException` into standard HTTP 404 JSON payloads.

#### Q25: What logging framework is used and how is it configured?
**A**: Serilog is configured across all services and Gateway, writing enriched logs (`Application`, `Environment`, `StatusCode`, `Elapsed`) to Console and rotating files (`./Logs/log-YYYYMMDD.log`).

#### Q26: What testing tools are used in SmartShip?
**A**: xUnit for test execution, Moq for dependency mocking, and EF Core InMemory provider for database context testing. Over 54+ tests actively pass in the test suite.

#### Q27: How does inter-service synchronous REST communication work?
**A**: Services use `IHttpClientFactory` with pre-configured `BaseAddress` URLs and forward incoming JWT Bearer headers to downstream internal endpoints.

#### Q28: How do you handle configuration secrets safely?
**A**: Sensitive settings (JWT keys, SQL connection strings, Razorpay secrets) are stored in `appsettings.json` placeholders and overridden in production via OS environment variables.

#### Q29: What are the current limitations of the system?
**A**: Lack of a stateful Saga orchestrator for complex multi-step rollbacks, basic retry policies without explicit Dead Letter Queues, and missing OpenTelemetry distributed tracing.

#### Q30: What production improvements would you recommend next?
**A**:
1. Implement MassTransit State Machine Sagas for distributed transaction coordination.
2. Integrate OpenTelemetry and Jaeger for distributed HTTP/RabbitMQ tracing.
3. Deploy services using Docker containers and Kubernetes (`k8s`) orchestration.
4. Add Redis caching for logistics hubs and parcel tracking queries.

---

*Interview Cheat Sheet compiled for **SmartShip Logistics System**.*
