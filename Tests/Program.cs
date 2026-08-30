using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UWUVCI_AIO_WPF;
using UWUVCI_AIO_WPF.Classes;
using UWUVCI_AIO_WPF.Models;

namespace UWUVCI.RegressionTests
{
    internal static partial class Program
    {
        private static int passed;
        private static int failed;
        private static string artifacts;
        private const int LayoutOffset = 0xA0;
        private const int FirstSection = LayoutOffset + 0x14;
        private const int FrameOffset = FirstSection + 0x1C + 0x60;
        private const int MaskOffset = FrameOffset + 0x80;

        [STAThread]
        private static int Main(string[] args)
        {
            artifacts = Path.GetFullPath(args.Length == 0 ?
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "artifacts") : args[0]);
            Directory.CreateDirectory(artifacts);
            string originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(artifacts);
            try
            {
                foreach (bool wide in new[] { false, true })
                    foreach (bool dark in new[] { false, true })
                        Run($"N64 widescreen={wide}, remove dark filter={dark}", () => TestN64(wide, dark));
                Run("N64 malformed layouts fail without partial patches", TestMalformedLayouts);
                Run("Quiz selects every topic and shuffles questions and answers", TestQuizRandomization);
                Run("Incomplete quiz cannot pass", TestIncompleteQuiz);
                Run("Incorrect quiz returns to page one and clears the attempt", TestQuizFailure);
                Run("Failed retries reselect and reshuffle questions and answers", TestQuizRetryRandomization);
                Run("Correct quiz passes", TestQuizPass);
                Run("Back navigation preserves an unfinished attempt", TestQuizBack);
                Run("QuizScreen replaces legacy flags and completion round-trips", TestQuizSettings);
                Run("Bundled FAQ includes retirement guidance and stable references without video links", TestBundledFaq);
                Run("NDS brightness leaves RenderScale at 1", () => TestNdsJson(95, 0));
                Run("NDS pixel-art scaling leaves RenderScale at 1", () => TestNdsJson(80, 2));
                Run("NDS combined display changes leave RenderScale at 1", () => TestNdsJson(90, 3));
                Run("NDS defaults leave RenderScale at 1", () => TestNdsJson(80, 0));
                RunTutorialUiTests();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
            Console.WriteLine($"{passed} passed; {failed} failed. Artifacts: {artifacts}");
            return failed == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine("FAIL " + name + Environment.NewLine + ex);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void TestN64(bool wide, bool dark)
        {
            byte[] original = MakeFrameLayout();
            byte[] actual = (byte[])original.Clone();
            N64FrameLayoutPatcher.Apply(actual, wide, dark);
            if (!wide && !dark)
            {
                Check(actual.SequenceEqual(original), "Default settings must not modify the archive.");
                return;
            }

            Check(Hex(actual, FrameOffset + 0x2C) == "00-00-00-00", "Translation must be cleared.");
            Check(Hex(actual, FrameOffset + 0x30) == "00-00-00-00", "X translation must be cleared.");
            Check(Hex(actual, FrameOffset + 0x44) == "3F-80-00-00", "X scale must be a big-endian float 1.");
            Check(Hex(actual, FrameOffset + 0x48) == "3F-80-00-00", "Y scale must be a big-endian float 1.");
            Check(Hex(actual, FrameOffset + 0x4C) == (wide ? "44-F0-00-00" : "44-B4-00-00"),
                "The requested aspect ratio was not written.");
            Check(actual[MaskOffset + 8] == (dark ? 0 : 1), "Dark-filter visibility is wrong.");

            var changedRanges = new HashSet<int>();
            foreach (int offset in new[] { 0x2C, 0x30, 0x44, 0x48, 0x4C })
                foreach (int index in Enumerable.Range(FrameOffset + offset, 4))
                    changedRanges.Add(index);
            changedRanges.Add(MaskOffset + 8);
            Check(Enumerable.Range(0, actual.Length).All(i => changedRanges.Contains(i) || actual[i] == original[i]),
                "The patch modified bytes outside the frame and filter fields.");
        }

        private static void TestMalformedLayouts()
        {
            var damaged = new List<byte[]> { MakeFrameLayout().Take(32).ToArray() };
            foreach (Action<byte[]> corrupt in new Action<byte[]>[]
            {
                bytes => bytes[0] = 0,
                bytes => Put32(bytes, 0x0C, 0x7FFFFFFF),
                bytes => Put16(bytes, LayoutOffset + 6, 4),
                bytes => Put32(bytes, LayoutOffset + 0x0C, 0x7FFFFFFF),
                bytes => Put32(bytes, FirstSection + 4, 0),
                bytes => Put32(bytes, FirstSection + 4, 0x7FFFFFFF),
                bytes => WriteName(bytes, MaskOffset, "missing_mask"),
                bytes => WriteTag(bytes, FrameOffset, "pan1")
            })
            {
                byte[] bytes = MakeFrameLayout();
                corrupt(bytes);
                damaged.Add(bytes);
            }

            foreach (byte[] bytes in damaged)
            {
                byte[] before = (byte[])bytes.Clone();
                bool rejected = false;
                try { N64FrameLayoutPatcher.Apply(bytes, true, true); }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected, "An invalid archive was accepted.");
                Check(bytes.SequenceEqual(before), "A failed patch changed the input.");
            }
        }

