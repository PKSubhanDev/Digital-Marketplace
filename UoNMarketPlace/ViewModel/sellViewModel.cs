using System.ComponentModel.DataAnnotations;

namespace UoNMarketPlace.ViewModel
{
    public class sellViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; }

        public List<IFormFile> ProductImages { get; set; } // Allow multiple images
    }
}
