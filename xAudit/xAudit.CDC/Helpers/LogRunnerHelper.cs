using System;
using System.Collections.Generic;
using System.Text;
using xAudit.CDC.Shared;

namespace xAudit.CDC.Helpers
{
    public static class LogRunnerHelper
    {
        public static int[] Activity { get; set; } = new int[] { };

        public static void Preserve(WhatNext activity)
        {
            if (Activity.Length == 0)
            {
                Activity[0] = ((int)activity);
                return;
            }
            Activity[Activity.Length] = ((int)activity);
        }
    }
}
