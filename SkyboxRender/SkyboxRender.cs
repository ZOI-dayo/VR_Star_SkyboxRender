using System.Globalization;

namespace SkyboxRender;

public class SkyboxRender
{
  private const string DataPath = @"F:\out\Compiled";
  private const string OutFile = @"F:\out\out.bin";
  private const int ImageHeight = 4096;

  private const double ZeroMagPower = 0.2d;

  // private readonly double _powerRate = Math.Pow(100, 1D / 5);
  private const double StarDiameter = 0.05d / 60 / 60;
  private const double PixelSize = 180d / ImageHeight;

  private const double DisplayGamma = 2.2;

  public static void Main()
  {
    // RGB
    var imageData = new double[ImageHeight * 2 * ImageHeight * 3];

    
    var nowTime = DateTime.Now;
    using var logSWriter =
      new StreamWriter(
        Path.GetDirectoryName(OutFile)
        + @"\log_skybox_"
        + nowTime.ToString($"{nowTime:yyyyMMddHHmmss}")
        + ".txt");
    logSWriter.WriteLine($"Begin: {DateTime.Now.ToString(new CultureInfo("ja-JP"))}");
    using var dataStream = new BinaryReader(new FileStream(DataPath, FileMode.Open));
    long count = 0;
    while (dataStream.BaseStream.Position != dataStream.BaseStream.Length)
    {
      count++;
      if (count % 1000000 == 0) Console.WriteLine(count);
      var ra = dataStream.ReadDouble();
      var dec = dataStream.ReadDouble();
      var vMag = dataStream.ReadSingle();
      var bvColor = dataStream.ReadSingle();
      var lightPower = ZeroMagPower * Math.Pow(100, -vMag / 5);
      // deg[]
      var boxSize = new[] { StarDiameter / Math.Sin(dec / 180 * Math.PI), StarDiameter };
      // deg[]
      var centerLoc = new[] { ra, -dec + 90 };

      // px[]
      var boundBox = new[,]
      {
        {
          (int)Math.Floor((centerLoc[0] - boxSize[0] / 2) / PixelSize),
          (int)Math.Ceiling((centerLoc[0] + boxSize[0] / 2) / PixelSize) - 1
        },
        {
          (int)Math.Floor((centerLoc[1] - boxSize[1] / 2) / PixelSize),
          (int)Math.Ceiling((centerLoc[1] + boxSize[1] / 2) / PixelSize) - 1
        }
      };
      // http://www.uenosato.net/hr_diagram/hrdiagram2.html 2-a
      var temperature = 9000d / (bvColor + 0.85);
      // https://zwxadz.hateblo.jp/entry/2017/05/02/065537
      var colorX = temperature <= 7000
        ? -4.607 / Math.Pow(temperature, 3) * Math.Pow(10, 9) +
          2.9678 / Math.Pow(temperature, 2) * Math.Pow(10, 6) + 0.09911 / temperature * Math.Pow(10, 3) + 0.244063
        : -2.0064 / Math.Pow(temperature, 3) * Math.Pow(10, 9) + 1.9018 / Math.Pow(temperature, 2) * Math.Pow(10, 6) +
          0.24748 / temperature * Math.Pow(10, 3) + 0.23704;

      var colorY = -3d * Math.Pow(colorX, 2) + 2.87d * colorX - 0.275;
      var colorZ = 1 - colorX - colorY;
      // http://www.uenosato.net/hr_diagram/hrdiagram3.html 3
      var colorRgbLiner = new[]
      {
        3.2410 * colorX + -1.5374 * colorY + -0.4986 * colorZ,
        -0.9692 * colorX + 1.8760 * colorY + 0.0416 * colorZ,
        0.0556 * colorX + -0.2040 * colorY + 1.0570 * colorZ
      };
      var colorRgb = colorRgbLiner.Select(val => Math.Pow(val, DisplayGamma)).ToArray();

      void WritePixel(int x, int y, double weight)
      {
        imageData[x + ImageHeight * y + 0] += colorRgb[0] * weight;
        imageData[x + ImageHeight * y + 1] += colorRgb[1] * weight;
        imageData[x + ImageHeight * y + 2] += colorRgb[2] * weight;
      }

      for (var x = boundBox[0, 0]; x <= boundBox[0, 1]; x++)
      {
        for (var y = boundBox[1, 0]; y <= boundBox[1, 1]; y++)
        {
          double weight;
          if (x * PixelSize <= centerLoc[0] - boxSize[0] / 2 && centerLoc[0] + boxSize[0] / 2 <= (x + 1) * PixelSize)
          {
            if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2 &&
                centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              weight = 1;
            }
            else if (centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              var yRate = (centerLoc[1] + boxSize[1] / 2 - y * PixelSize) / boxSize[1];
              weight = GetAreaRatio_0x1(yRate);
            }
            else if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2)
            {
              var yRate = (y + 1 - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              weight = GetAreaRatio_0x1(yRate);
            }
            else
            {
              var yMin = (y * PixelSize - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              var yMax = 1 - (centerLoc[1] + boxSize[1] / 2 - (y + 1) * PixelSize) / boxSize[1];
              weight = GetAreaRatio_0x2(yMin, yMax);
            }
          }
          else if (x * PixelSize <= centerLoc[0] - boxSize[0] / 2)
          {
            var xRate = ((x + 1) * PixelSize - (centerLoc[0] - boxSize[0] / 2)) / boxSize[0];
            if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2 &&
                centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              weight = GetAreaRatio_0x1(xRate);
            }
            else if (centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              var yRate = (centerLoc[1] + boxSize[1] / 2 - y * PixelSize) / boxSize[1];
              weight = GetAreaRatio_1x1(xRate, yRate);
            }
            else if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2)
            {
              var yRate = (y + 1 - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              weight = GetAreaRatio_1x1(xRate, yRate);
            }
            else
            {
              var yMin = (y * PixelSize - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              var yMax = 1 - (centerLoc[1] + boxSize[1] / 2 - (y + 1) * PixelSize) / boxSize[1];
              weight = GetAreaRatio_1x2(xRate, yMin, yMax);
            }
          }
          else if (centerLoc[0] + boxSize[0] / 2 <= (x + 1) * PixelSize)
          {
            var xRate = (centerLoc[0] + boxSize[0] / 2 - x * PixelSize) / boxSize[0];
            if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2 &&
                centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              weight = GetAreaRatio_0x1(xRate);
            }
            else if (centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              var yRate = (centerLoc[1] + boxSize[1] / 2 - y * PixelSize) / boxSize[1];
              weight = GetAreaRatio_1x1(xRate, yRate);
            }
            else if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2)
            {
              var yRate = (y + 1 - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              weight = GetAreaRatio_1x1(xRate, yRate);
            }
            else
            {
              var yMin = (y * PixelSize - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              var yMax = 1 - (centerLoc[1] + boxSize[1] / 2 - (y + 1) * PixelSize) / boxSize[1];
              weight = GetAreaRatio_1x2(xRate, yMin, yMax);
            }
          }
          else
          {
            var xMin = (x * PixelSize - (centerLoc[0] - boxSize[0] / 2)) / boxSize[0];
            var xMax = 1 - (centerLoc[0] + boxSize[0] / 2 - (x + 1) * PixelSize) / boxSize[0];
            if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2 &&
                centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              weight = GetAreaRatio_0x2(xMin, xMax);
            }
            else if (centerLoc[1] + boxSize[1] / 2 <= (y + 1) * PixelSize)
            {
              var yRate = (centerLoc[1] + boxSize[1] / 2 - y * PixelSize) / boxSize[1];
              weight = GetAreaRatio_1x2(yRate, xMin, xMax);
            }
            else if (y * PixelSize <= centerLoc[1] - boxSize[1] / 2)
            {
              var yRate = (y + 1 - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              weight = GetAreaRatio_1x2(yRate, xMin, xMax);
            }
            else
            {
              var yMin = (y * PixelSize - (centerLoc[1] - boxSize[1] / 2)) / boxSize[1];
              var yMax = 1 - (centerLoc[1] + boxSize[1] / 2 - (y + 1) * PixelSize) / boxSize[1];
              weight = GetAreaRatio_2x2(xMin, xMax, yMin, yMax);
            }
          }

          WritePixel(x, y, weight);
        }
      }
    }

    using var outFStream = File.Open(OutFile, FileMode.OpenOrCreate);
    outFStream.SetLength(0);
    foreach (var t in imageData)
    {
      outFStream.Write(BitConverter.GetBytes(t), 0, 8);
    }
    logSWriter.WriteLine($"End: {DateTime.Now.ToString(new CultureInfo("ja-JP"))}");
  }

  // 1軸のみ
  private static double GetAreaRatio_0x1(double x)
  {
    return 1 / Math.PI * Math.Acos(1 - 2 * x) - 2 / Math.PI * (1 - 2 * x) * Math.Sqrt(x - Math.Pow(x, 2));
  }

  // 1軸(2本)
  private static double GetAreaRatio_0x2(double min, double max)
  {
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
  private static double GetAreaRatio_1x2(double x, double yMin, double yMax)
  {
    return GetAreaRatio_0x1(x) - GetAreaRatio_1x1(yMin, x) - GetAreaRatio_1x1(1 - yMax, x);
  }

  // 2軸(2+2本)
  private static double GetAreaRatio_2x2(double xMin, double xMax, double yMin, double yMax)
  {
    return 1 - GetAreaRatio_0x1(xMin) - GetAreaRatio_0x1(1 - xMax) - GetAreaRatio_1x2(yMin, xMin, xMax) -
           GetAreaRatio_1x2(1 - yMax, xMin, xMax);
  }
}