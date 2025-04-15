using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessGame.GameForms
{
    public partial class GameChangeForm : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        int gameId { get; set; }
        int firstPlayer { get; set; }
        int secondPlayer { get; set; }
        public GameChangeForm(int gameId)
        {
            InitializeComponent();
            this.gameId = gameId;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string firstPlayerQuery = "SELECT FirstPlayer From Партия Where GameId = @GameId;";
                string secondPlayerQuery = "SELECT SecondPlayer From Партия Where GameId = @GameId;";
                using (SqlCommand nameCommand = new SqlCommand(firstPlayerQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        firstPlayer = int.Parse(result.ToString());
                    }
                }
                using (SqlCommand nameCommand = new SqlCommand(secondPlayerQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        secondPlayer = int.Parse(result.ToString());
                    }
                }
            }

            LoadForm();
        }
        void LoadForm()
        {
            label2.Text += gameId;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string firstPlayer = "SELECT (Select PlayerName From Шахматист Where Шахматист.PlayerId = FirstPlayer) From Партия Where GameId = @GameId;";

                using (SqlCommand nameCommand = new SqlCommand(firstPlayer, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        label3.Text += result.ToString();
                    }
                }

                string secondPlayer = "SELECT (Select PlayerName From Шахматист Where Шахматист.PlayerId = SecondPlayer) From Партия Where GameId = @GameId;";

                using (SqlCommand nameCommand = new SqlCommand(secondPlayer, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        label4.Text += result.ToString();
                    }
                }

                string resultQuery = "Select Result From Партия Where GameId = @GameId";

                using (SqlCommand nameCommand = new SqlCommand(resultQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        label6.Text = result.ToString();
                        switch (label6.Text)
                        {
                            case "Победил":
                                {
                                    label6.ForeColor = Color.Green;
                                    break;
                                }
                            case "Проиграл":
                                {
                                    label6.ForeColor = Color.Red;
                                    break;
                                }
                            case "Ничья":
                                {
                                    label6.ForeColor = Color.Blue;
                                    break;
                                }
                        }
                    }
                }
            }
            LoadGrids();
        }
        private void LoadGrids()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;

            dataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView2.MultiSelect = false;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT 
                    PlayerId,
                    PlayerName AS [Имя],
                    PlayerRank AS [Разряд],
                    PlayerGamePlayed AS [Игры],
                    (SELECT TeamName from Команда Where TeamId = Команда_TeamId) as [Команда]
                FROM 
                    Шахматист
                WHERE 
                    PlayerGamePlayed < 
	                (
	                SELECT TOP 1
	                COUNT(*) 
	                FROM Шахматист p
	                JOIN Команда c ON p.Команда_TeamId = c.TeamId
	                WHERE c.TeamId != (SELECT TOP 1 Команда_TeamId FROM Шахматист s WHERE s.PlayerId = PlayerId))
                UNION
                     SELECT 
                        PlayerId as [PlayerId], 
                        PlayerName as [Имя],
                        PlayerRank as [Разряд],
                        PlayerGamePlayed as [Игры],
                        (SELECT TeamName FROM dbo.Команда WHERE Команда.TeamId = Шахматист.Команда_TeamId) AS [Команда]
                    FROM Шахматист
                        Where PlayerId = @FirstPlayer OR PlayerId = @SecondPlayer
                ORDER BY 
                    [Команда] ASC";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FirstPlayer", firstPlayer);
                command.Parameters.AddWithValue("@SecondPlayer", secondPlayer);

                SqlDataAdapter dataAdapter = new SqlDataAdapter(command);

                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);
                dataGridView1.DataSource = dataTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.ReadOnly = true;
                }
                dataGridView1.Columns["PlayerId"].Visible = false;
            }

            dataGridView1.CellClick += DataGridView1_CellClick;
            
            DataGridView1_CellClick(null,new DataGridViewCellEventArgs(0,0));

        }

        private int previousRowIndex = -1; 
        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex != previousRowIndex)
            {
                previousRowIndex = e.RowIndex;
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];
                int firstPlayerId = Convert.ToInt32(selectedRow.Cells["PlayerId"].Value);


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    SELECT 
                        PlayerId as [PlayerId], 
                        PlayerName as [Имя],
                        PlayerRank as [Разряд],
                        PlayerGamePlayed as [Игры],
                        (SELECT TeamName FROM dbo.Команда WHERE Команда.TeamId = Шахматист.Команда_TeamId) AS [Команда]
                    FROM Шахматист
                    WHERE 
                        (Шахматист.Команда_TeamId != (SELECT Команда_TeamId FROM Шахматист WHERE PlayerId = @PlayerId)
                        AND NOT EXISTS (
                            SELECT 1
                            FROM Партия 
                            WHERE (FirstPlayer = @PlayerId AND SecondPlayer = Шахматист.PlayerId) 
                                OR (SecondPlayer = @PlayerId AND FirstPlayer = Шахматист.PlayerId))
                        AND (
                             PlayerGamePlayed < 
	                            (
	                            SELECT TOP 1
	                            COUNT(*) 
	                            FROM Шахматист p
	                            JOIN Команда c ON p.Команда_TeamId = c.TeamId
	                            WHERE c.TeamId != (SELECT TOP 1 Команда_TeamId FROM Шахматист s WHERE s.PlayerId = PlayerId))
                                ))

                    UNION ALL

                    SELECT 
                        PlayerId as [PlayerId], 
                        PlayerName as [Имя],
                        PlayerRank as [Разряд],
                        PlayerGamePlayed as [Игры],
                        (SELECT TeamName FROM dbo.Команда WHERE Команда.TeamId = Шахматист.Команда_TeamId) AS [Команда]
                    FROM Шахматист
                        Where PlayerId = @FirstPlayer OR PlayerId = @SecondPlayer
                    ORDER BY 
                    [Команда] ASC";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@PlayerId", firstPlayerId);
                    if (firstPlayerId != firstPlayer)
                        command.Parameters.AddWithValue("@FirstPlayer", firstPlayer); 
                    else
                        command.Parameters.AddWithValue("@FirstPlayer", -1);
                    if (firstPlayerId != secondPlayer)
                        command.Parameters.AddWithValue("@SecondPlayer", secondPlayer);
                    else
                        command.Parameters.AddWithValue("@SecondPlayer", -1);

                    SqlDataAdapter dataAdapter = new SqlDataAdapter(command);

                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    dataGridView2.DataSource = dataTable;
                    dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    // Делаем все колонки только для чтения
                    foreach (DataGridViewColumn column in dataGridView2.Columns)
                    {
                        column.ReadOnly = true;
                    }
                    dataGridView2.Columns["PlayerId"].Visible = false;
                }

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            switch (label6.Text)
            {
                case "Победил":
                    {
                        label6.Text = "Проиграл";
                        label6.ForeColor = Color.Red;
                        break;
                    }
                case "Проиграл":
                    {
                        label6.Text = "Ничья";
                        label6.ForeColor = Color.Blue;
                        break;
                    }
                case "Ничья":
                    {
                        label6.Text = "Победил";
                        label6.ForeColor = Color.Green;
                        break;
                    }

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                int firstPlayerId = -1;
                int secondPlayerId = -1;
                string gameResult = "";
                
                connection.Open();

                string firstPlayerQuery = "SELECT FirstPlayer From Партия Where GameId = @GameId;";
                string secondPlayerQuery = "SELECT SecondPlayer From Партия Where GameId = @GameId;";
                string resultQuery = "Select Result From Партия Where GameId = @GameId";
                using (SqlCommand nameCommand = new SqlCommand(firstPlayerQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        firstPlayerId = int.Parse(result.ToString());
                    }
                }
                using (SqlCommand nameCommand = new SqlCommand(secondPlayerQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        secondPlayerId = int.Parse(result.ToString());
                    }
                }
                using (SqlCommand nameCommand = new SqlCommand(resultQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@GameId", gameId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null) {
                        gameResult = result.ToString();
                    }
                }

                // Уменьшаем количество игр у игроков
                string updatePlayerGamesQuery = @"
                UPDATE Шахматист
                SET PlayerGamePlayed = PlayerGamePlayed - 1
                WHERE PlayerId IN (@FirstPlayerId, @SecondPlayerId);";

                using (SqlCommand updateCommand = new SqlCommand(updatePlayerGamesQuery, new SqlConnection(connectionString)))
                {
                    updateCommand.Parameters.AddWithValue("@FirstPlayerId", firstPlayerId);
                    updateCommand.Parameters.AddWithValue("@SecondPlayerId", secondPlayerId);

                    updateCommand.Connection.Open();
                    updateCommand.ExecuteNonQuery();
                }

                // Снимаем очки в зависимости от результата
                if (gameResult == "Победил")
                {
                    // Первый игрок проиграл, а второй выиграл
                    string updatePointsQuery = @"
                    UPDATE Шахматист
                    SET PlayerPoints = PlayerPoints - 1
                    WHERE PlayerId = @FirstPlayerId;";

                    using (SqlCommand updatePointsCommand = new SqlCommand(updatePointsQuery, new SqlConnection(connectionString)))
                    {
                        updatePointsCommand.Parameters.AddWithValue("@FirstPlayerId", firstPlayerId);

                        updatePointsCommand.Connection.Open();
                        updatePointsCommand.ExecuteNonQuery();
                    }
                }
                else if (gameResult == "Ничья")
                {
                    // Если ничья, то оба игрока теряют по 0.5 очка
                    string updateDrawPointsQuery = @"
                    UPDATE Шахматист
                    SET PlayerPoints = PlayerPoints - 0.5
                    WHERE PlayerId IN (@FirstPlayerId, @SecondPlayerId);";

                    using (SqlCommand updateDrawCommand = new SqlCommand(updateDrawPointsQuery, new SqlConnection(connectionString)))
                    {
                        updateDrawCommand.Parameters.AddWithValue("@FirstPlayerId", firstPlayerId);
                        updateDrawCommand.Parameters.AddWithValue("@SecondPlayerId", secondPlayerId);

                        updateDrawCommand.Connection.Open();
                        updateDrawCommand.ExecuteNonQuery();
                    }
                }
                else if (gameResult == "Проиграл")
                {
                    // Первый игрок выиграл, а второй проиграл
                    string updateLosePointsQuery = @"
                    UPDATE Шахматист
                    SET PlayerPoints = PlayerPoints - 1
                    WHERE PlayerId = @SecondPlayerId;";

                    using (SqlCommand updateLoseCommand = new SqlCommand(updateLosePointsQuery, new SqlConnection(connectionString)))
                    {
                        updateLoseCommand.Parameters.AddWithValue("@SecondPlayerId", secondPlayerId);

                        updateLoseCommand.Connection.Open();
                        updateLoseCommand.ExecuteNonQuery();
                    }
                }


                DataGridViewCell selectedCell1 = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow1 = selectedCell1.OwningRow;

                firstPlayerId = (int)selectedRow1.Cells["PlayerId"].Value;

                DataGridViewCell selectedCell2 = dataGridView2.SelectedCells[0];

                DataGridViewRow selectedRow2 = selectedCell2.OwningRow;

                secondPlayerId = (int)selectedRow2.Cells["PlayerId"].Value;


                string updateQuery = @"
                        UPDATE Шахматист
                        SET PlayerGamePlayed = PlayerGamePlayed + 1
                        WHERE PlayerId = @FirstPlayerId;

                        UPDATE Шахматист
                        SET PlayerGamePlayed = PlayerGamePlayed + 1
                        WHERE PlayerId = @SecondPlayerId;

                        UPDATE Шахматист
                        SET PlayerPoints = PlayerPoints + @FirstPlayerPoints
                        WHERE PlayerId = @FirstPlayerId;

                        UPDATE Шахматист
                        SET PlayerPoints = PlayerPoints + @SecondPlayerPoints
                        WHERE PlayerId = @SecondPlayerId;
                    ";

                // Рассчитываем очки
                double firstPlayerPoints = 0;
                double secondPlayerPoints = 0;

                if (label6.Text == "Победил")
                {
                    firstPlayerPoints = 1;
                    secondPlayerPoints = 0;
                }
                else if (label6.Text == "Ничья")
                {
                    firstPlayerPoints = 0.5;
                    secondPlayerPoints = 0.5;
                }
                else if (label6.Text == "Проиграл")
                {
                    firstPlayerPoints = 0;
                    secondPlayerPoints = 1;
                }

                // Обновление количества очков для игроков
                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@FirstPlayerId", firstPlayerId);
                    updateCommand.Parameters.AddWithValue("@SecondPlayerId", secondPlayerId);
                    updateCommand.Parameters.AddWithValue("@FirstPlayerPoints", firstPlayerPoints);
                    updateCommand.Parameters.AddWithValue("@SecondPlayerPoints", secondPlayerPoints);
                    updateCommand.ExecuteNonQuery();  // Выполняем запрос для обновления очков и количества игр
                   
                }

                string query = $"UPDATE Партия SET FirstPlayer = @NewFirstPlayer, SecondPlayer = @NewSecondPlayer, Result = @Result WHERE GameId = @GameId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NewFirstPlayer", firstPlayerId);
                    command.Parameters.AddWithValue("@NewSecondPlayer", secondPlayerId);
                    command.Parameters.AddWithValue("@Result", label6.Text);
                    command.Parameters.AddWithValue("@GameId", gameId);

                    try
                    {
                        command.ExecuteNonQuery();
                        MessageBox.Show("Данные успешно обновлены.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}");
                    }


                }
                this.DialogResult = DialogResult.OK;




            }
        }
    }
}
