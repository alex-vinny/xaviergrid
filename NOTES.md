# Dynamic MongoDB API - Comprehensive Tests

## Project: XavierGrid
Honor to Sainth Francis Xavier 12/03 -- the day this project started

## Environment Variables

The application supports the following environment variables for configuration:

- `PORT` - The port the application will listen on (default: 5000)
- `MONGODB_CONNECTION` - The MongoDB connection string (default: mongodb://localhost:27017)

This document provides comprehensive tests for all features of the Dynamic MongoDB API, with examples using objects and models to validate functionality.

## 1. Schema Management

### Creating Schemas with Validation

```http
POST /schemas
Content-Type: application/json

[
  {
    "namespace": "Test",
    "entity": "Users",
    "fields": [
      {
        "name": "FirstName",
        "type": "string",
        "required": true
      },
      {
        "name": "LastName",
        "type": "string",
        "required": true
      },
      {
        "name": "Email",
        "type": "string",
        "required": true
      }
    ],
    "rules": [
      {
        "action": "create",
        "field": "Email",
        "allowedValues": ["^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"],
        "message": "Invalid email format"
      }
    ]
  }
]
```

Expected Response:
```json
{
  "message": "1 schemas created successfully"
}
```

Note: The schema will be automatically converted to lowercase names:
- namespace: "test"
- entity: "users"
- fields: "firstname", "lastname", "email"

### Creating a Single Schema with Reserved Field Name (Should Fail)

```http
POST /schemas
Content-Type: application/json

{
  "namespace": "test",
  "entity": "products",
  "fields": [
    {
      "name": "id",
      "type": "string",
      "required": true
    },
    {
      "name": "name",
      "type": "string",
      "required": true
    }
  ]
}
```

Expected Response:
```json
{
  "error": "An error occurred while creating schemas: Field name 'id' is reserved and cannot be used"
}
```

### Creating a Single Schema with Rules

```http
POST /schemas
Content-Type: application/json

{
  "namespace": "test",
  "entity": "products",
  "fields": [
    {
      "name": "name",
      "type": "string",
      "required": true
    },
    {
      "name": "price",
      "type": "decimal",
      "required": true
    },
    {
      "name": "status",
      "type": "string",
      "enum": ["active", "inactive", "discontinued"]
    }
  ],
  "rules": [
    {
      "action": "create",
      "field": "price",
      "allowedValues": [">0"],
      "message": "Price must be greater than zero"
    },
    {
      "action": "update",
      "field": "status",
      "allowedValues": ["active", "inactive"],
      "message": "Cannot set status to discontinued directly"
    }
  ]
}
```

Expected Response:
```json
{
  "message": "Schema created successfully"
}
```

## 2. CRUD Operations

### Create Document

```http
POST /users
Content-Type: application/json

{
  "firstname": "John",
  "lastname": "Doe",
  "email": "john.doe@example.com"
}
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439011",
  "firstname": "John",
  "lastname": "Doe",
  "email": "john.doe@example.com",
  "isdeleted": false,
  "createdat": "2023-01-01T00:00:00.000Z",
  "updatedat": "2023-01-01T00:00:00.000Z"
}
```

### Get Document

```http
GET /users/507f1f77bcf86cd799439011
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439011",
  "firstname": "John",
  "lastname": "Doe",
  "email": "john.doe@example.com",
  "isdeleted": false,
  "createdat": "2023-01-01T00:00:00.000Z",
  "updatedat": "2023-01-01T00:00:00.000Z"
}
```

### Update Document

```http
PUT /users/507f1f77bcf86cd799439011
Content-Type: application/json

{
  "firstname": "Jane"
}
```

Expected Response:
```json
{
  "message": "Document updated successfully"
}
```

### Delete Document (Soft Delete)

```http
DELETE /users/507f1f77bcf86cd799439011
```

Expected Response:
```json
{
  "message": "Document deleted successfully"
}
```

## 3. Soft Delete and Restore

### Restore Document

```http
POST /users/507f1f77bcf86cd799439011/restore
```

Expected Response:
```json
{
  "message": "Document restored successfully"
}
```

## 4. Document Purge

### Purge All Documents

```http
POST /users/purge
```

Expected Response:
```json
{
  "message": "All documents purged for entity 'users'"
}
```

## 5. History/Versioning

### Get Document History

```http
GET /users/507f1f77bcf86cd799439011/history?page=1&pageSize=10
```

Expected Response:
```json
{
  "total": 3,
  "page": 1,
  "pageSize": 10,
  "data": [
    {
      "_id": "507f1f77bcf86cd799439012",
      "documentid": "507f1f77bcf86cd799439011",
      "version": 3,
      "data": {
        "_id": "507f1f77bcf86cd799439011",
        "firstname": "Jane",
        "lastname": "Doe",
        "email": "john.doe@example.com",
        "isdeleted": false,
        "createdat": "2023-01-01T00:00:00.000Z",
        "updatedat": "2023-01-02T00:00:00.000Z"
      },
      "timestamp": "2023-01-02T00:00:00.000Z",
      "action": "update"
    }
  ]
}
```

## 6. Multi-level Joins

### Create Related Documents

```http
POST /users
Content-Type: application/json

{
  "_id": "507f1f77bcf86cd799439011",
  "firstname": "John",
  "lastname": "Doe",
  "email": "john.doe@example.com"
}
```

```http
POST /invoices
Content-Type: application/json

{
  "userid": "507f1f77bcf86cd799439011",
  "amount": 100.50,
  "status": "paid"
}
```

### Get Document with Relations

```http
GET /invoices/{invoiceId}
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439013",
  "userid": "507f1f77bcf86cd799439011",
  "amount": 100.50,
  "status": "paid",
  "isdeleted": false,
  "user": [
    {
      "_id": "507f1f77bcf86cd799439011",
      "firstname": "John",
      "lastname": "Doe",
      "email": "john.doe@example.com",
      "isdeleted": false
    }
  ]
}
```

## 7. Virtual Fields

### Schema with Virtual Fields

```http
POST /schemas
Content-Type: application/json

{
  "namespace": "test",
  "entity": "employees",
  "fields": [
    {
      "name": "firstname",
      "type": "string",
      "required": true
    },
    {
      "name": "lastname",
      "type": "string",
      "required": true
    }
  ],
  "virtualFields": [
    {
      "name": "fullname",
      "expression": "firstname + ' ' + lastname"
    }
  ]
}
```

### Create Document with Virtual Fields

```http
POST /employees
Content-Type: application/json

{
  "firstname": "John",
  "lastname": "Doe"
}
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439014",
  "firstname": "John",
  "lastname": "Doe",
  "isdeleted": false,
  "fullname": "John Doe"
}
```

## 8. Schema-driven Validation

### Schema with Enums

```http
POST /schemas
Content-Type: application/json

{
  "namespace": "test",
  "entity": "orders",
  "fields": [
    {
      "name": "status",
      "type": "string",
      "enum": ["pending", "processing", "shipped", "delivered"]
    }
  ]
}
```

### Create Document with Valid Enum

```http
POST /orders
Content-Type: application/json

{
  "status": "pending"
}
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439015",
  "status": "pending",
  "isdeleted": false
}
```

### Create Document with Invalid Enum (Should Fail)

```http
POST /orders
Content-Type: application/json

{
  "status": "invalid"
}
```

Expected Response:
```json
{
  "error": "Field 'status' must be one of: pending, processing, shipped, delivered"
}
```

## 9. Field Functions

### Schema with Field Functions

```http
POST /schemas
Content-Type: application/json

{
  "namespace": "test",
  "entity": "accounts",
  "fields": [
    {
      "name": "email",
      "type": "string",
      "function": "lowercase"
    },
    {
      "name": "password",
      "type": "string",
      "function": "bcrypt"
    }
  ]
}
```

### Create Document with Field Functions

```http
POST /accounts
Content-Type: application/json

{
  "email": "USER@EXAMPLE.COM",
  "password": "secretpassword"
}
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439016",
  "email": "user@example.com",
  "password": "$2b$10$...", // bcrypt hashed password
  "isdeleted": false
}
```

## 10. Dynamic Search

### Simple Search

```http
POST /users/search
Content-Type: application/json

{
  "firstname": "John"
}
```

### Advanced Search with Operators

```http
POST /users/search
Content-Type: application/json

{
  "age": {
    "$gte": 18,
    "$lte": 65
  },
  "status": {
    "$in": ["active", "pending"]
  }
}
```

## 11. Dynamic Aggregation

### Aggregation Pipeline

```http
POST /users/aggregate
Content-Type: application/json

[
  {
    "$match": {
      "status": "active"
    }
  },
  {
    "$group": {
      "_id": "$department",
      "count": { "$sum": 1 }
    }
  }
]
```

Expected Response:
```json
[
  {
    "_id": "engineering",
    "count": 15
  },
  {
    "_id": "marketing",
    "count": 8
  }
]
```

## 12. Date Functions

### Schema with Date Functions

```http
POST /schemas
Content-Type: application/json

{
  "namespace": "test",
  "entity": "events",
  "fields": [
    {
      "name": "title",
      "type": "string",
      "required": true
    },
    {
      "name": "createdat",
      "type": "date",
      "function": "now"
    },
    {
      "name": "updatedat",
      "type": "date",
      "function": "now"
    }
  ]
}
```

### Create Document with Date Functions

```http
POST /events
Content-Type: application/json

{
  "title": "Team Meeting"
}
```

Expected Response:
```json
{
  "_id": "507f1f77bcf86cd799439017",
  "title": "Team Meeting",
  "isdeleted": false,
  "createdat": "2023-01-01T10:00:00.000Z",
  "updatedat": "2023-01-01T10:00:00.000Z"
}
```

## 13. System Controller

### Get Available Functions

```http
GET /system/functions
```

Expected Response:
```json
[
  {
    "name": "bcrypt",
    "description": "Hashes a string value using bcrypt algorithm",
    "type": "string",
    "parameters": [],
    "example": {
      "function": "bcrypt"
    }
  },
  {
    "name": "lowercase",
    "description": "Converts a string value to lowercase",
    "type": "string",
    "parameters": [],
    "example": {
      "function": "lowercase"
    }
  },
  {
    "name": "uppercase",
    "description": "Converts a string value to uppercase",
    "type": "string",
    "parameters": [],
    "example": {
      "function": "uppercase"
    }
  },
  {
    "name": "date",
    "description": "Sets the field to the current UTC date and time",
    "type": "date",
    "parameters": [],
    "example": {
      "function": "date"
    }
  },
  {
    "name": "now",
    "description": "Sets the field to the current UTC date and time (alias for date)",
    "type": "date",
    "parameters": [],
    "example": {
      "function": "now"
    }
  }
]
```

### Get API Version

```http
GET /system/version
```

Expected Response:
```json
{
  "version": "1.0.0",
  "name": "Dynamic MongoDB API",
  "description": "A modern .NET 8+ Web API with dynamic MongoDB operations"
}
```

## 14. Rules and Relations Endpoints

### Get Schema Rules

```http
GET /schemas/users/rules
```

Expected Response:
```json
[
  {
    "action": "create",
    "field": "email",
    "allowedValues": ["^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"],
    "message": "Invalid email format"
  }
]
```

### Get Schema Relations

```http
GET /schemas/users/relations
```

Expected Response:
```json
[
  {
    "collection": "orders",
    "foreignField": "userid",
    "as": "userOrders"
  }
]
```

### Add Rules to Schema

```http
POST /schemas/users/rules
Content-Type: application/json

[
  {
    "action": "update",
    "field": "status",
    "allowedValues": ["active", "inactive"],
    "message": "Status must be either active or inactive"
  }
]
```

Expected Response:
```json
{
  "message": "1 rules added successfully"
}
```

### Add Relations to Schema

```http
POST /schemas/users/relations
Content-Type: application/json

[
  {
    "collection": "profiles",
    "foreignField": "userid",
    "as": "userProfile"
  }
]
```

Expected Response:
```json
{
  "message": "1 relations added successfully"
}
```

### Delete All Schema Rules

```http
DELETE /schemas/users/rules
```

Expected Response:
```json
{
  "message": "All rules deleted successfully"
}
```

### Delete All Schema Relations

```http
DELETE /schemas/users/relations
```

Expected Response:
```json
{
  "message": "All relations deleted successfully"
}
```