using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Octokit;
using Plateforme.Services;

namespace Plateforme
{
    public partial class MainWindow : Window
    {
        private ServiceGitHub _serviceGitHub;
        private ServiceGit _serviceGit;
        private string _githubToken;
        private int _notificationCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Charger le token depuis appsettings.json
            LoadConfiguration();

            _serviceGitHub = new ServiceGitHub("Demande-De-Vincent-692");

            // Créer le service Git avec le dossier "Repo" à la racine du projet
            // Remonter de 3 niveaux depuis bin/Debug/net8.0-windows/ pour atteindre la racine
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            string repoDirectory = Path.Combine(projectRoot, "Repo");
            _serviceGit = new ServiceGit(repoDirectory);

            LoadProjects();
        }

        private void LoadConfiguration()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(settingsPath))
                {
                    string jsonContent = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(jsonContent);
                    _githubToken = settings?.GitHub?.Token ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                AddNotification($"⚠️ Failed to load configuration: {ex.Message}");
            }
        }

        private async void LoadProjects()
        {
            try
            {
                AddNotification("🔍 Loading projects from GitHub...");

                // Récupérer les repos
                var repos = await _serviceGitHub.GetOrganizationRepositoriesAsync();
                AddNotification($"✅ {repos.Count} project(s) found!");

                // Vider le panel avant d'ajouter les nouveaux projets
                ProjectsPanel.Children.Clear();

                // Créer une carte pour chaque repo
                foreach (var repo in repos)
                {
                    // Créer la carte du projet
                    Border card = CreateProjectCard(repo);
                    ProjectsPanel.Children.Add(card);

                    AddNotification($"   ➕ Card created: {repo.Name}");
                }

                AddNotification("✅ Interface updated!");
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}");
            }
        }

        private Border CreateProjectCard(Repository repo)
        {
            // Carte principale
            Border card = new Border
            {
                Style = (Style)FindResource("ProjectCardStyle")
            };

            // Grid pour organiser le contenu
            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Contenu principal
            StackPanel contentPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            // Icône et nom du projet
            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock icon = new TextBlock
            {
                Text = "📁",
                FontSize = 24,
                Margin = new Thickness(0, 0, 10, 0)
            };

            TextBlock nameText = new TextBlock
            {
                Text = repo.Name,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 200
            };

            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(nameText);
            contentPanel.Children.Add(headerPanel);

            // Description
            TextBlock descriptionText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(repo.Description) ? "No description available" : repo.Description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 40,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            contentPanel.Children.Add(descriptionText);

            Grid.SetRow(contentPanel, 0);
            cardGrid.Children.Add(contentPanel);

            // Footer avec badge de visibilité
            StackPanel footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            Border visibilityBadge = new Border
            {
                Background = repo.Private
                    ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                    : new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };

            TextBlock visibilityText = new TextBlock
            {
                Text = repo.Private ? "🔒 Private" : "🌍 Public",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            visibilityBadge.Child = visibilityText;
            footerPanel.Children.Add(visibilityBadge);

            Grid.SetRow(footerPanel, 1);
            cardGrid.Children.Add(footerPanel);

            card.Child = cardGrid;

            // Événement de clic
            card.MouseLeftButtonDown += (sender, e) => ProjectCard_Click(repo, card);

            return card;
        }

        private async void ProjectCard_Click(Repository repo, Border card)
        {
            // Afficher les informations du repo
            AddNotification($"\n🔵 Selected project: {repo.Name}");
            AddNotification($"   📝 Description: {repo.Description ?? "No description"}");
            AddNotification($"   🔗 URL: {repo.HtmlUrl}");
            AddNotification($"   📁 Clone URL: {repo.CloneUrl}");
            AddNotification($"   🔒 Private: {(repo.Private ? "Yes" : "No")}");

            // Modifier l'apparence de la carte pendant le traitement
            var originalBackground = card.Background;
            card.Background = new SolidColorBrush(Color.FromRgb(240, 240, 245));
            card.Cursor = Cursors.Wait;

            try
            {
                AddNotification($"\n⬇️ Downloading repository '{repo.Name}'...");

                // Cloner ou mettre à jour le repository
                string result = await _serviceGit.CloneOrPullRepositoryAsync(repo.CloneUrl, repo.Name, _githubToken);

                AddNotification($"{result}\n");
            }
            catch (Exception ex)
            {
                AddNotification($"❌ Error: {ex.Message}\n");
            }
            finally
            {
                // Restaurer l'apparence de la carte
                card.Background = originalBackground;
                card.Cursor = Cursors.Hand;
            }
        }

        private void AddNotification(string message)
        {
            // Ajouter le message au panneau de notification
            if (!string.IsNullOrWhiteSpace(NotificationTextBlock.Text))
            {
                NotificationTextBlock.Text += "\n";
            }
            NotificationTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] {message}";

            // Incrémenter le compteur si le panneau est caché
            if (NotificationPanel.Visibility == Visibility.Collapsed)
            {
                _notificationCount++;
                UpdateNotificationBadge();
            }

            // Auto-scroll vers le bas si le panneau est visible
            if (NotificationPanel.Visibility == Visibility.Visible)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(NotificationPanel);
                scrollViewer?.ScrollToEnd();
            }
        }

        private void UpdateNotificationBadge()
        {
            if (_notificationCount > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                NotificationCount.Text = _notificationCount > 99 ? "99+" : _notificationCount.ToString();
            }
            else
            {
                NotificationBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleNotification_Click(object sender, RoutedEventArgs e)
        {
            if (NotificationPanel.Visibility == Visibility.Collapsed)
            {
                // Ouvrir le panneau
                NotificationPanel.Visibility = Visibility.Visible;
                _notificationCount = 0;
                UpdateNotificationBadge();

                // Animer l'apparition
                var storyboard = (Storyboard)FindResource("NotificationSlideIn");
                storyboard.Begin(NotificationPanel);

                // Auto-scroll vers le bas
                var scrollViewer = FindVisualChild<ScrollViewer>(NotificationPanel);
                scrollViewer?.ScrollToEnd();
            }
            else
            {
                // Fermer le panneau
                NotificationPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            NotificationPanel.Visibility = Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Effacer les notifications précédentes
            NotificationTextBlock.Text = string.Empty;
            _notificationCount = 0;
            UpdateNotificationBadge();
            LoadProjects();
        }

        // Helper pour trouver un élément enfant dans l'arbre visuel
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}
