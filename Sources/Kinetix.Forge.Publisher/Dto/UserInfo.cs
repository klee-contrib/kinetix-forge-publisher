
namespace Kinetix.Forge.Publisher.Dto
{

    /// <summary>
    /// Objet stockant les informations sur un utilisateur.
    /// </summary>
    public class UserInfo
    {

        /// <summary>
        /// Obtient ou définit le nom du compte de l'utilisateur.
        /// </summary>
        public string AccountName
        {
            get;
            set;
        }

        /// <summary>
        /// Obtient ou définit le nom de l'utilisateur.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Obtient ou définit le nom de l'utilisateur.
        /// </summary>
        public string Email
        {
            get;
            set;
        }

        /// <summary>
        /// Obtient ou définit l'alias de l'utilisateur.
        /// </summary>
        public string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Obtient ou définit le nom du compte du suiveur de l'utilisateur.
        /// </summary>
        public UserInfo FordwardUser
        {
            get;
            set;
        }

        /// <summary>
        /// Indique si l'utilisateur possède un email.
        /// </summary>
        public bool HasEmail
        {
            get
            {
                return !string.IsNullOrEmpty(this.Email);
            }
        }

        /// <summary>
        /// Renvoie une chaîne de caractères qui représente l'objet.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.AccountName ?? this.Email;
        }
    }
}
