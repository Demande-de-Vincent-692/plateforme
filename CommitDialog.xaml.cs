using System.Collections.Generic;
using System.Windows;
using Plateforme.Services;

namespace Plateforme
{
    public partial class CommitDialog : Window
    {
        public string CommitTitle { get; private set; }
        public string CommitDescription { get; private set; }
        public bool WasCommitted { get; private set; }

        public CommitDialog(List<GitModifiedFile> modifiedFiles)
        {
            InitializeComponent();

            // Remplir la liste des fichiers modifiés
            FilesList.ItemsSource = modifiedFiles;
            FileCountText.Text = $"{modifiedFiles.Count} file(s) changed";

            WasCommitted = false;

            // Focus sur le champ titre
            CommitTitleTextBox.Focus();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation du titre
            if (string.IsNullOrWhiteSpace(CommitTitleTextBox.Text))
            {
                MessageBox.Show("Please enter a commit title.", "Missing Title", MessageBoxButton.OK, MessageBoxImage.Warning);
                CommitTitleTextBox.Focus();
                return;
            }

            // Récupérer les valeurs
            CommitTitle = CommitTitleTextBox.Text.Trim();
            CommitDescription = CommitDescriptionTextBox.Text.Trim();
            WasCommitted = true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            WasCommitted = false;
            DialogResult = false;
            Close();
        }
    }
}
