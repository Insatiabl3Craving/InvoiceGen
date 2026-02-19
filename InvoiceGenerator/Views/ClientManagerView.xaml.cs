using System;
using System.Windows;
using System.Windows.Controls;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.Views
{
    public partial class ClientManagerView : UserControl
    {
        private readonly ClientService _clientService = new();

        public ClientManagerView()
        {
            InitializeComponent();
            LoadClients();
        }

        private async void LoadClients()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                ClientsDataGrid.ItemsSource = clients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading clients: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddClientBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientEditDialog
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true)
            {
                LoadClients();
            }
        }

        private void EditClientBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is Client client)
            {
                var dialog = new ClientEditDialog(client)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    LoadClients();
                }
            }
            else
            {
                MessageBox.Show("Please select a client to edit.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteClientBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is Client client)
            {
                try
                {
                    var hasInvoices = await _clientService.HasInvoicesAsync(client.Id);
                    if (hasInvoices)
                    {
                        MessageBox.Show(
                            $"Cannot delete '{client.DisplayName}' because there are invoices associated with this client.\n\n" +
                            "Please delete all invoices for this client first, or keep the client record for history.",
                            "Cannot Delete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error checking client invoices: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to delete '{client.DisplayName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _clientService.DeleteClientAsync(client.Id);
                        LoadClients();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting client: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a client to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClientsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is Client client)
            {
                var dialog = new ClientEditDialog(client)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    LoadClients();
                }
            }
        }
    }
}
