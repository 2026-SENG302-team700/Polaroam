
using SENG302.Api.Models.Entities;

public interface IAlbumService
{
    public Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId);
    public Task UploadImagesToAlbumAsync(List<int> photoFileIds, int tripId);
    public Task DeleteImageFromAlbumAsync(int photoId);
}

public class AlbumService
{
    
}