using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.UniversalAccessibility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;

namespace WpfNastolSystem.Windows
{
    public partial class IncomingGameCopiesWindow : Window
    {
        private readonly DbManager _db = new();
        private ObservableCollection<IncomingItem> _items = new();

        public IncomingGameCopiesWindow()
        {
            InitializeComponent();
            tbDate.Text = $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}";

            // Загружаем список игр для ComboBox
            LoadGamesIntoCombo();

            dgItems.ItemsSource = _items;

            // Пример начальной пустой строки
            _items.Add(new IncomingItem());
        }

        private void LoadGamesIntoCombo()
        {
            string query = "SELECT game_id, title FROM games ORDER BY title";
            DataTable games = _db.Select(query);

            var comboColumn = dgItems.Columns.OfType<DataGridComboBoxColumn>()
                .First(c => c.Header.ToString() == "Игра");

            comboColumn.ItemsSource = games.DefaultView;
        }

        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            _items.Add(new IncomingItem());
            dgItems.ScrollIntoView(_items.Last());
        }

        private void dgItems_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Можно добавить валидацию при начале редактирования
        }

        private void dgItems_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.Item as IncomingItem;
                if (item != null && item.game_id <= 0)
                {
                    MessageBox.Show("Выберите игру.");
                    e.Cancel = true;
                }
            }
        }

        private void BtnSaveAndPrint_Click(object sender, RoutedEventArgs e)
        {
            dgItems.CommitEdit(DataGridEditingUnit.Row, true);

            var validItems = _items.Where(i => i.game_id > 0 && i.quantity > 0).ToList();

            if (!validItems.Any())
            {
                MessageBox.Show("Добавьте хотя бы одну позицию с игрой и количеством > 0.");
                return;
            }

            try
            {
                _db.InTransaction((conn, tx) =>
                {
                    foreach (var item in validItems)
                    {
                        for (int i = 0; i < item.quantity; i++)
                        {
                            string invNumber = $"{item.start_inventory ?? "INV"}{(i + 1):D4}";

                            var pars = new Dictionary<string, object>
                            {
                                { "@game_id", item.game_id },
                                { "@inventory_number", invNumber },
                                { "@acquired_date", DateTime.Now },
                                { "@location", item.location ?? "Склад 1" },
                                { "@is_available", 1 },
                                { "@conditions", item.conditions ?? "Новая" },
                                { "@notes", $"Приход {DateTime.Now:dd.MM.yyyy} | {item.notes ?? ""}" }
                            };

                            string sql = @"
                                INSERT INTO game_copies 
                                (game_id, inventory_number, acquired_date, location, is_available, conditions, notes)
                                VALUES (@game_id, @inventory_number, @acquired_date, @location, @is_available, @conditions, @notes)";

                            _db.NonQuery(sql, pars);
                        }
                    }
                });

                // Генерация PDF
                string pdfPath = GenerateIncomingPdf(validItems);

                MessageBox.Show($"Приход успешно сохранён.\nНакладная: {pdfPath}", "Успех");

                try { Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true }); }
                catch { }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения прихода:\n{ex.Message}", "Ошибка");
            }
        }

        private string GenerateIncomingPdf(List<IncomingItem> items)
        {
            var doc = new PdfDocument();
            doc.Info.Title = "Приходная накладная";
            doc.Info.Subject = $"Поступление копий игр {DateTime.Now:dd.MM.yyyy}";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            var gfx = XGraphics.FromPdfPage(page);
            var fontTitle = new XFont("Arial", 16);
            var fontHeader = new XFont("Arial", 12);
            var fontNormal = new XFont("Arial", 11);

            int y = 40;

            gfx.DrawString("ПРИХОДНАЯ НАКЛАДНАЯ", fontTitle, XBrushes.Black, new XRect(0, y, page.Width, 0), XStringFormats.TopCenter);
            y += 40;

            gfx.DrawString($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}", fontNormal, XBrushes.Black, 40, y); y += 30;

            // Шапка таблицы
            gfx.DrawString("№", fontHeader, XBrushes.Black, 40, y);
            gfx.DrawString("Игра", fontHeader, XBrushes.Black, 80, y);
            gfx.DrawString("Кол-во", fontHeader, XBrushes.Black, 400, y);
            gfx.DrawString("Состояние", fontHeader, XBrushes.Black, 480, y);
            gfx.DrawString("Инв. номер(а)", fontHeader, XBrushes.Black, 580, y);
            y += 25;

            gfx.DrawLine(XPens.Black, 35, y, page.Width - 35, y); y += 10;

            int index = 1;
            foreach (var item in items)
            {
                string gameName = GetGameTitleById(item.game_id);
                string invRange = $"{item.start_inventory ?? "—"} × {item.quantity}";

                gfx.DrawString(index.ToString(), fontNormal, XBrushes.Black, 40, y);
                gfx.DrawString(gameName, fontNormal, XBrushes.Black, 80, y);
                gfx.DrawString(item.quantity.ToString(), fontNormal, XBrushes.Black, 400, y);
                gfx.DrawString(item.conditions ?? "Новая", fontNormal, XBrushes.Black, 480, y);
                gfx.DrawString(invRange, fontNormal, XBrushes.Black, 580, y);

                y += 22;
                index++;
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filename = $"Приход_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
            string path = Path.Combine(desktop, filename);

            doc.Save(path);
            return path;
        }

        private string GetGameTitleById(int gameId)
        {
            string sql = "SELECT title FROM games WHERE game_id = @id";
            var res = _db.Scalar(sql, new Dictionary<string, object> { { "@id", gameId } });
            return res?.ToString() ?? $"Игра #{gameId}";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Модель строки прихода
    public class IncomingItem
    {
        public int game_id { get; set; }
        public int quantity { get; set; } = 1;
        public string? start_inventory { get; set; }
        public string? conditions { get; set; } = "Новая";
        public string? location { get; set; }
        public string? notes { get; set; }
    }
}