using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Text.RegularExpressions;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NHotkey.Wpf;
using NHotkey;
using System.Windows.Controls.Primitives;

namespace CSSoundboard
{
    /// Interaktionslogik für MainWindow.xaml
    //  Rudi Wagner
    //  C# Soundboard
    public partial class MainWindow : Window
    {
        //Varibale declaration
        private Button btn;
        private string btnName;
        private string projectFolder;
        private string soundspath;
        private Button[] hotkeyBtnArray = { null, null, null, null, null, null, null, null, null, null };
        private FileInfo[] files;
        private Button[] btnArray;
        private WaveOut waveOut;
        private WaveOut waveOutclient;
        private bool alreadyplaying = false;
        private string audioOutput;
        private int audioOutputID = 999;
        private string log = "";
        private SettingsWindow settings;

        private Mp3FileReader Reader;
        private Mp3FileReader ReaderClient;

        private float vol = 2.5f;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            log += $"#Log# STARTING {DateTime.Now:HH:mm:ss}\n";

            settings = new SettingsWindow(this);

            //Initalize WindowComponents
            InitializeComponent();

            //Get file path
            projectFolder = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\Rudi Wagner";
            soundspath = projectFolder + @"\Sounds";
            log += "#Log# Filedirectory: " + soundspath + "\n";

            //Searching Soundfiles and creating buttons
            SearchSounds();

            //Set Hotkeys
            SetHotkeys();

            //Find correct output device
            SetAudioDevice(settings.GetComboBoxItem());
            
            //Audio Device Error handeling
            if (audioOutputID == 999)
            {
                log += "#Error# Correct Audio Device not found!\n";
                MessageBoxResult resultAudioDeviceError = MessageBox.Show("It seems like you don't have VB-Cable installed!" +
                                                        "\n Do you want to install it now?",
                                                        "Audi Device Error",
                                                        MessageBoxButton.YesNo,
                                                        MessageBoxImage.Error);

                if (resultAudioDeviceError == MessageBoxResult.Yes)
                {//Open Website
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://vb-audio.com/Cable/",
                        UseShellExecute = true
                    });
                    Window_Shutdown(null, null);
                }
                if (resultAudioDeviceError == MessageBoxResult.No)
                {//Continue without Mic-Support handeling
                    MessageBoxResult resultContinueWithoutMic = MessageBox.Show("Continue without Mic-Support?",
                                                                             "Audi Device Error",
                                                                             MessageBoxButton.YesNo,
                                                                             MessageBoxImage.Warning);
                    //if yes continue
                    if (resultContinueWithoutMic == MessageBoxResult.No)
                    {//Shutdown
                        Window_Shutdown(null, null);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the audio device to use for sound playback.
        /// </summary>
        /// <param name="selectedDevice">The name of the audio device to use.</param>
        /// <remarks>
        /// The function retrieves a list of all active audio playback devices using an instance of the 'MMDeviceEnumerator' class.
        /// The function cycles through each audio device and compares the name of the device to the provided 'selectedDevice' parameter.
        /// If the name of the current device starts with the 'selectedDevice' parameter, the function sets the 'audioOutputID' variable to the index of the current device.
        /// The function also updates the 'log' variable to indicate which audio device is being used.
        /// </remarks>
        private void SetAudioDevice(string selectedDevice)
        {
            var enumerator = new MMDeviceEnumerator();          //create enumerator
            for (int i = 0; i < WaveOut.DeviceCount; i++)       //cycle trough all audio devices
            {
                audioOutput = "" + enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[i];
                if (audioOutput.StartsWith(selectedDevice))
                {
                    log += "#Log# Used Audio Device: " + audioOutput + " ID: " + i + "\n";
                    audioOutputID = i;
                }
            }
        }

        /// <summary>
        /// Handles button clicks for playing sound files.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The event arguments for the button click.</param>
        /// <remarks>
        /// The function stops any currently playing sound by calling the 'StopMusic' function.
        /// The function then retrieves the name of the sound file associated with the button that was clicked.
        /// The function updates the UI to show that the sound file is playing.
        /// The function calls the 'PlaySound' function to play the sound file.
        /// </remarks>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StopMusic(null, null);
            if (!alreadyplaying)
            {
                btn = sender as Button;                                         //Get Button as Object
                btnName = (string)btn.Content;                                  //Get Button Text Content
                log += $"#Log# Sound: '{btnName}' is playing!\n";
                btn.Content = "Playing...";                                     //Change Button Content
                status.Content = "Playing: " + btnName;                         //Display current playing sound
                alreadyplaying = true;                                          //Update Variable
                PlaySound(btnName);                                             //Start Method
                RefreshData(null, null);
            }
        }

        /// <summary>
        /// Plays a sound file with the given button name to the user and to the VB-Audio virtual microphone.
        /// </summary>
        /// <param name="btnName">The name of the sound file to play without the file extension.</param>
        /// <remarks>
        /// The function checks if the requested sound file exists. If it does not, the function returns without playing the sound.
        /// The function sets the volume of the sound file to the value stored in the 'vol' variable.
        /// The function also sets the delay between the start of the function and the start of the sound to the value returned by the 'GetSoundDelay' function.
        /// The function plays the sound file simultaneously to the VB-Audio virtual microphone and the default sound device of the user.
        /// </remarks>
        private void PlaySound(string btnName)
        {
            if (!File.Exists(soundspath + $"/{btnName}.mp3"))                     //Check if requested File exists
            {
                return;
            }
            else
            {
                //Play Sound to VB-Audio (to simulate mic input)
                Reader = new NAudio.Wave.Mp3FileReader(soundspath + $"/{btnName}.mp3");
                waveOut = new NAudio.Wave.WaveOut();                            //Player
                waveOut.PlaybackStopped += PlaybackDevicePlaybackStopped;       //Event Handler
                waveOut.DeviceNumber = audioOutputID;                           //Device Playback
                waveOut.Init(Reader);                                           //Initalize Soundplayer
                GetVolume(null, null);
                log += "#Log# Volume set to: " + vol * 10 + "\n";
                waveOut.Volume = vol;                                           //Set Volume

                //Play Sound to Default Device (to play it to the user)
                ReaderClient = new NAudio.Wave.Mp3FileReader(soundspath + $"/{btnName}.mp3");
                waveOutclient = new NAudio.Wave.WaveOut();
                waveOutclient.Init(ReaderClient);
                waveOutclient.Volume = vol;

                //Set delay
                log += "#Log# Delay set to: " + GetSoundDelay() + "\n";
                Thread.Sleep(GetSoundDelay());                                  //Set start delay

                //Starting both at the same time
                alreadyplaying = true;
                waveOut.Play();
                waveOutclient.Play();
            }
        }

        /// <summary>
        /// Retrieves the Sound Delay value from the TextBox.
        /// </summary>
        /// <remarks>
        /// If the TextBox is empty or contains only whitespace, the function returns 0.
        /// </remarks>
        /// <returns>The Sound Delay value as an integer.</returns>
        private int GetSoundDelay()
        {
            string delaytxt = delay.Text;                                       //Get TextBox-String
            if (delaytxt.Equals(""))                                            //Check if the String contains something
            {
                delaytxt = "0";
            }
            delaytxt = delaytxt.Replace(" ", "");                               //Remove all Whitespaces
            int delaytime = Int32.Parse(delaytxt);                              //Parse String to int
            delay.Text = delaytxt;                                              //Set TextBox Value
            return delaytime;
        }

        /// <summary>
        /// Retrieves the volume number from the slider control and updates the volume field.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void GetVolume(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.vol = (float)slider.Value / 10;
            volslider.Content = "Volume: " + (this.vol * 10).ToString("0.00");                          //Update Volume-Label
        }

