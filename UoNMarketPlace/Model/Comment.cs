using Microsoft.AspNetCore.Identity;

namespace UoNMarketPlace.Model
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime DateCommented { get; set; }
        public string UserId { get; set; } // The Student or Alumni User ID
        public virtual IdentityUser User { get; set; } // Reference to the Identity User

        public int PostId { get; set; }
        public virtual AlumniPost Post { get; set; }
        public virtual ICollection<Reply> Replies { get; set; }
    }
}
