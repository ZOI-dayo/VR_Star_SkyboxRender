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
  private const double Offset = 0.2;

  public static void Main()
  {
    var nowTime = DateTime.Now;
    using var logSWriter =
      new StreamWriter(
        Path.GetDirectoryName(OutFile)
        + @"\log_convert_"
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

      byte RoundToByte(double source) => Convert.ToByte((int)Math.Round(Math.Max(source, 1) * 255));

      imageData[count * 4 + 0] = RoundToByte(r / weight);
      imageData[count * 4 + 1] = RoundToByte(g / weight);
      imageData[count * 4 + 2] = RoundToByte(b / weight);
      imageData[count * 4 + 3] = RoundToByte(weight);
      count++;
    }

    var ms = new MemoryStream(imageData);
    var bm = new Bitmap(ms);
    bm.Save(OutFile, ImageFormat.Png);

    logSWriter.WriteLine($"End: {DateTime.Now.ToString(new CultureInfo("ja-JP"))}");
  }
}