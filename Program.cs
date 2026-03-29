using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using SemanticKernelAI.Data;  // Add this line


var builder = WebApplication.CreateBuilder(args);

// 1. ตั้งค่า Connection String (ชี้ไปที่ Docker pg-ai ของคุณ)
var connString = "Host=host.docker.internal;Port=5432;Database=postgres;Username=postgres;Password=your_password";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connString, o => o.UseVector()));

// 2. ลงทะเบียน Semantic Kernel
builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    // เชื่อมต่อกับ Ollama (Llama 3.2 ที่คุณรันไว้)
    kernelBuilder.AddOllamaChatCompletion(
        modelId: "llama3.2",
        endpoint: new Uri("http://host.docker.internal:11434")
    );

    // ลงทะเบียน Plugin โดยดึง DbContext จาก DI
    var db = sp.GetRequiredService<AppDbContext>();
    kernelBuilder.Plugins.AddFromObject(new AssetPlugin(db));

    return kernelBuilder.Build();
});

builder.Services.AddControllers();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // สั่งให้สร้าง DB/Table ตาม Model ถ้ายังไม่มี
}
app.MapControllers();
app.Run();