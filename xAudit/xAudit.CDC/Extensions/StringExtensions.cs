using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Extensions
{
    public static class StringExtensions
    {
        public static Tuple<string, string> SplitInstance(this string inst)
        {
            var split = inst.Split(new char[] { '_' }, 3);
            return new Tuple<string, string>(split[1], split[2]);

        }
    }
}
