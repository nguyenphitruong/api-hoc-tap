# 08/10/2026 - HocTap API (.NET 8) — build cho Render (giống Web-MultiClinic, không cần thư viện báo cáo)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . /src
RUN dotnet restore "1.HocTap.WebApi/1.HocTap.WebApi.csproj"
RUN dotnet publish "1.HocTap.WebApi/1.HocTap.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV TZ=Asia/Ho_Chi_Minh \
    ASPNETCORE_URLS=http://+:10000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
COPY --from=build /app/publish .
EXPOSE 10000
ENTRYPOINT ["dotnet", "HocTap.WebApi.dll"]
