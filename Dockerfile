FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64 AS base
WORKDIR /app
RUN apk add --no-cache icu-libs icu-data-full dotnet6-runtime 
RUN apk add --no-cache libgdiplus 
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY BotDeScans.App/BotDeScans.App.csproj ./BotDeScans.App/
RUN dotnet restore "./BotDeScans.App/BotDeScans.App.csproj"
COPY . .
WORKDIR /src/BotDeScans.App

RUN dotnet build -c Release -o /app/build

FROM build as publish
RUN dotnet publish -c Release -o /app/publish

From base AS final
#FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BotDeScans.App.dll"]
