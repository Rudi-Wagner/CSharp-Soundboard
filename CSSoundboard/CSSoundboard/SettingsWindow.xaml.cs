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

        /// <summary>
        /// Initializes a new instance of the SettingsWindow class.
        /// </summary>
        /// <param name="main">The MainWindow instance to associate with this SettingsWindow instance.</param>
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

        /// <summary>
        /// Redoes the filled text boxes based on the files found in the sound path directory.
        /// </summary>
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

        /// <summary>
        /// Populates the `hotkeysList` ListBox with the names of the available sound files.
        /// </summary>
        /// <remarks>
        /// This method clears the `hotkeysList` ListBox, then retrieves the names of all sound files with the ".mp3" extension in the "soundspath" directory, and adds them to the ListBox.
        /// The ListBox items are created by replacing the ".mp3" extension of each file name with an empty string.
        /// </remarks>
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

        /// <summary>
        /// Returns the selected item of the `ComboAudioDevices` combo box as a string.
        /// </summary>
        /// <returns>A string representing the selected item of the `ComboAudioDevices` combo box.</returns>
        internal string GetComboBoxItem()
        {
            return ComboAudioDevices.SelectedItem.ToString();
        }

        /// <summary>
        /// Populates the `ComboAudioDevices` combo box with the available audio output devices.
        /// </summary>
        /// <remarks>
        /// This method iterates through the `audioOutput` array, which contains the available audio output devices, and adds each device to the `ComboAudioDevices` combo box.
        /// It also sets the selected item of the combo box to the first device whose name starts with "CABLE Input".
        /// </remarks>
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

        /// <summary>
        /// Downloads a video from the specified URL, converts it to an MP3 file, and saves it to disk.
        /// </summary>
        /// <param name="VideoURL">The URL of the video to download and convert.</param>
        /// <returns>A string indicating whether the operation was successful or failed.</returns>
        /// <remarks>
        /// This method first logs the download of the specified video to the `log` variable.
        /// It then retrieves the current project folder, creates a `Sounds` subdirectory within that folder, and sets `SaveToFolder` to that subdirectory.
        /// The method then retrieves the specified video using the `YouTubeExplode` library, and saves it to disk in `SaveToFolder`.
        /// The video file is then converted to an MP3 file using the `MediaToolkit` library, and saved to disk.
        /// Finally, the original video file is deleted from disk, and a status message indicating success or failure is returned.
        /// </remarks>
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

        /// <summary>
        /// Downloads an MP3 file from the specified URL and updates the download status label.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method retrieves the download URL from the `DownloadLinkBox` text box, creates a new `Task` using the `SaveMP3` method with the URL as a parameter, 
        /// and awaits its completion. Once the `Task` has completed, it updates the download status label with the resulting status message.
        /// </remarks>
        private async void Download_Button_Click(object sender, RoutedEventArgs e)
        {
            Download_Status.Content = "Loading...";
            String url = DownloadLinkBox.Text;
            String status = await Task.Run(() => SaveMP3(url));
            Download_Status.Content = status;
            DownloadLinkBox.Text = "Link..";
        }

        //Window Functions
        /// <summary>
        /// Handles the shutdown of the window by logging the event and refreshing data in the main window.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method first logs the shutdown event to the `log` variable, then calls the `WriteOutput` method to write the log message to a file.
        /// It then calls the `RefreshData` method on the `mainWindow` object with `null` arguments to update the main window's data.
        /// Finally, it hides the current window.
        /// </remarks>
        private void Window_Shutdown(object sender, RoutedEventArgs e)
        {
            log += "#Settings# Settings Shutdown\n";
            WriteOutput(log);
            mainWindow.RefreshData(null, null);
            this.Hide();
        }

        /// <summary>
        /// Writes the specified log message to a file at the path "./settingslog.txt".
        /// </summary>
        /// <param name="log">The log message to write.</param>
        /// <remarks>
        /// This function creates a new file at the path "./settingslog.txt" if it does not already exist, or opens it for writing if it does. 
        /// If an error occurs while attempting to create or open the file, the log message will be updated to include an error message.
        /// The function then writes the log message to the file, and restores the console output to its original state.
        /// </remarks>
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
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            Console.WriteLine(log);
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }

        /// <summary>
        /// Handles the MouseButton event for moving the window by dragging it.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The MouseButtonEventArgs associated with the event.</param>
        private void Window_Movment(object sender, MouseButtonEventArgs e)
        {
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

        /// <summary>
        /// Handles the MouseDown event for the HotkeysList by initiating a Drag and Drop operation.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The MouseButtonEventArgs associated with the event.</param>
        private void HotkeysList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (hotkeysList.SelectedItem != null)
            {
                DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event for the HotkeysList by initiating a Drag and Drop operation.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The SelectionChangedEventArgs associated with the event.</param>
        private void HotkeysList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mainWindow.StopMusic(null, null);
            if (hotkeysList.SelectedItem != null)
            {
                DragDrop.DoDragDrop(hotkeysList, hotkeysList.SelectedItem.ToString(), DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// Handles the Drop event for the TextBox by copying the filename into the selected TextBox and "activating" the Hotkey.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The DragEventArgs associated with the event.</param>
        private void TextBox_Drop(object sender, DragEventArgs e)
        {
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

        /// <summary>
        /// Activates the specified hotkey by updating the filename and reloading the hotkey list and filled boxes.
        /// </summary>
        /// <param name="hotkeyNr">The number of the hotkey to activate.</param>
        /// <param name="fileName">The filename of the audio file associated with the hotkey.</param>
        /// <returns>
        ///   <c>true</c> if the hotkey was successfully activated; otherwise, <c>false</c> if the specified file was not found.
        /// </returns>
        private bool ActivateHotkey(string hotkeyNr, string fileName)
        {
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

        /// <summary>
        /// Deactivates the specified hotkey by updating the filename and reloading the hotkey list and filled boxes.
        /// </summary>
        /// <param name="hotkeyNr">The number of the hotkey to deactivate.</param>
        /// <param name="fileName">The filename of the audio file associated with the hotkey.</param>
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

        /// <summary>
        /// Handles the KeyDown event for the specified TextBox instance, preventing writing in the textbox.
        /// </summary>
        /// <param name="sender">The TextBox instance that raised the KeyDown event.</param>
        /// <param name="e">The KeyEventArgs object that contains the event data.</param>
        private void Textbox1_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Handles the MouseDoubleClick event for the specified TextBox instance.
        /// </summary>
        /// <param name="sender">The TextBox instance that raised the MouseDoubleClick event.</param>
        /// <param name="e">The MouseButtonEventArgs object that contains the event data.</param>
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

        /// <summary>
        /// Handles the Loaded event for this SettingsWindow instance.
        /// </summary>
        /// <param name="sender">The object that raised the Loaded event.</param>
        /// <param name="e">The RoutedEventArgs object that contains the event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FillHotkeysList();
            RedoFilledBoxes();
        }
    }
}