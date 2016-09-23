using System;
using System.Collections.Generic;
using System.Text;

namespace LogExpert
{
    class Test
    {
        [System.STAThreadAttribute()] 
        public static void Main()
        {
            RegexColumnizer c = new RegexColumnizer();
            c.LoadConfig(@"c:\");
            c.Configure(null, @"c:\");
        }
    }
}
