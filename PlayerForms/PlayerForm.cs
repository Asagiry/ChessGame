using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessGame.PlayerForms;

namespace ChessGame.Forms
{
    public partial class PlayerForm : Form
    {

        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";

        private Form1 mainForm { get; set; }
        public PlayerForm(Form1 mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
        }

        private void PlayerForm_FormClosed(object sender, FormClosedEventArgs e)
        {

            SaveData();
            mainForm.button1.Enabled = true;
            mainForm.button1.Text = "Открыть таблицу шахматистов";
            if (mainForm.isTournament)
            {
                mainForm.button5.Enabled = true;
            }
            else
            {
                mainForm.button2.Enabled = true;
                mainForm.button3.Enabled = true;
                mainForm.button4.Enabled = true;
            }
        }

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
            FillDataGridView();
            if (!mainForm.isTournament)
            {
                button4.Enabled = false; 
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
            }
        }


        private void button1_Click(object sender, EventArgs e) // Добавить шахматиста
        {
            string playerName = "Новый шахматист";
            int playerRank = 5;
            int playerGamePlayed = 0;
            int playerPoints = 0;
            int teamId = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    int maxId = 0;
                    using (SqlCommand getMaxIdCommand = new SqlCommand("SELECT ISNULL(MAX(PlayerId), 0) FROM Шахматист", connection))
                    {
                        maxId = (int)getMaxIdCommand.ExecuteScalar();
                    }

                    int newPlayerId = maxId + 1;
                    string insertQuery = @"
                INSERT INTO Шахматист (PlayerId, PlayerName, PlayerRank, PlayerGamePlayed, PlayerPoints, Команда_TeamId)
                VALUES (@PlayerId, @PlayerName, @PlayerRank, @PlayerGamePlayed, @PlayerPoints, @TeamId)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@PlayerId", newPlayerId);
                        insertCommand.Parameters.AddWithValue("@PlayerName", playerName);
                        insertCommand.Parameters.AddWithValue("@PlayerRank", playerRank);
                        insertCommand.Parameters.AddWithValue("@PlayerGamePlayed", playerGamePlayed);
                        insertCommand.Parameters.AddWithValue("@PlayerPoints", playerPoints);
                        insertCommand.Parameters.AddWithValue("@TeamId", teamId);

                        int rowsAffected = insertCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Шахматист успешно добавлен.");
                            SaveData();
                            FillDataGridView();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при добавлении шахматиста.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }


        private void button2_Click(object sender, EventArgs e)  //Удалить шахматиста
        {

            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow = selectedCell.OwningRow;

                int playerId = (int)selectedRow.Cells["PlayerId"].Value;

                DataRow row = ((DataRowView)selectedRow.DataBoundItem).Row;
                row.Delete();

                DataTable dataTable = (DataTable)dataGridView1.DataSource;

                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    dataAdapter.DeleteCommand = new SqlCommand(
                        "DELETE FROM Шахматист WHERE PlayerId = @PlayerId",
                        new SqlConnection(connectionString));

                    dataAdapter.DeleteCommand.Parameters.Add("@PlayerId", SqlDbType.Int, 0, "PlayerId");

                    dataAdapter.Update(dataTable);
                    MessageBox.Show("Удаление успешно выполнено.");
                }

