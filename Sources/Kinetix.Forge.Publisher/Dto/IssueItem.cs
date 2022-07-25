namespace Kinetix.Forge.Publisher.Dto
{
    /// <summary>
    /// Représente un warning.
    /// </summary>
    public class IssueItem
    {
        /// <summary>
        /// Clé de l'issue dans Sonar.
        /// </summary>
        public string SonarKey { get; set; }

        /// <summary>
        /// Nom du composant dans Sonar.
        /// Identifie un fichier.
        /// Exemple : acme:ACME.SuperProjet:Sources/ACME.SuperProjet.Wpfapp/SafeDictionary.cs.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Nom de la règle dans Sonar.
        /// Exemple : csharpsquid:S2931.
        /// </summary>
        public string Rule { get; set; }

        /// <summary>
        /// Message de l'issue dans Sonar.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Chemin relativ à la branche du fichier.
        /// Exemple : Sources/ACME.SuperProjet.Wpfapp/SafeDictionary.cs.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Numéro de la ligne ayant générée l'issue.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Numéro du caractère sur la ligne.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Type de l'issue : BUG, VULNERABILITY, CODE_SMELL.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Sévérité de l'issue : BLOCKER, CRITICAL, MAJOR, MINOR, INFO.
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Auteur selon Sonar. Contient l'email dans le cas d'un projet Git.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Lien vers le portail avec l'issue ouverte.
        /// Non fourni par Sonar.
        /// </summary>
        public string SonarLink { get; set; }

        /// <summary>
        /// Consolide les infos disponibles sur l'auteur (WindowsID ou Email).
        /// </summary>
        public UserInfo ResolvedAuthor { get; set; } = new UserInfo();
    }
}