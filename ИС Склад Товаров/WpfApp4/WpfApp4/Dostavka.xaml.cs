using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DonkeyRacingWarehouse
{
    public partial class Dostavka : Window
    {
        private string connectionString = "Server=WIN-GM7FV6G99FD; Database=ИС_Склад; Integrated Security=True;";
        public event EventHandler DeliveryConfirmed;

        public ObservableCollection<Product> ProductsList { get; set; } = new ObservableCollection<Product>();
        private Product SelectedProduct;

        public Dostavka()
        {
            InitializeComponent();
            DataContext = this;
            LoadProducts();
        }

        private void LoadProducts(string searchQuery = "", string searchCategory = "Имя поставщика")
        {
            string query = "SELECT Код_товара, Наименование, Имя_поставщика, Количество_вшт, Статус FROM ТоварыНаСкладе WHERE Статус = 'На складе'";

            if (!string.IsNullOrEmpty(searchQuery))
            {
                switch (searchCategory)
                {
                    case "Имя поставщика":
                        query += " AND Имя_поставщика LIKE @SearchQuery";
                        break;
                    case "Наименование товара":
                        query += " AND Наименование LIKE @SearchQuery";
                        break;
                    case "Код товара":
                        query += " AND Код_товара LIKE @SearchQuery";
                        break;
                }
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");

                DataTable table = new DataTable();
                connection.Open();
                adapter.Fill(table);

                ProductsList.Clear();
                foreach (DataRow row in table.Rows)
                {
                    ProductsList.Add(new Product
                    {
                        Code = row["Код_товара"].ToString(),
                        Name = row["Наименование"].ToString(),
                        Supplier = row["Имя_поставщика"].ToString(),
                        Quantity = Convert.ToInt32(row["Количество_вшт"]),
                        Status = row["Статус"].ToString()
                    });
                }

                ProductsGrid.ItemsSource = ProductsList;
            }
        }

        private void PerformSearch()
        {
            string query = SearchTextBox.Text;
            string category = (SupplierFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Имя поставщика";
            LoadProducts(query, category);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        private void SupplierFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PerformSearch();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SupplierFilter.SelectedIndex = -1;
            LoadProducts();
        }

        private void ConfirmDelivery_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один товар для оформления доставки.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProducts = ProductsGrid.SelectedItems.Cast<Product>().ToList();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        foreach (var product in selectedProducts)
                        {
                            // 1. Добавление в таблицу Доставка
                            string insertQuery = @"INSERT INTO Доставка (Код_товара, Наименование, Имя_поставщика, Количество, Статус) 
                                           VALUES (@Code, @Name, @Supplier, @Quantity, 'Оформлен на доставку')";
                            SqlCommand insertCmd = new SqlCommand(insertQuery, connection, transaction);
                            insertCmd.Parameters.AddWithValue("@Code", product.Code);
                            insertCmd.Parameters.AddWithValue("@Name", product.Name);
                            insertCmd.Parameters.AddWithValue("@Supplier", product.Supplier);
                            insertCmd.Parameters.AddWithValue("@Quantity", 1); // можно увеличить при необходимости
                            insertCmd.ExecuteNonQuery();

                            // 2. Обновление в ТоварыНаСкладе
                            string updateQuery = @"UPDATE ТоварыНаСкладе SET Статус = 'Оформлен на доставку'
                                           WHERE Код_товара = @Code AND Статус = 'На складе'";
                            SqlCommand updateCmd = new SqlCommand(updateQuery, connection, transaction);
                            updateCmd.Parameters.AddWithValue("@Code", product.Code);
                            updateCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }

                    // Обновление отображения
                    LoadProducts();
                    DeliveryConfirmed?.Invoke(this, EventArgs.Empty);

                    MessageBox.Show("Доставка успешно оформлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при оформлении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCount = ProductsGrid.SelectedItems.Count;
            SelectedCountText.Text = selectedCount.ToString();

            if (selectedCount == 1)
                SelectedProduct = ProductsGrid.SelectedItem as Product;
            else
                SelectedProduct = null;
        }
    }

    public class Product
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string Supplier { get; set; }
        public string Status { get; set; }
    }
}
