using System;
using System.Collections.Generic;

namespace eShop.Ordering.Domain
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Address Address { get; set; } = new();
        public string Description { get; set; } = "";
        public int OrderStatus { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
        
        public decimal GetTotal() => 100.0m; // Simplified
    }

    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
        public string ZipCode { get; set; } = "";
    }

    public class OrderItem
    {
        public string ProductName { get; set; } = "";
        public int Units { get; set; }
        public decimal UnitPrice { get; set; }
        public string PictureUrl { get; set; } = "";
    }
}

namespace eShop.Ordering.API.Application.Queries
{
    public record OrderViewModel
    {
        public int OrderNumber { get; init; }
        public DateTime Date { get; init; }
        public string Status { get; init; } = "";
        public string Description { get; init; } = "";
        public string Street { get; init; } = "";
        public string City { get; init; } = "";
        public string State { get; init; } = "";
        public string Zipcode { get; init; } = "";
        public string Country { get; init; } = "";
        public List<OrderItemViewModel> OrderItems { get; set; } = new();
        public decimal Total { get; set; }
    }

    public record OrderItemViewModel
    {
        public string ProductName { get; init; } = "";
        public int Units { get; init; }
        public double UnitPrice { get; init; }
        public string PictureUrl { get; init; } = "";
    }
}
