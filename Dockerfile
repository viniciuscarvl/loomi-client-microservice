FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ClientMicroservice.slnx ./
COPY src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj src/ClientMicroservice.Domain/
COPY src/ClientMicroservice.Application/ClientMicroservice.Application.csproj src/ClientMicroservice.Application/
COPY src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj src/ClientMicroservice.Infrastructure/
COPY src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj src/ClientMicroservice.Contracts/
COPY src/ClientMicroservice.API/ClientMicroservice.API.csproj src/ClientMicroservice.API/

RUN dotnet restore src/ClientMicroservice.API/ClientMicroservice.API.csproj

COPY . .
RUN dotnet publish src/ClientMicroservice.API/ClientMicroservice.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClientMicroservice.API.dll"]
