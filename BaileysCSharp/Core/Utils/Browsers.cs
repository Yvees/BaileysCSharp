using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Utils
{
    public class Browsers
    {
        public static string[] Ubuntu(string browser) => ["Ubuntu", browser, "22.04.4"];
        public static string[] MacOS(string browser) => ["Mac OS", browser, "14.4.1"];
        public static string[] Baileys(string browser) => ["Baileys", browser, "6.5.0"];
        public static string[] Windows(string browser) => ["Windows", browser, "10.0.22631"];
    }
}
