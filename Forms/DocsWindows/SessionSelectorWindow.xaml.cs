using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Style;

namespace WpfNastolSystem.Windows
{
    public partial class SessionSelectorWindow : Window
    {
        private readonly DbManager _db = new();

        public SessionSelectorWindow()
        {
            GlobalFontSettings.FontResolver = new WindowsFontResolver(); 
            InitializeComponent();
            LoadCompletedUnpaidSessions();
        }
        private void LoadCompletedUnpaidSessions()
        {
            try
            {
                string query = @"
            SELECT 
                s.session_id,
                s.started_at,
                s.ended_at,
                s.cost,
                t.table_number,
                p.full_name AS organizer_name,
                COALESCE(GROUP_CONCAT(DISTINCT g.title SEPARATOR ', '), '—') AS game_titles
            FROM sessions s
            INNER JOIN tables t ON s.table_id = t.table_id
            LEFT JOIN persons p ON s.organizer_id = p.person_id
            LEFT JOIN session_games sg ON sg.session_id = s.session_id
            LEFT JOIN game_copies gc ON gc.copy_id = sg.copy_id
            LEFT JOIN games g ON g.game_id = gc.game_id
            WHERE s.ended_at IS NOT NULL 
              AND (s.paid = 0 OR s.paid IS NULL)
            GROUP BY s.session_id
            ORDER BY s.ended_at DESC
            LIMIT 300";

                DataTable dt = _db.Select(query);
                dgSessions.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Нет завершённых сессий, ожидающих оплаты.", "Информация");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить сессии:\n{ex.Message}", "Ошибка");
            }
        }

        private void BtnCreateCheck_Click(object sender, RoutedEventArgs e)
        {
            if (dgSessions.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите сессию для создания чека.");
                return;
            }

            CreateCheckFromRow(row);
        }

