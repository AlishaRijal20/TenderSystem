using TenderSystem.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly TenderSystemContext _context;
    private static readonly ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();

    public ChatHub(TenderSystemContext context)
    {
        _context = context;
    }
    public override async Task OnConnectedAsync()
    {
        // Get the user ID from claims
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim))
        {
            // Convert user ID to Int16
            var userId = Convert.ToInt16(userIdClaim);
            _connections[Context.ConnectionId] = userId.ToString();

            Console.WriteLine($"User connected: {userId} with Connection ID: {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine("User ID not found in claims.");
        }

        await base.OnConnectedAsync();
    }




    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.TryRemove(Context.ConnectionId, out _);
        Console.WriteLine($"User disconnected with Connection ID: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }



    public async Task SendMessage(int senderId, int receiverId, string message)
    {
        try
        {
            Console.WriteLine($"Sending message from {senderId} to {receiverId}: {message}");

            // Validate inputs
            if (senderId <= 0 || receiverId <= 0 || string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Invalid input parameters.");
            }

            var maxMsgId = _context.Chats.Any() ? _context.Chats.Max(c => c.ChatId) + 1 : 1;
            var msg = new Chat
            {
                ChatId = (short)maxMsgId,
                SenderId = (short)senderId,
                ReceiverId = (short)receiverId,
                MessageText = message,
                CreatedDate = DateTime.UtcNow,

            };

            // Save the message to the database
            await _context.Chats.AddAsync(msg);
            await _context.SaveChangesAsync();

            // Log before broadcasting
            Console.WriteLine($"Broadcasting message to user {receiverId}: {message}");
            var connectionId = _connections.FirstOrDefault(c => c.Value == receiverId.ToString()).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message);
                Console.WriteLine("Message broadcasted successfully.");
            }
            else
            {
                Console.WriteLine("Receiver is not connected.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendMessage: {ex.Message}\n{ex.StackTrace}");
            throw; // Rethrow to SignalR
        }
    }
}