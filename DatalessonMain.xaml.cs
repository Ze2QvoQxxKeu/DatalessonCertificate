using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace DatalessonCertificate
{
    public partial class DatalessonMain : Window
    {
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public DatalessonMain()
        {
            InitializeComponent();
        }

        private async void Окно_Loaded(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                ИмяПользователя.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.UserName) ? Environment.UserName : Properties.Settings.Default.UserName;
                Сайт.NavigateUri = new Uri(СайтПрограммы);
                СайтПрограммыХинт.Text = СайтПрограммы;
                Значок.Source = IconToImageSource(Properties.Resources.datalesson);
                string error = string.Empty;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

                await Task.Run(() =>
                {
                    foreach (var Файл in ФайлКонфигурации)
                    {
                        Dispatcher.Invoke(() => НазваниеУрока.Items.Clear());
                        using (WebClient client = new WebClient())
                            try
                            {
                                using (StreamReader reader = new StreamReader(client.OpenRead(Файл)))
                                    while (reader.Peek() >= 0)
                                    {
                                        string[] item = reader.ReadLineAsync().Result.Split('|');
                                        if (item.Length == 2 || item.Length == 5)
                                        {
                                            Уроки.Add(new ДанныеУрока(item));
                                            Dispatcher.Invoke(() => НазваниеУрока.Items.Add(item[0]));
                                        }
                                    }
                            }
                            catch (WebException ex)
                            {
                                error = ex.Message;
                            }
                            catch (Exception ex)
                            {
                                error = ex.Message;
                            }
                        if (string.IsNullOrEmpty(error))
                            break;
                        else
                            error = string.Empty;
                    }
                });
                if (НазваниеУрока.Items.Count == 0 || !string.IsNullOrEmpty(error))
                {
                    НазваниеУрока.Items.Clear();
                    Уроки.Clear();
                    MessageBox.Show($"Ошибка загрузки конфигурации.\n{error}\nПерезапустите программу, либо попробуйте сделать это поздже.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                    НазваниеУрока.SelectedIndex = НазваниеУрока.Items.Count - 1;
            }
        }

        private async void ПолучитьСертификат_Click(object sender, RoutedEventArgs e)
        {
            ПолучитьСертификат.IsEnabled = false;
            using (new WaitCursor())
                try
                {
                    if (НазваниеУрока.SelectedIndex == -1)
                    {
                        MessageBox.Show("Список уроков не загружен.\nЗакройте программу и повторите попытку позднее.", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(ИмяПользователя.Text) && НазваниеУрока.SelectedIndex < НовыйАлгоритм)
                    {
                        MessageBox.Show("Введите имя и повторите попытку.", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    string error_text = string.Empty;
                    try
                    {
                        //Константы
                        const string MissingData = "Запрошенная страница не содержит искомых данных.\nПроверьте обновление программы.";
                        const string GettingIdError = "Ошибка получения нового идентификатора пользователя.\nПопробуйте повторить попытку позднее.";
                        const string SiteUnavailable = "Сайт \"{0}\" недоступен.\nПроверьте подключение к интернет или попробуйте повторить попытку позднее.";
                        const string SiteTemporaryUnavailable = "Сайт \"{0}\" временно недоступен.\nПопробуйте повторить попытку позднее.";

                        string user_id = string.Empty;
                        CookieContainer cookies = new CookieContainer();
                        HttpClientHandler handler = new HttpClientHandler
                        {
                            CookieContainer = cookies,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };
                        HttpClient client = new HttpClient(handler);
                        client.DefaultRequestHeaders.Add(HttpRequestHeader.UserAgent.ToString(), UserAgent);
                        HttpResponseMessage response;

                        if (НазваниеУрока.SelectedIndex < НовыйАлгоритм)
                        {
                            //Получаем новый user_id
                            response = await client.PostAsync("https://form.datalesson.ru/api/v1/auth",
                                                                    new StringContent(RandLoginRequest(), Encoding.UTF8, "application/json"));
                            if (!response.IsSuccessStatusCode)
                                throw new Exception(string.Format(SiteUnavailable, "form.datalesson.ru"));

                            //Обработка ответа
                            LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(await response.Content.ReadAsStringAsync());
                            if (loginResponse.data != null)
                                user_id = loginResponse.data.user_id;
                            else if (loginResponse.error != null)
                                throw new Exception($"{loginResponse.error.code}: {loginResponse.error.descrption}");
                            else
                                throw new Exception(GettingIdError);
                            if (new Regex(@"[a-f\d]{8}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{12}", RegexOptions.IgnoreCase).IsMatch(user_id))
                            {
                                //Загрузка сертификата
                                response = await client.GetAsync("https://form.datalesson.ru/api/v1/certificates/student/" +
                                    $"{user_id}?challenge_type={Уроки[НазваниеУрока.SelectedIndex].Тренажер}&name={ИмяПользователя.Text}");
                                if (!response.IsSuccessStatusCode)
                                    throw new Exception("Ошибка получения сертификата. Повторите попытку позднее.");
                                else
                                {
                                    string FileName = $"{Уроки[НазваниеУрока.SelectedIndex].Название}.pdf";
                                    if (GetSaveFileName(ref FileName))
                                    {
                                        using (var ms = await response.Content.ReadAsStreamAsync())
                                        using (var fs = File.Create(FileName))
                                        {
                                            ms.Seek(0, SeekOrigin.Begin);
                                            ms.CopyTo(fs);
                                        }
                                        MessageBox.Show("Сертификат успешно сохранён.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                    else
                                        MessageBox.Show("Сохранение сертификата отменено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                            }
                            else
                                throw new Exception(MissingData);
                        }
                        else
                        {
                            //Грузим главную страницу. Получаем куки, парсим данные
                            response = await client.GetAsync($"https://xn--h1adlhdnlo2c.xn--p1ai/?rnd={rand.Next()}");
                            if (!response.IsSuccessStatusCode)
                                throw new Exception(string.Format(SiteUnavailable, "урокцифры.рф"));

                            string buffer = await response.Content.ReadAsStringAsync();

                            //Переменные
                            string _token = string.Empty;
                            List<int> regions = new List<int>();

                            //Парсим токен
                            Match match = new Regex(@"name=_token\s+value=([A-z\d]{40})").Match(buffer);
                            if (match.Success)
                            {
                                _token = match.Groups[1].Value;

                                //Парсим регионы
                                int startIndex = buffer.IndexOf("<select name=id_region");
                                if (startIndex == -1)
                                    throw new Exception(MissingData);
                                int length = buffer.Substring(startIndex).IndexOf("</select>");
                                if (length == -1)
                                    throw new Exception(MissingData);
                                if (!ParseList(buffer.Substring(startIndex, length), ref regions))
                                    throw new Exception(MissingData);
                            }
                            else
                                throw new Exception(MissingData);

                            //Переменные
                            List<int> cities = new List<int>(), countries = new List<int>();
                            bool not_from_russia = Convert.ToBoolean(rand.Next(2));
                            int id_region = RandomListValue(regions), id_city = 0, id_country = 0;

                            if (not_from_russia) //Если не из рашки
                                //Парсим страны
                                response = await client.GetAsync($"https://xn--h1adlhdnlo2c.xn--p1ai/load-countries?rnd={rand.Next()}");
                            else //Из рашки
                                //Парсим города
                                response = await client.GetAsync($"https://xn--h1adlhdnlo2c.xn--p1ai/load-region-cities?rnd={rand.Next()}" +
                                                                 $"&id={id_region}&id_city=undefined");
                            if (!response.IsSuccessStatusCode)
                                throw new Exception(string.Format(SiteTemporaryUnavailable, "урокцифры.рф"));
                            if (!ParseList(await response.Content.ReadAsStringAsync(), ref countries))
                                throw new Exception(MissingData);
                            if (not_from_russia) //Если не из рашки
                                id_country = RandomListValue(countries);
                            else
                                id_city = RandomListValue(cities);

                            //Переменные
                            string type = new string[] { "pupil", "parent", "teacher" }[rand.Next(3)];
                            int age_id = rand.Next(3); //тренажёр[1-4][5-7][8-11]

                            //Получаем новый user_id
                            response = await client.PostAsync("https://xn--h1adlhdnlo2c.xn--p1ai/trainer/send-request",
                                                                new StringContent(string.Join("&", new Dictionary<string, string>
                                                                {
                                                                { "type", type
                                                                + (not_from_russia ? "&not_from_russia=on" : string.Empty) },
                                                                { "id_country", id_country.ToString() },
                                                                { "id_region", not_from_russia ? string.Empty : id_region.ToString() },
                                                                { "id_city", not_from_russia ? string.Empty : id_city.ToString() },
                                                                { "grade", type == "teacher" ? string.Empty : RandomGradeByAgeId(age_id).ToString() },
                                                                { "pass_type", "self" },
                                                                { "id_trainer", Уроки[НазваниеУрока.SelectedIndex].Тренажеры[age_id].ToString() },
                                                                { "id_lesson", Уроки[НазваниеУрока.SelectedIndex].Урок.ToString() },
                                                                { "_token", _token },
                                                                { "passType", "self" },
                                                                }.Select(x => x.Key + "=" + x.Value).ToArray()),
                                                                    Encoding.UTF8, "application/x-www-form-urlencoded"));
                            if (!response.IsSuccessStatusCode)
                                throw new Exception(GettingIdError);

                            //Обработка ответа
                            SendRequestResponse srr = JsonConvert.DeserializeObject<SendRequestResponse>(await response.Content.ReadAsStringAsync());
                            if (srr.status)
                            {
                                match = new Regex(@"[a-f\d]{8}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{12}", RegexOptions.IgnoreCase).Match(srr.url);
                                if (match.Success)
                                {
                                    user_id = match.Value;

                                    MessageBox.Show("Страница выдачи сертификата будет открыта в браузере.", "Информация",
                                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                    //Открытие страница выдачи сертификата в браузере
                                    Process.Start(Уроки[НазваниеУрока.SelectedIndex].Ссылка.Replace("{user_id}", user_id));
                                }
                            }
                        }
                    }
                    catch (WebException ex)
                    {
                        error_text = ex.Message;
                    }
                    catch (Exception ex)
                    {
                        error_text = ex.Message;
                    }
                    if (!string.IsNullOrEmpty(error_text))
                        MessageBox.Show($"Ошибка получения сертификата.\n{error_text}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ПолучитьСертификат.IsEnabled = true;
                }
        }

        private void Закрыть_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Свернуть_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ЗаголовокОкна_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Инфо_Click(object sender, RoutedEventArgs e)
        {
            DatalessonAbout aboutForm = new DatalessonAbout
            {
                Owner = this
            };
            aboutForm.ShowDialog();
        }

        private void MainDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.UserName = ИмяПользователя.Text.Trim().Equals(Environment.UserName) ? string.Empty : ИмяПользователя.Text.Trim();
            Properties.Settings.Default.Save();
        }
    }
}
