FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Api.Project.Template.Api/Api.Project.Template.Api.csproj", "src/Api.Project.Template.Api/"]
RUN dotnet restore "src/Api.Project.Template.Api/Api.Project.Template.Api.csproj"
COPY . .
WORKDIR "/src/src/Api.Project.Template.Api"
RUN dotnet build "Api.Project.Template.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.Project.Template.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.Project.Template.Api.dll"]
