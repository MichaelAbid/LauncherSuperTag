﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace WanderingClouds
{
    enum LauncherStatus
    {
        LAUCNHER_STATUS_READY,
        LAUCNHER_STATUS_FAILED,
        LAUCNHER_STATUS_DOWNLOADING_GAME,
        LAUCNHER_STATUS_DOWNLOADING_UPDATE
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private string versionLink = "https://www.dropbox.com/s/pgdp6o61rkuis2w/WanderingCloudsVersion.txt?dl=1";
        private string gameLink = "https://www.dropbox.com/s/r60j6ivgcip893r/WanderingClouds.zip?dl=1";

        private long bitPrevious = 0;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.LAUCNHER_STATUS_READY:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.LAUCNHER_STATUS_FAILED:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.LAUCNHER_STATUS_DOWNLOADING_GAME:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.LAUCNHER_STATUS_DOWNLOADING_UPDATE:
                        PlayButton.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "WanderingCloudsVersion.txt");
            gameZip = Path.Combine(rootPath, "WanderingClouds.zip");
            gameExe = Path.Combine(rootPath, "Build", "WanderingClouds.exe");

            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if(File.Exists(gameExe) && Status == LauncherStatus.LAUCNHER_STATUS_READY)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = System.IO.Path.Combine(rootPath, "Build");
                Process.Start(startInfo);
                Close();
            }
            else
            {
                if(Status == LauncherStatus.LAUCNHER_STATUS_FAILED)
                {
                    CheckForUpdates();
                }
            }
        }

        private void InstallGameFiles(bool _isUpdate,Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.LAUCNHER_STATUS_DOWNLOADING_UPDATE;
                }
                else
                {
                    Status = LauncherStatus.LAUCNHER_STATUS_DOWNLOADING_GAME;
                    _onlineVersion = new Version(webClient.DownloadString(versionLink));
                }
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    PlayButton.Content = $" Downloading Files {e.BytesReceived / 1000000}MB / {e.TotalBytesToReceive / 1000000}MB ({e.ProgressPercentage}%)";
                    bitPrevious = e.BytesReceived;
                };
                webClient.DownloadFileAsync(new Uri(gameLink),gameZip,_onlineVersion);
            }
            catch(Exception ex)
            {
                Status = LauncherStatus.LAUCNHER_STATUS_FAILED;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.LAUCNHER_STATUS_READY;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.LAUCNHER_STATUS_FAILED;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(versionLink));
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.LAUCNHER_STATUS_READY;
                    }
                }
                catch (Exception ex)
                {

                    Status = LauncherStatus.LAUCNHER_STATUS_FAILED;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if(_versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }
            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }


        internal bool IsDifferentThan(Version _otherVersion)
        {
            if(major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
