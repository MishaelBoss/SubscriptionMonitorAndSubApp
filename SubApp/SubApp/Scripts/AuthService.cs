using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SubApp.Scripts;

public static class AuthService
{
    public static UserSession? CurrentSession { get; private set; }
    public static bool IsLoggedIn => CurrentSession != null;

    public static async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            await using var db = new AppDbContext();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            if (!VerifyDjangoPassword(password, user.Password)) return false;

            CurrentSession = new UserSession(user.Id, user.Username);

            await SecureStorage.SetAsync("current_user_id", user.Id.ToString());

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TryAutoLoginAsync()
    {
        try
        {
            var userIdStr = await SecureStorage.GetAsync("current_user_id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return false;

            await using var db = new AppDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            CurrentSession = new UserSession(user.Id, user.Username);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Logout()
    {
        SecureStorage.Remove("current_user_id");
        CurrentSession = null;
        WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage());
        WeakReferenceMessenger.Default.Send(new UserLoggedInMessage());
    }

    private static bool VerifyDjangoPassword(string password, string djangoHash)
    {
        try
        {
            var parts = djangoHash.Split('$');
            if (parts.Length != 4) return false;

            var iterations = int.Parse(parts[1]);
            var salt = parts[2];
            var hash = parts[3];

            var saltBytes = System.Text.Encoding.UTF8.GetBytes(salt);
            var derived = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: 32);

            return Convert.ToBase64String(derived) == hash;
        }
        catch { return false; }
    }

    public static string HashPasswordDjango(string password)
    {
        const int iterations = 600000;
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var salt = new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());

        byte[] hashBytes = KeyDerivation.Pbkdf2(
            password: password,
            salt: System.Text.Encoding.UTF8.GetBytes(salt),
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: 32);

        return $"pbkdf2_sha256${iterations}${salt}${Convert.ToBase64String(hashBytes)}";
    }
}
