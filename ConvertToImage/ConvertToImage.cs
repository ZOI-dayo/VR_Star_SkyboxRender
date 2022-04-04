using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SkyboxRender;

public class ConvertToImage
{
  private const int ImageHeight = 4096;
  private const string DataPath = @"F:\out\out.bin";
  private const string OutFile = @"F:\out\out.png";
  private const double Offset = 0.5;

  public static void Main()
  {
    var nowTime = DateTime.Now;
    using var logSWriter =
      new StreamWriter(
        Path.GetDirectoryName(OutFile)
        + @"\log\log_convert_"
        + nowTime.ToString($"{nowTime:yyyyMMddHHmmss}")
        + ".txt");
    logSWriter.WriteLine($"Begin: {DateTime.Now.ToString(new CultureInfo("ja-JP"))}");

    using var dataStream = new BinaryReader(new FileStream(DataPath, FileMode.Open));
    var imageData = new byte[ImageHeight * 2 * ImageHeight * 4];
    var count = 0;
    while (dataStream.BaseStream.Position != dataStream.BaseStream.Length)
    {
      var r = dataStream.ReadDouble();
      var g = dataStream.ReadDouble();
      var b = dataStream.ReadDouble();
      var weight = Math.Max(Math.Max(r, g), b) / Offset;
      
      if (double.IsNaN(weight)) continue;

      byte RoundToByte(double source)
      {
        try
        {
          return Convert.ToByte((int)Math.Round(Math.Min(source, 1) * 255));
        }
        catch(Exception e)
        {
          Console.WriteLine(r);
          Console.WriteLine(g);
          Console.WriteLine(b);
          Console.WriteLine(source);
          throw e;
        }
      }

      imageData[count * 4 + 1] = RoundToByte(weight != 0 ? r / weight : 0);
      imageData[count * 4 + 2] = RoundToByte(weight != 0 ? g / weight : 0);
      imageData[count * 4 + 3] = RoundToByte(weight != 0 ? b / weight : 0);
      imageData[count * 4 + 0] = RoundToByte(Math.Max(1 - weight, 0));
      count++;
    }
    Console.WriteLine(count);

    Bitmap bmp = new Bitmap(ImageHeight * 2, ImageHeight);

    Rectangle rect = new Rectangle(0, 0, ImageHeight * 2, ImageHeight);
    System.Drawing.Imaging.BitmapData
      bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    IntPtr ptr = bmpData.Scan0;
    Marshal.Copy(imageData, 0,ptr,imageData.Length);
    bmp.UnlockBits(bmpData);
    bmp.Save(OutFile, ImageFormat.Png);

    logSWriter.WriteLine($"End: {DateTime.Now.ToString(new CultureInfo("ja-JP"))}");
  }
}