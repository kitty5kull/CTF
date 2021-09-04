using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;

namespace image2hex
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.Error.WriteLine("Usage: image2hex <channels> <functions> <imagefile>");
				return 1;
			}

			var img = Image.FromFile(args[2]);
			var data = GetBitmapData(img, PixelFormat.Format32bppArgb);

			int ptr = 0;
			string c = args[0].ToUpper();
			string f = args[1].ToUpper();

			bool[] channels = { c.Contains("B"), c.Contains("R"), c.Contains("G"), c.Contains("A") };
			bool[] functions = { f.Contains("F"), f.Contains("0"), f.Contains("A"),  f.Contains("S"), f.Contains("M") };
			bool[] haschan =new bool[4];

			for (int y = 0; y < img.Height; y++)
			{
				int linesum = 0;

				for (int x = 0; x < img.Width; x++)
				{
					int sum = 0;

					for (int i = 0; i < 4; i++)
					{
						if (channels[i])
						{
							haschan[i] = (data[ptr + i] > 0);

							if (functions[2])
							{
								if (((!functions[1]) || data[ptr + i] > 0) && ((!functions[0]) || data[ptr + i] < 255))
									Console.Write(data[ptr + i].ToString("x2"));
								else
									Console.Write("  ");
							}
							else if (functions[3] || functions[4])
								sum += data[ptr + i];
						}
						else
						{
							haschan[i] = true;
						}
					}

					linesum += sum;

					if (functions[3])
						Console.Write((sum & 1) > 0 ? "1": "0");
					else if (!functions[4])
						Console.Write(" ");

					ptr += 4;
				}

				if (functions[4])
					Console.WriteLine(linesum.ToString("x2"));
				else
					Console.WriteLine();
			}

			return 0;
		}

		private static byte[] GetBitmapData(Image img, PixelFormat format)
		{
			var bmp = new Bitmap(img);
			var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, format);

			// Get the address of the first line.
			var ptr = bmpData.Scan0;

			// Declare an array to hold the bytes of the bitmap.
			int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
			var rgbValues = new byte[bytes];

			// Copy the RGB values into the array.
			System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

			// Unlock the bits.
			bmp.UnlockBits(bmpData);

			return rgbValues;
		}
	}
}
