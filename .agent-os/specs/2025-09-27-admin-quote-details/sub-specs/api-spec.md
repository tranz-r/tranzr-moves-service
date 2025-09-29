# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-09-27-admin-quote-details/spec.md

## Endpoints

### GET /api/v1/quote/{id}/admin?admin=true

**Purpose:** Retrieve complete quote details for admin users with all related information
**Parameters:** 
- `id` (required) - Quote ID (UUID)
- `admin=true` (required) - Enables admin-specific features

**Response:** 
```json
{
  "id": "string",
  "quoteReference": "string",
  "type": "string",
  "status": "string",
  "totalCost": "decimal",
  "baseCost": "decimal",
  "additionalPaymentsTotal": "decimal",
  "paymentStatus": "string",
  "paymentMethod": "string",
  "createdAt": "datetime",
  "modifiedAt": "datetime",
  "createdBy": "string",
  "modifiedBy": "string",
  "customer": {
    "id": "string",
    "fullName": "string",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "phone": "string",
    "role": "string",
    "createdAt": "datetime",
    "lastLoginAt": "datetime"
  },
  "driver": {
    "id": "string",
    "fullName": "string",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "phone": "string",
    "role": "string",
    "availability": "string",
    "assignedAt": "datetime",
    "vehicleInfo": "string"
  },
  "origin": {
    "id": "string",
    "line1": "string",
    "line2": "string",
    "city": "string",
    "postcode": "string",
    "country": "string",
    "coordinates": {
      "latitude": "decimal",
      "longitude": "decimal"
    }
  },
  "destination": {
    "id": "string",
    "line1": "string",
    "line2": "string",
    "city": "string",
    "postcode": "string",
    "country": "string",
    "coordinates": {
      "latitude": "decimal",
      "longitude": "decimal"
    }
  },
  "inventoryItems": [
    {
      "id": "string",
      "name": "string",
      "description": "string",
      "quantity": "integer",
      "weight": "decimal",
      "dimensions": {
        "length": "decimal",
        "width": "decimal",
        "height": "decimal"
      },
      "fragile": "boolean",
      "requiresDismantling": "boolean",
      "requiresAssembly": "boolean"
    }
  ],
  "additionalPayments": [
    {
      "id": "string",
      "amount": "decimal",
      "description": "string",
      "paymentMethodId": "string",
      "paymentIntentId": "string",
      "receiptUrl": "string",
      "createdAt": "datetime",
      "status": "string"
    }
  ],
  "paymentHistory": [
    {
      "id": "string",
      "amount": "decimal",
      "status": "string",
      "paymentMethod": "string",
      "paymentIntentId": "string",
      "receiptUrl": "string",
      "processedAt": "datetime",
      "failureReason": "string"
    }
  ],
  "timeline": [
    {
      "id": "string",
      "action": "string",
      "description": "string",
      "performedBy": "string",
      "performedAt": "datetime",
      "metadata": "object"
    }
  ],
  "serviceDetails": {
    "vanType": "string",
    "driverCount": "integer",
    "estimatedHours": "decimal",
    "collectionDate": "datetime",
    "deliveryDate": "datetime",
    "flexibleTime": "boolean",
    "timeSlot": "string",
    "distanceMiles": "decimal",
    "pricingTier": "string"
  },
  "adminNotes": [
    {
      "id": "string",
      "note": "string",
      "createdBy": "string",
      "createdAt": "datetime",
      "isInternal": "boolean"
    }
  ]
}
```

**Errors:** 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 500 (Internal Server Error)

### PUT /api/v1/quote/{id}/admin/status?admin=true

**Purpose:** Update quote status for administrative purposes
**Parameters:** 
- `id` (required) - Quote ID (UUID)
- `admin=true` (required) - Enables admin-specific features

**Request Body:**
```json
{
  "status": "string",
  "reason": "string",
  "adminNote": "string",
  "notifyCustomer": "boolean",
  "notifyDriver": "boolean"
}
```

**Response:** 
```json
{
  "success": "boolean",
  "message": "string",
  "updatedQuote": {
    "id": "string",
    "status": "string",
    "modifiedAt": "datetime",
    "modifiedBy": "string"
  }
}
```

### PUT /api/v1/quote/{id}/admin/driver?admin=true

**Purpose:** Assign or reassign driver to quote
**Parameters:** 
- `id` (required) - Quote ID (UUID)
- `admin=true` (required) - Enables admin-specific features

