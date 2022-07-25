using System.Collections.Generic;
using Kinetix.Forge.Publisher.Dto;

namespace Kinetix.Forge.Publisher.Resolvers
{
    /// <summary>
    /// Contrat des résolveurs d'auteur d'issue.
    /// </summary>
    public interface IAuthorResolver
    {
        /// <summary>
        /// Résoud l'auteur d'une liste d'issue.
        /// </summary>
        /// <param name="issueList">Liste d'issue.</param>
        void ResolveAll(ICollection<IssueItem> issueList);
    }
}
