using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit
{
    [Flags]
    public enum WhatNext { NoUpdate, Install, Upgrade, Downgrade,ConfigChanged }
}
