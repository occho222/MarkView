using System.Collections.ObjectModel;
using MarkView.Models;

namespace MarkView.Services
{
    public interface IFavoriteService
    {
        Task<ObservableCollection<FavoriteItem>> LoadFavoritesAsync();
        Task SaveFavoritesAsync();
        Task<FavoriteItem> AddFavoriteAsync(string title, string filePath, string description = "", string category = "");
        Task RemoveFavoriteAsync(string favoriteId);
        Task<FavoriteItem?> GetFavoriteAsync(string favoriteId);
        Task UpdateFavoriteAsync(FavoriteItem favorite);
        Task<bool> IsFavoriteAsync(string filePath);
        Task<FavoriteItem?> FindFavoriteByPathAsync(string filePath);
        Task MarkFavoriteAccessedAsync(string favoriteId);
        Task<ObservableCollection<string>> GetCategoriesAsync();
        Task<ObservableCollection<FavoriteItem>> GetFavoritesByCategoryAsync(string category);
        ObservableCollection<FavoriteItem> Favorites { get; }
    }
}