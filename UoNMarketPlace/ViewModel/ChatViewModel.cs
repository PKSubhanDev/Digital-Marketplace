using UoNMarketPlace.Model;

namespace UoNMarketPlace.ViewModel
{
    public class ChatViewModel
    {
        public List<Message> Messages { get; set; }
        public int ProductId { get; set; }
        public string SellerId { get; set; }
    }
}
