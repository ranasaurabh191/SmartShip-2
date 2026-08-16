# SmartShip – Comprehensive API Reference

Complete catalog of all HTTP REST API controller endpoints implemented in the SmartShip logistics platform.

---

## Gateway Endpoint Overview (`Port 5000`)

All client requests are routed through the Ocelot API Gateway at `http://localhost:5000`.

---

## 1. Identity Service APIs (`Port 5002`)

### 1.1 `POST /gateway/auth/signup`
* **Downstream Endpoint**: `POST http://localhost:5002/api/auth/signup`
* **Authentication**: Anonymous (None)
* **Role**: None
* **Purpose**: Registers a new customer user account in `IdentityDb`.
* **Request Body**:
```json
{
  "name": "Jane Doe",
  "email": "jane@example.com",
  "phone": "+919876543210",
  "password": "Password123!"
}
```
* **Success Response (200 OK)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "CUSTOMER",
  "name": "Jane Doe",
  "userId": 15
}
```
* **Possible Error Codes**: `400 Bad Request` (Validation failure), `409 Conflict` ("User with this email already exists.").
* **Events Triggered**: None.

---

### 1.2 `POST /gateway/auth/login`
* **Downstream Endpoint**: `POST http://localhost:5002/api/auth/login`
* **Authentication**: Anonymous (None)
* **Role**: None
* **Purpose**: Authenticates user credentials and returns a signed JWT bearer token.
* **Request Body**:
```json
{
  "email": "jane@example.com",
  "password": "Password123!"
}
```
* **Success Response (200 OK)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "CUSTOMER",
  "name": "Jane Doe",
  "userId": 15
}
```
* **Possible Error Codes**: `404 Not Found` ("User not found with this email."), `401 Unauthorized` ("User account is inactive" / "Incorrect password.").
* **Events Triggered**: None.

---

### 1.3 `PUT /gateway/auth/profile`
* **Downstream Endpoint**: `PUT http://localhost:5002/api/auth/profile`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER` or `ADMIN`
* **Purpose**: Updates the current authenticated user's profile details (`Name`, `Email`, `Phone`).
* **Request Body**:
```json
{
  "name": "Jane Doe Updated",
  "email": "jane.updated@example.com",
  "phone": "+919876543211"
}
```
* **Success Response (200 OK)**:
```json
{
  "message": "Profile updated successfully."
}
```
* **Possible Error Codes**: `401 Unauthorized`, `404 Not Found`, `409 Conflict` (Email already taken).

---

### 1.4 `GET /gateway/admin/users/{id}`
* **Downstream Endpoint**: `GET http://localhost:5002/api/admin/users/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Fetches user details by integer user ID.
* **Path Parameters**: `id` (integer) - User ID.
* **Success Response (200 OK)**:
```json
{
  "id": 15,
  "name": "Jane Doe",
  "email": "jane@example.com",
  "phone": "+919876543210",
  "role": "CUSTOMER",
  "isActive": true,
  "createdAt": "2026-08-16T21:00:00Z"
}
```
* **Possible Error Codes**: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`.

---

### 1.5 `PUT /gateway/admin/users/{id}`
* **Downstream Endpoint**: `PUT http://localhost:5002/api/admin/users/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Updates active status (`IsActive`) of a non-admin user account.
* **Path Parameters**: `id` (integer) - User ID.
* **Request Body**:
```json
{
  "isActive": false
}
```
* **Success Response (200 OK)**:
```json
{
  "message": "Updated Successfully"
}
```
* **Possible Error Codes**: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict` ("ADMIN cannot be Updated.").

---

### 1.6 `DELETE /gateway/admin/users/{id}`
* **Downstream Endpoint**: `DELETE http://localhost:5002/api/admin/users/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Permanently deletes a non-admin user account.
* **Path Parameters**: `id` (integer) - User ID.
* **Success Response (200 OK)**:
```json
{
  "message": "Deleted Successfully"
}
```
* **Possible Error Codes**: `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict` ("ADMIN cannot be deleted.").
* **Events Triggered**: Publishes `UserDeletedEvent` to RabbitMQ.

---

### 1.7 `GET /api/auth/internal/users/{id}/exists` (Internal API)
* **Downstream Endpoint**: Direct service-to-service endpoint on `http://localhost:5002`
* **Authentication**: Internal HTTP call from `ShipmentService`
* **Purpose**: Validates if user exists and is active.
* **Success Response (200 OK)**:
```json
{
  "exists": true
}
```
* **Possible Error Codes**: `404 Not Found`.

