using DlmsMqttClientGrpc.Application.Services;
using DlmsMqttClientGrpc.Infrastructure.Grpc;
using DlmsMqttClientGrpc.Presentation.Extensions;
using DlmsMqttClientGrpc.Presentation.Interceptors;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogger();
builder.AddSettings();
builder.AddServices();

builder.Services.AddLogging();
builder.Services.AddGrpc(o =>
{
    o.Interceptors.Add<GrpcCatchExceptionInterceptor>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GrpcDlmsHandler>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

//change dlms response as Dictionary<key,values[]>
