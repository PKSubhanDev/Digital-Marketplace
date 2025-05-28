using Microsoft.AspNetCore.Identity;

namespace UoNMarketPlace.Model
{
    public class Reply
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime DateReplied { get; set; }
        public string UserId { get; set; } // The User ID who replied (Student/Alumni)
        public virtual IdentityUser User { get; set; } // Reference to the Identity User
        public int CommentId { get; set; }
        public virtual Comment Comment { get; set; }
    }
}
