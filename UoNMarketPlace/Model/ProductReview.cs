namespace UoNMarketPlace.Model
{
    public class ProductReview
    {
        public int Id { get; set; }
        public string SellerId { get; set; } // Track the seller being reviewed
        public string UserId { get; set; } // User who submitted the review
        public string UserName { get; set; } // Display name of the user
        public string ReviewText { get; set; } // Review content
        public double Rating { get; set; } // Rating given by the user (1-5 stars)
        public DateTime DateReviewed { get; set; } // Date of the review
        public int ProductId { get; set; }
        public sellProduct Product { get; set; } // Navigation property
    


    }
}
