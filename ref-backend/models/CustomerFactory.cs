using Microsoft.AspNetCore.Identity;

namespace ref_backend.models;

public class CustomerFactory
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly HttpContext _context;

    public CustomerFactory(HttpContext context, UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
        _context = context;
    }
    
    public Customer CreateCustomer(string name, string email, string phone)
    {
        var user = _context.User;
        if (user == null) return null;
        string userId = _userManager.GetUserId(user);
        return new Customer(name, email, phone, userId);
    }
}