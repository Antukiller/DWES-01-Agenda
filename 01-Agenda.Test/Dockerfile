FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["01-Agenda.Test/01-Agenda.Test.csproj", "01-Agenda.Test/"]
COPY ["01-Agenda/01-Agenda.csproj", "01-Agenda/"]
RUN dotnet restore "01-Agenda.Test/01-Agenda.Test.csproj"
COPY . .
WORKDIR "/src/01-Agenda.Test"
RUN dotnet build "./01-Agenda.Test.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ─── STAGE TEST: ejecuta la batería y genera el informe HTML ────
FROM build AS test
WORKDIR /src

RUN dotnet tool install --global dotnet-reportgenerator-globaltool
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet test "01-Agenda.Test/01-Agenda.Test.csproj" \
    --no-restore \
    --collect:"XPlat Code Coverage" \
    --logger "trx;LogFileName=TestResults.trx" \
    --results-directory /app/test-results \
    -c Release

RUN reportgenerator \
    -reports:"/app/test-results/**/coverage.cobertura.xml" \
    -targetdir:/app/test-results/html \
    -reporttypes:Html

# ─── STAGE TESTWEB: nginx sirve el informe de cobertura ─────────
FROM nginx:latest AS testweb
RUN rm -rf /usr/share/nginx/html/*
COPY --from=test /app/test-results/html /usr/share/nginx/html
