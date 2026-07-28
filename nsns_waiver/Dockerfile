FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nsns_waiver.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish nsns_waiver.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "nsns_waiver.dll"]