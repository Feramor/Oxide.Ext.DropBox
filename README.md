# The Installation Manual
## DropBox Extension For Oxide
This is an extension to [Oxide](http://www.oxidemod.org), a powerful modding framework for several survival games.
This extension adds a remote backup system to any server with just a few clicks.

#### Features
* Configurable backup interval.
* Configurable dropbox API key & secret.
* Configurable file list for backup.

## Terms of use
Installing the extension is and remains free for non-commercial and commercial use.

## Installation
1. Stop your server if its already running.
2. Make sure that your server is running a recent version of [Oxide](http://www.oxidemod.org/downloads/).
3. Download [Oxide.Ext.DropBox](http://oxidemod.org/extensions/dropbox-extension.1542/) or compile it for yourself
4. Place the just downloaded `Oxide.Ext.DropBox.dll` into your server's `Managed` directory.
5. Run server, wait for fully loaded and stop server.
6. New config file will be created under `oxide\config` called `DropBox.json`
7. Edit config file and start server
8. Check Oxide log for authorization link `https://www.dropbox.com/1/oauth/authorize?oauth_token=XXXXXXXXXXXXXXXXXXXX`
9. Open authorization link in your Browser
10. Login & Authorize your application
11. After you authorized you will start backing your files to DropBox
11. [Info] [DropBox] Authorizing
12. [Info] [DropBox] Authorization Succeed
13. [Info] [DropBox] Current Dropbox User : User Display Name(User Email)
14. [Info] [DropBox] First Backup : MM/DD/YYYY HH:MM:SS Acording to Culture Settings
15. [Info] [DropBox] Uploading...
16. [Info] [DropBox] Uploading Complated.Next Backup : MM/DD/YYYY HH:MM:SS Acording to Culture Settings

## Configration
| Option | Default | Description
|--------|---------|-------------
|DropboxApi.DropboxAppKey|`""`|Your DropBox application's AppKey.
|DropboxApi.DropboxAppSecret|`""`|Your DropBox application's Secret.
|UserToken|`""`|This will automaticly edited when you authorized.
|UserSecret|`""`|This will automaticly edited when you authorized.
|BackupName|`"Oxide DropBox Extension"`|This is the foldername which going to be created under your DropBox account.
|BackupInterval|`3600`|Every given seconds server will backup your files to dropbox. (Min:3600 Second)
|BackupOxideConfig|`true`|To disable backing up Oxide config files set this to `false`
|BackupOxideData|`true`|To disable backing up Oxide data files set this to `false`
|BackupOxideLang|`true`|To disable backing up Oxide lang files set this to `false`
|BackupOxideLogs|`true`|To disable backing up Oxide log files set this to `false`
|BackupOxidePlugins|`true`|To disable backing up Oxide plugin files set this to `false`
|FileList|`[]`|Since save files can be different for games you should manually add files or directories.
#### Default Configration File
```json
{
  "DropboxApi": {
    "DropboxAppKey": "",
    "DropboxAppSecret": ""
  },
  "UserToken": "",
  "UserSecret": "",
  "BackupName": "Oxide DropBox Extension",
  "BackupInterval": 3600,
  "BackupOxideConfig": true,
  "BackupOxideData": true,
  "BackupOxideLang": true,
  "BackupOxideLogs": true,
  "BackupOxidePlugins": true,
  "FileList": []
}
```
#### Obtaining Dropbox App Key & Secret
1. Head to [Dropbox Developers Page](https://www.dropbox.com/developers/apps/create) & Login with your DropBox account if you don't have one get it.
2. Select `Dropbox API`.
3. Select `Full Dropbox– Access to all files and folders in a user's Dropbox`.
4. Fill `Name your app` acording to your wishes.
5. Click `Create App`
6. You can edit your Application Informations acording to your wishes (App Icon etc.).
7. Note your `App key` and `App secret`
8. Edit  Configration File

## Modify  Configration File
 ```json
{
  "DropboxApi": {
    "DropboxAppKey": "YOUR_APP_KEY",
    "DropboxAppSecret": "YOUR_APP_SECRET"
  },
  "UserToken": "",
  "UserSecret": "",
  "BackupName": "Oxide DropBox Extension",
  "BackupInterval": 3600,
  "BackupOxideConfig": true,
  "BackupOxideData": true,
  "BackupOxideLang": true,
  "BackupOxideLogs": true,
  "BackupOxidePlugins": true,
  "FileList": []
}
```
## Adding Custom Files to BackUp (This example is valid for HurtWorld)
```json
"FileList": ["autosave_DiemensLand.plr", "autosave_DiemensLand.wld"]
```
**Note:** You may need to restart server after you change config file...

**Note:** Some game server providers restrict access to the `Managed` directory but you may usually ask their support to install Extension for you.
