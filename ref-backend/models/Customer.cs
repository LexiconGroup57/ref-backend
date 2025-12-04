using Microsoft.AspNetCore.Identity;

namespace ref_backend.models;

public class Customer
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }

    public string UserId { get; set; }

    public Customer() { }

    public Customer(string name, string email, string phone, string userId)
    {
        CreatedAt = DateTime.Now;
        Name = name;
        Email = email;
        Phone = phone;
        UserId = userId;
    }
}