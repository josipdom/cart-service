FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Cart.Domain/Cart.Domain.csproj src/Cart.Domain/
COPY src/Cart.Application/Cart.Application.csproj src/Cart.Application/
COPY src/Cart.Infrastructure/Cart.Infrastructure.csproj src/Cart.Infrastructure/
COPY src/Cart.Api/Cart.Api.csproj src/Cart.Api/
RUN dotnet restore src/Cart.Api/Cart.Api.csproj

COPY src/ src/
RUN dotnet publish src/Cart.Api/Cart.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cart.Api.dll"]
