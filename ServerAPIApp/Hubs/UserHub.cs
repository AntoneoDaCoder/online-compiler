using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ServerAPIApp.Hubs;

[Authorize(Policy = "DefaultAccess")] // по необходимости
public class UserHub : Hub
{
    private static readonly string[] RoleGroups = new[] { "Users", "Admins", "Editors" };

    public override async Task OnConnectedAsync()
    {

        // всегда добавляем в группу Users
        await Groups.AddToGroupAsync(Context.ConnectionId, "Users");

        // проверяем роли через ClaimsPrincipal.IsInRole или через claims напрямую
        if (Context.User?.IsInRole("Admin") == true ||
            Context.User?.Claims.Any(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase)) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

            await Groups.AddToGroupAsync(Context.ConnectionId, "Editors");
        }

        if (Context.User?.IsInRole("Editor") == true ||
            Context.User?.Claims.Any(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) && c.Value.Equals("Editor", StringComparison.OrdinalIgnoreCase)) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Editors");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var g in RoleGroups)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, g);
        }

        await base.OnDisconnectedAsync(exception);
    }
}