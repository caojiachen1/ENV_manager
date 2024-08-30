﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YourNamespace;
using Wpf.Ui.Appearance;
//using Wpf.Ui.Controls;

namespace EnvVarViewer
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private Dictionary<string, string> userEnvVars;
        private Dictionary<string, string> systemEnvVars;
        private Dictionary<string, string> modifiedEnvVars;
        private HashSet<string> deletedEnvVars;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica, true);
            //if (!IsAdministrator())
            //{
            //    Elevate();
            //    System.Windows.Application.Current.Shutdown();
            //}
            modifiedEnvVars = new Dictionary<string, string>();
            deletedEnvVars = new HashSet<string>();
            LoadEnvVars();
            SearchBox.Focus(); // Set focus to the search box after initialization
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
                    var userItem = EnvVarTreeView.Items[0] as TreeViewItem;
                    var systemItem = EnvVarTreeView.Items[1] as TreeViewItem;

                    userItem.Items.Clear();
                    systemItem.Items.Clear();

                    if (userEnvVars.ContainsKey(selectedVar))
                    {
                        userItem.Items.Add(new KeyValuePair<string, string>(selectedVar, userEnvVars[selectedVar]));
                    }

                    if (systemEnvVars.ContainsKey(selectedVar))
                    {
                        systemItem.Items.Add(new KeyValuePair<string, string>(selectedVar, systemEnvVars[selectedVar]));
                    }

                    if (modifiedEnvVars.ContainsKey(selectedVar))
                    {
                        if (userEnvVars.ContainsKey(selectedVar))
                        {
                            userItem.Items.Add(new KeyValuePair<string, string>(selectedVar, modifiedEnvVars[selectedVar]));
                        }
                        else if (systemEnvVars.ContainsKey(selectedVar))
                        {
                            systemItem.Items.Add(new KeyValuePair<string, string>(selectedVar, modifiedEnvVars[selectedVar]));
                        }
                    }

                    StatusLabel.Text = "";
                }
                else
                {
                    StatusLabel.Text = "Environment variable not found";
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
            StatusLabel.Text = "";
            SystemEnvList.Items.Clear();
            UserEnvList.Items.Clear();
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
            if (!IsAdministrator())
            {
                Elevate();
                Application.Current.Shutdown();
            }
            var addWindow = new AddModifyEnvVarWindow(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars);
            addWindow.EnvVarAdded += (s, ev) =>
            {
                UpdateListBox();
            };
            addWindow.ShowDialog();
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator())
            {
                Elevate();
                Application.Current.Shutdown();
            }

            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (selectedVar.ToLower() == "path")
                {
                    // Determine which TreeViewItem is selected
                    var selectedNode = GetSelectedTreeViewNode();
                    if (selectedNode != null)
                    {
                        bool isUserNode = selectedNode.Header.ToString() == "User";
                        string pathValue = isUserNode ?
                                           (userEnvVars.ContainsKey(selectedVar) ? userEnvVars[selectedVar] : null) :
                                           (systemEnvVars.ContainsKey(selectedVar) ? systemEnvVars[selectedVar] : null);

                        var modifyPathWindow = new ModifyPathWindow(pathValue, isUserNode);
                        modifyPathWindow.PathModified += (ss, se) =>
                        {
                            if (isUserNode)
                            {
                                userEnvVars[selectedVar] = modifyPathWindow.GetPathValue();
                            }
                            else
                            {
                                systemEnvVars[selectedVar] = modifyPathWindow.GetPathValue();
                            }
                            UpdateListBox();
                        };
                        modifyPathWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Please select either 'User' or 'System' node in the TreeView to modify the PATH variable.");
                    }
                }
                else
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
            }
        }

        private TreeViewItem GetSelectedTreeViewNode()
        {
            var selectedItem = EnvVarTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                // If the selected item is a child node, return its parent
                if (selectedItem.Parent is TreeViewItem parentItem)
                {
                    return parentItem;
                }
                return selectedItem;
            }
            return null;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator())
            {
                Elevate();
                Application.Current.Shutdown();
            }
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