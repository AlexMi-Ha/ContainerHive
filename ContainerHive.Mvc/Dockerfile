#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ContainerHive.Mvc/ContainerHive.Mvc.csproj", "ContainerHive.Mvc/"]
COPY ["ContainerHive.Core/ContainerHive.Core.csproj", "ContainerHive.Core/"]
COPY ["ContainerHive.Validation/ContainerHive.Validation.csproj", "ContainerHive.Validation/"]
RUN dotnet restore "ContainerHive.Mvc/ContainerHive.Mvc.csproj"
COPY . .
WORKDIR "/src/ContainerHive.Mvc"
RUN dotnet build "ContainerHive.Mvc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContainerHive.Mvc.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContainerHive.Mvc.dll"]