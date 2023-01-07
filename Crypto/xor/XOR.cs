using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xor
{
	public class XOR
	{
		private byte[] _bytes;
		private Dictionary<int, int> _distances;


		public XOR(byte[] bytes)
		{
			_bytes = bytes;
		}


		public void Decypher()
		{
			ReadMultigramDistances();
	
			int keylen = GetEstimatedKeyLength();
			if (keylen < 1)
				return;

			var candidates = GetKeyCandidates(keylen);

			var best = GetBestKey(keylen, candidates);
			PrintKey(best);
			PrintDecyphered(best);

			var bestLk = GetBestLowerCaseKey(keylen, candidates);
			PrintKey(bestLk);
			PrintDecyphered(bestLk);
		}


		private void PrintDecyphered(byte[] best)
		{
			var decoded = Enumerable.Range(0, _bytes.Length)
								 .Select(i => Convert.ToByte(_bytes[i] ^ best[i % best.Length]))
								 .ToArray();
			Log(Encoding.ASCII.GetString(decoded));
		}


		private byte[] GetBestKey(int keylen, Dictionary<int, List<int>> candidates)
		{
			var best = Enumerable.Range(0, keylen)
				.Select(i => Convert.ToByte(candidates[i].First()))
				.ToArray();
			return best;
		}


		private void PrintKey(byte[] best)
		{
			var bestKey = Encoding.ASCII.GetString(best);
			Log(new string('*', 80));
			Log($"{bestKey} [{string.Join(",", best)}]");
			Log(new string('*', 80));
		}


		private byte[] GetBestLowerCaseKey(int keylen, Dictionary<int, List<int>> candidates)
		{
			var best = Enumerable.Range(0, keylen)
				.Select(i => Convert.ToByte(candidates[i].First(k => StaticHelpers.ALPHABET_INTS_LOWER.Contains(k))))
				.ToArray();
			return best;
		}


		private Dictionary<int, List<int>> GetKeyCandidates(int keylen)
		{
			var cypherByKey = GetCypherTextByKeyByte(keylen);
			var scores = new Dictionary<int, Dictionary<int, int>>();

			for (int keyOffset = 0; keyOffset < keylen; keyOffset++)
			{
				scores.Add(keyOffset, new Dictionary<int, int>());
				var ourBytes = cypherByKey[keyOffset];
				for (int keyChar = 0; keyChar < 256; keyChar++)
				{
					int alphaScore = 0;
					int printableScore = 0;
					int keyPrintable = keyChar >=64 && keyChar < 128 ? 1 : 0;
					int keyAplha = StaticHelpers.ALPHABET_INTS.Contains(keyChar) ? 1 : 0;

					foreach(byte b in ourBytes)
					{
						var xor = b ^ keyChar;
						if (xor>=64 && xor < 128)
							printableScore++;
						if (StaticHelpers.ALPHABET_INTS.Contains(xor))
							alphaScore++;
					}
					scores[keyOffset].Add(keyChar, (10*keyAplha + keyPrintable) * (alphaScore + 10 * printableScore));
				}
			}

			return scores.ToDictionary(s => s.Key, s => s.Value.OrderByDescending(v => v.Value).Take(3).Select(v => v.Key).ToList());
		}


		private Dictionary<int, byte[]> GetCypherTextByKeyByte(int keylen)
		{
			var retval = new Dictionary<int, byte[]>();

			for (int keyOffset = 0; keyOffset < keylen; keyOffset++)
			{
				var bList = new List<byte>();
				for (int dataOffset = keyOffset; dataOffset < _bytes.Length; dataOffset += keylen)
					bList.Add(_bytes[dataOffset]);
				retval.Add(keyOffset, bList.ToArray());
			}

			return retval;
		}


		private int GetEstimatedKeyLength()
		{
			Log($"Estimating key length ...");

			Dictionary<int, int> GGTHigh = new Dictionary<int, int>();
			List<KeyValuePair<int, int>> analyse = _distances.OrderByDescending(d => d.Value).Take(Math.Min(10, _distances.Count / 4)).ToList();
			int ggt;

			for (int i = 0; i < analyse.Count; i++)
				for (int j = i + 1; j < analyse.Count; j++)
				{
					ggt = StaticHelpers.GGT(analyse[i].Key, analyse[j].Key);
					if (ggt > 1)
					{
						//Log(string.Format("GGT({0},{1}): {2}\r\n", analyse[i].Key, analyse[j].Key, ggt));
						if (!GGTHigh.ContainsKey(ggt))
							GGTHigh.Add(ggt, 1);
						else
							GGTHigh[ggt]++;
					}
				}

			if (GGTHigh.Count == 0)
			{
				Log("ERROR: Unable to estimate key length, analysis can not continue.");
				return 0;
			}

			var ggtitem = GGTHigh.OrderByDescending(g => g.Value).First();
			ggt = ggtitem.Key;

			var sum = GGTHigh.Values.Sum();
			var score = GGTHigh.Where(t => t.Key % ggt == 0).Sum(i => i.Value);

			Log($"Estimated key length: {ggt:n0} bytes (confidence: {1.0 * score / sum:p1}).");
			return ggt;
		}


		private void ReadMultigramDistances()
		{
			_distances = new Dictionary<int, int>();
			var sequences = ExtractSequences();

			for (int i = 2; i <= 8; i++)
				ReadDistances(sequences, i);
		}


		private void ReadDistances(Tuple<ulong, int>[] sequences, int byteLength)
		{
			Tuple<ulong, int>[] multigrams;

			if (byteLength == 8)
				multigrams = sequences;
			else
			{
				ulong mask = (1ul << (byteLength * 8)) - 1;
				multigrams = new Tuple<ulong, int>[sequences.Length];
				for (int i = 0; i < multigrams.Length; i++)
					multigrams[i] = new Tuple<ulong, int>(sequences[i].Item1 & mask, i);
			}


			foreach (var group in multigrams.GroupBy(m => m.Item1))
			{
				var sequence = group.ToList();

				if (sequence.Count < 2)
					continue;

				for (int i = 0; i < sequence.Count - 1; i++)
				{
					var distance = sequence[i + 1].Item2 - sequence[i].Item2;
					if (_distances.ContainsKey(distance))
						_distances[distance]++;
					else
						_distances.Add(distance, 1);
				}
			}
		}

		private Tuple<ulong, int>[] ExtractSequences()
		{
			var multigrams = new Tuple<ulong, int>[_bytes.Length - 1];
			var tmp = new byte[8];

			for (int i = 0; i < _bytes.Length - 7; i++)
				multigrams[i] = new Tuple<ulong, int>(BitConverter.ToUInt64(_bytes, i), i);

			for (int i = _bytes.Length - 2; i >= _bytes.Length - 7;  i--)
			{
				Array.Copy(_bytes, i, tmp, 0, _bytes.Length - i);
				multigrams[i] = new Tuple<ulong, int>(BitConverter.ToUInt64(tmp), i);
			}

			return multigrams;
		}


		private void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
