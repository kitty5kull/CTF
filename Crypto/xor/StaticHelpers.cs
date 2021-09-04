using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xor
{
	public static class StaticHelpers
	{
		public const string ALPHABET_STRING = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
		public static readonly HashSet<byte> ALPHABET_BYTES = new HashSet<byte>(new byte[] { 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 32 });
		public static readonly HashSet<int> ALPHABET_INTS = new HashSet<int>(new int[] { 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 32 });
		public static readonly HashSet<int> ALPHABET_INTS_LOWER = new HashSet<int>(new int[] { 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122 });

		public static int CompareByteSequence(this byte[] left, byte[] right, int startIndexLeft, int startIndexRight)
		{
			int sequence = 0;

			for (int i = 0; i < left.Length; i++)
			{
				if (i + startIndexLeft >= left.Length)
					return sequence;
				if (i + startIndexRight >= right.Length)
					return sequence;
				if (left[i + startIndexLeft] != right[i + startIndexRight])
					return sequence;
				sequence++;
			}

			return sequence;
		}

		public static bool CompareByteSequence(this byte[] left, byte[] right, int startIndexLeft, int startIndexRight, int length)
		{
			for (int i = 0; i < length; i++)
			{
				if (i + startIndexLeft >= left.Length)
					return false;
				if (i + startIndexRight >= right.Length)
					return false;
				if (left[i + startIndexLeft] != right[i + startIndexRight])
					return false;
			}

			return true;
		}

		public static void STARelay(object resetevents)
		{
			KeyValuePair<ManualResetEvent, ManualResetEvent[]> info = (KeyValuePair<ManualResetEvent, ManualResetEvent[]>)resetevents;
			WaitHandle.WaitAll(info.Value);
			info.Key.Set();
		}

		public static int GGT(int a, int b)
		{
			if (a == b) return a;

			int g = a > b ? a : b;
			int s = a > b ? b : a;

			int m = g % s ;

			return m == 0 ? s : GGT(s, g % s);
		}

	}
}
