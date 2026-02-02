using TaskAgent.FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var useAzure = builder.Configuration.GetValue<bool>("FileStorage:UseAzure");
var azureConn = builder.Configuration["FileStorage:AzureConnectionString"] ?? builder.Configuration["ConnectionStrings:AzureStorage"];

if (useAzure && !string.IsNullOrEmpty(azureConn))
    builder.Services.AddSingleton<IFileStorageService, AzureBlobFileStorageService>();
else
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
