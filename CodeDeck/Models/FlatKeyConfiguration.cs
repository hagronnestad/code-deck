using CodeDeck.Models.Configuration;

namespace CodeDeck.Models
{
    public class FlatKeyConfiguration
    {
        public Key Key { get; set; }
        public Page Page { get; set; }
        public Profile Profile { get; set; }

        public FlatKeyConfiguration(Key key, Page page, Profile profile)
        {
            Key = key;
            Page = page;
            Profile = profile;
        }
    }
}
