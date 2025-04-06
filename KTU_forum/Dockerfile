# Use the official .NET 7 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.sln ./
COPY KTU_forum/*.csproj ./KTU_forum/
RUN dotnet restore ./KTU_forum/KTU_forum.csproj

# Copy everything else and build the app
COPY . ./
WORKDIR /app/KTU_forum
RUN dotnet publish -c Release -o out



# Use a runtime-only image for production
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/KTU_forum/out ./
ENTRYPOINT ["dotnet", "KTU_forum.dll"]
