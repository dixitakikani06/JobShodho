using Hangfire;
using JobShodho.BackgroundJobs;
using JobShodho.Data;
using JobShodho.Options;
using JobShodho.Services;
using JobShodho.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<CandidateOptions>(builder.Configuration.GetSection(CandidateOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailBatchService, EmailBatchService>();
builder.Services.AddScoped<EmailBatchJob>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IInterviewService, InterviewService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DataSeeder.SeedAsync(db, env, seedLogger);
    }
    catch (Exception ex)
    {
        seedLogger.LogWarning(ex, "Skipped development seed data (has the database been created with 'dotnet ef database update'?)");
    }
}

app.Run();