---

## 2. Shipment Service APIs (`Port 5004`)

### 2.1 `POST /gateway/shipments/create`
* **Downstream Endpoint**: `POST http://localhost:5004/api/shipments/create`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Books a new parcel shipment in `Draft` state.
* **Request Body**:
```json
{
  "senderAddress": {
    "fullName": "Jane Sender",
    "phone": "+919876543210",
    "street": "123 MG Road",
    "city": "Bengaluru",
    "state": "Karnataka",
    "postalCode": "560001",
    "country": "India"
  },
  "receiverAddress": {
    "fullName": "John Receiver",
    "phone": "+919876543219",
    "street": "456 Park Street",
    "city": "Mumbai",
    "state": "Maharashtra",
    "postalCode": "400001",
    "country": "India"
  },
  "package": {
    "weightKg": 21.0,
    "lengthCm": 50.0,
    "widthCm": 40.0,
    "heightCm": 30.0,
    "description": "Industrial Spares"
  },
  "shipmentType": "Domestic",
  "notes": "Handle with care",
  "isFragile": true
}
```
* **Success Response (201 Created)**:
```json
{
  "id": 102,
  "trackingNumber": "SHP-20260816210415-4921",
  "customerId": 15,
  "shipmentType": "Domestic",
  "status": "Draft",
  "paymentStatus": "Pending",
  "shippingRate": 1680.00,
  "createdAt": "2026-08-16T21:04:15.000Z",
  "pickupScheduledAt": null,
  "deliveredAt": null,
  "senderAddress": { ... },
  "receiverAddress": { ... },
  "package": { ... },
  "notes": "Handle with care",
  "isFragile": true
}
```
* **Possible Error Codes**: `400 Bad Request`, `401 Unauthorized`, `404 Not Found` (Customer not active).
* **Events Triggered**: Publishes `ShipmentCreatedEvent`.

---

### 2.2 `GET /gateway/shipments/{id}`
* **Downstream Endpoint**: `GET http://localhost:5004/api/shipments/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER` or `ADMIN`
* **Purpose**: Fetches complete shipment details by integer ID.
* **Path Parameters**: `id` (integer) - Shipment ID.
* **Success Response (200 OK)**: Returns full `ShipmentResponse`.
* **Possible Error Codes**: `401 Unauthorized`, `404 Not Found`.

---

### 2.3 `POST /gateway/shipments/{id}/schedule-pickup`
* **Downstream Endpoint**: `POST http://localhost:5004/api/shipments/{id}/schedule-pickup`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Schedules pickup time, advancing status from `Draft` to `Booked`.
* **Path Parameters**: `id` (integer) - Shipment ID.
* **Request Body**:
```json
{
  "pickupTime": "2026-08-17T10:00:00Z"
}
```
* **Success Response (200 OK)**:
```json
{
  "message": "Pickup scheduled successfully."
}
```
* **Possible Error Codes**: `401 Unauthorized`, `404 Not Found`, `409 Conflict` (Invalid status transition).
* **Events Triggered**: Publishes `ShipmentStatusUpdatedEvent`.

---

### 2.4 `GET /gateway/shipments/rate`
* **Downstream Endpoint**: `GET http://localhost:5004/api/shipments/rate`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Calculates estimated shipping rate quote.
* **Query Parameters**: `weight` (double, kg), `type` (string enum: `Domestic`, `Express`, `Freight`, `International`).
* **Success Response (200 OK)**:
```json
{
  "rate": 1680.00
}
```
* **Possible Error Codes**: `400 Bad Request` (Invalid type or weight).

---

### 2.5 `PATCH /gateway/shipments/{id}/cancel`
* **Downstream Endpoint**: `PATCH http://localhost:5004/api/shipments/{id}/cancel`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Cancels a draft or booked shipment.
* **Path Parameters**: `id` (integer) - Shipment ID.
* **Request Body**:
```json
{
  "reason": "Change of plans"
}
```
* **Success Response (200 OK)**:
```json
{
  "message": "Shipment cancelled successfully."
}
```
* **Possible Error Codes**: `401 Unauthorized`, `404 Not Found`, `409 Conflict` (Shipment already in transit/delivered).
* **Events Triggered**: Publishes `ShipmentCancelledEvent`.

