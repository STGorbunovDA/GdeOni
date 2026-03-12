FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/GdeOni.API/GdeOni.API.csproj", "src/GdeOni.API/"]
COPY ["src/GdeOni.Application/GdeOni.Application.csproj", "src/GdeOni.Application/"]
COPY ["src/GdeOni.Domain/GdeOni.Domain.csproj", "src/GdeOni.Domain/"]
COPY ["src/GdeOni.Infrastructure/GdeOni.Infrastructure.csproj", "src/GdeOni.Infrastructure/"]
RUN dotnet restore "src/GdeOni.API/GdeOni.API.csproj"
COPY . .
WORKDIR "/src/src/GdeOni.API"
RUN dotnet build "GdeOni.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GdeOni.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GdeOni.API.dll"]
