namespace Kinetix.Forge.Publisher.Dto
{
    public class SonarConfig
    {
        /* URL de l'instance SONAR. */
        public string SonarUrl { get; set; }

        /* Token pour se connecter à Sonar. */
        public string SonarToken { get; set; }

        /* Clé du projet dans SONAR */
        public string SonarProjectKey { get; set; }

        /* Liste des langages, séparé par une virgule : cs,java,js */
        public string Languages { get; set; }

        /* Nombre maximum d'issues à requêter. */
        public int? MaxIssueCount { get; set; }

        /* Types des issues (par défaut tout : BUG, VULNERABILITY, CODE_SMELL ). */
        public string[] IssueTypes { get; set; }

        /* Indique si on doit filtrer depuis la dernière leak période. */
        public bool? SinceLeakPeriod { get; set; }

        /* Liste de règles Sonar à ne pas prendre en compte. */
        public string[] RulesBlackList { get; set; }
    }
}
