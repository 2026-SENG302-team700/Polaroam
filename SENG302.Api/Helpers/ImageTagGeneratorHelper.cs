namespace SENG302.Api.Helpers;

public class ImageTagGeneratorHelper
{
    public static async Task<FormFile> createFormFileForTagging(IFormFile image)
    {
        var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        ms.Position = 0;

        var safeFormFile = new FormFile(ms, 0, ms.Length, image.Name, image.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = image.ContentType
        };

        return safeFormFile;
    }
}