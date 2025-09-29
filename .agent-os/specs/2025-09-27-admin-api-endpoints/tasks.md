# Spec Tasks

## Tasks

- [ ] 1. Admin Quote Listing API Implementation
  - [ ] 1.1 Write tests for admin quote listing endpoint with pagination, filtering, and sorting
  - [ ] 1.2 Extend QuoteController with admin query parameters (admin=true, page, pageSize, search, sortBy, sortDir)
  - [ ] 1.3 Implement pagination logic with configurable page sizes and metadata
  - [ ] 1.4 Add search functionality across quote reference, customer name, and driver name
  - [ ] 1.5 Implement filtering by status, date range, and driver assignment
  - [ ] 1.6 Add multi-field sorting with direction control (asc/desc)
  - [ ] 1.7 Create standardized response format with pagination metadata
  - [ ] 1.8 Verify all tests pass and endpoint returns expected data structure

- [ ] 2. Admin User Management API Implementation
  - [ ] 2.1 Write tests for admin user listing endpoint with pagination and search
  - [ ] 2.2 Extend AuthController with admin user listing endpoint
  - [ ] 2.3 Implement pagination for user listings with configurable page sizes
  - [ ] 2.4 Add search functionality across email, firstName, and lastName
  - [ ] 2.5 Implement filtering by role and active status
  - [ ] 2.6 Add sorting by creation date, email, name, and last login
  - [ ] 2.7 Create standardized response format matching quote API structure
  - [ ] 2.8 Verify all tests pass and endpoint integrates with existing auth system

- [ ] 3. Admin Payment Dashboard API Implementation
  - [ ] 3.1 Write tests for admin payment listing endpoint with date filtering
  - [ ] 3.2 Extend CheckoutController with admin payment listing endpoint
  - [ ] 3.3 Implement pagination for payment listings with metadata
  - [ ] 3.4 Add search functionality across payment reference and customer name
  - [ ] 3.5 Implement filtering by payment status, date range, and amount range
  - [ ] 3.6 Add sorting by creation date, amount, status, and customer name
  - [ ] 3.7 Create standardized response format with payment-specific fields
  - [ ] 3.8 Verify all tests pass and endpoint handles payment data correctly

- [ ] 4. Admin Driver Management API Implementation
  - [ ] 4.1 Write tests for admin driver listing endpoint with availability filtering
  - [ ] 4.2 Extend DriverJobsController with admin driver listing endpoint
  - [ ] 4.3 Implement pagination for driver listings with metadata
  - [ ] 4.4 Add search functionality across driver name, email, and phone
  - [ ] 4.5 Implement filtering by availability status and active job assignments
  - [ ] 4.6 Add sorting by name, email, creation date, and last active
  - [ ] 4.7 Create standardized response format with driver-specific fields
  - [ ] 4.8 Verify all tests pass and endpoint integrates with driver job system

- [ ] 5. Dashboard Metrics API Implementation
  - [ ] 5.1 Write tests for dashboard metrics aggregation endpoint
  - [ ] 5.2 Create new AdminController for dashboard metrics
  - [ ] 5.3 Implement quote metrics aggregation (total, pending, confirmed, completed, cancelled)
  - [ ] 5.4 Implement payment metrics aggregation (total amount, success rate, transaction counts)
  - [ ] 5.5 Implement driver metrics aggregation (total, available, busy, utilization rate)
  - [ ] 5.6 Implement user metrics aggregation (total, active, new this month, growth rate)
  - [ ] 5.7 Create comprehensive metrics response format with lastUpdated timestamp
  - [ ] 5.8 Verify all tests pass and metrics calculations are accurate
