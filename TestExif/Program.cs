using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Text;

var mustSave = false;
Image image = null;
IImageFormat format = null; 

using (var pictureStream = File.Open(@"C:\Users\lbugn\Desktop\el20221202018D-NEW.jpg", FileMode.Open))
{
    var tuple = await Image.LoadWithFormatAsync(pictureStream);
    image = tuple.Image;
    format = tuple.Format;
}

var values = image.Metadata.ExifProfile.Values;

foreach (var value in values)
{
    if (value.IsArray)
    {
        byte[]? array = value.GetValue() as byte[];

        if (array != null)
        {
            var encoding = new ASCIIEncoding();
            var text = encoding.GetString(
                array,
                0,
                array.Length - 1);
            Console.WriteLine($"{value.Tag}: Byte array: {text}");
        }
        else
        {
            if (value.Tag == ExifTag.GPSLatitude
                || value.Tag == ExifTag.GPSLongitude)
            {
                var latitudeArray = (Rational[])value.GetValue();
                var latitude =
                    latitudeArray[0].Numerator
                    + (latitudeArray[1].Numerator / 60D)
                    + ((latitudeArray[2].Numerator / 1000000D) / 3600D);

                Console.WriteLine($"{value.Tag}: {latitude}");
            }
            else
            {
                Console.WriteLine($"{value.Tag}: Not a byte array");
            }
        }
    }
    else
    {
        var valueString = value.GetValue().ToString();

        Console.WriteLine($"{value.Tag}: {valueString}");

        if (valueString.Contains("LogoLicious"))
        {
            value.TrySetValue(string.Empty);
            mustSave = true;
        }
    }
}

if (mustSave)
{
    using (var saveStream = File.OpenWrite(@"C:\Users\lbugn\Desktop\el20221202018D-NEW.jpg"))
    {
        await image.SaveAsync(saveStream, format);
    }
}