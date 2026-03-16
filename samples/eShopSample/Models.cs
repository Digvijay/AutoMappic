using System;
using System.Collections.Generic;

namespace eShopSample;

// --- Entity Layer (mimicking dotnet/eShop Domain/Infrastructure) ---

public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PictureFileName { get; set; } = string.Empty;
}

public class Order
{
    public int OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Address ShippingAddress { get; set; } = new();
    public List<OrderItem> OrderItems { get; set; } = new();
    public decimal Total { get; set; }
}

public class OrderItem
{
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

// --- DTO Layer (Standard POCOs for widest compatibility in this sample) ---

public class CatalogItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PictureFileName { get; set; } = string.Empty;
}

public class OrderSummaryDto
{
    public int OrderNumber { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    
    // Flattened from ShippingAddress
    public string ShippingAddressCity { get; set; } = string.Empty;
    public string ShippingAddressState { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
}