---

### 2.6 `GET /gateway/shipments/by-tracking/{trackingNumber}`
* **Downstream Endpoint**: `GET http://localhost:5004/api/shipments/by-tracking/{trackingNumber}`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER` or `ADMIN`
* **Purpose**: Look up parcel details by unique tracking string.
* **Path Parameters**: `trackingNumber` (string).
* **Success Response (200 OK)**: Returns `ShipmentResponse`.
* **Possible Error Codes**: `404 Not Found`.

---

### 2.7 `PUT /gateway/admin/shipments/status/{id}`
* **Downstream Endpoint**: `PUT http://localhost:5004/api/admin/shipments/status/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Advances parcel status in logistics transit network.
* **Path Parameters**: `id` (integer) - Shipment ID.
* **Request Body**:
```json
{
  "status": "PickedUp",
  "location": "Bengaluru Main Warehouse Hub"
}
```
* **Success Response (200 OK)**:
```json
{
  "message": "Status updated successfully."
}
```
* **Possible Error Codes**: `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`.
* **Events Triggered**: Publishes `ShipmentStatusUpdatedEvent` (or `ShipmentDeliveredEvent` if status becomes `Delivered`).

---

## 3. Payment Service APIs (`Port 5003`)

### 3.1 `POST /gateway/payment/create-order`
* **Downstream Endpoint**: `POST http://localhost:5003/api/payment/create-order`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Initiates Razorpay online order creation or Cash on Delivery registration with tax calculations.
* **Request Body**:
```json
{
  "shipmentId": 102,
  "paymentMethod": "Online"
}
```
* **Success Response (200 OK)**:
```json
{
  "id": 55,
  "shipmentId": 102,
  "trackingNumber": "SHP-20260816210415-4921",
  "amount": 2234.92,
  "paymentMethod": "Online",
  "paymentStatus": "Pending",
  "razorpayOrderId": "order_P19x82KaLm9",
  "razorpayPaymentId": null,
  "createdAt": "16-Aug-2026 09:04 PM",
  "paidAt": null,
  "message": "Payment initiated. Please complete payment."
}
```
* **Possible Error Codes**: `401 Unauthorized`, `404 Not Found` (Shipment not found), `409 Conflict` (Already paid).

---

### 3.2 `POST /gateway/payment/verify`
* **Downstream Endpoint**: `POST http://localhost:5003/api/payment/verify`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Verifies HMAC-SHA256 Razorpay payment signature and marks payment as `Paid`.
* **Request Body**:
```json
{
  "razorpayOrderId": "order_P19x82KaLm9",
  "razorpayPaymentId": "pay_P19x99JkL01",
  "signature": "c8a9f2e3..."
}
```
* **Success Response (200 OK)**:
```json
{
  "id": 55,
  "shipmentId": 102,
  "trackingNumber": "SHP-20260816210415-4921",
  "amount": 2234.92,
  "paymentMethod": "Online",
  "paymentStatus": "Paid",
  "razorpayOrderId": "order_P19x82KaLm9",
  "razorpayPaymentId": "pay_P19x99JkL01",
  "createdAt": "16-Aug-2026 09:04 PM",
  "paidAt": "16-Aug-2026 09:05 PM",
  "message": "Payment successful!"
}
```
* **Possible Error Codes**: `400 Bad Request` / `409 Conflict` (Signature invalid), `401 Unauthorized`.
* **Events Triggered**: Publishes `PaymentCompletedEvent` (or `PaymentFailedEvent` on failure).

---

### 3.3 `POST /gateway/payment/demo-payment/{orderId}`
* **Downstream Endpoint**: `POST http://localhost:5003/api/payment/demo-payment/{orderId}`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Demo/testing helper endpoint generating a mock payment ID and valid HMAC signature.
* **Path Parameters**: `orderId` (string) - Razorpay Order ID.
* **Success Response (200 OK)**:
```json
{
  "razorpayOrderId": "order_P19x82KaLm9",
  "razorpayPaymentId": "pay_demo_871293",
  "signature": "8a3f91...",
  "message": "Demo payment generated successfully."
}
```

---

