namespace Visprech.Core.Interfaces
{
    public interface IZipFileExtractor
    {
        Task ExtractFile(string zipFilePath, string fileToExtract, string extractFileTo);
    }
}
