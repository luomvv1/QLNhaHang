using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;

namespace Restaurant.Api.Services;

public interface IAuditLogService
{
    Task<AuditLog> LogAsync(
        int? userId,
        string actionName,
        string entityName,
        string? entityId,
        object? oldData = null,
        object? newData = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public AuditLogService(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<AuditLog> LogAsync(
        int? userId,
        string actionName,
        string entityName,
        string? entityId,
        object? oldData = null,
        object? newData = null)
    {
        try
        {
            string? oldJson = oldData != null
                ? (oldData is string strOld ? strOld : JsonSerializer.Serialize(oldData, _jsonOptions))
                : null;

            string? newJson = newData != null
                ? (newData is string strNew ? strNew : JsonSerializer.Serialize(newData, _jsonOptions))
                : null;

            var log = new AuditLog
            {
                UserId = userId,
                ActionName = actionName,
                EntityName = entityName,
                EntityId = entityId,
                OldData = oldJson,
                NewData = newJson,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            // Lấy thông tin user nếu có để phát realtime
            string actorName = "Hệ thống";
            string actorRole = "System";
            if (userId.HasValue)
            {
                var actor = await _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId.Value);
                if (actor != null)
                {
                    actorName = actor.FullName;
                    actorRole = actor.Role?.RoleName ?? "Nhân viên";
                }
            }

            // Phát thông báo Realtime tới Admin / Manager
            try
            {
                await _hubContext.PublishAsync(_dbContext, "AuditLogCreated", new
                {
                    auditLogId = log.AuditLogId,
                    userId = log.UserId,
                    actorName,
                    actorRole,
                    actionName = log.ActionName,
                    entityName = log.EntityName,
                    entityId = log.EntityId,
                    createdAt = log.CreatedAt
                });
            }
            catch { }

            return log;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogService Error]: {ex.Message}");
            // Không làm ngắt quãng luồng chính nếu lỗi ghi log
            return new AuditLog { ActionName = actionName, EntityName = entityName };
        }
    }
}
