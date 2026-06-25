FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/LoanTracker.Api/LoanTracker.Api.csproj", "src/LoanTracker.Api/"]
COPY ["src/LoanTracker.Application/LoanTracker.Application.csproj", "src/LoanTracker.Application/"]
COPY ["src/LoanTracker.Domain/LoanTracker.Domain.csproj", "src/LoanTracker.Domain/"]
COPY ["src/LoanTracker.Infrastructure/LoanTracker.Infrastructure.csproj", "src/LoanTracker.Infrastructure/"]

RUN dotnet restore "src/LoanTracker.Api/LoanTracker.Api.csproj"

COPY . .
WORKDIR "/src/src/LoanTracker.Api"
RUN dotnet build "LoanTracker.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LoanTracker.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LoanTracker.Api.dll"]
