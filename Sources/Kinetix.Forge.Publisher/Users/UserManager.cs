using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Kinetix.Forge.Publisher.Dto;

namespace Kinetix.Forge.Publisher.Users
{

    /// <summary>
    /// Manager pour obtenir des informations sur les utilisateurs.
    /// </summary>
    public class UserManager
    {
        private readonly LdapConfig _config;
        private readonly UserList _allUsers;
        private readonly UserList _standardUsers;
        private readonly UserList _adminList;
        private readonly UserList _forwardList;
        private readonly UserList _defaultList;

        /// <summary>
        /// Créée une nouvelle instance de UserManager.
        /// </summary>
        /// <param name="team">Configuration de l'équipe.</param>
        public UserManager(LdapConfig config, TeamConfig team)
        {
            _config = config;
            _allUsers = new UserList();
            _standardUsers = new UserList();
            _adminList = new UserList();
            _forwardList = new UserList();
            _defaultList = new UserList();

            LoadTeamConfigurationFile(team);
        }

        /// <summary>
        /// Utilisateur anonyme utilisé pour les warnings qui ne sont pas associés à un utilisateur.
        /// </summary>
        public static UserInfo UserAnonymous { get; } = new UserInfo { Name = "Anonyme", Email = "anonymous@anonymous.com", AccountName = "anonymous" };

        /// <summary>
        /// Récupère une liste d'utilisateurs à partir d'un utilisateur ayant un email ou un idendifiant Windows.
        /// </summary>
        /// <param name="criteria">Critère pour trouve l'utilisateur.</param>
        /// <returns>Données issues de l'active directory.</returns>
        public ICollection<UserInfo> RetrieveUserListByCriteria(UserInfo criteria)
        {
            List<UserInfo> resultList = new List<UserInfo>();

            /* Cas d'une liste de warning non assignés */
            if (string.IsNullOrEmpty(criteria.ToString()))
            {
                resultList.Add(UserAnonymous);
                return resultList;
            }

            resultList.Add(RetrieveUserByCriteria(criteria));

            return resultList;
        }

        /// <summary>
        /// Récupère les informations de l'utilisateur à partir d'un texte (alias ou compte AD).
        /// Si aucun utilisateur n'est disponible, un utilisateur anonyme est renvoyé.
        /// </summary>
        /// <param name="criteria">Critère pour trouve l'utilisateur.</param>
        /// <returns>Données issues de l'active directory.</returns>
        private UserInfo RetrieveUserByCriteria(UserInfo criteria)
        {
            /* Cherche l'utilisateur dans la liste des utilisateurs connus. */
            var userInfo = _allUsers.Find(criteria);

            /* Cas où l'utilisateur est déjà dans enregistré. */
            if (userInfo != null)
            {
                return userInfo;
            }

            /* Cherche l'utilisateur dans le LDAP. */
            userInfo = SearchLdap(criteria);

            /* Cas où l'utilisateur est trouvé dans le LDAP. */
            if (userInfo != null)
            {
                _allUsers.Add(userInfo);
                return userInfo;
            }

            /* Cas où l'utilisateur est inconnu : on le créé. */
            userInfo = new UserInfo
            {
                Email = criteria.Email,
                AccountName = criteria.AccountName,
                Name = criteria.ToString()
            };
            _allUsers.Add(userInfo);

            return userInfo;
        }

