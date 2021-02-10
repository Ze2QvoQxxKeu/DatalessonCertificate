using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using SharpMik;
using SharpMik.Player;
using SharpMik.Drivers;

namespace DatalessonCertificate
{
    public partial class DatalessonAbout : Window
    {
        private SharpMik.Module m_Mod = null;
        private MikMod m_Player = null;
        private bool Mute = false;

        public DatalessonAbout()
        {
            InitializeComponent();

            m_Player = new MikMod();
        }

        ~DatalessonAbout()
        {
            ModPlayer.Player_Stop();
            ModDriver.MikMod_Exit();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void AboutForm_Loaded(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reload();
            this.Mute = Properties.Settings.Default.Mute;
            Музыка.IsChecked = !this.Mute;

            ModDriver.Mode = (ushort)(ModDriver.Mode | SharpMikCommon.DMODE_NOISEREDUCTION);
            try
            {
                m_Player.Init<NaudioDriver>("");
                m_Mod = m_Player.LoadModule(new MemoryStream(Properties.Resources.track));
                if (m_Mod != null)
                {
                    m_Mod.loop = true;
                    m_Mod.wrap = true;
                    if (!this.Mute)
                        m_Player.Play(m_Mod);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки NAudio.\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Значок.Source = DatalessonMain.IconToImageSource(Properties.Resources.datalesson);
            Match match = new Regex(@"^([^,]+),\s*Version=([^,]+)").Match(Assembly.GetExecutingAssembly().FullName);
            if (match.Success)
                Версия.Content = $"{match.Groups[1].Value} v{match.Groups[2].Value}";
            else
                Версия.Content = $"DatalessonCertificate v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
            СайтПрограммы.NavigateUri = new Uri(DatalessonMain.СайтПрограммы);
            СайтПрограммыХинт.Text = DatalessonMain.СайтПрограммы;
            УрокЦифры.NavigateUri = new Uri(DatalessonMain.УрокЦифры);
            УрокЦифрыХинт.Text = DatalessonMain.УрокЦифры;
            Копирайт.Content = $"Copyright © HEX0x29A, 2019 - {DateTime.Now.Year}";
        }

        private void ЗаголовокОкна_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ЗакрытьОкно(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Музыка_Checked(object sender, RoutedEventArgs e)
        {
            this.Mute = false;
            if (m_Mod != null)
                m_Player.Play(m_Mod);
        }

        private void Музыка_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Mute = true;
            if (m_Mod != null)
                m_Player.Stop();
        }

        private void AboutDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Mute = this.Mute;
            Properties.Settings.Default.Save();
            ModPlayer.Player_Stop();
            m_Player = null;
            m_Mod = null;
        }
    }
}
