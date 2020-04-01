#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/CalendarExtractor.API/CalendarExtractor.API.csproj", "src/CalendarExtractor.API/"]
RUN dotnet restore "src/CalendarExtractor.API/CalendarExtractor.API.csproj"
COPY . .
WORKDIR "/src/src/CalendarExtractor.API"
RUN dotnet build "CalendarExtractor.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CalendarExtractor.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CalendarExtractor.API.dll"]


#
#FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
#WORKDIR /app
#
#EXPOSE 443
#EXPOSE 5001
#
## Copy csproj and restore as distinct layers
#COPY *.csproj ./
#RUN dotnet restore
#
## Copy everything else and build
#COPY . ./
#RUN dotnet publish -c Release -o out
#
## Build runtime image
#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
#WORKDIR /app
#COPY --from=build-env /app/out .
#ENTRYPOINT ["dotnet", "CalendarExtractor.API.dll"]