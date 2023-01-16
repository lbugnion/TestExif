using System.Drawing;
using System.Text;

using (var pictureStream = File.Open(@"C:\Users\lbugn\Desktop\el20221202018D.jpg", FileMode.Open))
{
    var imageMetadata = Image.FromStream(pictureStream, false, false);

    foreach (var item in imageMetadata.PropertyItems)
    {
        if (item.Value == null)
        {
            continue;
        }

        var encoding = new ASCIIEncoding();
        var text = encoding.GetString(
            item.Value,
            0,
            item.Len - 1);

        Console.WriteLine($"0x{item.Id}: {text}");
    }
}