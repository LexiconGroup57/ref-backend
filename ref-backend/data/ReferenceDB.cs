using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ref_backend.models;

namespace ref_backend.data;

public class ReferenceDB : IdentityDbContext<IdentityUser>
{
    public ReferenceDB(DbContextOptions<ReferenceDB> options) : base(options)
    {
        
    }

    public DbSet<RefRecord> RefRecords { get; set; }
    public DbSet<Customer> Customers { get; set; }
}