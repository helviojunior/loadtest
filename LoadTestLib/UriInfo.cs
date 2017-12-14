using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    [Serializable()]
    public class UriInfo
    {
        public Uri Target { get; set; }
        public Uri Referer { get; set; }

        public UriInfo() { }

        public UriInfo(Uri target) :
            this(target, null) { }

        public UriInfo(Uri target, Uri referer)
        {
            this.Target = target;
            this.Referer = referer;
        }

        public Boolean Equal(UriInfo comparer)
        {
            return Target.Equals(comparer.Target);
        }
    }
}
