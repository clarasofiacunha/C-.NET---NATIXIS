FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PersonalFinance.Api/PersonalFinance.Api.csproj", "PersonalFinance.Api/"]
RUN dotnet restore "PersonalFinance.Api/PersonalFinance.Api.csproj"

COPY . .
WORKDIR /src/PersonalFinance.Api
RUN dotnet publish "PersonalFinance.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

USER $APP_UID

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
ENTRYPOINT ["dotnet", "PersonalFinance.Api.dll"]
