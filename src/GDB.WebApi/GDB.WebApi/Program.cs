using GDB.App.Application.Services.Contracts;
using GDB.App.Application.Services.Implementations;
using GDB.App.Infrastructure.Repositories.Contracts;
using GDB.App.Infrastructure.Repositories.Implementations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<AccountRepositoryDB>();
builder.Services.AddScoped<IAccountRepository>(services => services.GetRequiredService<AccountRepositoryDB>());
builder.Services.AddScoped<ITransactionRepository, TransactionRepositoryDB>();
builder.Services.AddScoped<IMoneyMovementSessionFactory, SqlMoneyMovementSessionFactory>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<DepositTransactionCommand>();
builder.Services.AddScoped<WithdrawTransactionCommand>();
builder.Services.AddScoped<TransferTransactionCommand>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<GDB.App.Application.Controllers.AccountController>();
builder.Services.AddTransient<GDB.App.Application.Controllers.TransactionController>();

var app = builder.Build();
_ = app.Services.GetRequiredService<IDbConnectionFactory>(); // fail early when API SQL config is absent

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
