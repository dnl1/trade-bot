FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TradeBot/TradeBot.csproj", "TradeBot/"]
RUN dotnet restore "TradeBot/TradeBot.csproj"
COPY . .
WORKDIR "/src/TradeBot"
RUN dotnet build "TradeBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TradeBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TradeBot.dll"]