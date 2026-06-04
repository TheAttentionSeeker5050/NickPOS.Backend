using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NickPOS.Backend.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}