        /// <summary>
        /// Handles the event when the playback device has stopped playing a sound.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void PlaybackDevicePlaybackStopped(object sender, EventArgs e)
        {
            btn.Content = btnName;                                              //Change Button Name back
            status.Content = "Playing: ";                                       //Remove current playing sound
            alreadyplaying = false;                                             //Update Variable
            StopMusic(null, null);                                              //dispose resources
            log += $"#Log# Sound: '{btnName}' stopped playing!\n";
        }

        /// <summary>
        /// Searches for sound files in the 'Sounds' directory and creates buttons for them.
        /// </summary>
        private void SearchSounds()
        {
            log += "#Log# Searching for Sound-Files\n";
            DirectoryInfo directory = new DirectoryInfo(soundspath);              //Look for Files in Directory
            if (!directory.Exists)
            {
                log += ("#Error# The Sound-Directory could not be found!\n");
                MessageBoxResult result = MessageBox.Show("It seems like you don't have a 'Sounds' directory!" +
                                                        "\n The .exe file and the 'Sounds' directory have to be in the same place!" +
                                                        "\n Do you want to automaticly create a directory in the right spot?",
                                                        "No directory error",
                                                        MessageBoxButton.YesNo,
                                                        MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                {//Create directory
                    System.IO.Directory.CreateDirectory(soundspath);
                }
                if (result == MessageBoxResult.No)
                {//Shutdown
                    Window_Shutdown(null, null);
                }
            }
            files = directory.GetFiles("*.mp3");                                //Get all .mp3 Files
            //File Error handeling
            if (files.Length == 0)
            {
                log += "#Error# No Sounds were found!\n";
                MessageBox.Show("It seems like you don't have any MP3 Files in the correct directory!" +
                                                        "\nYou need to place '.mp3' files in the 'Sounds' directory!",
                                                        "No files error",
                                                        MessageBoxButton.OK,
                                                        MessageBoxImage.Error);
            }

            btnArray = new Button[files.Length];                                //Create Button Array
            int row = 3;
            int col = 0;

            for (int i = 0; i < files.Length; i++)                              //Create Button Loop
            {
                //get button name
                string content = files[i].Name;
                content = content.Remove(content.LastIndexOf(".mp3"));
                //create button
                if (content.StartsWith("Hotkey"))
                {
                    //Create button with hotkey function
                    CreateAHotkeyButton(content, row, col, i);
                }
                else
                { 
                    //Create just a normal Button
                    CreateAButton(content, row, col, i);
                }
            }
        }

        /// <summary>
        /// Creates a special button with the given variables and adds it to the window.
        /// This special button has the ability to be pressed by a hotkey (Numpad 0-9)
        /// </summary>
        /// <param name="str">The string to use as the button content.</param>
        /// <param name="row">The row to place the button in on the Grid control.</param>
        /// <param name="col">The column to place the button in on the Grid control.</param>
        /// <param name="arrayplacment">The index to use for the button in the btnArray.</param>
        private void CreateAHotkeyButton(string str, int row, int col, int arrayplacment)
        {
            Button btn = new Button
            {
                Margin = new Thickness(5),                                      //Set Margin
                Content = str                                                  //Set Button Content
            };
            btn.SetValue(Grid.RowProperty, row);                                //Set Row
            btn.SetValue(Grid.ColumnProperty, col);                             //Set Column
            btn.Click += new RoutedEventHandler(Button_Click);                  //Add EventHandler

            string hotkeyName = str.Split("#")[0];
            int hotkey = int.Parse(hotkeyName.Substring(6));
            hotkeyBtnArray[hotkey] = btn;

            var bc = new BrushConverter();
            btn.Width = 300;
            btn.Height = 80;
            btn.Background = (Brush)bc.ConvertFrom("#2c2f33");                  //Set Background Colour
            btn.Foreground = (Brush)bc.ConvertFrom("#99aab5");                  //Set Foreground Colour
            btn.BorderBrush = (Brush)bc.ConvertFrom("#f34b43");                 //Set Border Coulour
            btn.ToolTip = "Click to play this Sound. Or use a Hotkey!";
            btnArray[arrayplacment] = btn;                                      //Save Button to array
            scrollViewerWrapperPanel.Children.Add(btn);                                            //Add Button to Window
            log += "#Log# Created Button: " + str + "\n";
        }

        /// <summary>
        /// Sets the hotkeys for the application.
        /// </summary>
        /// <remarks>
        /// This function adds or replaces hotkeys for stopping the application and clicking on the number keys from 0 to 9 on the number pad.
        /// </remarks>
        private void SetHotkeys()
        {
            log += "#Log# Set Hotkeys\n";

            HotkeyManager.Current.Remove("Stop");
            HotkeyManager.Current.AddOrReplace("Stop", Key.Divide, ModifierKeys.None, StopHotkey);

            Key[] keys = { Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9 };
            for (int i = 0; i < keys.Length; i++)
            {
                HotkeyManager.Current.Remove("Hotkey" + i);
                HotkeyManager.Current.AddOrReplace("Hotkey" + i, keys[i], ModifierKeys.None, HotkeyClick);
            }
        }

        /// <summary>
        /// Handles the event when the hotkey for stopping the currently playing sound is pressed.
        /// By simulating a Buttonpress on the Stop-Button.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments containing information about the hotkey that was pressed.</param>
        private void StopHotkey(object sender, HotkeyEventArgs e)
        {
            Stop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        /// Handles the event when a registered hotkey is pressed.
        /// It simulates a Button press to the according Sound-Button.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the hotkey event.</param>
        private void HotkeyClick(object sender, HotkeyEventArgs e)
        {
            for (int i = 0; i < hotkeyBtnArray.Length; i++)
            {
                if (hotkeyBtnArray[i] != null)
                {
                    if (e.Name == hotkeyBtnArray[i].Content.ToString().Split("#")[0])       //e.Name get the Name of the Hotkey
                    {                                                                       //hotkeyBtnArray[i].Content.ToString().Split("#")[0] returns the first part of the button content (Hotkeyx)
                        hotkeyBtnArray[i].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); //Fires the button event programmaticly
                    }
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Creates a new button with the specified label, row, column, and placement in the button array.
        /// The button is added to the scrollViewerWrapperPanel, and a log entry is created.
        /// </summary>
        /// <param name="str">The label to display on the button.</param>
        /// <param name="row">The row of the grid to place the button in.</param>
        /// <param name="col">The column of the grid to place the button in.</param>
        /// <param name="arrayplacment">The index of the button in the button array.</param>
        private void CreateAButton(string str, int row, int col, int arrayplacment)
        {//Create a Button with given Variables
            Button btn = new Button
            {
                Margin = new Thickness(5),                                      //Set Margin
                Content = str                                                   //Set Button Content
            };
            btn.SetValue(Grid.RowProperty, row);                                //Set Row
            btn.SetValue(Grid.ColumnProperty, col);                             //Set Column
            btn.Click += new RoutedEventHandler(Button_Click);                  //Add EventHandler

            var bc = new BrushConverter();
            btn.Width = 300;
            btn.Height = 80;
            btn.Background = (Brush)bc.ConvertFrom("#2c2f33");                  //Set Background Colour
            btn.Foreground = (Brush)bc.ConvertFrom("#99aab5");                  //Set Foreground Colour
            btn.BorderBrush = (Brush)bc.ConvertFrom("#131516");                 //Set Border Coulour
            btn.ToolTip = "Click to play this Sound.";
            btnArray[arrayplacment] = btn;                                      //Save Button to array
            scrollViewerWrapperPanel.Children.Add(btn);                         //Add Button to Window
            log += "#Log# Created Button: " + str + "\n";
        }

        /// <summary>
        /// Event handler for when the user clicks the "Refresh" button.
        /// Refreshes all sounds, sets the audio device, and logs the event to the application's log file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        public void RefreshData(object sender, RoutedEventArgs e)
        {
            //Refresh Button Data
            for (int i = 0; i < files.Length; i++)
            {
                scrollViewerWrapperPanel.Children.Remove(btnArray[i]);                             //Remove all Buttons from Window
            }
            SearchSounds();                                                     //Search for Sounds again
            SetAudioDevice(settings.GetComboBoxItem());
            log += "#Log# Window Refreshed\n";
        }

        /// <summary>
        /// Event handler for when the user clicks the "Stop" button.
        /// Stops the currently playing sound, disposes of its data, and logs the event to the application's log file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        public void StopMusic(object sender, RoutedEventArgs e)
        {
            if (alreadyplaying)
            {
                waveOut.Stop();                                                     //Dispose sound Data to stop Playback
                waveOutclient.Stop();
                alreadyplaying = false;
                Reader.Dispose();
                Reader.Close();
                ReaderClient.Dispose();
                ReaderClient.Close();
                log += "#Log# Sound stopped\n";
            }
        }

        /// <summary>
        /// Event handler for previewing text input in a text box.
        /// Filters out all characters that are not digits or whitespace characters.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private new void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[^0-9\\s]+$");                            //Filtering using Regex
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Event handler for when the user clicks the "Shutdown" button.
        /// Shuts down the application, closes the settings window, and logs the event to the application's log file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_Shutdown(object sender, RoutedEventArgs e)
        {
            waveOut = null;                                                     //Set Variable to null
            waveOutclient = null;
            log += "#Log# System Shutdown\n";
            WriteOutput(log);
            settings.Hide();
            System.Windows.Application.Current.Shutdown();                      //Shutdown program
        }

        /// <summary>
        /// Writes the given log message to a text file and redirects the console output to that file.
        /// </summary>
        /// <param name="log">The log message to write.</param>
        private void WriteOutput(string log)
        {
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream(projectFolder + "/log.txt", FileMode.OpenOrCreate, FileAccess.Write);
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
        /// Event handler for when the user clicks and drags on the application window.
        /// Allows the user to move the window by dragging it with the left mouse button.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_Movment(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        /// <summary>
        /// Event handler for when the user clicks the "Minimize Window" button.
        /// Minimizes the application window and logs the event to the application's log file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_Minimize(object sender, MouseButtonEventArgs e)
        {
            myWindow.WindowState = WindowState.Minimized;
            log += "#Log# Window minimized\n";
        }

        /// <summary>
        /// Event handler for when the settings button is clicked.
        /// Shows the settings window and positions it relative to the main window.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_Settings(object sender, MouseButtonEventArgs e)
        {
            settings.Show();
            settings.Left = this.Left - 20;
            settings.Top = this.Top - 20;
            settings.Activate();
        }

        /// <summary>
        /// Event handler for when the user clicks the "Open Sounds Folder" button.
        /// Opens the Windows Explorer window to the folder containing the application's sound files.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OpenSoundsFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", soundspath);
        }
    }
}
