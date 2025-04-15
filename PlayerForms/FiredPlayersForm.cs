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
    public partial class FiredPlayersForm : Form
    {

        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";


        public FiredPlayersForm()
        {
            InitializeComponent();
            LoadForm();
        }
        private void LoadForm()
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

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                // Получаем выбранную ячейку
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                // Получаем строку, к которой относится выбранная ячейка
                DataGridViewRow selectedRow = selectedCell.OwningRow;

                // Получаем PlayerId из выбранной строки
                int playerId = (int)selectedRow.Cells["PlayerId"].Value;

                EnemyForm enemyForm = new EnemyForm(playerId);
                enemyForm.ShowDialog();

            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для просмотра списка партий.");
            }
        }
    }
}
