using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ref_backend.data;
using ref_backend.models;
using SQLitePCL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ReferenceDB>();
builder.Services.ConfigureApplicationCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(2);
        options.SlidingExpiration = true;
    }
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ReferenceDB>(options =>
    options.UseSqlite("DataSource=references.db"));
builder.Services.AddAuthorization();
// builder.Services.AddAuthorization(options =>
// {
//     options.FallbackPolicy = new AuthorizationPolicyBuilder()
//         .RequireAuthenticatedUser()
//         .Build();
// });

builder.Services.AddCors(options =>
{
    options.AddPolicy("RefPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5175", "http://localhost:5173", "http://localhost:5174")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
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
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/user").MapIdentityApi<IdentityUser>();

app.MapPost("/user/logout", async (SignInManager<IdentityUser> signInManager,
        [FromBody] object empty) =>
    {
        if (empty != null)
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }

        return Results.Unauthorized();
    })
    .RequireAuthorization()
    .WithOpenApi();

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

app.MapPost("api/references/delete/{id}", (int id, ReferenceDB _context) =>
{
    _context.RefRecords.Remove(new RefRecord { Id = id });
    _context.SaveChanges();
    return Results.Ok();
});

app.MapPost("api/references/duplicate/{id}", (int id, ReferenceDB _context) =>
{
    var record = _context.RefRecords.Find(id);
    _context.RefRecords.Add(new RefRecord(record));
    _context.SaveChanges();
    return record;
});

app.MapPost("api/references/edit/{id}", (int id, RefRecord record, ReferenceDB _context) =>
{
    var oldRecord = _context.RefRecords.Find(id);
    oldRecord.Title = record.Title;
    oldRecord.Creator = record.Creator;
    oldRecord.Date = record.Date;
    oldRecord.Publisher = record.Publisher;
    _context.SaveChanges();
    return oldRecord;
});

app.Run();
