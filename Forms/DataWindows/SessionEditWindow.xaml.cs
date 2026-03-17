using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public class TableItem
    {
        public int Id { get; set; }
        public string DisplayText { get; set; }
        public override string ToString() => DisplayText;
    }

    public class PersonItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class ParticipantViewModel
    {
        public int RecordId { get; set; }
        public int PersonId { get; set; }
        public string FullName { get; set; }
        public string AddedAt { get; set; }
    }

    public partial class SessionEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _sessionId;
        private readonly int _currentUserPersonId;

        private ObservableCollection<ParticipantViewModel> _participants = new();

        // Данные выбранной игры
        private int? _selectedCopyId = null;
        private string _selectedGameTitle = null;
        private string _selectedInventoryNumber = null;

        private const decimal HOURLY_RATE = 300m;
        private const double ROUND_INTERVAL_MIN = 30;

        public SessionEditWindow(int? sessionId = null, int currentUserPersonId = 0)
        {
            InitializeComponent();
            _sessionId = sessionId;
            _currentUserPersonId = currentUserPersonId;

            Title = _sessionId.HasValue ? "Редактирование сессии" : "Добавление сессии";
            tbTitle.Text = Title;

            lvParticipants.ItemsSource = _participants;

            LoadOrganizersAndParticipantsList();
            LoadTables();
            LoadAvailableGames();
            AttachFloatingHints();
            AttachCostCalculationEvents();

            if (_sessionId.HasValue)
            {
                LoadSessionData();
            }
            else
            {
                dpStartDate.SelectedDate = DateTime.Today;
                tbStartHour.Text = DateTime.Now.Hour.ToString("00");
                tbStartMinute.Text = DateTime.Now.Minute.ToString("00");
                chkActiveSession.IsChecked = true;
                UpdateCalculatedCost();
            }
        }

        #region Инициализация

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(tbStartHour, HintStartTime, StartTimeTransform);
            FloatingHintHelper.Attach(tbStartMinute, HintStartTime, StartTimeTransform);
            FloatingHintHelper.Attach(tbEndHour, HintEndTime, EndTimeTransform);
            FloatingHintHelper.Attach(tbEndMinute, HintEndTime, EndTimeTransform);
            FloatingHintHelper.Attach(tbNotes, HintNotes, NotesTransform);
        }

        private void AttachCostCalculationEvents()
        {
            dpStartDate.SelectedDateChanged += (s, e) => UpdateCalculatedCost();
            tbStartHour.TextChanged += (s, e) => UpdateCalculatedCost();
            tbStartMinute.TextChanged += (s, e) => UpdateCalculatedCost();
            dpEndDate.SelectedDateChanged += (s, e) => UpdateCalculatedCost();
            tbEndHour.TextChanged += (s, e) => UpdateCalculatedCost();
            tbEndMinute.TextChanged += (s, e) => UpdateCalculatedCost();
            chkActiveSession.Checked += (s, e) => UpdateCalculatedCost();
            chkActiveSession.Unchecked += (s, e) => UpdateCalculatedCost();
        }

        #endregion

        #region Загрузка данных

        private void LoadOrganizersAndParticipantsList()
        {
            var dtVisitors = _db.GetActiveVisitors();
            var visitors = dtVisitors.AsEnumerable().Select(row => new PersonItem
            {
                Id = row.Field<int>("person_id"),
                Name = row.Field<string>("full_name")
            }).ToList();

            cmbAddParticipant.ItemsSource = visitors;
            cmbAddParticipant.DisplayMemberPath = "Name";
            cmbAddParticipant.SelectedValuePath = "Id";

            var dtMasters = _db.GetGameMasters(includeInactive: true);
            var masters = dtMasters.AsEnumerable().Select(row => new PersonItem
            {
                Id = row.Field<int>("person_id"),
                Name = row.Field<string>("full_name")
            }).ToList();

            if (_sessionId.HasValue)
            {
                var sessionData = _db.GetSessionById(_sessionId.Value);
                if (sessionData != null && sessionData.Rows.Count > 0)
                {
                    int orgId = Convert.ToInt32(sessionData.Rows[0]["organizer_id"]);
                    if (!masters.Any(m => m.Id == orgId) && orgId > 0)
                    {
                        var personDt = _db.GetPersonById(orgId);
                        if (personDt.Rows.Count > 0)
                        {
                            masters.Add(new PersonItem
                            {
                                Id = orgId,
                                Name = personDt.Rows[0]["full_name"].ToString()
                            });
                        }
                    }
                }
            }

            cmbOrganizer.ItemsSource = masters;
            cmbOrganizer.DisplayMemberPath = "Name";
            cmbOrganizer.SelectedValuePath = "Id";
        }

        private void LoadTables()
        {
            var dt = _db.GetTablesForGrid();
            var tables = new List<TableItem>();
            foreach (DataRow row in dt.Rows)
            {
                tables.Add(new TableItem
                {
                    Id = Convert.ToInt32(row["table_id"]),
                    DisplayText = $"Стол {row["table_number"]} • {row["capacity"]} чел. • {row["zone"]}"
                });
            }
            cmbTable.ItemsSource = tables;
            cmbTable.DisplayMemberPath = "DisplayText";
            cmbTable.SelectedValuePath = "Id";
        }

        private void LoadAvailableGames()
        {
            var dt = _db.GetAvailableGameCopies();
            var games = dt.AsEnumerable().Select(row => new
            {
                CopyId = row.Field<int>("copy_id"),
                DisplayName = row.Field<string>("display_name"),
                GameTitle = row.Field<string>("game_title"),
                InventoryNumber = row.Field<string>("inventory_number")
            }).ToList();

            cmbGame.ItemsSource = games;
            cmbGame.DisplayMemberPath = "DisplayName";
            cmbGame.SelectedValuePath = "CopyId";
        }

        private void LoadSessionData()
        {
            var dt = _db.GetSessionById(_sessionId.Value);
            if (dt == null || dt.Rows.Count == 0)
            {
                Close();
                return;
            }

            var r = dt.Rows[0];

            if (r["organizer_id"] != DBNull.Value)
                cmbOrganizer.SelectedValue = Convert.ToInt32(r["organizer_id"]);

            if (r["table_id"] != DBNull.Value)
                cmbTable.SelectedValue = Convert.ToInt32(r["table_id"]);

            var start = Convert.ToDateTime(r["started_at"]);
            dpStartDate.SelectedDate = start.Date;
            tbStartHour.Text = start.Hour.ToString("00");
            tbStartMinute.Text = start.Minute.ToString("00");

            if (r["ended_at"] != DBNull.Value)
            {
                var end = Convert.ToDateTime(r["ended_at"]);
                dpEndDate.SelectedDate = end.Date;
                tbEndHour.Text = end.Hour.ToString("00");
                tbEndMinute.Text = end.Minute.ToString("00");
                chkActiveSession.IsChecked = false;
            }
            else
            {
                chkActiveSession.IsChecked = true;
            }

            chkPaid.IsChecked = Convert.ToBoolean(r["paid"]);
            tbNotes.Text = r["notes"]?.ToString() ?? "";

            LoadParticipants();
            LoadSessionGame(); // загружаем игру, привязанную к сессии
        }

        private void LoadParticipants()
        {
            _participants.Clear();
            var dt = _db.GetSessionParticipants(_sessionId.Value);

            foreach (DataRow row in dt.Rows)
            {
                _participants.Add(new ParticipantViewModel
                {
                    RecordId = Convert.ToInt32(row["id"]),
                    PersonId = Convert.ToInt32(row["person_id"]),
                    FullName = row["full_name"].ToString(),
                    AddedAt = row["joined_time"].ToString()
                });
            }
        }

        private void LoadSessionGame()
        {
            var dt = _db.GetSessionGames(_sessionId.Value);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0]; // берём первую игру (по логике она одна)
                _selectedCopyId = Convert.ToInt32(row["copy_id"]);
                _selectedGameTitle = row["game_title"].ToString();
                _selectedInventoryNumber = row["inventory_number"].ToString();

                tbSelectedGame.Text = _selectedGameTitle;
                tbGameInventory.Text = $"Инв. №: {_selectedInventoryNumber}";
                borderSelectedGame.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                ClearSelectedGame();
            }
        }

        private void ClearSelectedGame()
        {
            _selectedCopyId = null;
            _selectedGameTitle = null;
            _selectedInventoryNumber = null;
            tbSelectedGame.Text = "Игра не выбрана";
            tbGameInventory.Text = "";
            borderSelectedGame.Background = System.Windows.Media.Brushes.WhiteSmoke;
        }

        #endregion

        #region Расчёт стоимости

        private void UpdateCalculatedCost()
        {
            // без изменений (как в предыдущей версии)
            if (!dpStartDate.SelectedDate.HasValue ||
                !int.TryParse(tbStartHour.Text, out int sh) ||
                !int.TryParse(tbStartMinute.Text, out int sm) ||
                sh < 0 || sh > 23 || sm < 0 || sm > 59)
            {
                tbCalculatedCost.Text = "—";
                return;
            }

            DateTime startDt = dpStartDate.SelectedDate.Value.Date.AddHours(sh).AddMinutes(sm);

            if (chkActiveSession.IsChecked == true)
            {
                tbCalculatedCost.Text = "";
                return;
            }

            if (!dpEndDate.SelectedDate.HasValue ||
                !int.TryParse(tbEndHour.Text, out int eh) ||
                !int.TryParse(tbEndMinute.Text, out int em) ||
                eh < 0 || eh > 23 || em < 0 || em > 59)
            {
                tbCalculatedCost.Text = "—";
                return;
            }

            DateTime endDt = dpEndDate.SelectedDate.Value.Date.AddHours(eh).AddMinutes(em);

            if (endDt <= startDt)
            {
                tbCalculatedCost.Text = "ошибка: время окончания ≤ времени начала";
                return;
            }

            TimeSpan duration = endDt - startDt;
            double totalMinutes = duration.TotalMinutes;

            if (totalMinutes <= 0)
            {
                tbCalculatedCost.Text = "0 ₽";
                return;
            }

            double roundedMinutes = Math.Ceiling(totalMinutes / ROUND_INTERVAL_MIN) * ROUND_INTERVAL_MIN;
            decimal hoursEquivalent = (decimal)roundedMinutes / 60m;
            decimal cost = hoursEquivalent * HOURLY_RATE;

            tbCalculatedCost.Text = $"{cost:N0} ₽";
        }

        #endregion

        #region Обработчики событий

        private void chkActiveSession_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (gridEndTime != null)
                gridEndTime.IsEnabled = chkActiveSession.IsChecked != true;
        }

        private void btnAddParticipant_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAddParticipant.SelectedItem is not PersonItem selected)
            {
                MessageBox.Show("Выберите участника из списка!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int personId = selected.Id;
            string name = selected.Name;

            if (_participants.Any(p => p.PersonId == personId))
            {
                MessageBox.Show("Этот человек уже в списке участников.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _participants.Add(new ParticipantViewModel
            {
                PersonId = personId,
                FullName = name,
                AddedAt = DateTime.Now.ToString("HH:mm")
            });

            cmbAddParticipant.SelectedIndex = -1;
        }

        private void btnRemoveParticipant_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ParticipantViewModel vm)
                _participants.Remove(vm);
        }

        private void btnSetGame_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGame.SelectedItem == null)
            {
                MessageBox.Show("Выберите игру из списка!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selected = cmbGame.SelectedItem; // анонимный тип
            _selectedCopyId = selected.CopyId;
            _selectedGameTitle = selected.GameTitle;
            _selectedInventoryNumber = selected.InventoryNumber;

            tbSelectedGame.Text = _selectedGameTitle;
            tbGameInventory.Text = $"Инв. №: {_selectedInventoryNumber}";
            borderSelectedGame.Background = System.Windows.Media.Brushes.LightGreen;
        }

        private void btnClearGame_Click(object sender, RoutedEventArgs e)
        {
            ClearSelectedGame();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAndCollectData(out var parameters, out var participantIds))
                return;

            try
            {
                int targetSessionId;

                if (_sessionId.HasValue)
                {
                    parameters["@session_id"] = _sessionId.Value;
                    _db.UpdateSession(parameters);
                    _db.ClearSessionParticipants(_sessionId.Value);
                    _db.ClearSessionGames(_sessionId.Value);
                    targetSessionId = _sessionId.Value;
                }
                else
                {
                    int newId = _db.InsertSessionAndGetId(parameters);
                    targetSessionId = newId;
                }

                // Добавляем участников
                foreach (int pid in participantIds)
                    _db.AddParticipantToSession(targetSessionId, pid);

                // Добавляем игру (если выбрана)
                if (_selectedCopyId.HasValue)
                    _db.AddGameToSession(targetSessionId, _selectedCopyId.Value);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateAndCollectData(out Dictionary<string, object> parameters, out List<int> participantPersonIds)
        {
            parameters = null;
            participantPersonIds = null;

            if (cmbOrganizer.SelectedValue == null)
            {
                MessageBox.Show("Выберите организатора", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbOrganizer.Focus();
                return false;
            }

            if (cmbTable.SelectedValue == null)
            {
                MessageBox.Show("Выберите стол", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbTable.Focus();
                return false;
            }

            if (!dpStartDate.SelectedDate.HasValue ||
                !int.TryParse(tbStartHour.Text, out int sh) ||
                !int.TryParse(tbStartMinute.Text, out int sm) ||
                sh < 0 || sh > 23 || sm < 0 || sm > 59)
            {
                MessageBox.Show("Некорректное время начала (часы 0–23, минуты 0–59)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            DateTime startDt = dpStartDate.SelectedDate.Value.Date.AddHours(sh).AddMinutes(sm);
            DateTime? endDt = null;

            if (chkActiveSession.IsChecked != true)
            {
                if (!dpEndDate.SelectedDate.HasValue ||
                    !int.TryParse(tbEndHour.Text, out int eh) ||
                    !int.TryParse(tbEndMinute.Text, out int em) ||
                    eh < 0 || eh > 23 || em < 0 || em > 59)
                {
                    MessageBox.Show("Некорректное время окончания (часы 0–23, минуты 0–59)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                endDt = dpEndDate.SelectedDate.Value.Date.AddHours(eh).AddMinutes(em);

                if (endDt <= startDt)
                {
                    MessageBox.Show("Время окончания должно быть позже времени начала", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            decimal cost = 0m;
            if (chkActiveSession.IsChecked != true && endDt.HasValue)
            {
                TimeSpan duration = endDt.Value - startDt;
                double totalMinutes = duration.TotalMinutes;
                if (totalMinutes > 0)
                {
                    double roundedMinutes = Math.Ceiling(totalMinutes / ROUND_INTERVAL_MIN) * ROUND_INTERVAL_MIN;
                    decimal hoursEquivalent = (decimal)roundedMinutes / 60m;
                    cost = hoursEquivalent * HOURLY_RATE;
                }
            }

            parameters = new Dictionary<string, object>
            {
                ["@organizer_id"] = cmbOrganizer.SelectedValue,
                ["@table_id"] = cmbTable.SelectedValue,
                ["@started_at"] = startDt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["@ended_at"] = endDt.HasValue ? endDt.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value,
                ["@cost"] = cost,
                ["@paid"] = chkPaid.IsChecked == true ? 1 : 0,
                ["@notes"] = string.IsNullOrWhiteSpace(tbNotes.Text) ? DBNull.Value : tbNotes.Text.Trim(),
                ["@created_by"] = _currentUserPersonId
            };

            participantPersonIds = _participants.Select(p => p.PersonId).ToList();
            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}