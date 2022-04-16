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
#pragma warning disable IDE0044 // Modifizierer "readonly" hinzufügen
        private string soundspath;
        private Button[] hotkeyBtnArray = { null, null, null, null, null, null, null, null, null, null };
#pragma warning restore IDE0044 // Modifizierer "readonly" hinzufügen
        private FileInfo[] files;
        private Button[] btnArray;
        private WaveOut waveOut;
        private WaveOut waveOutclient;
        private bool alreadyplaying = false;
        private string log = "";
        private SettingsWindow settings = new SettingsWindow();

        private string audioOutput;
        private int audioOutputID = 999;
        private float vol = 2.5f;
        public MainWindow()
        {
            log += "#Log# STARTING\n";

            //Initalize WindowComponents
            InitializeComponent();

            //Get file path
            string projectFolder = Directory.GetCurrentDirectory();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {//Logic for Button presses
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
            }
        }

        private void PlaySound(string btnName)
        {//Start Sound 
            if (!File.Exists(soundspath + $"/{btnName}.mp3"))                     //Check if requested File exists
            {
                return;
            }
            else
            {
                //Play Sound to VB-Audio (to simulate mic input)
                var Reader = new NAudio.Wave.Mp3FileReader(soundspath + $"/{btnName}.mp3");
                waveOut = new NAudio.Wave.WaveOut();                            //Player
                waveOut.PlaybackStopped += PlaybackDevicePlaybackStopped;       //Event Handler
                waveOut.DeviceNumber = audioOutputID;                           //Device Playback
                waveOut.Init(Reader);                                           //Initalize Soundplayer
                GetVolume(null, null);
                log += "#Log# Volume set to: " + vol * 10 + "\n";
                waveOut.Volume = vol;                                           //Set Volume

                //Play Sound to Default Device (to play it to the user)
                var ReaderClient = new NAudio.Wave.Mp3FileReader(soundspath + $"/{btnName}.mp3");
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

        private int GetSoundDelay()
        {//Get Sounddelay-Number from the TextBox
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

        private void GetVolume(object sender, RoutedPropertyChangedEventArgs<double> e)
        {//Get Volume-Number from the Slider
            this.vol = (float)slider.Value / 10;
            volslider.Content = "Volume: " + (this.vol * 10).ToString("0.00");                          //Update Volume-Label
        }

        private void PlaybackDevicePlaybackStopped(object sender, EventArgs e)
        {//Manages Playback Stopped
            btn.Content = btnName;                                              //Change Button Name back
            status.Content = "Playing: ";                                       //Remove current playing sound
            alreadyplaying = false;                                             //Update Variable
            StopMusic(null, null);                                              //dispose resources
            log += $"#Log# Sound: '{btnName}' stopped playing!\n";
        }

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

        private void CreateAHotkeyButton(string str, int row, int col, int arrayplacment)
        {//Create a Button with given Variables
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
            btnArray[arrayplacment] = btn;                                      //Save Button to array
            scrollViewerWrapperPanel.Children.Add(btn);                                            //Add Button to Window
            log += "#Log# Created Button: " + str + "\n";
        }

        private void SetHotkeys()
        {
            log += "#Log# Set Hotkeys";

            HotkeyManager.Current.Remove("Stop");
            HotkeyManager.Current.AddOrReplace("Stop", Key.Divide, ModifierKeys.None, StopHotkey);

            Key[] keys = { Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9 };
            for (int i = 0; i < keys.Length; i++)
            {
                HotkeyManager.Current.Remove("Hotkey" + i);
                HotkeyManager.Current.AddOrReplace("Hotkey" + i, keys[i], ModifierKeys.None, HotkeyClick);
            }
        }

        private void StopHotkey(object sender, HotkeyEventArgs e)
        {
            Stop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

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
            btnArray[arrayplacment] = btn;                                      //Save Button to array
            scrollViewerWrapperPanel.Children.Add(btn);                         //Add Button to Window
            log += "#Log# Created Button: " + str + "\n";
        }

        private void RefreshData(object sender, RoutedEventArgs e)
        {//Refresh Button Data
            for (int i = 0; i < files.Length; i++)
            {
                scrollViewerWrapperPanel.Children.Remove(btnArray[i]);                             //Remove all Buttons from Window
            }
            SearchSounds();                                                     //Search for Sounds again
            SetAudioDevice(settings.GetComboBoxItem());
            log += "#Log# Window Refreshed\n";
        }

        private void StopMusic(object sender, RoutedEventArgs e)
        {//Stop current playing Sound
            if (alreadyplaying)
            {
                waveOut.Stop();                                                     //Dispose sound Data to stop Playback
                waveOutclient.Stop();
                log += "#Log# Sound stopped\n";
                alreadyplaying = false;
            }
        }

        private new void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {//Filter all chars in the TextBox
            Regex regex = new Regex("^[^0-9\\s]+$");                            //Filtering using Regex
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Window_Shutdown(object sender, RoutedEventArgs e)
        {//Shutdown Program
            waveOut = null;                                                     //Set Variable to null
            waveOutclient = null;
            log += "#Log# System Shutdown\n";
            WriteOutput(log);
            settings.Hide();
            System.Windows.Application.Current.Shutdown();                      //Shutdown program
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

        private void Window_Minimize(object sender, MouseButtonEventArgs e)
        {//Minimze Window
            myWindow.WindowState = WindowState.Minimized;
            log += "#Log# Window minimized\n";
        }

        private void Window_Settings(object sender, MouseButtonEventArgs e)
        {
            settings = new SettingsWindow();
            settings.Show();
        }
    }
}
