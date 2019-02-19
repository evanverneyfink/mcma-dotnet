using System;

namespace Mcma.Core
{
    public abstract class McmaResource : McmaObject, IMcmaResource
    {
        public string Id { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }
    }
}