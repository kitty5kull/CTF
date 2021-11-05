using System;
using System.IO;

namespace rot
{
	class Program
	{
		static void Main(string[] args)
		{
			int bufsize = 512;
			var buffer = new byte[bufsize];
			int shift = Convert.ToInt32(args[0]);
			var stream = Console.OpenStandardInput(bufsize);

			while(true)
			{
				int read = stream.Read(buffer, 0, 512);
				if (read < 1)
					break;

				for (int i = 0; i < read; i++)
					Console.Write(new string(Convert.ToChar((buffer[i] + shift) & 0xff), 1));
			}
		}
	}
}
