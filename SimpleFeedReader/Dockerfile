FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

ENV ASPNETCORE_URLS=http://+:5000

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["SimpleFeedReader/SimpleFeedReader.csproj", "SimpleFeedReader/"]
RUN dotnet restore "SimpleFeedReader/SimpleFeedReader.csproj"
COPY . .
WORKDIR "/src/SimpleFeedReader"
RUN dotnet build "SimpleFeedReader.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "SimpleFeedReader.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleFeedReader.dll"]
