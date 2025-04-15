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
using ChessGame.Forms;

namespace ChessGame.TeamForms
{
    public partial class TeamForm : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        Form1 mainForm {  get; set; }
        public TeamForm(Form1 mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
        }

        private void TeamForm_Load(object sender, EventArgs e)
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
            FillDataGridView();
            if (mainForm.isTournament)
            {
                button1.Enabled = false;
                button2.Enabled = false;
            }
        }

        private void TeamForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveData();
            mainForm.button2.Enabled = true;
            mainForm.button2.Text = "Открыть таблицу команд";
            if (mainForm.isTournament)
            {
                mainForm.button5.Enabled = true;
            }
            else
            {
                mainForm.button1.Enabled = true;
                mainForm.button3.Enabled = true;
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
                    t.TeamId AS [TeamId],
                    t.TeamName AS [Название команды],
                    COUNT(p.PlayerId) AS [Количество шахматистов],
                    COALESCE(SUM(p.PlayerPoints), 0) AS [Общее количество очков]
                FROM 
                    Команда t
                LEFT JOIN 
                    Шахматист p ON t.TeamId = p.Команда_TeamId
                WHERE 
                    t.TeamId <> 0
                GROUP BY 
                    t.TeamId, t.TeamName
                ORDER BY 
                    [Общее количество очков] DESC,
                    [Количество шахматистов] ASC;";

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
                    dataGridView1.Columns["Название команды"].ReadOnly = false;
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
                        "UPDATE Команда " +
                        "SET TeamName = @TeamName, " +                      
                        "TeamId = @TeamId " +
                        "WHERE TeamId = @TeamId",
                        new SqlConnection(connectionString));
                    dataAdapter.UpdateCommand.Parameters.Add("@TeamName", SqlDbType.NVarChar, 100, "Название команды");
                    dataAdapter.UpdateCommand.Parameters.Add("@TeamId", SqlDbType.Int, 0, "TeamId");
                    dataAdapter.Update(changes);
                }
            }
        }
        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var newValue = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];
            DataGridViewRow selectedRow = selectedCell.OwningRow;
            int teamId = (int)selectedRow.Cells["TeamId"].Value;
            string currentTeamName = "";
            string queryCurrentTeamName = "SELECT TeamName FROM Команда WHERE TeamId = @TeamId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(queryCurrentTeamName, connection))
                {
                    command.Parameters.AddWithValue("@TeamId", teamId);
                    currentTeamName = (string)command.ExecuteScalar();
                }
            }   
            if (newValue.ToString() == currentTeamName && newValue.ToString().Count()>50&&newValue.ToString().Count()==0)
            {
                dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = currentTeamName;
                return;
            }
            string checkQuery = "SELECT COUNT(*) FROM Команда WHERE TeamName = @NewValue AND TeamId <> @TeamId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@NewValue", newValue);
                    checkCommand.Parameters.AddWithValue("@TeamId", teamId);

                    int count = (int)checkCommand.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show("Команда с таким названием уже существует.\nПожалуйста, выберите другое имя.");
                        dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = currentTeamName;
                        return;
                    }
                    
                }
                string updateQuery = "UPDATE Команда SET TeamName = @NewValue WHERE TeamId = @TeamId";

                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NewValue", newValue);
                    updateCommand.Parameters.AddWithValue("@TeamId", teamId);

                    try
                    {
                        updateCommand.ExecuteNonQuery();
                        MessageBox.Show("Данные успешно обновлены.");
                        FillDataGridView();
                        SaveData();
                    }
                    catch (Exception ex)
                    {
                        dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = currentTeamName;
                        MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}");
                    }
                }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            string teamName = "Новая команда";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    int teamId = 0;
                    using (SqlCommand getMaxIdCommand = new SqlCommand("SELECT ISNULL(MAX(TeamId), 0) FROM Команда", connection))
                    {
                        teamId = (int)getMaxIdCommand.ExecuteScalar();
                    }

                    int newTeamId = teamId + 1;

                    // Добавление нового шахматиста
                    string insertQuery = @"
                INSERT INTO Команда (TeamId, TeamName)
                VALUES (@TeamId,@TeamName)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@TeamId", newTeamId);
                        insertCommand.Parameters.AddWithValue("@TeamName", teamName);

                        int rowsAffected = insertCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Команда успешно добавлена.");
                            SaveData();
                            FillDataGridView();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при добавлении команды.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                DataGridViewRow selectedRow = selectedCell.OwningRow;

                int teamId = (int)selectedRow.Cells["TeamId"].Value;


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Обновляем шахматистов, присваивая им TeamId = 0
                    using (SqlCommand updatePlayersCommand = new SqlCommand(
                        "UPDATE Шахматист SET Команда_TeamId = 0 WHERE Команда_TeamId = @TeamId", connection))
                    {
                        updatePlayersCommand.Parameters.AddWithValue("@TeamId", teamId);
                        updatePlayersCommand.ExecuteNonQuery();
                    }
                }

                DataRow row = ((DataRowView)selectedRow.DataBoundItem).Row;
                row.Delete();

                DataTable dataTable = (DataTable)dataGridView1.DataSource;

                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    dataAdapter.DeleteCommand = new SqlCommand(
                        "DELETE FROM Команда WHERE TeamId = @TeamId",
                        new SqlConnection(connectionString));

                    dataAdapter.DeleteCommand.Parameters.Add("@TeamId", SqlDbType.Int, 0, "TeamId");

                    dataAdapter.Update(dataTable);
                    MessageBox.Show("Команда удалена\nЕё шахматисты теперь не принадлежат ни одной команде.");
                }

                SaveData();
                FillDataGridView();
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

                int teamId = (int)selectedRow.Cells["TeamId"].Value;
                TeamViewForm teamViewForm = new TeamViewForm(teamId);
                teamViewForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для просмотра.");
            }
        }
    }
}
