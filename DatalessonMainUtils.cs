using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DatalessonCertificate
{
    public partial class DatalessonMain : Window
    {
        private static readonly List<string> ФайлКонфигурации = new List<string>
        {
            @"https://raw.githubusercontent.com/Ze2QvoQxxKeu/DatalessonCertificate/main/[CONFIG]",
            @"https://pastebin.com/raw/hKCZjndf"
        };
        public static readonly string СайтПрограммы = @"https://github.com/Ze2QvoQxxKeu/DatalessonCertificate";
        public static readonly string УрокЦифры = @"https://урокцифры.рф/";
        private static string UserAgent
        {
            get
            {
                Match match = new Regex(@"^([^,]+),\s*Version=([^,]+)").Match(Assembly.GetExecutingAssembly().FullName);
                if (match.Success)
                    return $"{match.Groups[1].Value}/{match.Groups[2].Value}";
                else
                    return $"DatalessonCertificate/{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
            }
        }
        private readonly List<ДанныеУрока> Уроки = new List<ДанныеУрока>();
        private static readonly Random rand = new Random();
        private static readonly int НовыйАлгоритм = 5;

        private struct ДанныеУрока
        {
            public string Название;
            public string Тренажер;
            public int Урок;
            public int[] Тренажеры;
            public string Ссылка;
            public ДанныеУрока(string[] Данные)
            {
                this.Название = Данные[0];
                this.Тренажер = Данные[1];
                this.Урок = Данные.Length == 5 ? int.Parse(Данные[2]) : 0;
                this.Тренажеры = Данные.Length == 5 ? Array.ConvertAll(Данные[3].Split(','), int.Parse) : null;
                this.Ссылка = Данные.Length == 5 ? Данные[4] : string.Empty;
            }
        }

        private string RandLoginRequest()
        {
            LoginRequest loginData = new LoginRequest
            {
                age_group = rand.Next(1, 3),
                type = new string[] { "student", "teacher", "parent" }[rand.Next(3)],
                region = RandomString(rand.Next(6, 12)),
                state = RandomString(rand.Next(6, 12)),
                vk_id = null,
                trainer = Уроки[НазваниеУрока.SelectedIndex].Тренажер,
                class_no = rand.Next(1, 11),
                school = rand.Next(1, 2000).ToString(),
                discipline = new List<string> { },
                children_ages = new List<int>
                {
                    rand.Next(7, 17)
                },
                student_info = new List<StudentInfo>
                {
                    new StudentInfo
                    {
                        age = rand.Next(7, 17),
                        sex = new string[] { "male", "female" }[rand.Next(2)]
                    }
                }
            };
            int count = rand.Next(4);
            while (count-- > 0)
                loginData.discipline.Add(RandomString(rand.Next(6, 12)));
            return JsonConvert.SerializeObject(loginData);
        }

        private static string RandomString(int length = 10)
        {
            const string chars = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            return (new CultureInfo("ru-RU", false).TextInfo).ToTitleCase(new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rand.Next(s.Length)]).ToArray()));
        }

        private class StudentInfo
        {
            public int age { get; set; }
            public string sex { get; set; }
        }

        private class LoginRequest
        {
            public int age_group { get; set; }
            public string type { get; set; }
            public string region { get; set; }
            public string state { get; set; }
            public int? vk_id { get; set; }
            public string trainer { get; set; }
            public int class_no { get; set; }
            public string school { get; set; }
            public IList<string> discipline { get; set; }
            public IList<int> children_ages { get; set; }
            public IList<StudentInfo> student_info { get; set; }
        }

        private class LoginResponseError
        {
            public string code { get; set; }
            public string descrption { get; set; }
        }

        private class LoginResponseData
        {
            public string user_id { get; set; }
        }

        private class LoginResponse
        {
            public LoginResponseError error { get; set; }
            public LoginResponseData data { get; set; }
        }

        private class SendRequestResponse
        {
            public bool status { get; set; }
            public string message { get; set; }
            public string url { get; set; }
        }

        private bool GetSaveFileName(ref string FileName)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = GetExePath();
            saveFileDialog.Filter = "PDF файлы (*.pdf)|*.pdf";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.FileName = FileName;
            if (saveFileDialog.ShowDialog() == true)
            {
                FileName = saveFileDialog.FileName;
                return true;
            }
            else
                return false;
        }

        private static string GetExePath() => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
            Path.DirectorySeparatorChar;

        private bool ParseList(string buffer, ref List<int> list)
        {
            list.Clear();
            MatchCollection matches = new Regex(@"=(\d+)>").Matches(buffer);
            foreach (Match match in matches)
            {
                int value = Convert.ToInt32(match.Groups[1].Value);
                if (value != 0)
                    list.Add(value);
            }
            return list.Count > 0;
        }

        private int RandomListValue(List<int> list) => list.Count > 0 ? list[new Random().Next(list.Count)] : 0;

        private int RandomGradeByAgeId(int age_id) => age_id == 1 ? rand.Next(5, 7) : age_id == 2 ? rand.Next(8, 11) : rand.Next(1, 4);

        public static ImageSource IconToImageSource(System.Drawing.Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                new Int32Rect(0, 0, icon.Width, icon.Height),
                BitmapSizeOptions.FromEmptyOptions());
        }

        public class WaitCursor : IDisposable
        {
            private readonly Cursor _previousCursor;
            public WaitCursor()
            {
                _previousCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Wait;
            }
            #region IDisposable Members
            public void Dispose()
            {
                Mouse.OverrideCursor = _previousCursor;
            }
            #endregion
        }
    }
}
