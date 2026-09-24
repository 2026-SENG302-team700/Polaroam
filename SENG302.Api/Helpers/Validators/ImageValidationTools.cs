using SENG302.Api.Constants;
using System.Xml;

namespace SENG302.Api.Helpers.Validators;

public static class ImageValidator
{
    public static readonly string InvalidMimeString = "INVALID MIME";

    /// <summary>
    /// A list of image types that record which magic numbers correspond to which mime types
    /// </summary>
    static readonly Dictionary<string, string> ImageTypes = new() {
        {"89504e47", "image/png"},
        {"47494638", "image/gif"},
        {"ffd8ffe0", "image/jpeg"},
        {"ffd8ffe1", "image/jpeg"},
        {"ffd8ffe2", "image/jpeg"},
        {"ffd8ffe3", "image/jpeg"},
        {"ffd8ffe8", "image/jpeg"},
        {"3c737667", "image/svg+xml"},
        {"52494646????????57454250", "image/webp"}
    };

    /// <summary>
    /// Utilizes the first 4-12 bytes of a file to check the real mime type of the file
    /// It will return the real mime type, or "INVALID MIME" if it is not one of the valid types
    /// </summary>
    /// <param name="file">The file to be checked</param>
    /// <returns>The real mime type based on the first 4-12 bytes of the file if it is valid, else "INVALID MIME"</returns>
    public static string GetRealImageMime(IFormFile file)
    {
        if (file.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
        {
            return HasValidSvgRoot(file) ? "image/svg+xml" : InvalidMimeString;
        }

        var stream = file.OpenReadStream();

        byte[] magicNumber = new byte[12];

        var bytesRead = stream.Read(magicNumber, 0, magicNumber.Length);

        string fileMagicNumber = Convert.ToHexStringLower(magicNumber);

        foreach (var (refrMagicNumber, mimeType) in ImageTypes)
        {

            var valid = true;

            for (int i = 0; i < fileMagicNumber.Length / 8; i++)
            {
                if (i * 8 >= refrMagicNumber.Length) break;

                var fileByteStr = fileMagicNumber.Substring(i * 8, 8);
                var refrByteStr = refrMagicNumber.Substring(i * 8, 8);

                if (!(refrByteStr == "????????" || fileByteStr == refrByteStr))
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                return mimeType;
            }
        }

        return InvalidMimeString;
    }

    /// <summary>
    /// Validates SVG files by parsing their XML and checking the root element.
    /// Unlike binary image formats, an SVG may begin with whitespace, comments,
    /// a byte-order mark, or an XML declaration before its svg element.
    /// </summary>
    private static bool HasValidSvgRoot(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true
            });

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;

                return reader.LocalName.Equals("svg", StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(reader.NamespaceURI) ||
                     reader.NamespaceURI == "http://www.w3.org/2000/svg");
            }
        }
        catch (XmlException)
        {
            return false;
        }

        return false;
    }

    public static bool IsValidFilesize(IFormFile file)
    {
        return (file.Length <= 10000000);
    }

    public static bool IsValidMimeType(IFormFile file)
    {
        var contentType = file.ContentType;
        var realContentType = GetRealImageMime(file);

        return !(!MimeTypeSets.Images.Contains(contentType) || realContentType == InvalidMimeString);
    }

}
