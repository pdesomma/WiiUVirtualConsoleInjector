using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UWUVCI_AIO_WPF.Helpers;
using UWUVCI_AIO_WPF.Models;
using UWUVCI_AIO_WPF.UI.Windows;

namespace UWUVCI.RegressionTests
{
    internal static partial class Program
    {
        private static void RunTutorialUiTests()
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            try
            {
                foreach (string uri in new[]
                {
                    "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml",
                    "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml",
                    "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml",
                    "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Indigo.xaml"
                })
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uri) });

                Run("Tutorial renders every page at normal size", () => RenderTutorial(760, 650, 1));
                Run("Tutorial renders every page at minimum size and 200% output", () => RenderTutorial(560, 460, 2));
                Run("Quiz radio choices stay independent and update selections", TestRadioChoices);
                Run("Closing the tutorial does not pass", TestDialogClose);
                Run("Submitting an incomplete quiz keeps the dialog open", TestDialogIncomplete);
                Run("A correct quiz returns success without opening another app window", TestDialogPass);
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine("FAIL Tutorial UI initialization" + Environment.NewLine + ex);
            }
            finally
            {
                app.Shutdown();
            }
        }

        private static IntroductionWindow CreateWindow(double width = 760, double height = 650)
        {
            return new IntroductionWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                Width = width,
                Height = height,
                ShowActivated = false,
                ShowInTaskbar = false
            };
        }

        private static T Control<T>(IntroductionWindow window, string name) where T : FrameworkElement =>
            (T)window.FindName(name);

        private static void ClickNext(IntroductionWindow window)
        {
            Control<Button>(window, "NextButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            FlushLayout(window);
        }

        private static void FlushLayout(IntroductionWindow window)
        {
            window.UpdateLayout();
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            window.UpdateLayout();
        }

        private static void GoToQuiz(IntroductionWindow window)
        {
            for (int index = 0; index < TutorialContent.Pages.Count; index++)
                ClickNext(window);
        }

        private static void RenderTutorial(int width, int height, double scale)
        {
            var window = CreateWindow(width, height);
            try
            {
                window.Show();
                for (int page = 0; page < TutorialContent.Pages.Count; page++)
                {
                    Capture(window, $"tutorial-{width}-page-{page + 1:D2}.png", scale);
                    ClickNext(window);
                }
                Capture(window, $"tutorial-{width}-quiz-top.png", scale);
                Control<ScrollViewer>(window, "ContentScroll").ScrollToBottom();
                Capture(window, $"tutorial-{width}-quiz-bottom.png", scale);
            }
            finally { window.Close(); }
        }

        private static void Capture(IntroductionWindow window, string fileName, double scale)
        {
            FlushLayout(window);
            var root = Control<Grid>(window, "TutorialRoot");
            Rect content = Bounds(Control<ScrollViewer>(window, "ContentScroll"), root);
            Rect heading = Bounds(Control<TextBlock>(window, "PageHeading"), root);
            Rect next = Bounds(Control<Button>(window, "NextButton"), root);
            Check(heading.Bottom <= content.Top + 1, "Heading overlaps the content.");
            Check(content.Bottom <= next.Top + 1, "Content overlaps the navigation.");
            Check(next.Right <= root.ActualWidth + 1 && next.Bottom <= root.ActualHeight + 1,
                "Navigation falls outside the window.");

            foreach (var text in Descendants<TextBlock>(root).Where(t => t.IsVisible))
            {
                Rect bounds = Bounds(text, root);
                Check(bounds.Right <= root.ActualWidth + 1 && bounds.Left >= -1,
                    "Text overflows horizontally: " + text.Text);
            }

            int pixelWidth = (int)Math.Ceiling(root.ActualWidth * scale);
            int pixelHeight = (int)Math.Ceiling(root.ActualHeight * scale);
            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (DrawingContext drawing = visual.RenderOpen())
            {
                var bounds = new Rect(0, 0, root.ActualWidth, root.ActualHeight);
                drawing.DrawRectangle(Brushes.White, null, bounds);
                drawing.DrawRectangle(new VisualBrush(root), null, bounds);
            }
            bitmap.Render(visual);
            byte[] pixels = new byte[pixelWidth * pixelHeight * 4];
            bitmap.CopyPixels(pixels, pixelWidth * 4, 0);
            int nonwhite = 0;
            for (int index = 0; index < pixels.Length; index += 4)
                if (pixels[index] < 240 || pixels[index + 1] < 240 || pixels[index + 2] < 240)
                    nonwhite++;
            Check(nonwhite > 3000, "The tutorial rendered blank or nearly blank.");
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Path.Combine(artifacts, fileName)))
                encoder.Save(file);
        }

        private static Rect Bounds(FrameworkElement element, Visual root) =>
            element.TransformToAncestor(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

        private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match)
                    yield return match;
                foreach (T descendant in Descendants<T>(child))
                    yield return descendant;
            }
        }

        private static TutorialSession Session(IntroductionWindow window) =>
            (TutorialSession)typeof(IntroductionWindow).GetField("session",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);

        private static void SelectCorrectAnswers(IntroductionWindow window)
        {
            foreach (var radio in Descendants<RadioButton>(window))
                if (radio.DataContext is QuizAnswer answer && answer.IsCorrect)
                    radio.IsChecked = true;
            FlushLayout(window);
        }

        private static void TestRadioChoices()
        {
            var window = CreateWindow();
            try
            {
                window.Show();
                GoToQuiz(window);
                var radios = Descendants<RadioButton>(window).Where(r => r.DataContext is QuizAnswer).ToList();
                var first = Session(window).Questions[0];
                foreach (var choice in first.Answers.Concat(first.Answers.Reverse()))
                {
                    radios.Single(r => ReferenceEquals(r.DataContext, choice)).IsChecked = true;
                    FlushLayout(window);
                    Check(first.Answers.Count(a => a.IsSelected) == 1 && choice.IsSelected,
                        "Changing an answer did not update the bound selection.");
                }
                SelectCorrectAnswers(window);
                Check(Session(window).Questions.All(q => q.Answers.Count(a => a.IsSelected) == 1 &&
                    q.Answers.Single(a => a.IsSelected).IsCorrect), "Radio groups interfere with other questions.");
            }
            finally { window.Close(); }
        }

        private static bool? ExerciseDialog(Action<IntroductionWindow> action)
        {
            var window = CreateWindow();
            Exception error = null;
            window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                try { action(window); }
                catch (Exception ex)
                {
                    error = ex;
                    window.Close();
                }
            }));
            bool? result = window.ShowDialog();
            if (error != null)
                throw new InvalidOperationException("Dialog exercise failed.", error);
            return result;
        }

        private static void TestDialogClose()
        {
            int priorQuizScreen = JsonSettingsManager.Settings.QuizScreen;
            Check(ExerciseDialog(window => window.Close()) != true, "Closing marked the tutorial as passed.");
            Check(JsonSettingsManager.Settings.QuizScreen == priorQuizScreen, "Closing changed saved completion.");
        }

        private static void TestDialogIncomplete()
        {
            Check(ExerciseDialog(window =>
            {
                GoToQuiz(window);
                ClickNext(window);
                Check(window.IsVisible && Control<TextBlock>(window, "Feedback").Visibility == Visibility.Visible,
                    "An incomplete quiz did not show feedback and remain open.");
                window.Close();
            }) != true, "Incomplete quiz returned success.");
        }

        private static void TestDialogPass()
        {
            Check(ExerciseDialog(window =>
            {
                GoToQuiz(window);
                SelectCorrectAnswers(window);
                ClickNext(window);
            }) == true, "Correct quiz did not return dialog success.");
            Check(Application.Current.Windows.Count == 0, "The tutorial opened another application window.");
        }
    }
}