        private UserInfo SearchLdap(UserInfo criteria)
        {
            Console.Out.WriteLine("Resolving LDAP user for {0}...", criteria);

            /* Cherche l'utilisateur dans le LDAP. */
            try
            {

                using (var entry = new DirectoryEntry(_config.DirectoryPath))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = BuildFilter(criteria);
                    SearchResult result = searcher.FindOne();
                    if (result != null)
                    {
                        return new UserInfo
                        {
                            Email = result.Properties["mail"][0].ToString(),
                            Name = result.Properties["cn"][0].ToString(),
                            AccountName = result.Properties["samaccountname"][0].ToString(),
                        };
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Erreur à l'appel du LDAP : {e}");
                return null;
            }

            /* Utilisateur non trouvé. */
            Console.Error.WriteLine("Impossible de retrouver le compte ActiveDirectory correspondant à {0}", criteria);
            return null;
        }

        private static string BuildFilter(UserInfo criteria)
        {
            var sb = new StringBuilder();
            sb.Append("(&(objectClass=user)");

            if (!string.IsNullOrEmpty(criteria.AccountName))
            {
                sb.Append($"(SAMAccountName={RemoveDomain(criteria.AccountName)})");
            }

            if (!string.IsNullOrEmpty(criteria.Email))
            {
                sb.Append($"(mail={criteria.Email})");
            }

            sb.Append(")");

            return sb.ToString();
        }

        private static string RemoveDomain(string userName)
        {
            return Regex.Replace(userName, @"[^\\]*\\", string.Empty);
        }

        /// <summary>
        /// Charge le fichier de configuration de l'équipe projet.
        /// </summary>
        /// <param name="team">Configuration de l'équipe.</param>
        private void LoadTeamConfigurationFile(TeamConfig team)
        {
            if (team == null)
            {
                LogUtils.Info("ERROR : Configuration d'équipe manquante : impossible d'envoyer les notifications.");
                return;
            }

            foreach (var node in team.AdminUsers)
            {
                var userInfo = RetrieveUserByCriteria(ExtractUserInfo(node));
                _adminList.Add(userInfo);
            }

            foreach (var node in team.Forwarding)
            {
                var userInfo = RetrieveUserByCriteria(ExtractUserInfo(node));

                var forwardUserInfo = RetrieveUserByCriteria(ExtractForwardUserInfo(node));

                userInfo.FordwardUser = forwardUserInfo;
                _forwardList.Add(userInfo);
            }

            foreach (var node in team.StandardUsers)
            {
                UserInfo userInfo = RetrieveUserByCriteria(ExtractUserInfo(node));
                _standardUsers.Add(userInfo);
            }

            foreach (var node in team.DefaultUsers)
            {
                var userInfo = RetrieveUserByCriteria(ExtractUserInfo(node));
                _defaultList.Add(userInfo);
            }
        }

        private static UserInfo ExtractUserInfo(UserConfig item)
        {
            return new UserInfo
            {
                AccountName = item?.Account,
                Email = item?.Email
            };
        }

        private static UserInfo ExtractForwardUserInfo(UserConfig item)
        {
            return new UserInfo
            {
                AccountName = item?.ForwardAccount,
                Email = item?.ForwardEmail
            };
        }

        /// <summary>
        /// Paramètre le mail avec les destinataires selon la configuration.
        /// </summary>
        /// <param name="message">Message à paramètrer.</param>
        /// <param name="userInfo">Destinataire du message.</param>
        public void SetMessageUsers(MailMessage message, UserInfo userInfo)
        {
            /* Cas d'une personne qui ne fait plus partie de l'équipe et qui a un suiveur. */
            if (_forwardList.Match(userInfo))
            {
                AddRecever(message, userInfo.FordwardUser);
            }
            /* Cas d'un utilisateur non standard : on envoie aux utilisateurs par défaut. */
            else if (IsSendToDefaultUser(userInfo))
            {
                foreach (UserInfo defaultUser in _defaultList)
                {
                    AddRecever(message, defaultUser);
                }
            }
            else
            {
                /* Cas standard d'une personne qui fait partie de l'équipe. */
                AddRecever(message, userInfo);
            }

            /* Ajout des administrateurs. */
            foreach (UserInfo admin in _adminList)
            {
                AddRecever(message, admin);
            }
        }

        /// <summary>
        /// Indique si le courriel d'un utilisateur doit être court-circuité et envoyé à l'utilisateur par défaut.
        /// </summary>
        /// <param name="userInfo">Utilisateur.</param>
        /// <returns><code>True</code> si le courriel est n'est pas envoyé à l'utilisateur.</returns>
        private bool IsSendToDefaultUser(UserInfo userInfo)
        {
            /* Cas d'un warning non affecté. */
            if (userInfo == UserAnonymous)
            {
                return true;
            }

            /* Cas d'un utilisateur non standard (OPT-IN) */
            if (!_standardUsers.Match(userInfo))
            {
                return true;
            }

            /* Cas standard. */
            return false;
        }

        /// <summary>
        /// Ajoute un destinataire au mail. Seul le premier appel ajoute au To, les appels suivants ajoutent au Cc.
        /// </summary>
        /// <param name="message">Mail</param>
        /// <param name="userInfo">Destinataire.</param>
        /// <returns>Indique si l'ajout s'est bien passé.</returns>
        private bool AddRecever(MailMessage message, UserInfo userInfo)
        {
            if (!userInfo.HasEmail)
            {
                return false;
            }
            string email = userInfo.Email;
            if (message.To.Count == 0)
            {
                message.To.Add(new MailAddress(email));
            }
            else
            {
                if (message.To.All(x => x.Address != email) && message.CC.All(x => x.Address != email))
                {
                    message.CC.Add(new MailAddress(email));
                }
            }
            return true;
        }

        /// <summary>
        /// Liste d'utilisateur avec un critère d'égalité sur le WindowsID ou l'email.
        /// </summary>
        private class UserList : List<UserInfo>
        {

            private static bool IsUserEquivalent(UserInfo candidate, UserInfo criteria)
            {
                /* Correspondance sur le account name. */
                if (!string.IsNullOrEmpty(criteria.AccountName) && criteria.AccountName == candidate.AccountName)
                {
                    return true;
                }

                /* Correspondance sur le courriel. */
                if (!string.IsNullOrEmpty(criteria.Email) && criteria.Email == candidate.Email)
                {
                    return true;
                }

                return false;
            }

            public bool Match(UserInfo criteria)
            {
                return this.Any(x => IsUserEquivalent(x, criteria));
            }

            public UserInfo Find(UserInfo criteria)
            {
                return this.FirstOrDefault(x => IsUserEquivalent(x, criteria));
            }
        }
    }
}
