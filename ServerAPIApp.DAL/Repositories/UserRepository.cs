using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using Shared.Helpers;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private UserManager<UserEntity> _userManager;
        private RoleManager<IdentityRole<Guid>> _roleManager;
        private BaseDbContext _context;

        public UserRepository(UserManager<UserEntity> userManager, RoleManager<IdentityRole<Guid>> roleManager, BaseDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<UserEntity?> GetByIdAsync
            (Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            return user;
        }

        public async Task<UserEntity?> GetByHashedEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var hash = CryptoHelpers.ComputeSha256Hex(email);
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmailHash == hash, cancellationToken);
        }

        public async Task<List<UserEntity>?> GetFilteredUsersAsync
            (Expression<Func<UserEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var entries = await _context.Users
                .AsNoTracking()
                .Where(filter)
                .ToListAsync(cancellationToken);

            return entries;
        }

        public async Task<UserEntity?> GetByEmailAsync
            (string email,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);

            return user;
        }

        public async Task<(UserEntity? user, List<string>? roles)> GetByIdWithRolesAsync
            (Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query =
                from u in _context.Users
                where u.Id == userId
                join ur in _context.UserRoles on u.Id equals ur.UserId into urj
                from ur in urj.DefaultIfEmpty()
                join r in _context.Roles on ur.RoleId equals r.Id into rj
                from r in rj.DefaultIfEmpty()
                select new { User = u, Role = r.Name };

            var rows = await query.AsNoTracking().ToListAsync(cancellationToken);

            if (rows.Count == 0)
                return (null, null);

            var user = rows[0].User;
            var roles = rows.Where(x => x.Role is not null).Select(x => x.Role).Distinct().ToList();

            return (user, roles);
        }

        public async Task<(UserEntity? user, List<string>? roles)> GetByEmailWithRolesAsync
            (string email,
            CancellationToken cancellationToken = default)
        {
            var query =
                from u in _context.Users
                where u.Email == email
                join ur in _context.UserRoles on u.Id equals ur.UserId into urj
                from ur in urj.DefaultIfEmpty()
                join r in _context.Roles on ur.RoleId equals r.Id into rj
                from r in rj.DefaultIfEmpty()
                select new { User = u, Role = r.Name };

            var rows = await query.AsNoTracking().ToListAsync(cancellationToken);

            if (rows.Count == 0)
                return (null, null);

            var user = rows[0].User;
            var roles = rows.Where(x => x.Role is not null).Select(x => x.Role).Distinct().ToList();

            return (user, roles);
        }

        public async Task<IdentityResult> CreateAsync
            (UserEntity user,
            string password,
            CancellationToken cancellationToken = default)
        {
            var result = await _userManager.CreateAsync(user, password);

            return result;
        }

        public async Task<IdentityResult> UpdateAsync
            (UserEntity entity,
            CancellationToken cancellationToken = default)
        {
            var result = await _userManager.UpdateAsync(entity);

            return result;
        }

        public async Task<bool> CheckPasswordAsync
            (UserEntity user,
            string password,
            CancellationToken cancellationToken = default)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IdentityResult> DeleteAsync
            (UserEntity user,
            CancellationToken cancellationToken = default)
        {
            return await _userManager.DeleteAsync(user);
        }

        //TODO: implement batch user delete. for now i'll keep one at a time deletion strategy (im fucking lazy wcyd)
    }
}
