using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace SmartCondoApi.Models
{
    public class User : IdentityUser<long>
    {
        public bool Enabled { get; set; } = false;

        [JsonIgnore]
        public UserProfile UserProfile { get; set; }
    }
}