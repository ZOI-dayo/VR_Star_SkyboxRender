namespace SkyboxRender;

public class SkyboxRender
{
  private const string DataPath = @"F:\out\Compiled";
  private const string ImagePath = @"F:\out\Compiled";
  private const int ImageHeight = 4096;

  private const double ZeroMagPower = 0.2d;

  // private readonly double _powerRate = Math.Pow(100, 1D / 5);
  private const double StarDiameter = 0.05d / 60 / 60;
  private const double PixelSize = 180d / ImageHeight;

  private const double DisplayGamma = 2.2;

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
        // deg[]
        var boxSize = new[] { StarDiameter / Math.Sin(dec / 180 * Math.PI), StarDiameter };
        // deg[]
        var centerLoc = new[] { ra, -dec + 90 };

        // px[]
        var boundBox = new[,]
        {
          {
            (int)Math.Floor((boxSize[0] - centerLoc[0] / 2) / PixelSize),
            (int)Math.Ceiling((boxSize[0] + centerLoc[0] / 2) / PixelSize) - 1
          },
          {
            (int)Math.Floor((boxSize[1] - centerLoc[1] / 2) / PixelSize),
            (int)Math.Ceiling((boxSize[1] + centerLoc[1] / 2) / PixelSize) - 1
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
          imageData[x, y, 0] += colorRgb[0];
          imageData[x, y, 1] += colorRgb[1];
          imageData[x, y, 2] += colorRgb[2];
          imageData[x, y, 3] += lightPower;
        }

        if (boundBox[0, 0] == boundBox[0, 1])
        {
          var x = boundBox[0, 0];
          if (boundBox[1, 0] == boundBox[1, 1])
          {
            var y = boundBox[1, 0];
            WritePixel(x, y, 1);
            continue;
          }

          for (var y = boundBox[1, 0]; y < boundBox[1, 1]; y++)
          {
            double[] nearest;
            if (y + 1 < centerLoc[1] / PixelSize)
            {
              nearest = new[] { centerLoc[0], y + 1 };
            }
            else if (centerLoc[1] / PixelSize < y)
            {
              nearest = new[] { centerLoc[0], y };
            }
            else
            {
              nearest = centerLoc;
            }

            var distance =
              Math.Sqrt(Math.Pow(centerLoc[0] - nearest[0], 2) + Math.Pow(centerLoc[0] - nearest[0], 2));
            var weight = GetAreaRatio_0x2(,);
            // ここまで
            WritePixel(x, y, weight);
          }

          continue;
        }

        for (var x = boundBox[0, 0]; x < boundBox[0, 1]; x++)
        {
          if (boundBox[1, 0] == boundBox[1, 1])
          {
            var y = boundBox[1, 0];
            imageData[x, y, 0] += colorRgb[0];
            imageData[x, y, 1] += colorRgb[1];
            imageData[x, y, 2] += colorRgb[2];
            imageData[x, y, 3] += lightPower;
            continue;
          }

          for (var y = boundBox[1, 0]; y < boundBox[1, 1]; y++)
          {
            imageData[,] = new double[2] { };
          }
        }
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