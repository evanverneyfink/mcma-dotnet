using System;
using System.Collections.Generic;

namespace Mcma.Core
{
    public class Service : McmaResource
    {
        public string Name { get; set; }

        public ICollection<ServiceResource> Resources { get; set; }

        public string JobType { get; set; }

        public string[] JobProfiles { get; set; }

        public ICollection<Locator> InputLocations { get; set; }

        public ICollection<Locator> OutputLocations { get; set; }
    }
}
