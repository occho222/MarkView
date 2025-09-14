using System.Collections.ObjectModel;
using System.IO;
using MarkView.Models;

namespace MarkView.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IDataPersistenceService _dataPersistence;
        private const string FavoritesFileName = "favorites.json";

        private ObservableCollection<FavoriteItem> _favorites = new();

        public ObservableCollection<FavoriteItem> Favorites => _favorites;

        public FavoriteService(IDataPersistenceService dataPersistence)
        {
            _dataPersistence = dataPersistence;
        }

        public async Task<ObservableCollection<FavoriteItem>> LoadFavoritesAsync()
        {
            try
            {
                var favorites = await _dataPersistence.LoadDataAsync<List<FavoriteItem>>(FavoritesFileName);

                _favorites.Clear();

                if (favorites != null)
                {
                    foreach (var favorite in favorites.OrderByDescending(f => f.IsPinned).ThenByDescending(f => f.LastAccessedAt))
                    {
                        _favorites.Add(favorite);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"お気に入り読み込み完了: {_favorites.Count}件");
                return _favorites;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り読み込みエラー: {ex.Message}");
                return _favorites;
            }
        }

        public async Task SaveFavoritesAsync()
        {
            try
            {
                var favoriteList = _favorites.ToList();
                await _dataPersistence.SaveDataAsync(favoriteList, FavoritesFileName);
                System.Diagnostics.Debug.WriteLine($"お気に入り保存完了: {favoriteList.Count}件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り保存エラー: {ex.Message}");
                throw;
            }
        }

        public async Task<FavoriteItem> AddFavoriteAsync(string title, string filePath, string description = "", string category = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                    throw new ArgumentException("タイトルを入力してください。", nameof(title));

                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("ファイルパスを指定してください。", nameof(filePath));

                var existingFavorite = await FindFavoriteByPathAsync(filePath);
                if (existingFavorite != null)
                {
                    throw new InvalidOperationException("このファイルは既にお気に入りに登録されています。");
                }

                var favorite = new FavoriteItem(title, filePath, description, category);

                _favorites.Insert(0, favorite);
                await SaveFavoritesAsync();

                System.Diagnostics.Debug.WriteLine($"お気に入り追加完了: {favorite.Title}");
                return favorite;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り追加エラー: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveFavoriteAsync(string favoriteId)
        {
            try
            {
                var favorite = _favorites.FirstOrDefault(f => f.Id == favoriteId);
                if (favorite != null)
                {
                    _favorites.Remove(favorite);
                    await SaveFavoritesAsync();

                    System.Diagnostics.Debug.WriteLine($"お気に入り削除完了: {favorite.Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り削除エラー: {ex.Message}");
                throw;
            }
        }

        public async Task<FavoriteItem?> GetFavoriteAsync(string favoriteId)
        {
            try
            {
                return await Task.FromResult(_favorites.FirstOrDefault(f => f.Id == favoriteId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り取得エラー: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateFavoriteAsync(FavoriteItem favorite)
        {
            try
            {
                var existingFavorite = _favorites.FirstOrDefault(f => f.Id == favorite.Id);
                if (existingFavorite != null)
                {
                    var index = _favorites.IndexOf(existingFavorite);
                    _favorites[index] = favorite;

                    await SaveFavoritesAsync();
                    System.Diagnostics.Debug.WriteLine($"お気に入り更新完了: {favorite.Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り更新エラー: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsFavoriteAsync(string filePath)
        {
            try
            {
                var favorite = await FindFavoriteByPathAsync(filePath);
                return favorite != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入り判定エラー: {ex.Message}");
                return false;
            }
        }

        public async Task<FavoriteItem?> FindFavoriteByPathAsync(string filePath)
        {
            try
            {
                return await Task.FromResult(_favorites.FirstOrDefault(f =>
                    Path.GetFullPath(f.FilePath).Equals(Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入りパス検索エラー: {ex.Message}");
                return null;
            }
        }

        public async Task MarkFavoriteAccessedAsync(string favoriteId)
        {
            try
            {
                var favorite = _favorites.FirstOrDefault(f => f.Id == favoriteId);
                if (favorite != null)
                {
                    favorite.MarkAsAccessed();
                    await SaveFavoritesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"お気に入りアクセス記録エラー: {ex.Message}");
            }
        }

        public async Task<ObservableCollection<string>> GetCategoriesAsync()
        {
            try
            {
                var categories = await Task.FromResult(
                    _favorites
                        .Where(f => !string.IsNullOrWhiteSpace(f.Category))
                        .Select(f => f.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList()
                );

                return new ObservableCollection<string>(categories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"カテゴリ取得エラー: {ex.Message}");
                return new ObservableCollection<string>();
            }
        }

        public async Task<ObservableCollection<FavoriteItem>> GetFavoritesByCategoryAsync(string category)
        {
            try
            {
                var filteredFavorites = await Task.FromResult(
                    _favorites
                        .Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(f => f.IsPinned)
                        .ThenByDescending(f => f.LastAccessedAt)
                        .ToList()
                );

                return new ObservableCollection<FavoriteItem>(filteredFavorites);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"カテゴリ別お気に入り取得エラー: {ex.Message}");
                return new ObservableCollection<FavoriteItem>();
            }
        }
    }
}