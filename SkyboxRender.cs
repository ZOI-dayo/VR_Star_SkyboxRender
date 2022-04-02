namespace SkyboxRender;

public class SkyboxRender
{
  private const string DataPath = @"F:\out\Compiled";
  private const string ImagePath = @"F:\out\Compiled";
  private const int ImageHeight = 4096;

  private const double ZeroMagPower = 2d;

  // private readonly double _powerRate = Math.Pow(100, 1D / 5);
  private const double StarDiameter = 0.05d / 60 / 60;
  private const double PixelSize = 180d / ImageHeight;

  public static void Main()
  {
    var imageData = new double[ImageHeight * 2, ImageHeight, 4];

    using (var dataStream = new BinaryReader(new FileStream(DataPath, FileMode.Open)))
    {
      while (dataStream.BaseStream.Position != dataStream.BaseStream.Length)
      {
        var ra = dataStream.ReadDouble();
        var dec = dataStream.ReadDouble();
        var vMag = dataStream.ReadSingle();
        var bvColor = dataStream.ReadSingle();
        var lightPower = ZeroMagPower * Math.Pow(100, -vMag / 5);
        var boxSize = new[] { StarDiameter / Math.Sin(dec / 180 * Math.PI), StarDiameter };
        var centerLoc = new[] { ra / PixelSize, (-dec + 90) / PixelSize };
      }
    }

    using (var imageStream = new BinaryWriter(new FileStream(ImagePath, FileMode.OpenOrCreate)))
    {
    }
  }

  // 1軸のみ
  private static double GetAreaRatio_0x1(double x)
  {
    return 1 / Math.PI * Math.Acos(1 - 2 * x) - 2 / Math.PI * (1 - 2 * x) * Math.Sqrt(x - Math.Pow(x, 2));
  }

  // 1軸(2本)
  private static double GetAreaRatio_0x2(double a, double b)
  {
    var max = Math.Max(a, b);
    var min = Math.Min(a, b);
    return 1 - GetAreaRatio_0x1(min) - GetAreaRatio_0x1(1 - max);
  }

  // 2軸
  private static double GetAreaRatio_1x1(double x, double y)
  {
    double Calc(double a, double b) =>
      1 / (2 * Math.PI) * (Math.Acos(1 - 2 * a) - Math.Atan(1 - 2 * b) - 2 * a - 2 * b + 8 * a * b);

    if (x <= 0.5 && y <= 0.5)
    {
      if (x * x + y * y > 1) return 0;
      return Calc(x, y);
    }

    switch ((x - 0.5) * (y - 0.5))
    {
      case < 0:
      {
        var max = Math.Max(x, y);
        var min = Math.Min(x, y);
        if (x * x + y * y > 1) return GetAreaRatio_0x1(min);
        return GetAreaRatio_0x1(min) - Calc(max - 0.5, min);
      }
      case > 0:
        if (x * x + y * y > 1) return 1 - GetAreaRatio_0x1(1 - x) - GetAreaRatio_0x1(1 - y);
        return 1 - GetAreaRatio_0x1(1 - x) - GetAreaRatio_0x1(1 - y) + GetAreaRatio_1x1(1 - x, 1 - y);
    }

    return 0;
  }

  // 2軸(1+2本)
  private static double GetAreaRatio_1x2(int xa, int xb, int y)
  {
    var xMax = Math.Max(xa, xb);
    var xMin = Math.Min(xa, xb);
    return GetAreaRatio_0x1(y) - GetAreaRatio_1x1(xMin, y) - GetAreaRatio_1x1(1 - xMax, y);
  }
}