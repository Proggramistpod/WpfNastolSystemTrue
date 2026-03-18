using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Style;

namespace WpfNastolSystem.Windows
{
    public partial class IncomingGameCopiesWindow : Window
    {
        private readonly DbManager _db = new();
        private ObservableCollection<IncomingViewItem> _items = new();

        public IncomingGameCopiesWindow()
        {
            InitializeComponent();
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
            dpDate.SelectedDate = DateTime.Now;
            dgItems.ItemsSource = _items;
        }

        private string TranslateCondition(string condition)
        {
            return condition?.ToLower() switch
            {
                "good" => "Хорошее",
                "fair" => "Удовлетворительное",
                "bad" => "Плохое",
                "new" => "Новое",
                _ => condition ?? "-"
            };
        }
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (dpDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату");
                return;
            }

            DateTime date = dpDate.SelectedDate.Value;

            string sql = @"
                SELECT 
                    g.title,
                    gc.inventory_number,
                    gc.acquired_date,
                    gc.location,
                    gc.conditions
                FROM game_copies gc
                JOIN games g ON g.game_id = gc.game_id
                WHERE DATE(gc.acquired_date) = @date
                ORDER BY g.title";

            DataTable table = _db.Select(sql, new Dictionary<string, object>
            {
                { "@date", date.ToString("yyyy-MM-dd") }
            });

            _items.Clear();

            foreach (DataRow row in table.Rows)
            {
                _items.Add(new IncomingViewItem
                {
                    GameTitle = row["title"].ToString(),
                    InventoryNumber = row["inventory_number"].ToString(),
                    AcquiredDate = Convert.ToDateTime(row["acquired_date"]),
                    Location = row["location"].ToString(),
                    Conditions = row["conditions"].ToString()
                });
            }

            // ❗ Валидация
            if (!_items.Any())
            {
                MessageBox.Show("За выбранную дату поступлений нет");
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!_items.Any())
            {
                MessageBox.Show("Нет данных для печати");
                return;
            }

            DateTime date = dpDate.SelectedDate.Value;

            string path = GeneratePdf(_items.ToList(), date);

            MessageBox.Show($"PDF создан:\n{path}");

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }

        private string GeneratePdf(List<IncomingViewItem> items, DateTime date)
        {
            var doc = new PdfDocument();
            var page = doc.AddPage();

            var gfx = XGraphics.FromPdfPage(page);

            var fontTitle = new XFont("Arial", 16);
            var fontHeader = new XFont("Arial", 11);
            var font = new XFont("Arial", 10);

            int y = 40;

            // Заголовок
            gfx.DrawString("ОТЧЕТ ПО ПОСТУПЛЕНИЯМ", fontHeader, XBrushes.Blue,
                           new XRect(0, y, page.Width, 20), XStringFormats.TopCenter);

            y += 40;

            gfx.DrawString($"Дата: {date:dd.MM.yyyy}",
                font,
                XBrushes.Black,
                40, y);

            y += 30;

            // --- Таблица ---
            int startX = 40;

            int colNum = startX;
            int colGame = colNum + 30;
            int colInv = colGame + 200;
            int colCond = colInv + 120;
            int colLoc = colCond + 120;

            // Заголовки
            gfx.DrawString("№", fontHeader, XBrushes.Black, colNum, y);
            gfx.DrawString("Игра", fontHeader, XBrushes.Black, colGame, y);
            gfx.DrawString("Инв. номер", fontHeader, XBrushes.Black, colInv, y);
            gfx.DrawString("Состояние", fontHeader, XBrushes.Black, colCond, y);
            gfx.DrawString("Место", fontHeader, XBrushes.Black, colLoc, y);

            y += 15;

            gfx.DrawLine(XPens.Black, startX, y, page.Width - 40, y);
            y += 10;

            int i = 1;

            foreach (var item in items)
            {
                // 👉 перенос на новую страницу если не хватает места
                if (y > page.Height - 100)
                {
                    page = doc.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }

                gfx.DrawString(i.ToString(), font, XBrushes.Black, colNum, y);
                gfx.DrawString(item.GameTitle, font, XBrushes.Black, colGame, y);
                gfx.DrawString(item.InventoryNumber, font, XBrushes.Black, colInv, y);

                gfx.DrawString(TranslateCondition(item.Conditions),
                    font, XBrushes.Black, colCond, y);

                gfx.DrawString(item.Location, font, XBrushes.Black, colLoc, y);

                y += 18;
                i++;
            }

            // --- Подпись ---
            y += 30;

            // если не помещается — новая страница
            if (y > page.Height - 100)
            {
                page = doc.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }

            gfx.DrawString("Принимал:", font, XBrushes.Black, 40, y);
            gfx.DrawLine(XPens.Black, 110, y + 10, 300, y + 10);

            gfx.DrawString("Подпись:", font, XBrushes.Black, 320, y);
            gfx.DrawLine(XPens.Black, 390, y + 10, 550, y + 10);

            // путь
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Отчет_{date:yyyy-MM-dd}.pdf");

            doc.Save(path);

            return path;
        }

        // =========================
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
    public class IncomingViewItem
    {
        public string GameTitle { get; set; }
        public string InventoryNumber { get; set; }
        public DateTime AcquiredDate { get; set; }
        public string Location { get; set; }
        public string Conditions { get; set; }
    }
}