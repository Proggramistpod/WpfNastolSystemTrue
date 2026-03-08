using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfNastolSystem.Moduls.Visual
{
    internal static class FloatingHintHelper
    {
        private const double ANIM_DURATION_MS = 200;
        private const double UP_OFFSET = -24;
        private static readonly Color COLOR_FOCUSED = Color.FromRgb(51, 153, 255);   // #3399FF
        private static readonly Color COLOR_NORMAL = Color.FromRgb(136, 136, 136);  // #888888

        public static void Attach(TextBox textBox, TextBlock hint, TranslateTransform transform)
        {
            textBox.GotFocus += (s, e) => AnimateUp(hint, transform);
            textBox.LostFocus += (s, e) => UpdateState(textBox, hint, transform);
            textBox.TextChanged += (s, e) => UpdateState(textBox, hint, transform);

            UpdateState(textBox, hint, transform);
        }

        public static void Attach(PasswordBox passwordBox, TextBlock hint, TranslateTransform transform)
        {
            passwordBox.GotFocus += (s, e) => AnimateUp(hint, transform);
            passwordBox.LostFocus += (s, e) => UpdateState(passwordBox, hint, transform);
            passwordBox.PasswordChanged += (s, e) => UpdateState(passwordBox, hint, transform);

            UpdateState(passwordBox, hint, transform);
        }
        public static void Attach(DatePicker datePicker, TextBlock hint, TranslateTransform transform)
        {
            datePicker.GotFocus += (s, e) => AnimateUp(hint, transform);
            datePicker.LostFocus += (s, e) => UpdateState(datePicker, hint, transform);
            datePicker.SelectedDateChanged += (s, e) => UpdateState(datePicker, hint, transform);
            datePicker.Loaded += (s, e) => UpdateState(datePicker, hint, transform); // Для начального состояния

            UpdateState(datePicker, hint, transform);
        }

        private static void UpdateState(DatePicker dp, TextBlock hint, TranslateTransform transform)
        {
            bool hasText = dp.SelectedDate.HasValue;
            bool isFocused = dp.IsFocused;

            if (hasText || isFocused)
                AnimateUp(hint, transform);
            else
                AnimateDown(hint, transform);
        }

        private static void UpdateState(TextBox tb, TextBlock hint, TranslateTransform transform)
        {
            bool hasText = !string.IsNullOrEmpty(tb.Text);
            bool isFocused = tb.IsFocused;

            if (hasText || isFocused)
                AnimateUp(hint, transform);
            else
                AnimateDown(hint, transform);
        }

        private static void UpdateState(PasswordBox pb, TextBlock hint, TranslateTransform transform)
        {
            bool hasText = pb.Password.Length > 0;
            bool isFocused = pb.IsFocused;

            if (hasText || isFocused)
                AnimateUp(hint, transform);
            else
                AnimateDown(hint, transform);
        }

        private static void AnimateUp(TextBlock hint, TranslateTransform transform)
        {
            AnimateY(transform, UP_OFFSET);
            hint.FontSize = 12;
            hint.Foreground = new SolidColorBrush(COLOR_FOCUSED);
        }

        private static void AnimateDown(TextBlock hint, TranslateTransform transform)
        {
            AnimateY(transform, 0);
            hint.FontSize = 14;
            hint.Foreground = new SolidColorBrush(COLOR_NORMAL);
        }

        private static void AnimateY(TranslateTransform transform, double toValue)
        {
            var anim = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(ANIM_DURATION_MS),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.YProperty, anim);
        }
    }
}
