FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5004
ENV ASPNETCORE_URLS="http://+:5004"

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

ARG GITHUB_PAT

COPY ["src/Play.Inventory.Contracts/Play.Inventory.Contracts.csproj", "src/Play.Inventory.Contracts/"]
COPY ["src/Play.Inventory.Service/Play.Inventory.Service.csproj", "src/Play.Inventory.Service/"]
COPY ["src/Play.Inventory.Service/nuget.config", "src/Play.Inventory.Service/"]

RUN dotnet restore "src/Play.Inventory.Service/Play.Inventory.Service.csproj"

COPY ["./src", "./src"]

WORKDIR "/src/Play.Inventory.Service"
RUN dotnet publish "Play.Inventory.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Play.Inventory.Service.dll"]
