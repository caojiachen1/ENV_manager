using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EnvVarViewer
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> envVars;

        public MainWindow()
        {
            InitializeComponent();
            LoadEnvVars();
            SearchBox.Focus(); // Set focus to the search box after initialization
        }

        private void LoadEnvVars()
        {
            envVars = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());
            UpdateListBox();
        }

        private void UpdateListBox()
        {
            EnvVarListBox.ItemsSource = envVars.Keys.OrderBy(k => k);
        }

        private void EnvVarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (envVars.ContainsKey(selectedVar))
                {
                    ValueLabel.Text = envVars[selectedVar];
                    StatusLabel.Text = "";
                }
                else
                {
                    ValueLabel.Text = "Environment variable not found";
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchBox.Text.ToLower();
            EnvVarListBox.ItemsSource = envVars.Keys.Where(k => k.ToLower().Contains(searchTerm)).OrderBy(k => k);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEnvVars();
            SearchBox.Text = ""; // 清空搜索栏
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter file = new StreamWriter(saveFileDialog.FileName))
                {
                    foreach (var kv in envVars)
                    {
                        file.WriteLine($"{kv.Key}={kv.Value}");
                    }
                }
            }
        }

        private void EnvVarListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (envVars.ContainsKey(selectedVar))
                {
                    Clipboard.SetText(envVars[selectedVar]);
                    StatusLabel.Text = $"Copied {selectedVar} to clipboard";
                }
                else
                {
                    StatusLabel.Text = $"Environment variable {selectedVar} not found";
                }
            }
        }
    }
}