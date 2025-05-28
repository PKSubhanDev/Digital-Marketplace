namespace UoNMarketPlace.Model
{
    public class Like
    {
        public int Id { get; set; }
        public string UserId { get; set; } // The User who liked (Student/Alumni)
        public int PostId { get; set; }
        public virtual AlumniPost Post { get; set; }
    }
}
