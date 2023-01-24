using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a Profile
    /// </summary>
    public class Profile
    {
        public const string PROFILE_DEFAULT_NAME = "DefaultProfile";
        public const string PROFILE_TYPE_NORMAL = "Normal";
        public const string PROFILE_TYPE_LOCK_SCREEN = "LockScreen";

        public string ProfileType { get; set; } = PROFILE_TYPE_NORMAL;

        public string Name { get; set; } = PROFILE_DEFAULT_NAME;
        public List<Page> Pages { get; set; } = new();
    }
}
