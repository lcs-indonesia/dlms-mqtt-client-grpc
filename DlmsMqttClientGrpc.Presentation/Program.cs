using DlmsMqttClientGrpc.Application.Services;
using DlmsMqttClientGrpc.Presentation.Extensions;
using DlmsMqttClientGrpc.Presentation.Interceptors;
using DlmsMqttClientGrpc.Services;

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
app.MapGrpcService<GreeterService>();
app.MapGrpcService<DlmsService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();