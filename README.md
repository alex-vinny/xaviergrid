# Dynamic MongoDB API

## Project: XavierGrid
Honor to Sainth Francis Xavier 12/03 -- the day this project started

A modern .NET 8+ Web API with dynamic MongoDB operations including CRUD, soft delete, history tracking, multi-level joins, and more.

## Features

- Full CRUD operations with namespaced databases
- Soft delete and restore functionality
- Document purge per schema
- History/versioning with pagination
- Multi-level joins (parent → child → grandchild)
- Virtual fields support
- Schema-driven validation, enums, and business rules
- Field functions (bcrypt, lowercase, etc.)
- Dynamic search with advanced query operators
- Dynamic aggregation with security controls
- Docker support for easy deployment

## Project Structure

```
DynamicMongoAPI/
├── Controllers/
│   ├── DynamicMongoController.cs
│   ├── SchemaController.cs
│   ├── SystemController.cs
│   └── SwaggerController.cs
├── Models/
│   └── EntitySchema.cs
├── Services/
│   ├── MongoSchemaService.cs
│   └── DynamicMongoService.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── Program.cs
├── DynamicMongoAPI.csproj
├── Dockerfile
├── README.md
└── NOTES.md
```

## Getting Started

1. Install .NET 8 SDK
2. Install Docker (for containerized deployment)
3. Configure MongoDB connection in `appsettings.json`
4. Run with `dotnet run` or using Docker

## Environment Variables

The application supports the following environment variables for configuration:

- `PORT` - The port the application will listen on (default: 5000)
- `MONGODB_CONNECTION` - The MongoDB connection string (default: mongodb://localhost:27017)

## API Endpoints

### Crud Entity Operations
- `POST /api/{entity}` - Create document
- `GET /api/{entity}/{id}` - Get document by ID
- `PUT/PATCH /api/{entity}/{id}` - Update document
- `DELETE /api/{entity}/{id}` - Soft delete document

### Entity History Operations
- `POST /api/{entity}/{id}/restore` - Restore deleted document
- `POST /api/{entity}/purge` - Purge all documents
- `GET /api/{entity}/{id}/history` - Get document history

### Entity Advanced Operations
- `POST /api/{entity}/search` - Dynamic search
- `POST /api/{entity}/aggregate` - Dynamic aggregation

### Schema Operations
- `POST /schemas` - Create schemas
- `GET /schemas` - Get all schemas
- `GET /schemas/{entity}` - Get schema by entity name

### Rules and Relations Operations
- `GET /schemas/{entity}/rules` - Get schema rules
- `GET /schemas/{entity}/relations` - Get schema relations
- `POST /schemas/{entity}/rules` - Add rules to schema
- `POST /schemas/{entity}/relations` - Add relations to schema
- `DELETE /schemas/{entity}/rules` - Delete all rules from schema
- `DELETE /schemas/{entity}/relations` - Delete all relations from schema

### System Operations
- `GET /system/functions` - Get available functions
- `GET /system/version` - Get API version

## Docker Support

```bash
# Build the image
docker build -t xavigrid-api .

# Run with environment variables
docker run -it --rm -p 5000:5000 -e PORT=5000 -e MONGODB_CONNECTION="mongodb://localhost:27017" xavigrid-api

# Run with volume mapping (Linux/Mac)
docker run -it --rm -v ${PWD}/data:/app/data -p 8080:80 xavigrid-api

# Run with volume mapping (Windows)
docker run -it --rm -v %cd%/data:/app/data -p 8080:80 xavigrid-api

# Run with environment variables
docker run -it --rm -p 5000:5000 -e PORT=5000 -e MONGODB_CONNECTION="mongodb://localhost:27017" xavigrid-api

# Run bash inside the container for debugging
docker run -it --rm xavigrid-api /bin/bash

# Complete command to debug
docker run -it --rm -w /app -v ${PWD}:/app/ -p 5000:5000 -e PORT=5000 -e MONGODB_CONNECTION="mongodb://localhost:27017" mcr.microsoft.com/dotnet/sdk:8.0 /bin/bash

# Development commands
# Clean, restore, and build the project
dotnet clean && dotnet restore && dotnet build

# Run the application
dotnet run
```