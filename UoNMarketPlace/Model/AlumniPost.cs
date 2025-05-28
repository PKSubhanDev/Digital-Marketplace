namespace UoNMarketPlace.Model
{
    public class AlumniPost
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ImagePath { get; set; } // Optional image for the post
        public DateTime DatePosted { get; set; }
        public string? AlumniId { get; set; } // The Alumni User ID

        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Like>? Likes { get; set; }
    }
}
