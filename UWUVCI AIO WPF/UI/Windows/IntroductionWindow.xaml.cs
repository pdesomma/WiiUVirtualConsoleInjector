using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using UWUVCI_AIO_WPF.Models;

namespace UWUVCI_AIO_WPF.UI.Windows
{
    public partial class IntroductionWindow : Window
    {
        private readonly TutorialSession session = new TutorialSession();

        public IntroductionWindow()
        {
            InitializeComponent();
            MinWidth = Math.Min(MinWidth, SystemParameters.WorkArea.Width);
            MinHeight = Math.Min(MinHeight, SystemParameters.WorkArea.Height);
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
            ShowCurrentPage();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (!session.IsQuiz)
            {
                session.Next();
                ShowCurrentPage();
                return;
            }

            switch (session.Submit())
            {
                case QuizResult.Incomplete:
                    Feedback.Text = "Answer every question before submitting the quiz.";
                    Feedback.Visibility = Visibility.Visible;
                    break;
                case QuizResult.Failed:
                    ShowCurrentPage();
                    MessageBox.Show(this,
                        "At least one answer was incorrect. Review the information from the first page and try again. The next attempt will select and shuffle the questions and answers again.",
                        "Quiz Not Passed", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case QuizResult.Passed:
                    DialogResult = true;
                    break;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            session.Back();
            ShowCurrentPage();
        }

        private void ShowCurrentPage()
        {
            bool isQuiz = session.IsQuiz;
            InformationPanel.Visibility = isQuiz ? Visibility.Collapsed : Visibility.Visible;
            QuizPanel.Visibility = isQuiz ? Visibility.Visible : Visibility.Collapsed;
            Feedback.Visibility = Visibility.Collapsed;
            BackButton.IsEnabled = session.PageIndex > 0;
            NextButton.Content = isQuiz ? "Submit Quiz" :
                session.PageIndex == TutorialContent.Pages.Count - 1 ? "Start Quiz" : "Next";
            PageProgress.Maximum = TutorialContent.Pages.Count + 1;
            PageProgress.Value = session.PageIndex + 1;

            if (isQuiz)
            {
                PageHeading.Text = "Knowledge Check";
                StepLabel.Text = $"{session.Questions.Count} questions";
                QuizQuestions.ItemsSource = session.Questions;
            }
            else
            {
                var page = TutorialContent.Pages[session.PageIndex];
                PageHeading.Text = page.Title;
                StepLabel.Text = $"Topic {session.PageIndex + 1} of {TutorialContent.Pages.Count}";
                Paragraphs.ItemsSource = page.Paragraphs;
                ReferenceLinks.ItemsSource = page.Links;
                if (session.Questions == null)
                    QuizQuestions.ItemsSource = null;
            }

            ContentScroll.ScrollToTop();
        }

        private void ReferenceLink_Click(object sender, RoutedEventArgs e)
        {
            string target = ((Hyperlink)sender).Tag as string;
            try
            {
                if (target == "readme")
                {
                    target = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Readme.txt");
                    if (!File.Exists(target))
                        throw new FileNotFoundException("Readme.txt was not found next to the UWUVCI application.");
                }

                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to Open Reference",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
