using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessGame.Forms
{
    public partial class TeamViewForm : Form
    {
        int teamId {  get; set; }
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        public TeamViewForm(int teamId)
        {
            this.teamId = teamId;
            InitializeComponent();
            loadForm();
        }
        private void loadForm()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Запрос для получения списка шахматистов
                string query = @"
        SELECT 
            PlayerName as [Имя шахматиста],
            PlayerRank as [Разряд],
            PlayerGamePlayed as [Количество игр],
            PlayerPoints as [Количество очков]         
        FROM Шахматист
        WHERE Шахматист.Команда_TeamId = @teamId";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                dataAdapter.SelectCommand.Parameters.AddWithValue("@teamId", this.teamId);

                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);

                // Привязываем данные к DataGridView
                dataGridView1.DataSource = dataTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Делаем все колонки только для чтения
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.ReadOnly = true;
                }

                // Теперь получим название команды для label2
                string teamNameQuery = "SELECT TeamName FROM dbo.Команда WHERE TeamId = @teamId";
                SqlCommand teamNameCommand = new SqlCommand(teamNameQuery, connection);
                teamNameCommand.Parameters.AddWithValue("@teamId", this.teamId);

                // Получаем название команды
                object result = teamNameCommand.ExecuteScalar();
                string teamName = result != DBNull.Value ? result.ToString() : "Неизвестно"; // Если команда не найдена, выводим "Неизвестно"

                // Обновляем label2 с названием команды
                label2.Text += teamName;
            }
        }


    }
}
