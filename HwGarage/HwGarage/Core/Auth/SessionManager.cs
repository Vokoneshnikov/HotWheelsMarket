using System;
using System.Collections.Concurrent;

namespace HwGarage.Core.Auth
{
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, (int UserId, DateTime ExpiresAt)> _sessions = new();
        private readonly TimeSpan _sessionLifetime = TimeSpan.FromHours(3);
        
        public string CreateSession(int userId)
        {
            var token = Guid.NewGuid().ToString("N");
            _sessions[token] = (userId, DateTime.UtcNow.Add(_sessionLifetime));
            return token;
        }
        
        public bool IsValid(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (session.ExpiresAt > DateTime.UtcNow)
                    return true;

                _sessions.TryRemove(sessionId, out _);
            }
            return false;
        }

        
        public int? GetUserId(string token)
        {
            if (_sessions.TryGetValue(token, out var data))
            {
                if (data.ExpiresAt > DateTime.UtcNow)
                {
                    return data.UserId;
                }
                else
                {
                    _sessions.TryRemove(token, out _);
                }
            }
            return null;        
        }
        
        public void DestroySession(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }
}