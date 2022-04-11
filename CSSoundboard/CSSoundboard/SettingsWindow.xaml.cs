using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VideoLibrary;
using MediaToolkit.Model;
using MediaToolkit;
using System.Threading;
using System.Windows.Controls;

namespace CSSoundboard
{
    /// Interaktionslogik für SettingsWindow.xaml
    //  Rudi Wagner
    //  C# Soundboard
    public partial class SettingsWindow : Window
    {
        private string log = "";
        private string[] audioOutput = new string[WaveOut.DeviceCount];
        public SettingsWindow()
        {
            log += "#Settings# STARTING\n";
            //Initalize WindowComponents
            InitializeComponent();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();          //create enumerator
            for (int i = 0; i < WaveOut.DeviceCount; i++)       //cycle trough all audio devices
            {
                //Find correct output device
                string audioOutputName = "" + enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[i];
                log += "#Log# Used Audio Device: " + audioOutput + " ID: " + i + "\n";
                audioOutput[i] = audioOutputName;
            }

            FillComboAudioDevices();
            FillHotkeysList();
            hotkeyBox0.Text = "Hotkey 0" + System.Environment.NewLine + "Second Line";
        }

        public string GetComboBoxItem()
        {
            return ComboAudioDevices.SelectedItem.ToString();
        }

        private void FillHotkeysList()
        {
            string projectFolder = Directory.GetCurrentDirectory();
            string soundspath = projectFolder + @"\Sounds";
            DirectoryInfo directory = new DirectoryInfo(soundspath);
            FileInfo[] files = directory.GetFiles("*.mp3");
            for (int i = 0; i < files.Length; i++)                              //Create Button Loop
            {
                string content = files[i].Name;
                content = content.Remove(content.LastIndexOf(".mp3"));
                hotkeysList.Items.Add(content);
            }
        }

        private void HotkeysList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
        }

        private void TextBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }

        private void HotkeysList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
        }

        private void textbox1_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void FillComboAudioDevices()
        {
            for(int i = 0; i < audioOutput.Length; i++)
            { 
                ComboAudioDevices.Items.Add(audioOutput[i]);
                if (audioOutput[i].StartsWith("CABLE Input"))
                {
                    ComboAudioDevices.SelectedItem = audioOutput[i];
                }
            }
        }

        private void Window_Shutdown(object sender, RoutedEventArgs e)
        {//Shutdown Program
            log += "#Settings# Settings Shutdown\n";
            WriteOutput(log);
            this.Hide();
        }

        private void WriteOutput(string log)
        {
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("./log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
#pragma warning disable IDE0059 // Unnötige Zuweisung eines Werts.
                log += "Cannot open Redirect.txt for writing";
#pragma warning restore IDE0059 // Unnötige Zuweisung eines Werts.
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            Console.WriteLine(log);
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }

        private void Window_Movment(object sender, MouseButtonEventArgs e)
        {//Window movement handler
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void SaveMP3(string VideoURL)
        {
            new Thread(() =>
            {
                try
                {
                    log += $"#Settings# Downloading {VideoURL}\n";
                    string projectFolder = Directory.GetCurrentDirectory();
                    string SaveToFolder = projectFolder + @"\Sounds";
                    string source = SaveToFolder;
                    var youtube = YouTube.Default;
                    var vid = youtube.GetVideo(VideoURL);
                    string videopath = Path.Combine(source, vid.FullName);

                    File.WriteAllBytes(videopath, vid.GetBytes());

                    var inputFile = new MediaFile { Filename = Path.Combine(source, vid.FullName) };
                    var outputFile = new MediaFile { Filename = Path.Combine(source, $"{vid.FullName}.mp3") };

                    using (var engine = new Engine())
                    {
                        engine.GetMetadata(inputFile);
                        engine.Convert(inputFile, outputFile);
                    }

                    File.Delete(Path.Combine(source, vid.FullName));
                    log += $"#Settings# Saved Sound {vid.FullName}";
                    Download_Status.Content = "Finished";
                }
                catch (Exception e)
                {
                    log += $"#Settings# A Error occured while downloading a video!\n{e}\n";
                    Download_Status.Content = "Failed!";
                }
            }).Start();
        }

        private void Download_Button_Click(object sender, RoutedEventArgs e)
        {
            Download_Status.Content = "Starting..";
            SaveMP3(DownloadLinkBox.Text);
            DownloadLinkBox.Text = "Link..";
        }
    }
}