FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish ./DlmsMqttClientGrpc.Presentation/DlmsMqttClientGrpc.Presentation.csproj \
    -c Release -r linux-x64 --self-contained false \
    -p:PublishSingleFile=true -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled
WORKDIR /app

COPY --from=build /app/out ./

ENTRYPOINT ["./DlmsMqttClientGrpc.Presentation"]
