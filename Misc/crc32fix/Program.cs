using System;
using System.Text;

namespace crc32fix
{
	class Program
	{
		static void Main(string[] args)
		{
			string text = Console.ReadLine();
			var crc = new Crc32();
			var bytes = Encoding.ASCII.GetBytes(text);
			crc.FixChecksum(bytes, bytes.Length - 4, 0xe1ca95ee);
			foreach(byte b in bytes)
				Console.Write($"\\x{b:x2}");

			Console.ReadLine();
		}
	}
}
