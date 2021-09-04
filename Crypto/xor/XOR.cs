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
			var ggts = CalculateGGTs(_distances);

			int keylen = GetEstimatedKeyLength();
			if (keylen < 1)
				return;

			var candidates = GetKeyCandidates(keylen);

			var best = GetBestKey(keylen, candidates);
			var bestLk = GetBestLowerCaseKey(keylen, candidates);

			PrintKey(best);
			PrintDecyphered(best);
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
				for (int dataOffset = keyOffset; dataOffset < _distances.Count; dataOffset += keylen)
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


			ggt = GGTHigh.OrderByDescending(g => g.Value).First().Key;
			Log($"Estimated key length: {ggt:n0} bytes.");

			return ggt;
		}

		private Dictionary<int, int> CalculateGGTs(Dictionary<int, int> originalValues)
		{
			var retval = new Dictionary<int, int>();

			foreach(var outer in originalValues)
			{
				Parallel.ForEach(originalValues, inner =>
				{
					if (inner.Key != outer.Key)
					{
						int ggt = StaticHelpers.GGT(inner.Key, outer.Key);
						if (ggt > 1)
						{
							lock(retval)
							{
								if (!retval.ContainsKey(ggt))
									retval.Add(ggt, 0);
								retval[ggt] += 1;
							}
						}
					}
				});
			}

			return retval;
		}

		#region Mutligram Search

		private void ReadMultigramDistances()
		{
			_distances = new Dictionary<int, int>();
			int Cores = Environment.ProcessorCount;
			Log($"Seaching for multigrams, number of threads: {Cores} ...");

			ManualResetEvent[] events = new ManualResetEvent[Cores];

			for (int i = 0; i < Cores; i++)
			{
				ThreadInfo info = new ThreadInfo();
				info.Cores = Cores;
				info.Modulo = i;
				info.resetevent = new ManualResetEvent(false);
				events[i] = info.resetevent;

				Thread worker = new Thread(new ParameterizedThreadStart(FindMultis));
				worker.Start(info);
			}

			ManualResetEvent relay = new ManualResetEvent(false);
			Thread starelay = new Thread(new ParameterizedThreadStart(StaticHelpers.STARelay));
			starelay.Start(new KeyValuePair<ManualResetEvent, ManualResetEvent[]>(relay, events));
			relay.WaitOne();
		}

		private void FindMultis(object infoObj)
		{
			ThreadInfo info = infoObj as ThreadInfo;

			for (int l = info.Modulo; l <= 255; l += info.Cores)
				SearchForMultigrams((byte)l);
			info.resetevent.Set();
		}

		private void SearchForMultigrams(byte b)
		{
			int current = 0;
			int i, sequence, dist, multis = 0;

			for (i = 0; i < _bytes.Length; i++)
				if (_bytes[i] == b)
					for (int j = i + 1; j < _bytes.Length; j++)
						if (_bytes[j] == b)
						{
							sequence = StaticHelpers.CompareByteSequence(_bytes, _bytes, i, j);
							if (sequence > 1 && j - i >= sequence)
							{
								dist = j - i;
								multis += sequence - 2;

								lock (this)
								{
									if (!_distances.ContainsKey(dist))
										_distances.Add(dist, 0);
									_distances[dist] += 1; // (sequence - 2) * (sequence - 2);
									current = i;
								}
							}
						}
		}

		#endregion

		private void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
