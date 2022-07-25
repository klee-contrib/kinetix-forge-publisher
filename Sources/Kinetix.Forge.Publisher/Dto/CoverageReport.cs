namespace Kinetix.Forge.Publisher.Dto
{
    /// <summary>
    /// Rapport de couverture de test.
    /// </summary>
    public class CoverageReport
    {
        /// <summary>
        /// Obtient ou définit le nom du projet.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Nombre de test en échec.
        /// </summary>
        public int NbFailure { get; set; }

        /// <summary>
        /// Nombres de tests en erreur.
        /// </summary>
        public int NbError { get; set; }

        /// <summary>
        /// Lien vers le portail sur liste des tests en échec.
        /// </summary>
        public string FailureSonarLink { get; set; }

        /// <summary>
        /// Lien vers le portail sur liste des tests en erreur.
        /// </summary>
        public string ErrorSonarLink { get; set; }

        /// <summary>
        /// Obtient ou définit le corps du courriel.
        /// </summary>
        public string EmailBody { get; set; }

        /// <summary>
        /// Indique si la couverture est dans le vert.
        /// </summary>
        public bool IsCoverageGreen
        {
            get
            {
                return NbFailure == 0 && NbError == 0;
            }
        }
    }
}
