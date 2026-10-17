FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY enx-fit/enx-fit.csproj enx-fit/
RUN dotnet restore enx-fit/enx-fit.csproj
COPY enx-fit/ enx-fit/
RUN dotnet publish enx-fit/enx-fit.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/App_Data/DataProtectionKeys && chown -R app:app /app/App_Data
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "enx-fit.dll"]
