using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plateforme.Services
{
    public class ServiceGitHub
    {
        private readonly GitHubClient _client;
        private readonly string _organizationName;

        public ServiceGitHub(string organizationName)
        {
            _organizationName = organizationName;

            // Créer le client GitHub
            _client = new GitHubClient(new ProductHeaderValue("ProjectLauncher"));

            // Charger le token depuis appsettings.json
            LoadGitHubToken();
        }

        private void LoadGitHubToken()
        {
            try
            {
                // Chemin vers le fichier appsettings.json
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(settingsPath))
                {
                    string jsonContent = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(jsonContent);

                    if (!string.IsNullOrWhiteSpace(settings?.GitHub?.Token))
                    {
                        _client.Credentials = new Credentials(settings.GitHub.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                // Si le fichier n'existe pas ou est invalide, on continue sans token
                Console.WriteLine($"Impossible de charger le token: {ex.Message}");
            }
        }

        /// Récupère toutes les organisations accessibles au token
        public async Task<List<Organization>> GetAllOrganizationsAsync()
        {
            try
            {
                // Appel à l'API GitHub via Octokit pour récupérer toutes les organisations
                var orgs = await _client.Organization.GetAllForCurrent();

                return new List<Organization>(orgs);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération des organisations: {ex.Message}", ex);
            }
        }

        /// Récupère tous les repositories publics et privés d'une organisation
        public async Task<List<Repository>> GetOrganizationRepositoriesAsync()
        {
            try
            {
                // Appel à l'API GitHub via Octokit
                var repos = await _client.Repository.GetAllForOrg(_organizationName);

                return new List<Repository>(repos);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération des repos: {ex.Message}", ex);
            }
        }
    }

    // Classes pour la désérialisation du fichier appsettings.json
    public class AppSettings
    {
        public GitHubSettings GitHub { get; set; }
    }

    public class GitHubSettings
    {
        public string Token { get; set; }
        public string OrganizationName { get; set; }
    }
}

