using System;
using System.Collections.Generic;
using System.Text;

namespace xAudit.CDC.Shared
{
    public class RunTables
    {
        public int Id { get; set; }
        public Run Run { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public WhatNext Action { get; set; }
    }
}
