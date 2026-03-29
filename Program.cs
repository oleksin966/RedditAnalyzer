using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("out.log", rollingInterval: RollingInterval.Infinite)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<RedditAnalyzer.Services.RedditService>();
builder.Services.AddScoped<RedditAnalyzer.Services.AnalysisService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();  
app.UseStaticFiles();  
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();