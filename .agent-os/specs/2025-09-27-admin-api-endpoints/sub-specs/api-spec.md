# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-09-27-admin-api-endpoints/spec.md

## Endpoints

### GET /api/v1/quote?admin=true&page=1&pageSize=50&search=...&sortBy=...&sortDir=...

**Purpose:** Admin paginated quote listing with advanced filtering and sorting capabilities
**Parameters:** 
- `admin=true` (required) - Enables admin-specific features
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 50, max: 100) - Number of items per page
- `search` (optional) - Text search across quote reference, customer name, driver name
- `sortBy` (optional, default: createdAt) - Field to sort by (createdAt, amount, status, driverName)
- `sortDir` (optional, default: desc) - Sort direction (asc, desc)
- `status` (optional) - Filter by quote status (pending, confirmed, completed, cancelled)
- `dateFrom` (optional) - Filter quotes from this date (ISO format)
- `dateTo` (optional) - Filter quotes to this date (ISO format)
- `driverId` (optional) - Filter by assigned driver ID

**Response:** 
```json
{
  "data": [
    {
      "id": "string",
      "reference": "string",
      "customerName": "string",
      "amount": "decimal",
      "status": "string",
      "createdAt": "datetime",
      "driverName": "string",
      "driverId": "string"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 150,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

**Errors:** 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 500 (Internal Server Error)

### GET /api/v1/auth/users?page=1&pageSize=50&search=...&sortBy=...&sortDir=...

**Purpose:** Admin paginated user listing with search and filtering capabilities
**Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 50, max: 100) - Number of items per page
- `search` (optional) - Text search across email, firstName, lastName
- `sortBy` (optional, default: createdAt) - Field to sort by (createdAt, email, firstName, lastName, lastLogin)
- `sortDir` (optional, default: desc) - Sort direction (asc, desc)
- `role` (optional) - Filter by user role
- `isActive` (optional) - Filter by active status (true/false)

**Response:**
```json
{
  "data": [
    {
      "id": "string",
      "email": "string",
      "firstName": "string",
      "lastName": "string",
      "role": "string",
      "isActive": "boolean",
      "createdAt": "datetime",
      "lastLogin": "datetime"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 75,
    "totalPages": 2,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

**Errors:** 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 500 (Internal Server Error)

### GET /api/v1/checkout/payments?page=1&pageSize=50&search=...&sortBy=...&sortDir=...

**Purpose:** Admin paginated payment listing with status filtering and date range queries
**Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 50, max: 100) - Number of items per page
- `search` (optional) - Text search across payment reference, customer name
- `sortBy` (optional, default: createdAt) - Field to sort by (createdAt, amount, status, customerName)
- `sortDir` (optional, default: desc) - Sort direction (asc, desc)
- `status` (optional) - Filter by payment status (pending, completed, failed, refunded)
- `dateFrom` (optional) - Filter payments from this date (ISO format)
- `dateTo` (optional) - Filter payments to this date (ISO format)
- `minAmount` (optional) - Filter by minimum amount
- `maxAmount` (optional) - Filter by maximum amount

**Response:**
```json
{
  "data": [
    {
      "id": "string",
      "reference": "string",
      "customerName": "string",
      "amount": "decimal",
      "status": "string",
      "createdAt": "datetime",
      "completedAt": "datetime",
      "quoteId": "string"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 200,
    "totalPages": 4,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

**Errors:** 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 500 (Internal Server Error)

### GET /api/v1/driver-jobs/drivers?page=1&pageSize=50&search=...&sortBy=...&sortDir=...

**Purpose:** Admin driver listing with availability and assignment filtering
**Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 50, max: 100) - Number of items per page
- `search` (optional) - Text search across driver name, email, phone
- `sortBy` (optional, default: name) - Field to sort by (name, email, createdAt, lastActive)
- `sortDir` (optional, default: asc) - Sort direction (asc, desc)
- `isAvailable` (optional) - Filter by availability status (true/false)
- `hasActiveJobs` (optional) - Filter by active job assignment (true/false)

**Response:**
```json
{
  "data": [
    {
      "id": "string",
      "name": "string",
      "email": "string",
      "phone": "string",
      "isAvailable": "boolean",
      "activeJobsCount": "integer",
      "createdAt": "datetime",
      "lastActive": "datetime"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 25,
    "totalPages": 1,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

**Errors:** 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 500 (Internal Server Error)

### GET /api/v1/admin/dashboard/metrics

**Purpose:** Real-time dashboard metrics aggregation for admin overview
**Parameters:** None

**Response:**
```json
{
  "quotes": {
    "total": 150,
    "pending": 25,
    "confirmed": 100,
    "completed": 20,
    "cancelled": 5
  },
  "payments": {
    "totalAmount": 125000.00,
    "completedAmount": 100000.00,
    "pendingAmount": 25000.00,
    "totalTransactions": 120,
    "successRate": 95.5
  },
  "drivers": {
    "total": 25,
    "available": 15,
    "busy": 10,
    "utilizationRate": 40.0
  },
  "users": {
    "total": 75,
    "active": 60,
    "newThisMonth": 10,
    "growthRate": 15.4
  },
  "lastUpdated": "2025-09-27T10:30:00Z"
}
```

**Errors:** 401 (Unauthorized), 403 (Forbidden), 500 (Internal Server Error)
