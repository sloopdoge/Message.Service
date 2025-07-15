FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Releas
WORKDIR /src
COPY ["Message.Application/Message.Application.csproj", "Message.Application/"]
COPY ["Message.Domain/Message.Domain.csproj", "Message.Domain/"]
COPY ["Message.Repository/Message.Repository.csproj", "Message.Repository/"]
RUN dotnet restore "Message.Application/Message.Application.csproj"
COPY . .
WORKDIR "/src/Message.Application"
RUN dotnet build "./Message.Application.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Message.Application.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Message.Application.dll"]
