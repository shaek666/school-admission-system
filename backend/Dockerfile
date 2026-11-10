FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY SchoolAdmission.sln Directory.Build.props ./
COPY src/SchoolAdmission.Api/SchoolAdmission.Api.csproj src/SchoolAdmission.Api/
COPY src/SchoolAdmission.Business/SchoolAdmission.Business.csproj src/SchoolAdmission.Business/
COPY src/SchoolAdmission.DataAccess/SchoolAdmission.DataAccess.csproj src/SchoolAdmission.DataAccess/
COPY tests/SchoolAdmission.Api.Tests/SchoolAdmission.Api.Tests.csproj tests/SchoolAdmission.Api.Tests/
COPY tests/SchoolAdmission.Business.Tests/SchoolAdmission.Business.Tests.csproj tests/SchoolAdmission.Business.Tests/
RUN dotnet restore SchoolAdmission.sln

FROM restore AS build
COPY . .
RUN dotnet build SchoolAdmission.sln -c Release --no-restore

FROM build AS test
ENTRYPOINT ["dotnet", "test", "SchoolAdmission.sln", "-c", "Release", "--no-build"]

FROM restore AS publish
COPY src/ src/
RUN dotnet publish src/SchoolAdmission.Api/SchoolAdmission.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
