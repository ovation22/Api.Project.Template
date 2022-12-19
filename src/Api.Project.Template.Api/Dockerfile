FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apk --no-cache add curl
HEALTHCHECK --interval=5s --timeout=10s --retries=3 CMD curl --silent --fail http://localhost/healthz/ready || exit 1

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
COPY ["src/Api.Project.Template.Api/Api.Project.Template.Api.csproj", "src/Api.Project.Template.Api/"]
RUN dotnet restore "src/Api.Project.Template.Api/Api.Project.Template.Api.csproj"
COPY . .
WORKDIR "/src/src/Api.Project.Template.Api"
RUN dotnet build "Api.Project.Template.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.Project.Template.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false -r linux-musl-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.Project.Template.Api.dll"]
