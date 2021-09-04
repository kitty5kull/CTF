using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace freq
{
	class Program
	{
		private static bool AsAscii = true;

		private class BufferTuple
		{
			public byte[] Data;
			public List<int> Offsets;
			public int Count => Offsets.Count;
		}
	
		static int Main(string[] args)
		{
			if (!ConsumeParameters(args))
			{
				PrintUsage();
				return 1;
			}

			try
			{
				var bytes = File.ReadAllBytes(args[0]);
				DoFrequencyAnalysis(bytes);
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
			Console.WriteLine("Usage: freq <filename>");
		}

		private static bool ConsumeParameters(string[] args)
		{
			return args.Length == 1;
		}
		private static void DoFrequencyAnalysis(byte[] bytes)
		{
			for (int i = 1; i <= bytes.Length; i++)
				if (!SeachForMultigrams(i, bytes))
					return;
		}

		private static bool SeachForMultigrams(int n, byte[] bytes)
		{
			var grams = new List<BufferTuple>();

			for (int i = 0; i < bytes.Length - n; i++)
			{
				bool found = false;
				var buf = new byte[n];
				Array.Copy(bytes, i, buf, 0, n);

				foreach (var tuple in grams)
				{
					if (AreBuffersEqual(tuple.Data, buf))
					{
						found = true;
						tuple.Offsets.Add(i);
						break;
					}
				}

				if (!found)
				{
					grams.Add(new BufferTuple {Data = buf, Offsets = new List<int> {i}});
				}
			}

			foreach (var tuple in grams.OrderByDescending(t => t.Count).Take(256))
			{
				if (AsAscii)
					Console.Write(Encoding.ASCII.GetString(tuple.Data));
				else
					Console.Write(string.Join("", tuple.Data.Select(t => t.ToString("x2"))));

				Console.Write(":");
				Console.Write(tuple.Count);
				Console.Write(":");
				Console.Write(string.Join(",", tuple.Offsets));
				Console.WriteLine();
			}

			return grams.Any(g => g.Count > 1);
		}

		private static bool AreBuffersEqual(byte[] a, byte[] b)
		{
			if (a == null || b == null)
				return false;

			if (a.Length != b.Length)
				return false;

			for (int i =0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;

			return true;
		}
	}
}
