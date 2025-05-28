namespace UoNMarketPlace.ViewModel
{
    public class EditProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public List<string>? ExistingImages { get; set; }
        public List<IFormFile>? ProductImages { get; set; } // For new image uploads
    }
}
