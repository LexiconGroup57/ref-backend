using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ref_backend.data;
using ref_backend.models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ReferenceDB>(options =>
    options.UseSqlite("DataSource=references.db"));
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ReferenceDB>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("RefPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var _context = scope.ServiceProvider.GetRequiredService<ReferenceDB>();
}

app.UseHttpsRedirection();
app.UseCors("RefPolicy");
app.MapIdentityApi<IdentityUser>();
app.MapGet("api/references", (ReferenceDB _context) =>
    {
        return _context.RefRecords.ToList();
    })
    .RequireAuthorization();
app.MapPost("api/references", (RefRecord record, ReferenceDB _context) =>
{
    _context.RefRecords.Add(record);
    _context.SaveChanges();
    return record;
});

app.Run();
