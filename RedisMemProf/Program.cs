using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using StackExchange.Redis;

namespace RedisMemProf
{
	class Program
	{
		private static Dictionary<string, long> _keyStats;

		static void Main(string[] args) {
			while (true) {
				Console.Write("prefix (optional): ");
				_keyStats = new Dictionary<string, long>();
				ProfileMem(Console.ReadLine(), ReadConfig());
				var total = _keyStats.Values.Sum();
				foreach (var kv in _keyStats.OrderByDescending(kv => kv.Value).Take(10))
					Console.WriteLine("{0}\t{1:0.00}%", kv.Key, 100.00 * kv.Value / total);
				Console.WriteLine();
			}
		}

		static void ProfileMem(string prefix, Config config) {
			var redis = ConnectionMultiplexer.Connect(config.Host + ",allowAdmin=true,connectTimeout=5000,syncTimeout=10000");
			var server = redis.GetServer(config.Host, config.Port);
			var db = redis.GetDatabase();
			var redisKeys = server.Keys(pattern: prefix + "*").ToList();

			var sampleSize = Math.Min(config.SampleSize ?? redisKeys.Count, redisKeys.Count);
			Console.WriteLine("Sampling {0} of {1} keys...", sampleSize, redisKeys.Count);

			var div = redisKeys.Count / sampleSize;
			var sample = redisKeys.Where((k, i) => i % div == 0);

			foreach (var redisKey in sample) {
				if (!db.KeyExists(redisKey)) // the key may have been removed since intitial sampling
					continue;

				var size = ParseSize(db.DebugObject(redisKey));
				var statKey = GetStatKey(redisKey, prefix);
				if (_keyStats.ContainsKey(statKey))
					_keyStats[statKey] += size;
				else
					_keyStats.Add(statKey, size);
			}
		}

		static Config ReadConfig() {
			var settings = ConfigurationManager.AppSettings;
			return new Config {
				Host = settings["Host"] ?? "localhost",
				Port = ParseInt(settings["Port"]) ?? 6379,
				SampleSize = ParseInt(settings["SampleSize"]),
				KeySeperator = settings["KeySeperator"] ?? ":"
			};
		}

		static int? ParseInt(string s) {
			int i;
			return int.TryParse(s, out i) ? (int?)i : null;
		}

		static string GetStatKey(string redisKey, string prefix) {
			var take = (prefix == "") ? 1 : prefix.Split(':').Length + 1;
			return string.Join(":", redisKey.Split(':').Take(take));
		}

		static long ParseSize(string debugInfo) {
			return long.Parse(debugInfo.Split(' ').Single(s => s.StartsWith("serializedlength:")).Split(':')[1]);
		}
	}
}
