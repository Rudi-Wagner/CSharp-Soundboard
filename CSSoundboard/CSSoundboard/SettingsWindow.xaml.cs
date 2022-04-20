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
        private readonly string projectFolder = Directory.GetCurrentDirectory();
        private string[] alreadySet = { null, null, null, null, null, null, null, null, null, null };
        private MainWindow mainWindow;
        public SettingsWindow(MainWindow main)
        {
            log += "#Settings# STARTING\n";

            mainWindow = main;
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
            RedoFilledBoxes();
        }

        private void RedoFilledBoxes()
        {
            TextBox[] boxes = { Hotkey0, Hotkey1, Hotkey2, Hotkey3, Hotkey4, Hotkey5, Hotkey6, Hotkey7, Hotkey8, Hotkey9 };

            string soundspath = projectFolder + @"\Sounds";
            DirectoryInfo directory = new DirectoryInfo(soundspath);
            FileInfo[] files = directory.GetFiles("Hotkey*.mp3");

        
            //Retrieve set hotkeys
            for (int i = 0; i < files.Length; i++)
            {
                string content = files[i].Name;
                int number = int.Parse(content.Substring(6, 1));        //Get Number of the Field
                content = content.Replace(".mp3", "");                  //Remove the filetype
                content = content.Substring(8);                         //Remove the starting argument "HotkeyX#"
                int maxLength = 13;
                if (content.Length <= 13)
                {
                    maxLength = content.Length;
                }
                content = content.Substring(0, maxLength);              //Cut the string to a max length

                alreadySet[number] = content;
            }

            
            //Place HotkeySounds in boxes
            for (int i = 0; i < boxes.Length; i++)
            {
                if (alreadySet[i] != null)
                {
                    boxes[i].Text = boxes[i].Name + Environment.NewLine + alreadySet[i];
                }
                else
                {
                    boxes[i].Text = boxes[i].Name;
                }
            }
        }

        private void FillHotkeysList()
        {
            hotkeysList.Items.Clear();
            string soundspath = projectFolder + @"\Sounds";
            DirectoryInfo directory = new DirectoryInfo(soundspath);
            FileInfo[] files = directory.GetFiles("*.mp3");
            for (int i = 0; i < files.Length; i++)                              //Create Button Loop
            {
                string content = files[i].Name;
                if (!content.StartsWith("Hotkey"))
                { 
                    hotkeysList.Items.Add(content.Replace(".mp3", ""));
                }               
            }
        }

        internal string GetComboBoxItem()
        {
            return ComboAudioDevices.SelectedItem.ToString();
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

        //Window Functions
        private void Window_Shutdown(object sender, RoutedEventArgs e)
        {//Shutdown Program
            log += "#Settings# Settings Shutdown\n";
            WriteOutput(log);
            mainWindow.RefreshData(null, null);
            this.Hide();
        }

        private void WriteOutput(string log)
        {
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("./settingslog.txt", FileMode.OpenOrCreate, FileAccess.Write);
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

        //Drag and Drop for the visual Hotkeys

        private void HotkeysList_MouseDown(object sender, MouseButtonEventArgs e)
        {//Do Drag and Drop OnClick
            if (hotkeysList.SelectedItem != null)
            {
                DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
            }
        }

        private void HotkeysList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {//Do Drag and Drop OnSelectionChanged
            DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {//Copy Filename into the selected TextBox and "Activate" the Hotkey
            try
            {
                e.Handled = true;
                TextBox box = sender as TextBox;
                box.Clear();
                string dragData = e.Data.GetData(DataFormats.StringFormat).ToString().Replace(".mp3", "");
                int maxLength = 13;
                if (dragData.Length < 13)
                {
                    maxLength = dragData.Length;
                }

                box.Text = box.Name + System.Environment.NewLine + dragData.Substring(0, maxLength);
                ActivateHotkey(box.Name, dragData);
                RedoFilledBoxes();
            }catch (FileNotFoundException)
            { 
                //Nothing
            }
        }

        private void ActivateHotkey(string hotkeyNr, string fileName)
        {//Update the Filename to set/activate the Hotkeys
            string soundspath = projectFolder + @"\Sounds";
            DirectoryInfo directory = new DirectoryInfo(soundspath);
            File.Move($"{directory}/{fileName}.mp3", $"{soundspath}/{hotkeyNr}#{fileName}.mp3");
        }

        private void DeactivateHotkey(int hotkeyNr, string fileName)
        {
            //First get the full name of the file
            string soundspath = projectFolder + @"\Sounds";
            DirectoryInfo directory = new DirectoryInfo(soundspath);
            FileInfo[] files = directory.GetFiles($"Hotkey{hotkeyNr}#{fileName}*.mp3");

            //Update the Filename to set/activate the Hotkeys
            File.Move(directory + "/" + files[0].Name, soundspath + "/" + files[0].Name.Substring(8));
        }

        private void Textbox1_KeyDown(object sender, KeyEventArgs e)
        {//Prevents writing in the textbox
            e.Handled = true;
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box.Text.Length >= 10)
            {
                int nr = int.Parse(box.Name.Substring(6, 1));
                string fileName = box.Text.Substring(9);
                DeactivateHotkey(nr, fileName);
                box.Text = box.Name;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FillHotkeysList();
            RedoFilledBoxes();
        }
    }
}