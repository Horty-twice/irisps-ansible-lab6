using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Выбираем конфиг Ocelot в зависимости от окружения
var env = builder.Environment.EnvironmentName;
var ocelotFile = env == "Docker" ? "ocelot.Docker.json" : "ocelot.json";

builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

var app = builder.Build();

await app.UseOcelot();

app.Run();