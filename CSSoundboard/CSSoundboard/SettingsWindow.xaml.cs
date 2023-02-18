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
using System.Threading.Tasks;

namespace CSSoundboard
{
    /// Interaktionslogik für SettingsWindow.xaml
    //  Rudi Wagner
    //  C# Soundboard
    public partial class SettingsWindow : Window
    {
        private string log = "";
        private string[] audioOutput = new string[WaveOut.DeviceCount];
        private string[] alreadySet = { null, null, null, null, null, null, null, null, null, null };
        private MainWindow mainWindow;
        private string projectFolder;
        private string soundspath;
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

            projectFolder = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\Rudi Wagner";
            soundspath = projectFolder + @"\Sounds";

            FillComboAudioDevices();
            FillHotkeysList();
            RedoFilledBoxes();
        }

        private void RedoFilledBoxes()
        {
            TextBox[] boxes = { Hotkey0, Hotkey1, Hotkey2, Hotkey3, Hotkey4, Hotkey5, Hotkey6, Hotkey7, Hotkey8, Hotkey9 };
            for (int i = 0; i < alreadySet.Length; i++)
            {
                alreadySet[i] = null;
            }

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
                    boxes[i].IsReadOnly = true;
                }
                else
                {
                    boxes[i].Text = boxes[i].Name;
                    boxes[i].IsReadOnly = false;
                }
            }
        }

        private void FillHotkeysList()
        {
            hotkeysList.Items.Clear();
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

        

        private String SaveMP3(string VideoURL)
        {
            String status = "";
            try
            {
                log += $"#Settings# Downloading {VideoURL}\n";
                string projectFolder = Directory.GetCurrentDirectory();
                string SaveToFolder = projectFolder + @"\Sounds";
                string source = SaveToFolder;

                var youtube = Client.For(YouTube.Default);
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
                status = "Finished";
            }
            catch (Exception e)
            {
                log += $"#Settings# A Error occured while downloading a video!\n{e}\n";
                status = "Failed";
            }
            return status;
        }

        private async void Download_Button_Click(object sender, RoutedEventArgs e)
        {
            Download_Status.Content = "Loading...";
            String url = DownloadLinkBox.Text;
            String status = await Task.Run(() => SaveMP3(url));
            Download_Status.Content = status;
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
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    this.DragMove();
                }
            }
            catch (InvalidOperationException ex)
            {
                log += ex.Message + "\n";
            }
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
            mainWindow.StopMusic(null, null);
            if (hotkeysList.SelectedItem != null)
            {
                DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
            }
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

                if (!ActivateHotkey(box.Name, dragData))
                {
                    box.Text = box.Name;
                    return;
                }
                box.Text = box.Name + Environment.NewLine + dragData.Substring(0, maxLength);
                
                RedoFilledBoxes();
                box.IsReadOnly = true;
            }catch (FileNotFoundException)
            { 
                //Nothing
            }
        }

        private bool ActivateHotkey(string hotkeyNr, string fileName)
        {//Update the Filename to set/activate the Hotkeys
            mainWindow.StopMusic(null, null);
            try {
                string soundspath = projectFolder + @"\Sounds";
                DirectoryInfo directory = new DirectoryInfo(soundspath);
                File.Move($"{directory}/{fileName}.mp3", $"{soundspath}/{hotkeyNr}#{fileName}.mp3");
                return true;
            }catch (FileNotFoundException)
            {
                return false;
            }
        }

        private void DeactivateHotkey(int hotkeyNr, string fileName)
        {
            mainWindow.StopMusic(null, null);
            //First get the full name of the file
            string soundspath = projectFolder + @"\Sounds";
            DirectoryInfo directory = new DirectoryInfo(soundspath);
            FileInfo[] files = directory.GetFiles($"Hotkey{hotkeyNr}#{fileName}*.mp3");

            //Update the Filename to set/activate the Hotkeys
            File.Move(directory + "/" + files[0].Name, soundspath + "/" + files[0].Name.Substring(8));

            FillHotkeysList();
            RedoFilledBoxes();
        }

        private void Textbox1_KeyDown(object sender, KeyEventArgs e)
        {//Prevents writing in the textbox
            e.Handled = true;
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox box = sender as TextBox;
            box.IsReadOnly = false;
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