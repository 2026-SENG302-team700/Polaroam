namespace SENG302.Api.Services;

public interface IScrapboardService
{
    
}

public class ScrapboardService :IScrapboardService
{
    private readonly FileService _fileService;

    public ScrapboardService(FileService fileService)
    {
        _fileService = fileService;
    }
}