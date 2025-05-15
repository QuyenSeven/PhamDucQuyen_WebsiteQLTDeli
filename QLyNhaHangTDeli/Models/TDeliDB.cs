using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace QLyNhaHangTDeli.Models
{
    public partial class TDeliDB : DbContext
    {
        public TDeliDB()
            : base("name=TDeliDB")
        {
        }

        public virtual DbSet<AppliedDiscount> AppliedDiscounts { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<Complaint> Complaints { get; set; }
        public virtual DbSet<CustomerDiscountCode> CustomerDiscountCodes { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<DiscountCode> DiscountCodes { get; set; }
        public virtual DbSet<Inventory> Inventories { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Post> Posts { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Table> Tables { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .HasMany(e => e.AppliedDiscounts)
                .WithRequired(e => e.Customer)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Customer>()
                .HasMany(e => e.CustomerDiscountCodes)
                .WithRequired(e => e.Customer)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiscountCode>()
                .HasMany(e => e.AppliedDiscounts)
                .WithRequired(e => e.DiscountCode)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiscountCode>()
                .HasMany(e => e.CustomerDiscountCodes)
                .WithRequired(e => e.DiscountCode)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Inventory>()
                .Property(e => e.InPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(e => e.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(e => e.Total)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(e => e.Discount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(e => e.TotalAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .HasMany(e => e.AppliedDiscounts)
                .WithRequired(e => e.Order)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Order>()
                .HasMany(e => e.OrderDetails)
                .WithOptional(e => e.Order)
                .WillCascadeOnDelete();

            modelBuilder.Entity<Product>()
                .Property(e => e.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<User>()
                .Property(e => e.PasswordHash)
                .IsUnicode(false);
        }
    }
}