        private static TutorialSession EnterQuiz(TutorialSession session)
        {
            while (!session.IsQuiz)
                session.Next();
            return session;
        }

        private static void AnswerCorrectly(TutorialSession session)
        {
            foreach (var question in session.Questions)
                question.Answers.Single(a => a.IsCorrect).IsSelected = true;
        }

        private static void TestQuizRandomization()
        {
            var prompts = new HashSet<string>();
            var firstTopics = new HashSet<string>();
            var correctPositions = new HashSet<int>();
            for (int seed = 0; seed < 128; seed++)
            {
                var session = EnterQuiz(new TutorialSession(new Random(seed)));
                Check(session.Questions.Count == TutorialContent.Pages.Count, "A topic was omitted.");
                Check(session.Questions.Select(q => q.Topic).Distinct().Count() == TutorialContent.Pages.Count,
                    "A topic was repeated.");
                firstTopics.Add(session.Questions[0].Topic);
                foreach (var question in session.Questions)
                {
                    var page = TutorialContent.Pages.Single(p => p.Title == question.Topic);
                    Check(page.Questions.Any(q => q.Prompt == question.Prompt), "Question is not taught by its page.");
                    Check(question.Answers.Count == 4 && question.Answers.Count(a => a.IsCorrect) == 1,
                        "Each question needs four answers with one correct answer.");
                    Check(question.Answers.Select(a => a.Text).Distinct().Count() == 4, "An answer was repeated.");
                    Check(question.Answers.All(a => !a.IsSelected), "A new question has a preselected answer.");
                    prompts.Add(question.Prompt);
                    correctPositions.Add(question.Answers.ToList().FindIndex(a => a.IsCorrect));
                }
            }
            Check(prompts.Count == TutorialContent.Pages.Sum(p => p.Questions.Count), "Question selection did not vary.");
            Check(firstTopics.Count == TutorialContent.Pages.Count, "Question order did not vary across topics.");
            Check(correctPositions.Count == 4, "Correct answers did not appear in every answer position.");
        }

        private static void TestIncompleteQuiz()
        {
            var session = new TutorialSession(new Random(1));
            Check(session.Submit() == QuizResult.Incomplete, "A quiz passed before reading any pages.");
            session.Back();
            Check(session.PageIndex == 0, "Back navigation went before the first page.");
            EnterQuiz(session);
            Check(session.Submit() == QuizResult.Incomplete && session.IsQuiz, "An empty quiz was accepted.");
            session.Questions[0].Answers.Single(a => a.IsCorrect).IsSelected = true;
            Check(session.Submit() == QuizResult.Incomplete && session.IsQuiz, "A partially answered quiz was accepted.");
        }

