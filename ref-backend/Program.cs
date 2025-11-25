using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ref_backend.data;
using ref_backend.models;

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

app.Run();
