FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Xcaciv.LooseNotes.Web/Xcaciv.LooseNotes.Web.csproj", "Xcaciv.LooseNotes.Web/"]
RUN dotnet restore "Xcaciv.LooseNotes.Web/Xcaciv.LooseNotes.Web.csproj"
COPY . .
WORKDIR "/src/Xcaciv.LooseNotes.Web"
RUN dotnet build "Xcaciv.LooseNotes.Web.csproj" -c Release -o /app/build
RUN dotnet publish "Xcaciv.LooseNotes.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Xcaciv.LooseNotes.Web.dll"]
