using System;
using System.Collections.Generic;
using System.Linq;

namespace UWUVCI_AIO_WPF.Models
{
    internal enum QuizResult
    {
        Incomplete,
        Failed,
        Passed
    }

    internal sealed class TutorialSession
    {
        private readonly Random random;

        public int PageIndex { get; private set; }
        public bool IsQuiz => PageIndex == TutorialContent.Pages.Count;
        public IReadOnlyList<QuizQuestion> Questions { get; private set; }

        public TutorialSession(Random random = null)
        {
            this.random = random ?? new Random(Guid.NewGuid().GetHashCode());
        }

        public void Next()
        {
            if (IsQuiz)
                return;

            PageIndex++;
            if (IsQuiz && Questions == null)
                Questions = CreateQuestions();
        }

        public void Back()
        {
            if (PageIndex > 0)
                PageIndex--;
        }

        public QuizResult Submit()
        {
            if (!IsQuiz || Questions == null || Questions.Any(q => q.Answers.Count(a => a.IsSelected) != 1))
                return QuizResult.Incomplete;

            if (Questions.All(q => q.Answers.Single(a => a.IsSelected).IsCorrect))
                return QuizResult.Passed;

            PageIndex = 0;
            Questions = null;
            return QuizResult.Failed;
        }

        private IReadOnlyList<QuizQuestion> CreateQuestions()
        {
            var questions = new List<QuizQuestion>();
            foreach (var page in TutorialContent.Pages)
            {
                var source = page.Questions[random.Next(page.Questions.Count)];
                string groupName = Guid.NewGuid().ToString("N");
                var answers = source.OtherAnswers.Select(a => new QuizAnswer(a, false, groupName)).ToList();
                answers.Add(new QuizAnswer(source.CorrectAnswer, true, groupName));
                Shuffle(answers);
                questions.Add(new QuizQuestion(page.Title, source.Prompt, answers));
            }

            Shuffle(questions);
            for (int index = 0; index < questions.Count; index++)
                questions[index].Number = index + 1;

            return questions;
        }

        private void Shuffle<T>(IList<T> items)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                T item = items[index];
                items[index] = items[other];
                items[other] = item;
            }
        }
    }

    internal sealed class QuizQuestion
    {
        public int Number { get; set; }
        public string Topic { get; }
        public string Prompt { get; }
        public string Heading => $"{Number}. {Prompt}";
        public IReadOnlyList<QuizAnswer> Answers { get; }

        public QuizQuestion(string topic, string prompt, IReadOnlyList<QuizAnswer> answers)
        {
            Topic = topic;
            Prompt = prompt;
            Answers = answers;
        }
    }

    internal sealed class QuizAnswer
    {
        public string Text { get; }
        public string GroupName { get; }
        public bool IsSelected { get; set; }
        internal bool IsCorrect { get; }

        public QuizAnswer(string text, bool isCorrect, string groupName)
        {
            Text = text;
            IsCorrect = isCorrect;
            GroupName = groupName;
        }
    }
}
