using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

// Add SwaggerGen
builder.Services.AddSwaggerGen();


//ref https://stackoverflow.com/questions/68655350/stackexchange-redis-dependency-injection-of-idatabase
// Add redis
builder.Services.AddSingleton<IDatabase>(cfg =>
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

//ref https://stackoverflow.com/questions/75466179/c-sharp-serilog-config-in-asp-net-core-6
// Add Serilog
builder.Host.UseSerilog((ctx, services, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