**Request Body:**
```json
{
  "driverId": "string",
  "reason": "string",
  "adminNote": "string",
  "notifyDriver": "boolean",
  "notifyCustomer": "boolean"
}
```

**Response:** 
```json
{
  "success": "boolean",
  "message": "string",
  "updatedQuote": {
    "id": "string",
    "driverId": "string",
    "driverName": "string",
    "assignedAt": "datetime",
    "assignedBy": "string"
  }
}
```

### POST /api/v1/quote/{id}/admin/notes?admin=true

**Purpose:** Add administrative notes to quote
**Parameters:** 
- `id` (required) - Quote ID (UUID)
- `admin=true` (required) - Enables admin-specific features

**Request Body:**
```json
{
  "note": "string",
  "isInternal": "boolean",
  "category": "string"
}
```

**Response:** 
```json
{
  "success": "boolean",
  "message": "string",
  "note": {
    "id": "string",
    "note": "string",
    "createdBy": "string",
    "createdAt": "datetime",
    "isInternal": "boolean",
    "category": "string"
  }
}
```

### GET /api/v1/quote/{id}/admin/history?admin=true&page=1&pageSize=50

**Purpose:** Retrieve quote modification history and audit trail
**Parameters:** 
- `id` (required) - Quote ID (UUID)
- `admin=true` (required) - Enables admin-specific features
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 50, max: 100) - Number of items per page

**Response:** 
```json
{
  "data": [
    {
      "id": "string",
      "action": "string",
      "description": "string",
      "performedBy": "string",
      "performedAt": "datetime",
      "metadata": "object",
      "changes": [
        {
          "field": "string",
          "oldValue": "string",
          "newValue": "string"
        }
      ]
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

### GET /api/v1/quote/{id}/admin/analytics?admin=true

**Purpose:** Retrieve quote analytics and performance metrics
**Parameters:** 
- `id` (required) - Quote ID (UUID)
- `admin=true` (required) - Enables admin-specific features

**Response:** 
```json
{
  "quoteId": "string",
  "metrics": {
    "totalViews": "integer",
    "customerInteractions": "integer",
    "driverInteractions": "integer",
    "paymentAttempts": "integer",
    "statusChanges": "integer",
    "averageResponseTime": "decimal",
    "customerSatisfactionScore": "decimal"
  },
  "timeline": {
    "createdAt": "datetime",
    "firstPaymentAttempt": "datetime",
    "lastPaymentAttempt": "datetime",
    "driverAssignedAt": "datetime",
    "completedAt": "datetime"
  },
  "performance": {
    "quoteToPaymentTime": "decimal",
    "paymentToCompletionTime": "decimal",
    "totalProcessingTime": "decimal",
    "efficiencyScore": "decimal"
  }
}
```

## Data Models

### AdminQuoteDetailsDto
```csharp
public record AdminQuoteDetailsDto(
    Guid Id,
    string QuoteReference,
    string Type,
    string Status,
    decimal TotalCost,
    decimal BaseCost,
    decimal AdditionalPaymentsTotal,
    string PaymentStatus,
    string PaymentMethod,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    string CreatedBy,
    string ModifiedBy,
    AdminCustomerDto Customer,
    AdminDriverDto? Driver,
    AdminAddressDto? Origin,
    AdminAddressDto? Destination,
    List<AdminInventoryItemDto> InventoryItems,
    List<AdminAdditionalPaymentDto> AdditionalPayments,
    List<AdminPaymentHistoryDto> PaymentHistory,
    List<AdminTimelineDto> Timeline,
    AdminServiceDetailsDto ServiceDetails,
    List<AdminNoteDto> AdminNotes);
```

### AdminCustomerDto
```csharp
public record AdminCustomerDto(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
```

### AdminDriverDto
```csharp
public record AdminDriverDto(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Role,
    string Availability,
    DateTimeOffset? AssignedAt,
    string? VehicleInfo);
```

## Security Considerations

- All endpoints require admin authentication
- Admin role verification for all operations
- Audit logging for all administrative actions
- Sensitive data encryption in transit and at rest
- Rate limiting for administrative endpoints
- Input validation and sanitization

## Performance Requirements

- Quote details endpoint: < 500ms response time
- Status update endpoint: < 200ms response time
- Driver assignment endpoint: < 300ms response time
- History endpoint: < 400ms response time
- Analytics endpoint: < 600ms response time

## Error Handling

- Comprehensive error responses with appropriate HTTP status codes
- Detailed error messages for debugging
- Graceful handling of missing or invalid data
- Proper exception handling and logging
- User-friendly error messages for frontend display


