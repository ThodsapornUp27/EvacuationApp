using EvacuationApp.Entities.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

// Add DbContext to the container
builder.Services.AddDbContext<EvacuationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Connect Redis Cloud
//builder.Services.AddSingleton<IConnectionMultiplexer>(s =>
//{
//    var configuration = new ConfigurationOptions
//    {
//        EndPoints = { "redis-13392.c56.east-us.azure.redns.redis-cloud.com:13392" },
//        User = "default",
//        Password = "t7txC9w4bxDO0iy4IV7HtStW6zRtPGDz",
//        Ssl = true,
//        AbortOnConnectFail = false
//    };

//    return ConnectionMultiplexer.Connect(configuration);
//});

//https://stackoverflow.com/questions/68655350/stackexchange-redis-dependency-injection-of-idatabase
builder.Services.AddScoped<IDatabase>(cfg =>
{
    var configuration = new ConfigurationOptions
    {
        EndPoints = { "redis-13392.c56.east-us.azure.redns.redis-cloud.com:13392" },
        User = "default",
        Password = "t7txC9w4bxDO0iy4IV7HtStW6zRtPGDz",
    };

    IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(configuration);
    return multiplexer.GetDatabase();
});


builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
