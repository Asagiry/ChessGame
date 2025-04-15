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

namespace ChessGame.GameForms
{
    public partial class GameForm : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        Form1 mainForm { get; set; }
        public GameForm(Form1 mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
        }

        private void GameForm_Load(object sender, EventArgs e)
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            FillDataGridView();
            if (!mainForm.isTournament)
            {
                button2.Enabled = false;
                button3.Enabled = false;
            }
        }

        private void GameForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //SaveData();
            mainForm.button3.Enabled = true;
            mainForm.button3.Text = "Открыть таблицу партий";
            if (mainForm.isTournament)
            {
                mainForm.button5.Enabled = true;
            }
            else
            {
                mainForm.button1.Enabled = true;
                mainForm.button2.Enabled = true;
                mainForm.button4.Enabled = true;
            }
        }

        private void FillDataGridView()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT 
                    GameId as [Номер игры],
                    (SELECT PlayerName FROM Шахматист Where PlayerId = FirstPlayer) as [Первый игрок],
                    Result as [Результат],
                    (SELECT PlayerName FROM Шахматист Where PlayerId = SecondPlayer) as [Второй игрок],
                    FirstPlayer,
                    SecondPlayer
                FROM
                    Партия
                ORDER BY 
                    [Результат] ASC
                ;";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);
                dataGridView1.DataSource = dataTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.ReadOnly = true;
                }
                dataGridView1.Columns["FirstPlayer"].Visible = false;
                dataGridView1.Columns["SecondPlayer"].Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow = selectedCell.OwningRow;

                int gameId = (int)selectedRow.Cells["Номер игры"].Value;
                int firstPlayerId = (int)selectedRow.Cells["FirstPlayer"].Value;
                int secondPlayerId = (int)selectedRow.Cells["SecondPlayer"].Value;
                string result = (string)selectedRow.Cells["Результат"].Value;

                // Удаляем строку из DataGridView
                DataRow row = ((DataRowView)selectedRow.DataBoundItem).Row;
                row.Delete();

                DataTable dataTable = (DataTable)dataGridView1.DataSource;

                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    dataAdapter.DeleteCommand = new SqlCommand(
                        "DELETE FROM Партия WHERE GameId = @GameId",
                        new SqlConnection(connectionString));

                    dataAdapter.DeleteCommand.Parameters.Add("@GameId", SqlDbType.Int, 0, "Номер игры");

                    dataAdapter.Update(dataTable);

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
                    if (result == "Победил")
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
                    else if (result == "Ничья")
                    {
                        // Если ничья, то оба игрока теряют по 0.5 очка
                        string updateDrawPointsQuery = @"
                    UPDATE Шахматист
                    SET PlayerPoints = PlayerPoints - 0.5
                    WHERE PlayerId IN (@FirstPlayerId, @SecondPlayerId);";

                        using (SqlCommand updateDrawCommand = new SqlCommand(updateDrawPointsQuery, new SqlConnection(connectionString)))
                        {
                            updateDrawCommand.Parameters.AddWithValue("@FirstPlayerId", firstPlayerId);

                            updateDrawCommand.Connection.Open();
                            updateDrawCommand.ExecuteNonQuery();
                        }
                    }
                    else if (result == "Проиграл")
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

                    MessageBox.Show("Партия успешно удалена и данные обновлены.");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для удаления.");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow = selectedCell.OwningRow;

                int gameId = (int)selectedRow.Cells["Номер игры"].Value;

                GameChangeForm gameChangeForm = new GameChangeForm(gameId);
                gameChangeForm.ShowDialog();
                FillDataGridView();

              
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для изменения.");
            }
        }
    }
}
