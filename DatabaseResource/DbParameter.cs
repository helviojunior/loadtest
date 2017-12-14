using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data
{
    public class DbParameter
    {
        public String ParameterName { get; set; }
        public Type Type { get; set; }
        public Int32 Size { get; set; }
        public Object Value { get; set; }

        public DbParameter(String parameterName, Type type)
            : this(parameterName, type, 0) { }

        public DbParameter(String parameterName, Type type, Int32 size)
        {
            this.ParameterName = parameterName;
            this.Type = type;
            this.Size = size;
            this.Value = null;
        }
    }
    
}
