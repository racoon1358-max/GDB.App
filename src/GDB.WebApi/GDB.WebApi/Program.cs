using GDB.WebApi.Application.Service.Implementation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Singletons keep the dummy in-memory data available between Swagger calls.
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<TransactionService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