        private static void TestQuizFailure()
        {
            var session = EnterQuiz(new TutorialSession(new Random(2)));
            var oldQuestions = session.Questions;
            AnswerCorrectly(session);
            oldQuestions[0].Answers.Single(a => a.IsCorrect).IsSelected = false;
            oldQuestions[0].Answers.First(a => !a.IsCorrect).IsSelected = true;
            Check(session.Submit() == QuizResult.Failed, "An incorrect answer was accepted.");
            Check(session.PageIndex == 0 && session.Questions == null, "Failure did not reset the tutorial.");
            EnterQuiz(session);
            Check(!ReferenceEquals(oldQuestions, session.Questions), "Retry reused the old question objects.");
            Check(session.Questions.All(q => q.Answers.All(a => !a.IsSelected)), "Retry kept old selections.");
        }

        private static void TestQuizPass()
        {
            var session = EnterQuiz(new TutorialSession(new Random(3)));
            AnswerCorrectly(session);
            Check(session.Submit() == QuizResult.Passed, "A fully correct attempt did not pass.");
        }

        private static void TestQuizRetryRandomization()
        {
            var session = new TutorialSession(new Random(5));
            var questionSets = new HashSet<string>();
            var topicOrders = new HashSet<string>();
            var answerPositions = new HashSet<string>();
            IReadOnlyList<QuizQuestion> previousQuestions = null;
            string previousFingerprint = null;
            for (int attempt = 0; attempt <= 128; attempt++)
            {
                EnterQuiz(session);
                Check(!ReferenceEquals(previousQuestions, session.Questions), "Retry reused the old attempt.");
                Check(session.Questions.All(q => q.Answers.All(a => !a.IsSelected)), "Retry kept old selections.");
                var byTopic = session.Questions.OrderBy(q => q.Topic).ToList();
                questionSets.Add(string.Join("|", byTopic.Select(q => q.Prompt)));
                topicOrders.Add(string.Join("|", session.Questions.Select(q => q.Topic)));
                answerPositions.Add(string.Join("|", byTopic.Select(q => q.Answers.ToList().FindIndex(a => a.IsCorrect))));
                string fingerprint = string.Join("\n", session.Questions.Select(q =>
                    q.Prompt + "\n" + string.Join("\n", q.Answers.Select(a => a.Text))));
                Check(fingerprint != previousFingerprint, "Retry repeated the exact same quiz and answer order.");
                if (previousQuestions != null)
                {
                    var oldGroups = new HashSet<string>(previousQuestions.Select(q => q.Answers[0].GroupName));
                    Check(session.Questions.All(q => !oldGroups.Contains(q.Answers[0].GroupName)),
                        "Retry reused a previous radio-button group.");
                }
                previousQuestions = session.Questions;
                previousFingerprint = fingerprint;
                AnswerCorrectly(session);
                session.Questions[0].Answers.Single(a => a.IsCorrect).IsSelected = false;
                session.Questions[0].Answers.First(a => !a.IsCorrect).IsSelected = true;
                Check(session.Submit() == QuizResult.Failed && session.PageIndex == 0 && session.Questions == null,
                    "A failed retry did not return to the first page and discard its questions.");
            }
            Check(questionSets.Count > 1, "Retries did not select new questions.");
            Check(topicOrders.Count > 1, "Retries did not shuffle question order.");
            Check(answerPositions.Count > 1, "Retries did not shuffle correct answer positions.");
        }

