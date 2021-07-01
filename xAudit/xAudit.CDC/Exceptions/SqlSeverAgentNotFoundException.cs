using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Exceptions
{
    public class SqlSeverAgentNotFoundException:Exception
    {
        public SqlSeverAgentNotFoundException(string message):base(message)
        {

        }
    }
}
