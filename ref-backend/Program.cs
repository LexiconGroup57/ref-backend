using System.ClientModel;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
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
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
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
builder.Services.AddSingleton<ChatClient>(provider =>
{
    return new ChatClient(
        model: "gemma-3-12b-it-qat",
        credential: new ApiKeyCredential("text"),
        options: new OpenAIClientOptions()
        {
            Endpoint = new Uri("http://127.0.0.1:1234/v1")
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
app.UseSession();
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

app.MapGet("/user/current", (HttpContext context, UserManager<IdentityUser> userManager) =>
{
    var userFrontend = context.User;
    return userManager.GetUserName(userFrontend);
}).RequireAuthorization();

app.MapPost("api/customer", (Customer customer, ReferenceDB _context, UserManager<IdentityUser> userManager, HttpContext context) =>
{
    CustomerFactory factory = new CustomerFactory(context, userManager);
    Customer newCustomer = factory.CreateCustomer(customer.Name, customer.Email, customer.Phone);
    _context.Customers.Add(newCustomer);
    _context.SaveChanges();
    return newCustomer;
});

app.MapGet("api/references", (ReferenceDB _context, HttpContext context, UserManager<IdentityUser> userManager) =>
    {
        List<RefRecord> records = _context.RefRecords
            .Where(r => r.CustomerId == userManager.GetUserId(context.User))
            .ToList();
        List<RefRecordDto> recordsDto = records.Select(r => new RefRecordDto(r)).ToList();
        return recordsDto;
    })
    .RequireAuthorization();

app.MapPost("api/references", (RefRecord record, ReferenceDB _context, HttpContext context, UserManager<IdentityUser> userManager) =>
{
    record.CustomerId = userManager.GetUserId(context.User);
    RefRecord newRecord = new RefRecord(record);
    _context.RefRecords.Add(record);
    _context.SaveChanges();
    return newRecord;
}).RequireAuthorization();

app.MapPost("api/references/delete/{id}", (int id, ReferenceDB _context) =>
{
    _context.RefRecords.Remove(new RefRecord { Id = id });
    _context.SaveChanges();
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("api/references/duplicate/{id}", (int id, ReferenceDB _context) =>
{
    var record = _context.RefRecords.Find(id);
    _context.RefRecords.Add(new RefRecord(record));
    _context.SaveChanges();
    return record;
}).RequireAuthorization();

app.MapPost("/api/chat", async (string message, HttpContext context, ChatClient client) =>
{
    string userMessage = context.Session.GetString("userMessage") ?? "";
    string assistantMessage = context.Session.GetString("assistantMessage") ?? "";
    
    List<ChatMessage> messages =
        [ new SystemChatMessage($"You are a helpful assistant. Respond with a reasonable answer to the following question:"),];
    if (userMessage != "")
    {
        messages.Add( new UserChatMessage(userMessage));
        messages.Add(new AssistantChatMessage(assistantMessage));  
    }
    messages.Add(new UserChatMessage(message));
    
    var completion = await client.CompleteChatAsync(messages);
    
    context.Session.SetString("userMessage", message);
    context.Session.SetString("assistantMessage", completion.Value.Content[0].Text);
    
    return completion.Value.Content[0].Text;
});

app.MapPost("/api/chatdb", async (string message, int sessionId, ReferenceDB _context, ChatClient client) =>
{
    List<ChatMessage> messages = new();
    var session = _context.ChatSessions.Find(sessionId);
    if (session == null)
    {
        messages.Add(new SystemChatMessage("You are a helpful assistant. Respond with a reasonable answer to the following question:"));
        messages.Add( new UserChatMessage(message));
    }
    else
    {
        var dbMessages = JsonSerializer.Deserialize<List<ChatMessageDto>>(session.MessageHistory);
        messages = dbMessages.Select<ChatMessageDto, ChatMessage>(m =>
            m.Role switch
            {
                "system" => new SystemChatMessage(m.Content),
                "user" => new UserChatMessage(m.Content),
                "assistant" => new AssistantChatMessage(m.Content),
                _ => throw new ArgumentException($"Unknown role: {m.Role}")
        }).ToList();
        messages.Add(new UserChatMessage(message));
    }
    var completion = await client.CompleteChatAsync(messages);
    messages.Add(new AssistantChatMessage(completion.Value.Content[0].Text));
    var dtos = messages.Select(m => new ChatMessageDto
    {
        Role = m is SystemChatMessage ? "system" : m is UserChatMessage ? "user" : "assistant",
        Content = m.Content[0].Text
    }).ToList();
    string json = JsonSerializer.Serialize(dtos);
    if (session == null)
    {
        session = new ChatSession()
        {
            UserId = "User",
            MessageHistory = json
        };
        _context.ChatSessions.Add(session);
    }
    else
    {
        session.MessageHistory = json;
        _context.Update(session);
    }
    _context.SaveChanges();
    return session;
});


app.MapPost("/api/translate", async (string language, string phrase, ChatClient client) =>
{
    List<ChatMessage> messages =
    [
        new SystemChatMessage($"Translate the given phrase into {language}. Return one single phrase."),
        new UserChatMessage(phrase)
    ];
    var completion = await client.CompleteChatAsync(messages);
    return completion.Value.Content[0].Text;
});

app.MapPost("api/references/edit/{id}", (int id, RefRecord record, ReferenceDB _context) =>
{
    var oldRecord = _context.RefRecords.Find(id);
    oldRecord.Title = record.Title;
    oldRecord.Creators = record.Creators;
    oldRecord.Date = record.Date;
    oldRecord.Publisher = record.Publisher;
    _context.SaveChanges();
    return oldRecord;
}).RequireAuthorization();

app.Run();
