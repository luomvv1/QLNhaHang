using Microsoft.AspNetCore.SignalR;

using Microsoft.AspNetCore.Authorization;
using Restaurant.Api.Security;
using Restaurant.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Restaurant.Api.Hubs;

[Authorize(Roles = RestaurantRoles.Employees + ",customer")]
public class RestaurantHub : Hub
{
    public const string EmployeesGroup = "employees";
    public const string CustomersGroup = "customers";
    public static string AccountGroup(int userId, string role) => $"account:{userId}:{role}";

    public override async Task OnConnectedAsync()
    {
        if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Invalid account.");
        await Groups.AddToGroupAsync(Context.ConnectionId,
            AccountGroup(userId, Context.User!.FindFirstValue(ClaimTypes.Role)!));
        await Groups.AddToGroupAsync(Context.ConnectionId,
            Context.User!.IsInRole("customer") ? CustomersGroup : EmployeesGroup);
        await base.OnConnectedAsync();
    }

    public async Task JoinRoom(string roomName)
    {
        if (roomName != EmployeesGroup || Context.User!.IsInRole("customer"))
            throw new HubException("Room access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
    }

    public async Task LeaveRoom(string roomName)
    {
        if (roomName != EmployeesGroup || Context.User!.IsInRole("customer"))
            throw new HubException("Room access denied.");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
    }
}

public static class RestaurantNotifications
{
    public static async Task PublishAsync(this IHubContext<RestaurantHub> hub, RestaurantDbContext db, string eventName, object? payload = null)
    {
        // Resolve recipients at send time, so an old connection cannot retain revoked permissions.
        var users = await db.Users.AsNoTracking().Where(user => user.IsActive && user.Role.IsActive)
            .Select(user => new { user.UserId, user.Role.RoleCode }).ToListAsync();
        var employees = users.Where(user => user.RoleCode is "admin" or "manager" or "staff" or "chef")
            .Select(user => RestaurantHub.AccountGroup(user.UserId, user.RoleCode)).ToList();
        await hub.Clients.Groups(employees)
            .SendCoreAsync(eventName, payload == null ? [] : [payload]);
        // Customer clients only receive an invalidation, never another table's order or payment payload.
        if (eventName is "ReservationUpdated" or "StaffOrderUpdated" or "PaymentReceived")
            await hub.Clients.Groups(users.Where(user => user.RoleCode == "customer")
                .Select(user => RestaurantHub.AccountGroup(user.UserId, user.RoleCode)).ToList()).SendAsync("AccountUpdated");
    }
}