                SaveData();
                FillDataGridView();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для удаления.");
            }

        }

        private void button3_Click(object sender, EventArgs e) //Зачислить в команду
        {

            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow = selectedCell.OwningRow;

                int playerId = (int)selectedRow.Cells["PlayerId"].Value;
                string name = (string)selectedRow.Cells["Имя шахматиста"].Value;
                string teamName = (string)selectedRow.Cells["Название команды"].Value;
                TeamChoose teamChoose = new TeamChoose(playerId, name, teamName);
                if (teamChoose.ShowDialog() == DialogResult.OK)
                {
                    string query = "UPDATE Шахматист SET Команда_TeamId = @TeamId WHERE PlayerId = @PlayerId";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                // Добавляем параметры для безопасной передачи значений
                                command.Parameters.AddWithValue("@TeamId", teamChoose.teamId);
                                command.Parameters.AddWithValue("@PlayerId", playerId);

                                // Выполняем запрос
                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Команда успешно обновлена.");
                                }
                                else
                                {
                                    MessageBox.Show("Ошибка при обновлении команды.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка: {ex.Message}");
                        }
                        SaveData();
                        FillDataGridView();
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для зачисления в команду.");
            }
        }

        private void button4_Click(object sender, EventArgs e) //Записать на игру
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow = selectedCell.OwningRow;

                int playerId = (int)selectedRow.Cells["PlayerId"].Value;

                int maxGames = GetMaxGamesForPlayer(playerId);
                if (maxGames == (int)selectedRow.Cells["Количество игр"].Value)
                {
                    MessageBox.Show("Этот игрок сыграл со всеми игроками");
                    return;
                }
                EnemySelectForm enemySelectForm = new EnemySelectForm(playerId);
                enemySelectForm.ShowDialog();
                FillDataGridView();


            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для зачисления в команду.");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FiredPlayersForm firedPlayersForm = new FiredPlayersForm();
            firedPlayersForm.ShowDialog();
        }

        private void FillDataGridView()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT 
                    PlayerId as [PlayerId], 
                    PlayerName as [Имя шахматиста],
                    PlayerRank as [Разряд],
                    PlayerGamePlayed as [Количество игр],
                    PlayerPoints as [Количество очков],
                    Команда_TeamId as [TeamId],  -- Добавляем TeamId для использования в обновлении
                    (SELECT TeamName FROM dbo.Команда WHERE Команда.TeamId = Шахматист.Команда_TeamId) AS [Название команды]
                FROM Шахматист
                ORDER BY
                [Название команды] ASC";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);
                dataGridView1.DataSource = dataTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.ReadOnly = true;
                }
                if (!mainForm.isTournament)
                    dataGridView1.Columns["Имя шахматиста"].ReadOnly = false;
                dataGridView1.Columns["PlayerId"].Visible = false;
                dataGridView1.Columns["TeamId"].Visible = false;
            }
        }

        public void SaveData()
        {
            DataTable dataTable = (DataTable)dataGridView1.DataSource;
            DataTable changes = dataTable.GetChanges();
            if (changes != null)
            {
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    // Создаем команду для обновления данных
                    dataAdapter.UpdateCommand = new SqlCommand(
                        "UPDATE Шахматист " +
                        "SET PlayerName = @PlayerName, " +
                        "PlayerRank = @PlayerRank, " +
                        "PlayerGamePlayed = @PlayerGamePlayed, " +
                        "PlayerPoints = @PlayerPoints, " +
                        "Команда_TeamId = @TeamId " +
                        "WHERE PlayerId = @PlayerId",
                        new SqlConnection(connectionString));
                    dataAdapter.UpdateCommand.Parameters.Add("@PlayerName", SqlDbType.NVarChar, 100, "Имя шахматиста");
                    dataAdapter.UpdateCommand.Parameters.Add("@PlayerRank", SqlDbType.Int, 0, "Разряд");
                    dataAdapter.UpdateCommand.Parameters.Add("@PlayerGamePlayed", SqlDbType.Int, 0, "Количество игр");
                    dataAdapter.UpdateCommand.Parameters.Add("@PlayerPoints", SqlDbType.Int, 0, "Количество очков");
                    dataAdapter.UpdateCommand.Parameters.Add("@TeamId", SqlDbType.Int, 0, "TeamId");
                    dataAdapter.UpdateCommand.Parameters.Add("@PlayerId", SqlDbType.Int, 0, "PlayerId");
                    dataAdapter.Update(changes);
                }
            }
        }



        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var newValue = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];
            DataGridViewRow selectedRow = selectedCell.OwningRow;
            int playerId = (int)selectedRow.Cells["PlayerId"].Value;
            if (newValue.ToString().Count() > 50 && newValue.ToString().Count()==0)
            {
                dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = GetPlayerNameById(playerId);
            }
            string query = $"UPDATE Шахматист SET PlayerName = @NewValue WHERE PlayerId = @PlayerId";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NewValue", newValue);
                    command.Parameters.AddWithValue("@PlayerId", playerId);

                    try
                    {
                        command.ExecuteNonQuery();
                        MessageBox.Show("Данные успешно обновлены.");
                        FillDataGridView();
                        SaveData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}");
                    }


                }
            }
        }

        private int GetMaxGamesForPlayer(int playerId)
        {
            int maxGames = 0;

            // Ваш запрос для получения максимального количества игр, например:
            string query = @"
            SELECT COUNT(*) 
            FROM Шахматист p
            JOIN Команда c ON p.Команда_TeamId = c.TeamId
            WHERE c.TeamId != (SELECT Команда_TeamId FROM Шахматист WHERE PlayerId = @PlayerId)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerId", playerId);

                    maxGames = (int)command.ExecuteScalar(); // Получаем максимальное количество игр
                }
            }

            return maxGames;
        }

        private string GetPlayerNameById(int playerId)
        {
            string playerName = string.Empty;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("GetPlayerNameById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure; 
                    command.Parameters.AddWithValue("@PlayerId", playerId); 

                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        playerName = result.ToString(); 
                    }
                }
            }
            return playerName; 
        }


    }
        


}

