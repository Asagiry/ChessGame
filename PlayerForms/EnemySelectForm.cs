using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessGame.Forms;

namespace ChessGame.PlayerForms
{
    public partial class EnemySelectForm : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        int playerId {  get; set; }
        public EnemySelectForm(int playerId)
        {
            InitializeComponent();
            this.playerId = playerId;
            LoadForm();
        }
        private void LoadForm()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            label2.Text += playerId;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();


                string playerNameQuery = "SELECT PlayerName FROM Шахматист WHERE PlayerId = @PlayerId";

                using (SqlCommand nameCommand = new SqlCommand(playerNameQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@PlayerId", playerId);

                    object result = nameCommand.ExecuteScalar();
                    if (result != null)
                    {
                        label3.Text += result.ToString();
                    }
                }

                string teamNameQuery = @"
                SELECT TeamName 
                FROM Команда 
                WHERE TeamId = (SELECT Команда_TeamId FROM Шахматист WHERE PlayerId = @PlayerId)";

                using (SqlCommand nameCommand = new SqlCommand(teamNameQuery, connection))
                {
                    nameCommand.Parameters.AddWithValue("@PlayerId", playerId);

                    object result = nameCommand.ExecuteScalar();
                    label4.Text += result.ToString();  
                   
                }


                string query = @"
                    SELECT 
                        PlayerId as [PlayerId], 
                        PlayerName as [Имя],
                        PlayerRank as [Разряд],
                        PlayerGamePlayed as [Игры],
                        PlayerPoints as [Очки],
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
                                ))";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlayerId", playerId);  

                SqlDataAdapter dataAdapter = new SqlDataAdapter(command);

                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);

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
            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];
                DataGridViewRow selectedRow = selectedCell.OwningRow;

                // Получаем идентификаторы игроков и результат игры
                int firstPlayerId = playerId; // Используем заранее определенный playerId
                int secondPlayerId = (int)selectedRow.Cells["PlayerId"].Value;
                string result = label6.Text;

                // Запрашиваем максимальный GameId из таблицы Партия
                int maxGameId = 0;
                string maxGameIdQuery = "SELECT MAX(GameId) FROM Партия";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(maxGameIdQuery, connection))
                    {
                        object resultObj = command.ExecuteScalar();
                        if (resultObj != DBNull.Value)
                        {
                            maxGameId = Convert.ToInt32(resultObj);
                        }
                    }

                    // Увеличиваем на 1 для нового GameId
                    int newGameId = maxGameId + 1;

                    // Подготовка строки для INSERT
                    string insertQuery = @"
                    INSERT INTO Партия (GameId, FirstPlayer, SecondPlayer, Result)
                    VALUES (@GameId, @FirstPlayer, @SecondPlayer, @Result);";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        // Добавляем параметры
                        insertCommand.Parameters.AddWithValue("@GameId", newGameId);
                        insertCommand.Parameters.AddWithValue("@FirstPlayer", firstPlayerId);
                        insertCommand.Parameters.AddWithValue("@SecondPlayer", secondPlayerId);
                        insertCommand.Parameters.AddWithValue("@Result", result);

                        try
                        {
                            insertCommand.ExecuteNonQuery();  // Выполняем запрос
                            MessageBox.Show("Партия успешно записана!");

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при добавлении партии: {ex.Message}");
                        }

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

                        if (result == "Победил")
                        {
                            firstPlayerPoints = 1;
                            secondPlayerPoints = 0;
                        }
                        else if (result == "Ничья")
                        {
                            firstPlayerPoints = 0.5;
                            secondPlayerPoints = 0.5;
                        }
                        else if (result == "Проиграл")
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

                            try
                            {
                                updateCommand.ExecuteNonQuery();  // Выполняем запрос для обновления очков и количества игр
                                MessageBox.Show("Очки и количество игр успешно обновлены!");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}");
                            }
                        }

                        // Закрытие окна или выполнение других действий после успешного завершения
                        this.DialogResult = DialogResult.OK;
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для выбора оппонента.");
            }
        }
    }
}
