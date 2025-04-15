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

namespace ChessGame.StatisticForms
{
    public partial class StatisticForm : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        public StatisticForm()
        {
            InitializeComponent();
            LoadForm();
        }
        void LoadForm()
        {
            GetBestTeam();
            GetBestPlayer();
            FillGradePlayers();
            FillFiredPlayers();

        }

        private void GetBestTeam()
        {
            string query = @"
                SELECT TOP 1
                    c.TeamName,
                    SUM(s.PlayerPoints) AS TotalPoints
                FROM 
                    Команда c
                JOIN 
                    Шахматист s ON c.TeamId = s.Команда_TeamId
                GROUP BY 
                    c.TeamId, c.TeamName
                ORDER BY 
                    TotalPoints DESC;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {
                            string teamName = reader["TeamName"].ToString();
                            double totalPoints = double.Parse(reader["TotalPoints"].ToString());

                            label2.Text += totalPoints;
                            label3.Text += teamName;
                        }
                        else
                        {
                            MessageBox.Show("Нет команд в базе данных.");
                        }
                    }
                }
            }
        }

        private void GetBestPlayer()
        {
            string query = @"
            SELECT TOP 1
                s.PlayerName,
                s.PlayerPoints,
                s.PlayerGamePlayed
            FROM 
                Шахматист s
            ORDER BY 
                s.PlayerPoints DESC,
                s.PlayerGamePlayed ASC ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string playerName = reader["PlayerName"].ToString();
                            double totalPoints = double.Parse(reader["PlayerPoints"].ToString());
                            label4.Text += playerName;
                            label5.Text += totalPoints;
                        }
                        else
                        {
                            MessageBox.Show("Нет игроков в базе данных.");
                        }
                    }
                }
            }

        }

        private void FillGradePlayers()
        {
            dataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView2.MultiSelect = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Запрос для получения списка шахматистов
                string query = @"
                Select
                    PlayerId,
	                PlayerName as [Имя],
	                PlayerRank as [Разряд],
	                PlayerGamePlayed as [Игры],
	                PlayerPoints as [Очки],
	                (Select TeamName from Команда Where TeamId = Команда_TeamId) as [Команда]
                From
	                Шахматист
                Where
	                PlayerGamePlayed <>0 AND PlayerPoints/PlayerGamePlayed > 0.5";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);

                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);

                // Привязываем данные к DataGridView
                dataGridView2.DataSource = dataTable;
                dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Делаем все колонки только для чтения
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.ReadOnly = true;
                }
                dataGridView2.Columns["PlayerId"].Visible = false;
            }
        }
        private void FillFiredPlayers()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Запрос для получения списка шахматистов
                string query = @"
                SELECT 
                    p.PlayerId,
                    p.PlayerName AS [Имя],
                    p.PlayerRank AS [Разряд],
                    p.PlayerGamePlayed AS [Игры],
                    p.PlayerPoints AS [Очки],
                    (SELECT TeamName FROM Команда WHERE Команда.TeamId = p.Команда_TeamId) AS [Команда]
                FROM 
                    Шахматист p
                WHERE 
                    EXISTS 
                    (
                        SELECT 1 
                        FROM Партия game
                        WHERE game.FirstPlayer = p.PlayerId OR game.SecondPlayer = p.PlayerId
                    )
                AND NOT EXISTS (
                        SELECT 1
                        FROM Партия game
                        WHERE 
                            (game.FirstPlayer = p.PlayerId AND game.Result = 'Победил') -- Победил как первый игрок
                            OR 
                            (game.SecondPlayer = p.PlayerId AND game.Result = 'Проиграл') -- Победил как второй игрок
                            OR 
                            (game.FirstPlayer = p.PlayerId AND game.Result = 'Ничья') -- Ничья как первый игрок
                            OR 
                            (game.SecondPlayer = p.PlayerId AND game.Result = 'Ничья') -- Ничья как второй игрок
                );";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);

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
                dataGridView1.Columns["PlayerId"].Visible = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)//UP
        {
            List<int> playerIdsToGrade = new List<int>();

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Cells["PlayerId"].Value != null)
                {
                    int playerId = Convert.ToInt32(row.Cells["PlayerId"].Value);
                    playerIdsToGrade.Add(playerId);
                }
            }

            if (playerIdsToGrade.Count > 0)
            {
                string ids = string.Join(",", playerIdsToGrade);

                string deleteQuery = $"UPDATE Шахматист SET PlayerGamePlayed = 0, PlayerRank = PlayerRank-1, PlayerPoints = PlayerPoints -(PlayerRank-5)*0.05 WHERE PlayerId IN ({ids});";
               
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery(); 

                        MessageBox.Show($"{rowsAffected} игроков повышено.");
                    }

                    string deleteGamesQuery = "DELETE FROM Партия;";
                    using (SqlCommand deleteCommand = new SqlCommand(deleteGamesQuery, connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                MessageBox.Show("Нет игроков для повышения.");
            }
            FillGradePlayers();
        }

        private void button1_Click(object sender, EventArgs e) //FIRE
        {


            List<int> playerIdsToDelete = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["PlayerId"].Value != null) 
                {
                    int playerId = Convert.ToInt32(row.Cells["PlayerId"].Value);
                    playerIdsToDelete.Add(playerId);
                }
            }

            // Формирование SQL-запроса для удаления
            if (playerIdsToDelete.Count > 0)
            {
                // Преобразование списка в строку для SQL-запроса
                string ids = string.Join(",", playerIdsToDelete);

                string deleteQuery = $"DELETE FROM Шахматист WHERE PlayerId IN ({ids});";
                string deleteGamesQuery = "DELETE FROM Партия;";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand deleteCommand = new SqlCommand(deleteGamesQuery, connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }

                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery(); // Выполнение запроса на удаление

                        MessageBox.Show($"{rowsAffected} записей удалено.");
                    }
                }
            }
            else
            {
                MessageBox.Show("Нет игроков для удаления.");
            }
            FillFiredPlayers();
        }
    }
}
