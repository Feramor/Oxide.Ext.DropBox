using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using Spring.Social.Dropbox.Connect;
using Spring.Social.Dropbox.Api;
using Spring.Social.OAuth1;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Spring.IO;

namespace Oxide.Ext.DropBox.Libraries
{
    public class DropBox : Library
    {
        class Settings
        {
            public Dictionary<string, string> DropboxApi { get; set; }
            public string UserToken { get; set; }
            public string UserSecret { get; set; }
            public string BackupName { get; set; }
            public int BackupInterval { get; set; }
            public bool BackupOxideConfig { get; set; }
            public bool BackupOxideData { get; set; }
            public bool BackupOxideLang { get; set; }
            public bool BackupOxideLogs { get; set; }
            public bool BackupOxidePlugins { get; set; }
            public List<string> FileList { get; set; }
    }

        public override bool IsGlobal => false;

        private readonly string _ConfigDirectory;
        private readonly string _DataDirectory;
        private readonly DataFileSystem _DataFileSystem;
        
        private bool _running = true;
        private Settings _Settings;

        private Thread WorkerThread = null;
        public DropBox()
        {
            _DataFileSystem = Interface.Oxide.DataFileSystem;
            _ConfigDirectory = Interface.Oxide.ConfigDirectory;
            _DataDirectory = Interface.Oxide.DataDirectory;
        }
        internal void Initialize()
        {
            if ((WorkerThread == null) || (!WorkerThread.IsAlive))
            {
                WorkerThread = new Thread(Worker);
                WorkerThread.IsBackground = true;
                WorkerThread.Start();
            }
        }
        private void Worker()
        {
            Interface.Oxide.LogInfo("[DropBox] Initialized");
            _Settings = _DataFileSystem.ReadObject<Settings>(Path.Combine(_ConfigDirectory, "DropBox"));

            if (_Settings.FileList == null)
            {
                _Settings = new Settings();

                _Settings.BackupOxideConfig = true;
                _Settings.BackupOxideData = true;
                _Settings.BackupOxideLang = true;
                _Settings.BackupOxideLogs = true;
                _Settings.BackupOxidePlugins = true;

                _Settings.FileList = new List<string>();
                _Settings.UserToken = "";
                _Settings.UserSecret = "";
                _Settings.BackupName = "Oxide DropBox Extension";
                _Settings.BackupInterval = 3600;
                _Settings.DropboxApi = new Dictionary<string, string>();
                _Settings.DropboxApi.Add("DropboxAppKey","");
                _Settings.DropboxApi.Add("DropboxAppSecret", "");
                _DataFileSystem.WriteObject<Settings>(Path.Combine(_ConfigDirectory, "DropBox"), _Settings);
            }

            if (_Settings.BackupInterval < 3600)
            {
                Interface.Oxide.LogError("[DropBox] You can't set backup interval lower than 1 hour.");
                _Settings.BackupInterval = 3600;
                _DataFileSystem.WriteObject<Settings>(Path.Combine(_ConfigDirectory, "DropBox"), _Settings);
            }

            if ((string.IsNullOrEmpty(_Settings.DropboxApi["DropboxAppKey"])) || (string.IsNullOrEmpty(_Settings.DropboxApi["DropboxAppSecret"])))
            {
                _running = false;
                Interface.Oxide.LogWarning("[DropBox] To able to use DropBox Extension you need to set DropboxAppKey and DropboxAppSecret in config file.");
            }
            if (_running)
            {
                DropboxServiceProvider dropboxServiceProvider = new DropboxServiceProvider(_Settings.DropboxApi["DropboxAppKey"], _Settings.DropboxApi["DropboxAppSecret"], AccessLevel.Full);

                OAuthToken oauthAccessToken = null;
                bool Authorized = false;
                IDropbox dropbox = null;
                do
                {
                    if ((_running) && (string.IsNullOrEmpty(_Settings.UserToken) || string.IsNullOrEmpty(_Settings.UserSecret)))
                    {
                        Interface.Oxide.LogInfo("[DropBox] Getting request token...");
                        OAuthToken oauthToken = dropboxServiceProvider.OAuthOperations.FetchRequestToken(null, null);
                        Interface.Oxide.LogInfo("[DropBox] Done");
                        OAuth1Parameters parameters = new OAuth1Parameters();
                        string authenticateUrl = dropboxServiceProvider.OAuthOperations.BuildAuthenticateUrl(oauthToken.Value, parameters);
                        Interface.Oxide.LogInfo("[DropBox] Redirect user for authorization");
                        Interface.Oxide.LogInfo("[DropBox] {0}", authenticateUrl);
                        while ((_running) && (Authorized == false))
                        {
                            try
                            {
                                AuthorizedRequestToken requestToken = new AuthorizedRequestToken(oauthToken, null);
                                oauthAccessToken = dropboxServiceProvider.OAuthOperations.ExchangeForAccessToken(requestToken, null);
                                Authorized = true;
                                _Settings.UserToken = oauthAccessToken.Value;
                                _Settings.UserSecret = oauthAccessToken.Secret;
                                _DataFileSystem.WriteObject<Settings>(Path.Combine(_ConfigDirectory, "DropBox"), _Settings);
                            }
                            catch
                            {
                                Thread.Sleep(5000);
                            }
                        }
                    }
                    else if (_running)
                    {
                        oauthAccessToken = new OAuthToken(_Settings.UserToken, _Settings.UserSecret);
                    }
                    if (_running)
                    {
                        try
                        {
                            Interface.Oxide.LogInfo("[DropBox] Authorizing");
                            dropbox = dropboxServiceProvider.GetApi(oauthAccessToken.Value, oauthAccessToken.Secret);
                            Interface.Oxide.LogInfo("[DropBox] Authorization Succeed");
                            Authorized = true;
                        }
                        catch
                        {
                            Interface.Oxide.LogWarning("[DropBox] Authorization Failed");
                            _Settings.UserToken = "";
                            _Settings.UserSecret = "";
                            _DataFileSystem.WriteObject<Settings>(Path.Combine(_ConfigDirectory, "DropBox"), _Settings);
                            Authorized = false;
                        }
                    }
                } while ((_running) && (Authorized == false));
                if ((_running) && (dropbox != null) && (Authorized == true))
                {
                    DropboxProfile profile = dropbox.GetUserProfile();
                    Interface.Oxide.LogInfo("[DropBox] Current Dropbox User : {0}({1})", profile.DisplayName, profile.Email);
                    DateTime NextUpdate = DateTime.Now.AddSeconds(60);
                    Interface.Oxide.LogInfo("[DropBox] First Backup : {0}", NextUpdate.ToString());
                    while (_running)
                    {
                        if (DateTime.Now < NextUpdate)
                        {
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            string BackUpRoot = Path.Combine(Interface.Oxide.RootDirectory, "OxideExtBackup");
                            string OxideBackUpRoot = Path.Combine(BackUpRoot, "Oxide");
                            string FileBackUpRoot = Path.Combine(BackUpRoot, "Files");

                            Directory.CreateDirectory(BackUpRoot);
                            DirectoryInfo OxideExtBackup = new DirectoryInfo(BackUpRoot);
                            foreach (FileInfo file in OxideExtBackup.GetFiles()) file.Delete();
                            foreach (DirectoryInfo subDirectory in OxideExtBackup.GetDirectories()) subDirectory.Delete(true);

                            Directory.CreateDirectory(OxideBackUpRoot);
                            Directory.CreateDirectory(FileBackUpRoot);

                            if (_Settings.BackupOxideConfig)
                                DirectoryCopy(Interface.Oxide.ConfigDirectory, Path.Combine(OxideBackUpRoot, "config"), true);

                            if (_Settings.BackupOxideData)
                                DirectoryCopy(Interface.Oxide.DataDirectory, Path.Combine(OxideBackUpRoot, "data"), true);

                            if (_Settings.BackupOxideLang)
                                DirectoryCopy(Interface.Oxide.LangDirectory, Path.Combine(OxideBackUpRoot, "lang"), true);

                            if (_Settings.BackupOxideLogs)
                                DirectoryCopy(Interface.Oxide.LogDirectory, Path.Combine(OxideBackUpRoot, "logs"), true);

                            if (_Settings.BackupOxidePlugins)
                                DirectoryCopy(Interface.Oxide.PluginDirectory, Path.Combine(OxideBackUpRoot, "plugins"), true);

                            foreach (string Current in _Settings.FileList)
                            {
                                if (!string.IsNullOrEmpty(Current))
                                {
                                    string CurrentPath = Path.Combine(Interface.Oxide.RootDirectory, Current);
                                    if ((File.GetAttributes(CurrentPath) & FileAttributes.Directory) == FileAttributes.Directory)
                                    {
                                        if (CurrentPath != Interface.Oxide.RootDirectory)
                                            DirectoryCopy(CurrentPath, Path.Combine(FileBackUpRoot, new DirectoryInfo(CurrentPath).Name), true);
                                    }
                                    else
                                    {
                                        File.Copy(CurrentPath, Path.Combine(FileBackUpRoot, new FileInfo(CurrentPath).Name));
                                    }
                                }
                            }
                            string FileName = string.Format("{0}.{1}.{2}.{3}.{4}.{5}.zip", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second);
                            FileStream fsOut = File.Create(Path.Combine(Interface.Oxide.RootDirectory, FileName));
                            ZipOutputStream zipStream = new ZipOutputStream(fsOut);
                            zipStream.SetLevel(3);
                            string folderName = Path.Combine(Interface.Oxide.RootDirectory, "OxideExtBackup");
                            int folderOffset = folderName.Length + (folderName.EndsWith("\\") ? 0 : 1);
                            CompressFolder(folderName, zipStream, folderOffset);
                            zipStream.IsStreamOwner = true;
                            zipStream.Close();
                            Interface.Oxide.LogInfo("[DropBox] Uploading...");
                            Entry uploadFileEntry = dropbox.UploadFile(new FileResource(Path.Combine(Interface.Oxide.RootDirectory, FileName)), string.Format("/{0}/{1}", _Settings.BackupName, FileName), true, null);
                            Directory.Delete(folderName, true);
                            File.Delete(Path.Combine(Interface.Oxide.RootDirectory, FileName));
                            NextUpdate = NextUpdate.AddSeconds(_Settings.BackupInterval);
                            Interface.Oxide.LogInfo("[DropBox] Uploading Complated.Next Backup : {0}", NextUpdate.ToString());
                        }
                    }
                }
            }
        }
        internal void Shutdown()
        {
            _running = false;
        }
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                Interface.Oxide.LogWarning("[DropBox] Source directory does not exist or could not be found : " + sourceDirName);
            }
            else
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temppath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, false);
                }
                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        string temppath = Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                    }
                }
            }
        }

        private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                FileInfo fi = new FileInfo(filename);
                string entryName = filename.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime;
                newEntry.Size = fi.Length;
                zipStream.PutNextEntry(newEntry);
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }
    }
}
