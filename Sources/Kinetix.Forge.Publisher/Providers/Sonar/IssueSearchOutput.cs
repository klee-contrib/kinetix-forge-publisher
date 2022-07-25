using System.Collections.Generic;

namespace Kinetix.Forge.Publisher.Providers.Sonar
{
    class IssueSearchOutput
    {
        public int total { get; set; }
        public int p { get; set; }

        public int s { get; set; }

        public ICollection<Issue> issues { get; set; }
    }
}
