using System;
using System.Data.SqlClient;
using System.Windows;

namespace DonkeyRacingWarehouse
{
    public partial class Oformlenie : Window
    {
        private string connectionString = "Server=WIN-GM7FV6G99FD; Database=ИС_Склад; Integrated Security=True;";

        // Делегат для обновления данных в главном окне
        public delegate void RefreshDataDelegate();
        public event RefreshDataDelegate RefreshDataEvent;

        public Oformlenie()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string supplier = SupplierTextBox.Text;
            string productName = ProductNameTextBox.Text;
            string quantity = QuantityTextBox.Text;

            // Генерация случайного кода товара
            Random rand = new Random();
            string productCode = rand.Next(10000, 99999).ToString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Начало транзакции
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        // Вставка в таблицу Поставщик
                        string insertToSupplier = @"INSERT INTO Поставщик (Имя_поставщика, Наименование, Количесвтво_вшт, Код_товара) 
                                                    VALUES (@Supplier, @Name, @Quantity, @Code)";
                        SqlCommand cmd1 = new SqlCommand(insertToSupplier, connection, transaction);
                        cmd1.Parameters.AddWithValue("@Supplier", supplier);
                        cmd1.Parameters.AddWithValue("@Name", productName);
                        cmd1.Parameters.AddWithValue("@Quantity", quantity);
                        cmd1.Parameters.AddWithValue("@Code", productCode);
                        cmd1.ExecuteNonQuery();

                        // Вставка в таблицу ТоварыНаСкладе
                        string insertToWarehouse = @"INSERT INTO ТоварыНаСкладе (Имя_поставщика, Наименование, Количество_вшт, Код_товара, Статус) 
                                                     VALUES (@Supplier, @Name, @Quantity, @Code, 'На складе')";
                        SqlCommand cmd2 = new SqlCommand(insertToWarehouse, connection, transaction);
                        cmd2.Parameters.AddWithValue("@Supplier", supplier);
                        cmd2.Parameters.AddWithValue("@Name", productName);
                        cmd2.Parameters.AddWithValue("@Quantity", quantity);
                        cmd2.Parameters.AddWithValue("@Code", productCode);
                        cmd2.ExecuteNonQuery();

                        // Подтверждение транзакции
                        transaction.Commit();

                        // Уведомление пользователя
                        MessageBox.Show($"Товар успешно добавлен.\nСгенерированный код: {productCode}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // После успешного добавления вызываем событие для обновления данных
                        RefreshDataEvent?.Invoke();
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }
    }
}
