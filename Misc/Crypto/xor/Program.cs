using System;
using System.IO;

namespace xor
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length != 1)
			{
				PrintUsage();
				return 1;
			}

			try
			{
				var bytes = File.ReadAllBytes(args[0]);
				var xor = new XOR(bytes);
				xor.Decypher();
				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
				Console.Error.WriteLine(ex.StackTrace);
				return -1;
			}		
		}

		private static void PrintUsage()
		{
			Console.WriteLine("Usage: xor <filename>");
		}

	}
}
