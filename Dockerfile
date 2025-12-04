# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Set environment variables with default values
ENV PORT=5000
ENV MONGODB_CONNECTION=mongodb://localhost:27017

# Expose port
EXPOSE $PORT

# Run the application
ENTRYPOINT ["dotnet", "DynamicMongoAPI.dll"]