        private static void TestQuizBack()
        {
            var session = EnterQuiz(new TutorialSession(new Random(4)));
            var questions = session.Questions;
            questions[0].Answers[0].IsSelected = true;
            session.Back();
            Check(!session.IsQuiz, "Back did not leave the quiz.");
            session.Next();
            Check(ReferenceEquals(questions, session.Questions) && questions[0].Answers[0].IsSelected,
                "Reviewing a page unexpectedly replaced the current attempt.");
        }

        private static void TestQuizSettings()
        {
            Check(new JsonAppSettings().QuizScreen == 0, "New settings must require the quiz.");
            var settings = JsonConvert.DeserializeObject<JsonAppSettings>(
                "{\"IsFirstLaunch\":false,\"CompletedTutorialVersion\":1,\"ShowZestyFork\":false,\"BasePath\":\"original-base\"}");
            Check(settings.QuizScreen == 0,
                "An old tutorial-completed setting bypasses the new quiz.");
            settings.QuizScreen = 1;
            var saved = JObject.FromObject(settings);
            Check(saved["QuizScreen"].Type == JTokenType.Integer && (int)saved["QuizScreen"] == 1,
                "Quiz completion was not saved as numeric QuizScreen=1.");
            Check(saved["IsFirstLaunch"] == null && saved["CompletedTutorialVersion"] == null &&
                saved["ShowZestyFork"] == null, "Saving settings retained obsolete tutorial or promotion flags.");
            var restored = saved.ToObject<JsonAppSettings>();
            Check(restored.QuizScreen == 1 && restored.BasePath == "original-base",
                "Saving quiz completion changed unrelated settings.");
            restored.QuizScreen = 0;
            var reset = JsonConvert.DeserializeObject<JsonAppSettings>(JsonConvert.SerializeObject(restored));
            Check(reset.QuizScreen == 0 && reset.BasePath == "original-base",
                "Resetting QuizScreen did not persist or changed unrelated settings.");
        }

        private static void TestBundledFaq()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Readme.txt");
            Check(File.Exists(path), "The FAQ was not copied beside the built application.");
            string faq = File.ReadAllText(path);
            Check(Regex.IsMatch(faq, @"(?i)V3\.N2[^\r\n]*final[^\r\n]*retirement"),
                "The FAQ does not identify V3.N2 as the final retirement update.");
            Check(!Regex.IsMatch(faq,
                @"(?i)https?://[^\s]*(?:youtube\.com|youtu\.be|vimeo\.com|dailymotion\.com)|official[^\r\n]*video"),
                "The FAQ still links to or promotes video guides.");
            Check(!Regex.IsMatch(faq,
                @"(?i)\b(?:FAQ|ReadMe(?:\.txt)?)[ \t]*(?:(?:entry|question|item)[ \t]*)?[:# \t]*\d+\b"),
                "The FAQ refers to a movable entry number.");
            var topics = new HashSet<string>(Regex.Matches(faq, @"(?m)^\d+\)[ \t]+([^\r\n]+)")
                .Cast<Match>().Select(match => match.Groups[1].Value), StringComparer.Ordinal);
            foreach (string topic in new[] { "Image format or bit-depth errors", "Missing pre.iso", "Missing temp/temp folder" })
                Check(topics.Contains(topic), "An injection error's FAQ topic is missing: " + topic);
            foreach (Match reference in Regex.Matches(faq, @"(?i)\bsee ""([^""]+)"""))
                Check(topics.Contains(reference.Groups[1].Value), "An FAQ topic reference is broken: " + reference.Value);
        }

