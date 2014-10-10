using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisMemProf
{
	public class Config
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public int? SampleSize { get; set; }
		public string KeySeperator { get; set; }
	}
}
