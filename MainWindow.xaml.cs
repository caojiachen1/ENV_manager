using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EnvVarViewer
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> userEnvVars;
        private Dictionary<string, string> systemEnvVars;
        private Dictionary<string, string> modifiedEnvVars;
        private HashSet<string> deletedEnvVars;

        public MainWindow()
        {
            InitializeComponent();
            if (!IsAdministrator())
            {
                Elevate();
                Application.Current.Shutdown();
            }
            else
            {
                modifiedEnvVars = new Dictionary<string, string>();
                deletedEnvVars = new HashSet<string>();
                LoadEnvVars();
                SearchBox.Focus(); // Set focus to the search box after initialization
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Elevate()
        {
            var processInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName)
            {
                Verb = "runas",
                UseShellExecute = true
            };
            Process.Start(processInfo);
        }

        private void LoadEnvVars()
        {
            userEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            systemEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            UpdateListBox();
        }

        private void UpdateListBox()
        {
            var combinedEnvVars = userEnvVars.Keys
                .Union(systemEnvVars.Keys)
                .Union(modifiedEnvVars.Keys)
                .Except(deletedEnvVars)
                .OrderBy(k => k)
                .ToDictionary(k => k, k =>
                {
                    string userValue = userEnvVars.ContainsKey(k) ? userEnvVars[k] : null;
                    string systemValue = systemEnvVars.ContainsKey(k) ? systemEnvVars[k] : null;
                    string modifiedValue = modifiedEnvVars.ContainsKey(k) ? modifiedEnvVars[k] : null;

                    if (modifiedValue != null)
                        return "Modified:\n" + FormatValue(modifiedValue);
                    else if (userValue != null && systemValue != null)
                        return $"User:\n{FormatValue(userValue)}\n\nSystem:\n{FormatValue(systemValue)}";
                    else if (userValue != null)
                        return "User:\n" + FormatValue(userValue);
                    else if (systemValue != null)
                        return "System:\n" + FormatValue(systemValue);
                    else
                        return "Unknown";
                });

            EnvVarListBox.ItemsSource = combinedEnvVars.Keys;
        }

        private string FormatValue(string value)
        {
            if (value == null)
                return null;

            return string.Join(";\n", value.Split(';'));
        }

        private void EnvVarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (userEnvVars.ContainsKey(selectedVar) || systemEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    string userValue = userEnvVars.ContainsKey(selectedVar) ? userEnvVars[selectedVar] : null;
                    string systemValue = systemEnvVars.ContainsKey(selectedVar) ? systemEnvVars[selectedVar] : null;
                    string modifiedValue = modifiedEnvVars.ContainsKey(selectedVar) ? modifiedEnvVars[selectedVar] : null;

                    if (modifiedValue != null)
                        ValueLabel.Text = $"Modified:\n{FormatValue(modifiedValue)}";
                    else if (userValue != null && systemValue != null)
                        ValueLabel.Text = $"User:\n{FormatValue(userValue)}\n\nSystem:\n{FormatValue(systemValue)}";
                    else if (userValue != null)
                        ValueLabel.Text = $"User:\n{FormatValue(userValue)}";
                    else if (systemValue != null)
                        ValueLabel.Text = $"System:\n{FormatValue(systemValue)}";
                    else
                        ValueLabel.Text = "Environment variable not found";

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
            var filteredKeys = userEnvVars.Keys
                .Union(systemEnvVars.Keys)
                .Union(modifiedEnvVars.Keys)
                .Except(deletedEnvVars)
                .Where(k => k.ToLower().Contains(searchTerm))
                .OrderBy(k => k);
            EnvVarListBox.ItemsSource = filteredKeys;
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
                    foreach (var kv in userEnvVars)
                    {
                        file.WriteLine($"User: {kv.Key}={kv.Value}");
                    }
                    foreach (var kv in systemEnvVars)
                    {
                        file.WriteLine($"System: {kv.Key}={kv.Value}");
                    }
                }
            }
        }

        private void EnvVarListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (userEnvVars.ContainsKey(selectedVar) || systemEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    string value = modifiedEnvVars.ContainsKey(selectedVar) ? modifiedEnvVars[selectedVar] :
                                   userEnvVars.ContainsKey(selectedVar) ? userEnvVars[selectedVar] : systemEnvVars[selectedVar];
                    Clipboard.SetText(value);
                    StatusLabel.Text = $"Copied {selectedVar} to clipboard";
                }
                else
                {
                    StatusLabel.Text = $"Environment variable {selectedVar} not found";
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddModifyEnvVarWindow(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars);
            addWindow.EnvVarAdded += (s, ev) =>
            {
                UpdateListBox();
            };
            addWindow.ShowDialog();
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (userEnvVars.ContainsKey(selectedVar) || systemEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    string value = modifiedEnvVars.ContainsKey(selectedVar) ? modifiedEnvVars[selectedVar] :
                                   userEnvVars.ContainsKey(selectedVar) ? userEnvVars[selectedVar] : systemEnvVars[selectedVar];
                    var modifyWindow = new AddModifyEnvVarWindow(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars, selectedVar, value);
                    modifyWindow.EnvVarModified += (s, ev) =>
                    {
                        UpdateListBox();
                    };
                    modifyWindow.ShowDialog();
                }
                else
                {
                    StatusLabel.Text = $"Environment variable {selectedVar} not found";
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (userEnvVars.ContainsKey(selectedVar) || systemEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    var result = MessageBox.Show($"Are you sure you want to delete the environment variable '{selectedVar}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(selectedVar, null, EnvironmentVariableTarget.User);
                            Environment.SetEnvironmentVariable(selectedVar, null, EnvironmentVariableTarget.Machine);
                            if (modifiedEnvVars.ContainsKey(selectedVar))
                            {
                                modifiedEnvVars.Remove(selectedVar);
                            }
                            deletedEnvVars.Add(selectedVar);
                            UpdateListBox();
                            StatusLabel.Text = $"Deleted {selectedVar}";
                        }
                        catch (System.Security.SecurityException ex)
                        {
                            MessageBox.Show("Permission denied. You do not have sufficient privileges to delete environment variables at this scope.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred: {ex.Message}");
                        }
                    }
                }
                else
                {
                    StatusLabel.Text = $"Environment variable {selectedVar} not found";
                }
            }
        }
    }
}