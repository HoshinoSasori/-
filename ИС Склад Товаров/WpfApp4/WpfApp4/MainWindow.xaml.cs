using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace DonkeyRacingWarehouse
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=WIN-GM7FV6G99FD; Database=ИС_Склад; Integrated Security=True;";
        public static Oformlenie Product;
        public static Dostavka dostavka;

        public MainWindow()
        {
            InitializeComponent();
            LoadTovaryNaSkladeData();
        }

        // Метод для загрузки данных о товарах на складе
        private void LoadWarehouseData()
        {
            string query = "SELECT Код_товара, Наименование, Имя_поставщика, Количество_вшт, Статус FROM ТоварыНаСкладе";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();

                try
                {
                    connection.Open();
                    dataAdapter.Fill(dataTable);
                    TovarDataGrid.ItemsSource = dataTable.DefaultView; // Привязка данных к DataGrid
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
                }
            }
        }

        // Метод для загрузки данных с фильтром по поисковому запросу
        private void LoadProducts(string searchQuery = "", string searchCategory = "")
        {
            string query = "SELECT Код_товара, Наименование, Имя_поставщика, Количество_вшт, Статус FROM ТоварыНаСкладе";

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query += $" WHERE {searchCategory} LIKE @SearchQuery";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                dataAdapter.SelectCommand.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");

                DataTable dataTable = new DataTable();
                connection.Open();
                dataAdapter.Fill(dataTable);
                TovarDataGrid.ItemsSource = dataTable.DefaultView; // Привязка данных к DataGrid
            }
        }

        // Открытие окна оформления
        private void Oformlenie_Click(object sender, RoutedEventArgs e)
        {
            if (Product == null || !Product.IsLoaded)
            {
                Product = new Oformlenie();
                Product.Owner = this;
                Product.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Подписка на событие обновления данных
                Product.RefreshDataEvent += LoadWarehouseData;

                // Обработка закрытия окна Oformlenie
                Product.Closed += (s, args) =>
                {
                    // Проверяем, что объект Product не равен null, перед отпиской от события
                    if (Product != null)
                    {
                        Product.RefreshDataEvent -= LoadWarehouseData;
                        Product = null; // Очищаем ссылку на окно после его закрытия
                    }
                };

                Product.Show();
            }
            else
            {
                Product.Activate();

                if (Product.WindowState == WindowState.Minimized)
                {
                    Product.WindowState = WindowState.Normal;
                }
            }
        }

        // Открытие окна доставки
        private void Dostavka_Click(object sender, RoutedEventArgs e)
        {
            if (dostavka == null || !dostavka.IsLoaded)
            {
                dostavka = new Dostavka();
                dostavka.Owner = this;
                dostavka.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dostavka.Closed += (s, args) => dostavka = null;
                dostavka.Show();
            }
            else
            {
                dostavka.Activate();

                if (dostavka.WindowState == WindowState.Minimized)
                {
                    dostavka.WindowState = WindowState.Normal;
                }
            }

            // Подписка на событие подтверждения доставки
            dostavka.DeliveryConfirmed += (s, args) =>
            {
                LoadWarehouseData(); // Обновляем данные в DataGrid после оформления доставки
            };
        }

        // Перезагрузка данных в таблице
        private void ReloadTable_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts(); // Загружаем таблицу заново
        }

        // Очистка всех данных из таблицы
        private void ClearTable_Click(object sender, RoutedEventArgs e)
        {
            // Подтверждение удаления всех данных
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить все данные из таблицы?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        // Удаляем все записи из таблицы Доставка, которые ссылаются на товары на складе
                        string deleteDostavkaQuery = "DELETE FROM Доставка WHERE Код_товара IN (SELECT Код_товара FROM ТоварыНаСкладе)";
                        SqlCommand deleteDostavkaCommand = new SqlCommand(deleteDostavkaQuery, connection);
                        deleteDostavkaCommand.ExecuteNonQuery();

                        // Теперь удаляем все записи из таблицы ТоварыНаСкладе
                        string deleteTovaryQuery = "DELETE FROM ТоварыНаСкладе";
                        SqlCommand deleteTovaryCommand = new SqlCommand(deleteTovaryQuery, connection);
                        deleteTovaryCommand.ExecuteNonQuery();

                        MessageBox.Show("Все данные успешно удалены.");
                        TovarDataGrid.ItemsSource = null; // Очищаем таблицу визуально
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении данных: " + ex.Message);
                    }
                }
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем текстовые поля
            NameSearchTextBox.Clear();
            CodeSearchTextBox.Clear();
            SupplierSearchTextBox.Clear();

            // Загружаем все товары (без фильтрации)
            LoadWarehouseData();  // Этот метод, который загружает все товары с базы данных
        }


        // Метод для обновления данных в таблице ТоварыНаСкладе
        private void LoadTovaryNaSkladeData()
        {
            string query = "SELECT Код_товара, Наименование, Имя_поставщика, Количество_вшт, Статус FROM ТоварыНаСкладе";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();

                try
                {
                    connection.Open();
                    dataAdapter.Fill(dataTable);
                    TovarDataGrid.ItemsSource = dataTable.DefaultView; // Привязка данных к DataGrid
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
                }
            }
        }
        private void SearchTextBoxes_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch(); // автоматический поиск
        }
        private void PerformSearch()
        {
            string nameSearch = NameSearchTextBox.Text.Trim();
            string codeSearch = CodeSearchTextBox.Text.Trim();
            string supplierSearch = SupplierSearchTextBox.Text.Trim();

            string query = "SELECT Код_товара, Наименование, Имя_поставщика, Количество_вшт, Статус FROM ТоварыНаСкладе WHERE 1 = 1";

            if (!string.IsNullOrEmpty(nameSearch))
                query += " AND Наименование LIKE @NameSearch";
            if (!string.IsNullOrEmpty(codeSearch))
                query += " AND Код_товара LIKE @CodeSearch";
            if (!string.IsNullOrEmpty(supplierSearch))
                query += " AND Имя_поставщика LIKE @SupplierSearch";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);

                dataAdapter.SelectCommand.Parameters.AddWithValue("@NameSearch", "%" + nameSearch + "%");
                dataAdapter.SelectCommand.Parameters.AddWithValue("@CodeSearch", "%" + codeSearch + "%");
                dataAdapter.SelectCommand.Parameters.AddWithValue("@SupplierSearch", "%" + supplierSearch + "%");

                DataTable dataTable = new DataTable();
                connection.Open();
                dataAdapter.Fill(dataTable);

                TovarDataGrid.ItemsSource = dataTable.DefaultView;
            }
        }

    }
}
