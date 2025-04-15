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
    public partial class EnemyForm : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        int id {  get; set; }
        public EnemyForm(int id)
        {
            InitializeComponent();
            this.id = id;
            LoadForm();
        }
        private void LoadForm()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();


                // Запрос для получения имени игрока по id
                string playerNameQuery = "SELECT PlayerName FROM Шахматист WHERE PlayerId = @PlayerId";

                using (SqlCommand nameCommand = new SqlCommand(playerNameQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@PlayerId", id);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        label2.Text += result.ToString(); // Добавляем имя игрока в label2
                    }
                }

                // Запрос для получения списка соперников
                string query = @"
                SELECT DISTINCT 
                    game.GameId AS [ID Партии],
                    CASE 
                        WHEN game.FirstPlayer = @PlayerId THEN secondPlayer.PlayerName
                        WHEN game.SecondPlayer = @PlayerId THEN firstPlayer.PlayerName
                    END AS [Имя],
                    CASE 
                        WHEN game.FirstPlayer = @PlayerId THEN secondPlayer.PlayerRank
                        WHEN game.SecondPlayer = @PlayerId THEN firstPlayer.PlayerRank
                    END AS [Разряд],
                    CASE 
                        WHEN game.FirstPlayer = @PlayerId THEN secondPlayer.PlayerGamePlayed
                        WHEN game.SecondPlayer = @PlayerId THEN firstPlayer.PlayerGamePlayed
                    END AS [Количество игр],
                    CASE 
                        WHEN game.FirstPlayer = @PlayerId THEN 
                            (SELECT TeamName FROM Команда WHERE Команда.TeamId = secondPlayer.Команда_TeamId)
                        WHEN game.SecondPlayer = @PlayerId THEN 
                            (SELECT TeamName FROM Команда WHERE Команда.TeamId = firstPlayer.Команда_TeamId)
                    END AS [Команда]
                FROM 
                    Партия game
                JOIN 
                    Шахматист firstPlayer ON game.FirstPlayer = firstPlayer.PlayerId
                JOIN 
                    Шахматист secondPlayer ON game.SecondPlayer = secondPlayer.PlayerId
                WHERE 
                    game.FirstPlayer = @PlayerId OR game.SecondPlayer = @PlayerId;
                ";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Устанавливаем значение параметра @PlayerId
                    command.Parameters.AddWithValue("@PlayerId", id);

                    using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
                    {
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
                    }
                }
            }
        }

    }
}
