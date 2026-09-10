using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace JobShodho.Tests.Fakes;

public class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "JobShodhoTests", Guid.NewGuid().ToString("N"));
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "JobShodho.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public string EnvironmentName { get; set; } = "Testing";
}
