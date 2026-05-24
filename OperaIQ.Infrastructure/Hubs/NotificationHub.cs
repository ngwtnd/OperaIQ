using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace OperaIQ.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Trong thực tế sẽ thêm user vào Group theo TenantId hoặc các cấu hình khác
            await base.OnConnectedAsync();
        }
    }
}
