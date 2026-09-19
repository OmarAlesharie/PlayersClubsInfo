# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project file first for better Docker layer caching
COPY ["PlayersClubsInfo/PlayersClubsInfo.csproj", "PlayersClubsInfo/"]

RUN dotnet restore "PlayersClubsInfo/PlayersClubsInfo.csproj"

# Copy the remaining source
COPY . .

WORKDIR "/src/PlayersClubsInfo"

RUN dotnet publish "PlayersClubsInfo.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "PlayersClubsInfo.dll"]