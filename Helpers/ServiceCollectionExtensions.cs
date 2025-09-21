using Microsoft.Extensions.DependencyInjection;
using MarkView.Services;
using MarkView.ViewModels;

namespace MarkView.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMarkViewServices(this IServiceCollection services)
        {
            // Services
            services.AddSingleton<IPlantUmlService, PlantUmlService>();
            services.AddSingleton<IMarkdownService, MarkdownService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDataPersistenceService, DataPersistenceService>();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IFavoriteService, FavoriteService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();

            return services;
        }
    }
}