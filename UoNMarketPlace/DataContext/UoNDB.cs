using UoNMarketPlace.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UoNMarketPlace.DataContext
{
    public class UoNDB : IdentityDbContext<IdentityUser>
    {
        public UoNDB(DbContextOptions<UoNDB> options)
         : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            seedRoles(builder);

        }
        public DbSet<User> users { get; set; }
        public DbSet<sellProduct> Products { get; set; }
        public DbSet<AlumniPost> AlumniPosts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        private void seedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData
                (
                new IdentityRole() { Name = "Student", ConcurrencyStamp = "1", NormalizedName = "Student" },
                new IdentityRole() { Name = "Alumini", ConcurrencyStamp = "4", NormalizedName = "Alumini" },
                new IdentityRole() { Name = "Admin", ConcurrencyStamp = "5", NormalizedName = "Admin" }
                );
        }
    }
}
