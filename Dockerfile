FROM mcr.microsoft.com/dotnet/sdk:3.1 as build
COPY . /usr/src/app
WORKDIR /usr/src/app
RUN cd /usr/src/app && \
  mkdir -p /usr/src/build && \
  dotnet restore ./Clout.Cast.sln --&& \
  dotnet restore ./src/Api/CloutCast.Api.csproj && \
  dotnet build \
    --configuration Release \
    ./Clout.Cast.sln && \
  dotnet publish -c Release \
    -o /usr/src/build \
    ./src/Api/CloutCast.Api.csproj


FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /usr/src/app
COPY --from=build /usr/src/build/ /usr/src/app
CMD ["dotnet", "/usr/src/app/CloutCast.Api.dll"]