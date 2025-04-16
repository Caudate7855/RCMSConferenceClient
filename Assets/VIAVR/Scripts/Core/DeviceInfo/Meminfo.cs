using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VIAVR.Scripts.Core.DeviceInfo
{
	public class Meminfo  {
		public struct meminf{
			//all numbers are in kiloBytes
			public int memtotal;
			public int memfree;
			public int memavailable;
			public int active;
			public int inactive;
			public int cached;
			public int swapcached;
			public int swaptotal;
			public int swapfree;
		}
		
		public static meminf minf = new meminf();
		
		private static Regex re = new Regex(@"\d+");
		
		public static bool getMemInfo(){
			
			if(!File.Exists("/proc/meminfo")) return false;
		
			FileStream fs = new FileStream("/proc/meminfo", FileMode.Open, FileAccess.Read, FileShare.Read);
			StreamReader sr = new StreamReader(fs);
			
			string line;
			while((line = sr.ReadLine())!=null){
				line = line.ToLower().Replace(" ","");
				if(line.Contains("memtotal")){ minf.memtotal = mVal(line); }
				if(line.Contains("memfree")){ minf.memfree = mVal(line); }
				if(line.Contains("memavailable")){ minf.memavailable = mVal(line); }
				if(line.Contains("active")){ minf.active = mVal(line); }
				if(line.Contains("inactive")){ minf.inactive = mVal(line); }
				if(line.Contains("cached") && !line.Contains("swapcached")){ minf.cached = mVal(line); }
				if(line.Contains("swapcached")){ minf.swapcached = mVal(line); }
				if(line.Contains("swaptotal")){ minf.swaptotal = mVal(line); }
				if(line.Contains("swapfree")){ minf.swapfree = mVal(line); }
			}
			
			sr.Close(); fs.Close(); fs.Dispose();
			return true;
		}
		
		private static int mVal(string s){
			Match m = re.Match(s); return int.Parse(m.Value);
		}
	
		public static void gc_Collect() {
			var jc = new AndroidJavaClass("java.lang.System");
			jc.CallStatic("gc");
			jc.Dispose();
		}
	}
}