        private static void TestNdsJson(int brightness, int pixelArt)
        {
            // Avoid the real view-model constructor: it performs setup, downloads, and settings writes.
            var viewModel = (MainViewModel)FormatterServices.GetUninitializedObject(typeof(MainViewModel));
            viewModel.Brightness = brightness;
            viewModel.PixelArtUpscaler = pixelArt;
            typeof(Injection).GetField("mvvm", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, viewModel);
            string path = Path.Combine(artifacts, "bin", "temp", "baserom", "content", "0010", "configuration_cafe.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var before = JObject.Parse("{\"configuration\":{\"3DRendering\":{\"RenderScale\":1,\"Other3D\":true}," +
                "\"Display\":{\"Brightness\":80,\"PixelArtUpscaler\":0,\"Keep\":\"original\"}},\"Untouched\":[1,2,3]}");
            File.WriteAllText(path, before.ToString());
            typeof(Injection).GetMethod("UpdateConfigurationCafeJson", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, null);
            var after = JObject.Parse(File.ReadAllText(path));
            Check(JToken.DeepEquals(before["configuration"]["3DRendering"], after["configuration"]["3DRendering"]),
                "NDS display changes modified rendering settings.");
            Check((int)after["configuration"]["3DRendering"]["RenderScale"] == 1, "RenderScale is not 1.");
            Check((int)after["configuration"]["Display"]["Brightness"] == brightness, "Brightness was not updated.");
            Check((int)after["configuration"]["Display"]["PixelArtUpscaler"] == pixelArt, "Pixel-art scaling was not updated.");
            Check((string)after["configuration"]["Display"]["Keep"] == "original" &&
                JToken.DeepEquals(before["Untouched"], after["Untouched"]), "Unrelated JSON was changed.");
        }

        private static byte[] MakeFrameLayout()
        {
            byte[] bytes = Enumerable.Repeat((byte)0xCC, 0x500).ToArray();
            WriteTag(bytes, 0, "SARC");
            Put16(bytes, 4, 0x14);
            Put16(bytes, 6, 0xFEFF);
            Put32(bytes, 8, bytes.Length);
            Put32(bytes, 0x0C, 0x80);
            WriteTag(bytes, 0x14, "SFAT");
            Put16(bytes, 0x18, 0x0C);
            Put16(bytes, 0x1A, 2);
            Put32(bytes, 0x28, 0);
            Put32(bytes, 0x2C, 0x10);
            Put32(bytes, 0x38, 0x20);
            Put32(bytes, 0x3C, 0x230);
            WriteTag(bytes, LayoutOffset, "FLYT");
            Put16(bytes, LayoutOffset + 4, 0xFEFF);
            Put16(bytes, LayoutOffset + 6, 0x14);
            Put32(bytes, LayoutOffset + 0x0C, 0x210);
            Put16(bytes, LayoutOffset + 0x10, 5);
            WriteTag(bytes, FirstSection, "lyt1");
            Put32(bytes, FirstSection + 4, 0x1C);
            WriteTag(bytes, FirstSection + 0x1C, "pan1");
            Put32(bytes, FirstSection + 0x20, 0x60);
            WriteName(bytes, FirstSection + 0x1C, "frame");
            foreach (var pane in new[]
            {
                new { Offset = FrameOffset, Name = "frame" },
                new { Offset = MaskOffset, Name = "frame_mask" },
                new { Offset = MaskOffset + 0x80, Name = "power_save_bg" }
            })
            {
                WriteTag(bytes, pane.Offset, "pic1");
                Put32(bytes, pane.Offset + 4, 0x80);
                bytes[pane.Offset + 8] = 1;
                WriteName(bytes, pane.Offset, pane.Name);
            }
            return bytes;
        }

        private static string Hex(byte[] bytes, int offset) => BitConverter.ToString(bytes, offset, 4);
        private static void WriteTag(byte[] bytes, int offset, string tag) =>
            Encoding.ASCII.GetBytes(tag).CopyTo(bytes, offset);

        private static void WriteName(byte[] bytes, int paneOffset, string name)
        {
            Array.Clear(bytes, paneOffset + 0x0C, 0x18);
            WriteTag(bytes, paneOffset + 0x0C, name);
        }

        private static void Put16(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)(value >> 8);
            bytes[offset + 1] = (byte)value;
        }

        private static void Put32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }
    }
}
