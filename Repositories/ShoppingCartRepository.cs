using Microsoft.EntityFrameworkCore;
using TeaStoreApi.Data;
using TeaStoreApi.Interfaces;
using TeaStoreApi.Models;

namespace TeaStoreApi.Repositories
{
    public class ShoppingCartRepository : IShoppingCartRepository
    {
        private ApiDbContext dbContext;

        public ShoppingCartRepository(ApiDbContext dbContext)
        {
            this.dbContext = dbContext; 
        }
        public async Task<bool> AddToCart(ShoppingCartItem shoppingCartItem)
        {
            var product = await dbContext.Products.FindAsync(shoppingCartItem.ProductId);
            if (product == null) 
            {
                return false;
            }
            // Check if item is in the cart.
            var existingCartItem = await dbContext.ShoppingCartItems.FirstOrDefaultAsync(s=>s.ProductId == shoppingCartItem.ProductId && s.UserId==shoppingCartItem.UserId);
            if(existingCartItem != null)
            {
                // Item exists , update quantity and total amount
               existingCartItem.Qty += shoppingCartItem.Qty;
               existingCartItem.TotalAmount = existingCartItem.Price * existingCartItem.Qty;
            }
            else
            {
                // Item doesn't exist, add new items to to cart
                shoppingCartItem.Price=product.Price;
                shoppingCartItem.TotalAmount = product.Price * shoppingCartItem.Qty;
                await dbContext.ShoppingCartItems.AddRangeAsync(shoppingCartItem);
            }
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<object>> GetShoppingCartItems(int userId)
        {
            var shoppingCart = await (from shoppingCartItems in dbContext.ShoppingCartItems.Where(s=>s.UserId == userId)
            join product in dbContext.Products on shoppingCartItems.ProductId equals product.Id
            select new
            { 
              ProductId=product.Id,
              ProductName=product.Name,
              ImageUrl=product.ImageUrl,
              Price=shoppingCartItems.Price,
              TotalAmount=shoppingCartItems.TotalAmount,
              Qty=shoppingCartItems.Qty,
            }).ToListAsync();

            return shoppingCart;
        }

        public async Task<bool> UpdateCart(int productId, int userId,  string action)
        {
            var existingShoppingCart = await dbContext.ShoppingCartItems.FirstOrDefaultAsync(s=>s.ProductId==productId &&  s.UserId == userId);
            if (existingShoppingCart == null) 
            {
                return false;
            }
            switch (action)
            {
                case "increase":
                    existingShoppingCart.Qty += 1;
                    existingShoppingCart.TotalAmount=existingShoppingCart.Price * existingShoppingCart.Qty;
                    break;
                case "decrease":
                    if (existingShoppingCart.Qty > 0)
                    {
                        existingShoppingCart.Qty -= 1;
                        existingShoppingCart.TotalAmount = existingShoppingCart.Qty * existingShoppingCart.Qty;
                    }
                    else
                    {
                        dbContext.ShoppingCartItems.Remove(existingShoppingCart);
                    }
                    break ;
                case "delete":
                    dbContext.ShoppingCartItems.Remove(existingShoppingCart);
                    break ;
                    default: return false;
            }
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}
