using SensorSimulator.Configuration;
using SensorSimulator.Models;
using SensorSimulator.Services;
using SensorSimulator.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:7070");

builder.Services.Configure<DatasetSettings>(builder.Configuration.GetSection("Dataset"));
builder.Services.Configure<EdgeXSettings>(builder.Configuration.GetSection("EdgeX"));

builder.Services.AddSingleton<SimulatorState>();
builder.Services.AddSingleton<SimulatorCommandService>();
builder.Services.AddHostedService<SensorReadingPublisherService>();

var app = builder.Build();

app.MapSimulatorEndpoints();

app.Run();
