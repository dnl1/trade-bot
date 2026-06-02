FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
RUN mkdir -p /app/logs && \
    groupadd -r tradebot && \
    useradd -r -g tradebot -s /sbin/nologin tradebot && \
    chown -R tradebot:tradebot /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
USER tradebot
ENTRYPOINT ["dotnet", "TradeBot.dll"]
