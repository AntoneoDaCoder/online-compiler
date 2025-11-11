using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIAPP.DAL.Contexts
{
    public sealed class UsersDbContext : IdentityDbContext<UserEntity, IdentityRole<Guid>, Guid>
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
