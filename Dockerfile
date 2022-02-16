FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
#COPY ["Serialino/Serialino.csproj", "Serialino/"]
COPY ["Serialino.csproj", "Serialino/"]
RUN dotnet restore "Serialino/Serialino.csproj"
#COPY . .
COPY . Serialino/
WORKDIR "/src/Serialino"
RUN dotnet build "Serialino.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Serialino.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Serialino.dll"]