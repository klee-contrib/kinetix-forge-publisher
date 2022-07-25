using System.Collections.Generic;
using Kinetix.Forge.Publisher.Dto;

namespace Kinetix.Forge.Publisher.Providers
{
    /// <summary>
    /// Contrat des fournisseurs d'issues.
    /// </summary>
    public interface IIssueProvider
    {
        ICollection<IssueItem> GetIssues();

        /// <summary>
        /// Obtient le rapport de couverture.
        /// </summary>
        /// <returns></returns>
        CoverageReport GetCoverageReport();
    }
}