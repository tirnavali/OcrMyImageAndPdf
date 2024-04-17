using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.Limits.MaxRequestBodySize = int.MaxValue);
builder.Services.Configure<FormOptions>(opt => opt.MultipartBodyLengthLimit = long.MaxValue);
builder.Services.Configure<KestrelServerOptions>(opt => opt.Limits.MaxRequestBodySize = int.MaxValue);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
