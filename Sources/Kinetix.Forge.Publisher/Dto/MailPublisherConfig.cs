namespace Kinetix.Forge.Publisher.Dto
{
    public class MailPublisherConfig
    {
        /// <summary>
        /// Adresses de courriel pour la notification de couverture en erreur.
        /// </summary>
        public string FailedCoverageEmail { get; set; }

        /// <summary>
        /// Expéditeur des courriels.
        /// </summary>
        public string SenderEmail { get; set; }
    }
}
