using Octokit;
using System;
using System.Collections.Generic;
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

            // Créer le client GitHub (sans token pour l'instant)
            _client = new GitHubClient(new ProductHeaderValue("ProjectLauncher"));
        }

        /// Récupère tous les repositories publics d'une organisation
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
}