        private void dgSessions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgSessions.SelectedItem is DataRowView row)
            {
                CreateCheckFromRow(row);
            }
        }

        private void CreateCheckFromRow(DataRowView row)
        {
            int sessionId = Convert.ToInt32(row["session_id"]);

            // Диалог выбора способа оплаты
            var paymentDialog = new Window
            {
                Title = "Способ оплаты",
                Width = 360,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var sp = new StackPanel { Margin = new Thickness(20) };
            var cb = new ComboBox { Width = 280, SelectedIndex = 0 };
            cb.Items.Add(new ComboBoxItem { Content = "Наличные", Tag = "cash" });
            cb.Items.Add(new ComboBoxItem { Content = "Банковская карта", Tag = "card" });
            cb.Items.Add(new ComboBoxItem { Content = "Счёт / Invoice", Tag = "invoice" });

            var btnConfirm = new Button
            {
                Content = "Создать чек",
                Width = 180,
                Margin = new Thickness(0, 16, 0, 0)
            };

            string selectedMethod = "cash";

            cb.SelectionChanged += (s, ev) =>
            {
                if (cb.SelectedItem is ComboBoxItem item)
                    selectedMethod = item.Tag?.ToString() ?? "cash";
            };

            btnConfirm.Click += (s, ev) =>
            {
                GenerateCheckPdfAndUpdateSession(sessionId, row, selectedMethod);
                paymentDialog.Close();
            };

            sp.Children.Add(new TextBlock { Text = "Выберите способ оплаты:", Margin = new Thickness(0, 0, 0, 8) });
            sp.Children.Add(cb);
            sp.Children.Add(btnConfirm);

            paymentDialog.Content = sp;
            paymentDialog.ShowDialog();
        }

        private void GenerateCheckPdfAndUpdateSession(int sessionId, DataRowView row, string paymentMethod)
        {
            try
            {
                // Данные из строки – game_titles теперь гарантированно не NULL
                string gameTitles = row["game_titles"].ToString(); // всегда строка благодаря COALESCE
                string tableNumber = row["table_number"]?.ToString() ?? "—";
                string organizer = row["organizer_name"]?.ToString() ?? "—";
                DateTime start = Convert.ToDateTime(row["started_at"]);
                DateTime end = Convert.ToDateTime(row["ended_at"]);
                decimal cost = row["cost"] != DBNull.Value ? Convert.ToDecimal(row["cost"]) : 0m;

                double hours = (end - start).TotalHours;
                string pdfPath = CreateSimpleCheckPdf(sessionId, gameTitles, tableNumber, organizer, start, end, hours, cost, paymentMethod);

                // Обновление сессии (без изменений)
                var pars = new Dictionary<string, object>
        {
            { "@session_id",      sessionId },
            { "@paid",            1 },
            { "@payment_method",  paymentMethod },
            { "@notes_add",       $"Чек PDF создан {DateTime.Now:dd.MM.yyyy HH:mm} | {Path.GetFileName(pdfPath)}" }
        };

                string updateSql = @"
            UPDATE sessions SET
                paid = @paid,
                payment_method = @payment_method,
                notes = CONCAT(IFNULL(notes, ''), '\n', @notes_add)
            WHERE session_id = @session_id";

                _db.NonQuery(updateSql, pars);

                MessageBox.Show($"Чек успешно создан и сохранён:\n{pdfPath}\n\nСессия отмечена как оплаченная.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                try { Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true }); } catch { }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании чека:\n{ex.Message}", "Ошибка");
            }
        }

        private string CreateSimpleCheckPdf(int sessionId, string gameTitles, string table, string organizer,
                                          DateTime start, DateTime end, double hours, decimal amount, string payment)
        {
            var document = new PdfDocument();
            document.Info.Title = $"Чек сессия №{sessionId}";
            document.Info.Creator = "Настольный клуб";

            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A5;
            page.Orientation = PdfSharp.PageOrientation.Portrait;

            var gfx = XGraphics.FromPdfPage(page);

            var fontHeader = new XFont("Arial", 14);
            var fontBold = new XFont("Arial", 12);
            var fontNormal = new XFont("Arial", 11);
            var fontSmall = new XFont("Arial", 9);

            int y = 50;

            // Шапка
            gfx.DrawString("НАСТОЛЬНЫЙ КЛУБ", fontHeader, XBrushes.DarkBlue, new XRect(0, y, page.Width, 20), XStringFormats.TopCenter);
            y += 35;
            gfx.DrawString($"ЧЕК №{sessionId}", fontBold, XBrushes.Black, new XRect(0, y, page.Width, 20), XStringFormats.TopCenter);
            y += 30;

            gfx.DrawString($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}", fontNormal, XBrushes.Black, 40, y); y += 20;

            gfx.DrawString($"Игра: {gameTitles}", fontNormal, XBrushes.Black, 40, y); y += 18;
            gfx.DrawString($"Стол № {table}", fontNormal, XBrushes.Black, 40, y); y += 18;
            gfx.DrawString($"Организатор: {organizer}", fontNormal, XBrushes.Black, 40, y); y += 18;
            gfx.DrawString($"Время: {start:dd.MM.yyyy HH:mm} – {end:HH:mm}", fontNormal, XBrushes.Black, 40, y); y += 18;
            gfx.DrawString($"Длительность: {hours:F1} ч", fontNormal, XBrushes.Black, 40, y); y += 25;

            gfx.DrawLine(XPens.Gray, 35, y, page.Width - 35, y); y += 20;

            // Итог
            gfx.DrawString("ИТОГО К ОПЛАТЕ:", fontBold, XBrushes.Black, 40, y);
            gfx.DrawString($"{amount:N0} ₽", fontBold, XBrushes.Black, page.Width - 80, y, XStringFormats.TopRight);
            y += 30;

            gfx.DrawString($"Оплата: {GetPaymentDisplayName(payment)}", fontNormal, XBrushes.Black, 40, y);

            // Подвал
            y = (int)page.Height - 70;
            gfx.DrawString("Спасибо за посещение! Ждём Вас снова!", fontSmall, XBrushes.Gray, new XRect(0, y, page.Width, 0), XStringFormats.TopCenter);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filename = $"Чек_сессия_{sessionId}_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
            string fullPath = Path.Combine(desktop, filename);

            document.Save(fullPath);
            return fullPath;
        }

        private string GetPaymentDisplayName(string tag)
        {
            return tag switch
            {
                "cash" => "Наличные",
                "card" => "Банковская карта",
                "invoice" => "Счёт / Invoice",
                _ => tag
            };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}