using System;

namespace Microsoft.eShopWeb.ApplicationCore.Entities
{
    // Base class from eShop
    public abstract class BaseEntity
    {
        public virtual int Id { get; protected set; } = 0;
    }

    public class CatalogItem : BaseEntity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }
        public string PictureUri { get; private set; }
        public int CatalogTypeId { get; private set; }
        public int CatalogBrandId { get; private set; }

        public CatalogItem(int id, int catalogTypeId, int catalogBrandId, string description, string name, decimal price, string pictureUri)
        {
            Id = id;
            CatalogTypeId = catalogTypeId;
            CatalogBrandId = catalogBrandId;
            Description = description;
            Name = name;
            Price = price;
            PictureUri = pictureUri;
        }
    }

    public class CatalogType : BaseEntity
    {
        public string Type { get; private set; }
        public CatalogType(int id, string type) { Id = id; Type = type; }
    }

    public class CatalogBrand : BaseEntity
    {
        public string Brand { get; private set; }
        public CatalogBrand(int id, string brand) { Id = id; Brand = brand; }
    }
}

namespace Microsoft.eShopWeb.PublicApi
{
    public class CatalogItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string PictureUri { get; set; } = "";
    }

    public class CatalogTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class CatalogBrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