### 3.4 `GET /gateway/payment/payment-status`
* **Downstream Endpoint**: `GET http://localhost:5003/api/payment/payment-status`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Query payment transaction state.
* **Query Parameters**: `razorpayOrderId` (optional string), `shipmentId` (optional int), `trackingNumber` (optional string).
* **Success Response (200 OK)**: Returns `PaymentResponse`.

---

### 3.5 `GET /gateway/payment/shipment/{shipmentId}`
* **Downstream Endpoint**: `GET http://localhost:5003/api/payment/shipment/{shipmentId}`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER` or `ADMIN`
* **Purpose**: Get payment record by shipment ID.
* **Path Parameters**: `shipmentId` (integer).
* **Success Response (200 OK)**: Returns `PaymentResponse`.

---

### 3.6 `GET /gateway/payment/my`
* **Downstream Endpoint**: `GET http://localhost:5003/api/payment/my`
* **Authentication**: Bearer JWT Token
* **Role**: `CUSTOMER`
* **Purpose**: Fetches payment history for currently authenticated customer.
* **Success Response (200 OK)**: Returns `List<PaymentResponse>`.

---

### 3.7 `GET /gateway/payment/all`
* **Downstream Endpoint**: `GET http://localhost:5003/api/payment/all`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Fetches system-wide payment transactions list.
* **Success Response (200 OK)**: Returns `List<PaymentResponse>`.

---

## 4. Admin Service APIs (`Port 5001`)

### 4.1 `GET /gateway/admin/dashboard`
* **Downstream Endpoint**: `GET http://localhost:5001/api/admin/dashboard`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Fetches real-time system performance metrics.
* **Success Response (200 OK)**:
```json
{
  "totalShipments": 154,
  "activeShipments": 42,
  "deliveredShipments": 108,
  "totalRevenue": 284950.50,
  "registeredCustomers": 89,
  "lastUpdated": "2026-08-16T21:05:00Z"
}
```

---

### 4.2 `GET /gateway/admin/hubs/all-active`
* **Downstream Endpoint**: `GET http://localhost:5001/api/admin/hubs/all-active`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Lists all active logistics hubs.
* **Success Response (200 OK)**: Returns `List<HubDTO>`.

---

### 4.3 `GET /gateway/admin/hubs/{id}`
* **Downstream Endpoint**: `GET http://localhost:5001/api/admin/hubs/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Gets hub details by ID.
* **Success Response (200 OK)**: Returns `HubDTO`.

---

### 4.4 `POST /gateway/admin/hubs`
* **Downstream Endpoint**: `POST http://localhost:5001/api/admin/hubs`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Provisions a new logistics hub.
* **Request Body**:
```json
{
  "hubCode": "HUB-BLR-01",
  "name": "Bengaluru Central Hub",
  "city": "Bengaluru",
  "state": "Karnataka",
  "address": "Industrial Layout, Electronic City",
  "pincode": "560100",
  "contactPhone": "+918023456789",
  "isActive": true
}
```
* **Success Response (200 OK)**: Returns created `HubDTO`.

---

### 4.5 `PUT /gateway/admin/hubs/{id}`
* **Downstream Endpoint**: `PUT http://localhost:5001/api/admin/hubs/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Updates an existing logistics hub.
* **Success Response (200 OK)**: `"Updated Successfully"`.

---

### 4.6 `DELETE /gateway/admin/hubs/{id}`
* **Downstream Endpoint**: `DELETE http://localhost:5001/api/admin/hubs/{id}`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Deletes a logistics hub.
* **Success Response (200 OK)**: `{ "message": "Deleted Successfully" }`.

---

### 4.7 `POST /gateway/admin/reports`
* **Downstream Endpoint**: `POST http://localhost:5001/api/admin/reports`
* **Authentication**: Bearer JWT Token
* **Role**: `ADMIN`
* **Purpose**: Generates an operational business report.
* **Request Body**:
```json
{
  "reportType": "Revenue",
  "startDate": "2026-08-01T00:00:00Z",
  "endDate": "2026-08-16T23:59:59Z"
}
```
* **Success Response (200 OK)**:
```json
{
  "id": 12,
  "reportType": "Revenue",
  "generatedBy": "Admin",
  "generatedAt": "2026-08-16T21:05:00Z",
  "summaryJson": "{\"totalRevenue\":284950.50,\"count\":108}",
  "filePath": "/reports/Revenue_20260816.pdf"
}
```

---

*API Reference documentation compiled for **SmartShip Logistics System**.*
