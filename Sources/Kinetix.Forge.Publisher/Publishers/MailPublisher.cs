using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using Kinetix.Forge.Publisher.Dto;
using Kinetix.Forge.Publisher.Users;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace Kinetix.Forge.Publisher.Publishers
{
    /// <summary>
    /// Génère et envoie les emails de rapport.
    /// </summary>
    public class MailPublisher
    {
        private const string IssueTemplatePath = @"Publishers\Templates\IssueTemplate.cshtml";
        private const string CoverageTemplatePath = @"Publishers\Templates\CoverageTemplate.cshtml";
        private const string TemplateKey = "DefaultTemplate";

        private readonly UserManager _userManager;
        private readonly MailPublisherConfig _config;

        public MailPublisher(UserManager userManager, MailPublisherConfig config)
        {
            _userManager = userManager;
            _config = config;
        }

        public void GenerateIssueMail(ICollection<Report> reportList)
        {
            /* Charge le template CSHTML. */
            string templateContent = GetTemplateContent(IssueTemplatePath);

            /* Instancie le moteur Razor. */
            using (var templateService = GetTemplateService())
            {
                /* Parcourt les rapports. */
                foreach (var report in reportList)
                {
                    LogUtils.Info();
                    LogUtils.Info($"Email pour {report.MainRecepient.Name}");

                    /* Génère le corps du mail. */
                    report.EmailBody = templateService.RunCompile(templateContent, TemplateKey, typeof(Report), report);

                    SendEmail(report);
                }
            }
        }

        public void GenerateMail(CoverageReport report)
        {
            if (string.IsNullOrWhiteSpace(_config.FailedCoverageEmail))
            {
                LogUtils.Info("Le paramètre FailedCoverageEmail n'est pas renseigné : pas d'envoi de notification.");
                LogUtils.Info();
                return;
            }

            /* Charge le template CSHTML. */
            string templateContent = GetTemplateContent(CoverageTemplatePath);

            /* Instancie le moteur Razor. */
            using (var templateService = GetTemplateService())
            {
                /* Parcourt les rapports. */
                LogUtils.Info();
                LogUtils.Info($"Email pour {_config.FailedCoverageEmail}");

                /* Génère le corps du mail. */
                report.EmailBody = templateService.RunCompile(templateContent, TemplateKey, typeof(CoverageReport), report);

                SendEmail(report);
            }
        }

        private static IRazorEngineService GetTemplateService()
        {

            return RazorEngineService.Create(new TemplateServiceConfiguration
            {
                /* Sert à empêcher l'usage de fichiers temporaires non nettoyés à la fin de l'exécution. */
                DisableTempFileLocking = true
            });
        }

        private void SendEmail(Report report)
        {
            UserInfo userInfo = report.MainRecepient;
            string aliasPart = null;
            if (!string.IsNullOrEmpty(userInfo.Alias))
            {
                aliasPart = "[" + userInfo.Alias + "] ";
            }
            int warningTotal = report.WarningCount;
            string subject = $"[Sonar] {report.ProjectName} {aliasPart}(Total : {warningTotal}) {DateTime.Now:dd/MM/yyyy HH:mm}";
            string date = string.Format("{0:dd/MM/yyyy HH:mm}", DateTime.Now);

            MailMessage mail = new MailMessage
            {
                Body = report.EmailBody,
                From = new MailAddress(_config.SenderEmail)
            };

            _userManager.SetMessageUsers(mail, userInfo);
            Console.Out.WriteLine("Envoi d'un email de résultat :");

            string toList = string.Empty;
            foreach (MailAddress item in mail.To)
            {
                toList += item.Address + " ";
            }
            string ccList = string.Empty;
            foreach (MailAddress item in mail.CC)
            {
                ccList += item.Address + " ";
            }
            Console.Out.WriteLine("\tTo : {0}", toList);
            if (!string.IsNullOrEmpty(ccList))
            {
                Console.Out.WriteLine("\tCc : {0}", ccList);
            }

            mail.Subject = subject;
            mail.IsBodyHtml = true;
            if (mail.To.Count == 0)
            {
                Console.Error.WriteLine("Pas d'adresse disponible pour l'envoi du courriel de {0}.", userInfo.Email);
                return;
            }
            using (SmtpClient client = new SmtpClient())
            {
                client.Send(mail);
            }
        }

        private void SendEmail(CoverageReport report)
        {
            string subject = $"[Sonar] {report.ProjectName} | Couverture test KO {DateTime.Now:dd/MM/yyyy HH:mm}";

            MailMessage mail = new MailMessage { Body = report.EmailBody };
            mail.Subject = subject;
            mail.IsBodyHtml = true;
            mail.From = new MailAddress(_config.SenderEmail);
            foreach (var address in _config.FailedCoverageEmail.Split(';'))
            {
                mail.To.Add(new MailAddress(address.Trim()));
            }

            LogUtils.Info("Envoi d'un email de notification de couverture KO.");
            Console.Out.WriteLine("Envoi d'un email de résultat :");

            using (SmtpClient client = new SmtpClient())
            {
                client.Send(mail);
            }
        }

        private static string GetTemplateContent(string templatePath)
        {
            return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.RelativeSearchPath, templatePath));
        }
    }
}
