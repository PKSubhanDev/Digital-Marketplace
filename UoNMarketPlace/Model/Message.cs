namespace UoNMarketPlace.Model
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public int? ProductId { get; set; }  // Link message to the product being discussed
        public sellProduct Product { get; set; }
    }
}
