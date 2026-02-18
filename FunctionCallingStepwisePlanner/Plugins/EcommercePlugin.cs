using Common;
using Common.Models;

using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text;

namespace FunctionCallingStepwisePlanner.Plugins
{
    public class EcommercePlugin
    {
        private readonly List<Product> _products;

        public EcommercePlugin()
        {
            _products = new List<Product>()
            {
                new Product { Id = 1, Name = "Red Shoes",  Description = "Comfortable red running shoes", Category = "Clothing",     StockQuantity = 100, AverageRating = 3.0F, Price = 10M },
                new Product { Id = 2, Name = "Blue Shoes", Description = "Stylish blue casual shoes",     Category = "Clothing",     StockQuantity = 200, AverageRating = 4.0F, Price = 55M },
                new Product { Id = 3, Name = "SSD Drive",  Description = "1TB SSD high speed storage",   Category = "Electronics",  StockQuantity = 75,  AverageRating = 4.5F, Price = 89.99M },
            };
        }

        // ─────────────────────────────────────────────
        // SEARCH / DISCOVERY
        // ─────────────────────────────────────────────

        [KernelFunction]
        [Description("""
            Search products by name or description keyword.
            Returns a list of matching products with their IDs, prices, ratings and stock.
            Use the returned IDs for further lookups like CheckStock or GetProductRating.
            Example: SearchByNameOrDescription("shoes")
        """)]
        public string SearchByNameOrDescription(
            [Description("Keyword to search in product name or description, e.g. 'shoes', 'SSD', '1TB'")] string searchTerm)
        {
            var results = _products.Where(p =>
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

            return FormatProductList(results, "search term", searchTerm);
        }

        [KernelFunction]
        [Description("""
            Search products by category.
            Returns a list of matching products with their IDs, prices, ratings and stock.
            Use the returned IDs for further lookups like CheckStock or GetProductRating.
            Example: SearchByCategory("clothing")
        """)]
        public string SearchByCategory(
            [Description("Category to search, e.g. 'clothing', 'electronics'")] string category)
        {
            var results = _products.Where(p =>
                p.Category.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();

            return FormatProductList(results, "category", category);
        }

        // ─────────────────────────────────────────────
        // FILTERING
        // ─────────────────────────────────────────────

        [KernelFunction]
        [Description("""
            Filter products at or below a maximum price.
            Optionally scope to a comma-separated list of product IDs from a previous search.
            Returns matching products with IDs for further lookups.
            Example: FilterByPrice(60) or FilterByPrice(60, "1,2,3")
        """)]
        public string FilterByPrice(
            [Description("Maximum price limit, e.g. 50 or 99.99")] decimal priceLimit,
            [Description("Optional comma-separated product IDs to filter from a previous search result, e.g. '1,2,3'. Leave empty to search all products.")] string productIds = "")
        {
            var query = _products.Where(p => p.Price <= priceLimit);

            if (!string.IsNullOrWhiteSpace(productIds))
            {
                var ids = productIds.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
                    .Where(id => id != -1)
                    .ToList();

                query = query.Where(p => ids.Contains(p.Id));
            }

            return FormatProductList(query.ToList(), "max price", $"${priceLimit}");
        }

        [KernelFunction]
        [Description("""
            Filter products at or above a minimum rating threshold.
            Optionally scope to a comma-separated list of product IDs from a previous search.
            Returns matching products with IDs for further lookups.
            Example: FilterByRating(4.0) or FilterByRating(4.0, "1,2,3")
        """)]
        public string FilterByRating(
            [Description("Minimum average rating threshold between 0.0 and 5.0, e.g. 4.0")] float minRating,
            [Description("Optional comma-separated product IDs to filter from a previous search result, e.g. '1,2,3'. Leave empty to search all products.")] string productIds = "")
        {
            var query = _products.Where(p => p.AverageRating >= minRating);

            if (!string.IsNullOrWhiteSpace(productIds))
            {
                var ids = productIds.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
                    .Where(id => id != -1)
                    .ToList();

                query = query.Where(p => ids.Contains(p.Id));
            }

            return FormatProductList(query.ToList(), "min rating", $"{minRating}/5.0");
        }

        // ─────────────────────────────────────────────
        // DETAIL LOOKUPS  (use after getting an ID)
        // ─────────────────────────────────────────────

        [KernelFunction]
        [Description("""
            Get the current stock quantity for a product using its ID.
            Always call a search function first to obtain the product ID.
            Example: CheckStock(1)
        """)]
        public string CheckStock(
            [Description("Numeric product ID obtained from a previous search result")] int productId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return $"No product found with ID {productId}. Use a search function first to get a valid ID.";

            return product.StockQuantity > 0
                ? $"{product.Name} (ID:{product.Id}) has {product.StockQuantity} units in stock."
                : $"{product.Name} (ID:{product.Id}) is currently out of stock.";
        }

        [KernelFunction]
        [Description("""
            Get the average customer rating for a product using its ID.
            Always call a search function first to obtain the product ID.
            Example: GetProductRating(2)
        """)]
        public string GetProductRating(
            [Description("Numeric product ID obtained from a previous search result")] int productId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return $"No product found with ID {productId}. Use a search function first to get a valid ID.";

            return $"{product.Name} (ID:{product.Id}) has an average customer rating of {product.AverageRating}/5.0";
        }

        [KernelFunction]
        [Description("""
            Get full details of a product using its ID — name, description, category, price, stock and rating.
            Always call a search function first to obtain the product ID.
            Example: GetProductDetails(3)
        """)]
        public string GetProductDetails(
            [Description("Numeric product ID obtained from a previous search result")] int productId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return $"No product found with ID {productId}. Use a search function first to get a valid ID.";

            return $"""
                Product Details:
                  ID          : {product.Id}
                  Name        : {product.Name}
                  Description : {product.Description}
                  Category    : {product.Category}
                  Price       : ${product.Price}
                  Stock       : {product.StockQuantity} units
                  Rating      : {product.AverageRating}/5.0
                """;
        }

        // ─────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────

        private static string FormatProductList(List<Product> products, string filterType, string filterValue)
        {
            if (!products.Any())
                return $"No products found for {filterType}: '{filterValue}'.";

            var sb = new StringBuilder();
            sb.AppendLine($"Found {products.Count} product(s) for {filterType}: '{filterValue}':");
            sb.AppendLine("(Use the ID with CheckStock, GetProductRating, or GetProductDetails for more info)");
            sb.AppendLine();

            foreach (var p in products)
            {
                sb.AppendLine($"  ID:{p.Id} | {p.Name} | ${p.Price} | Rating:{p.AverageRating}/5.0 | Stock:{p.StockQuantity} units | Category:{p.Category}");
            }

            return sb.ToString();
        }
    }
}