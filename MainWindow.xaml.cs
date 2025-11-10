using System;
using System.Windows;
using Octokit;
using Plateforme.Services;

namespace Plateforme
{
    public partial class MainWindow : Window
    {
        private ServiceGitHub _serviceGitHub;
        public MainWindow()
        {
            InitializeComponent();

            _serviceGitHub = new ServiceGitHub("Demande-De-Vincent-692");

            LoadProjects();
        }

        private async void LoadProjects()
        {
            try
            {
                LogsTextBox.AppendText("Chargement des projets depuis GitHub...\n");

                // Récupérer les repos
                var repos = await _serviceGitHub.GetOrganizationRepositoriesAsync();

                LogsTextBox.AppendText($"{repos.Count} projet(s) trouvé(s) !\n");

                // Afficher les noms des repos dans la console
                foreach (var repo in repos)
                {
                    LogsTextBox.AppendText($" - {repo.Name}\n");
                }
            }
            catch (Exception ex)
            {
                LogsTextBox.AppendText($"Erreur: {ex.Message}\n");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Supprime les logs précédants de la console et viens rechercher les repos de l'organisation
            LogsTextBox.Clear();
            LoadProjects();
        }
    }
}