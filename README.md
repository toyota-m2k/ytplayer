# BooTube ... YouTube Downloader & Player for Windows

BooTube is a GUI application to download videos from youtube. This program depends on [youtube-dl](https://youtube-dl.org/) ([yt-dlp](https://github.com/yt-dlp/yt-dlp) currently) and [ffmpeg](https://ffmpeg.org/). These are wonderful CUI tools. I respect the contributers of the projects and use them with gratitude.

![image](https://user-images.githubusercontent.com/11642381/161997295-9f653e7e-c4e3-43da-9f23-b9121914a8e5.png)

BooTube can be used to:

- download videos from youtube.
- play downloaded videos on built-in player
- or play on the remote player :[BooDroid](https://github.com/toyota-m2k/boodroid).
- put some additional information like category, mark, rating, comment to videos.
- sort and filter by these information
- trim the video.
- register chapters automatically or manually.

## Installation

1. Download and install `yt-dlp.exe` from https://github.com/yt-dlp/yt-dlp
2. Download and install  `ffmpeg.exe` from https://ffmpeg.org/
3. Open ytplayer.sln with Visual Studio 2022 and build it.
4. Execute BooTube.exe.

## Settings

At the first startup, `Settings` dialog box will be shown.

![image](https://user-images.githubusercontent.com/11642381/162000494-d4e69121-157b-4044-a595-a00d47fdb458.png)

|Item|Description|
|:--|:--|
|Data File|db file path.|
|yt-dlp.exe in|the directory where `yt-dlp.exe` is installed. if `PATH` environment variable is set, leave it blank.|
|ffmpeg.exe in|the directory where `ffmpeg.exe` is installed. if `PATH` environment variable is set, leave it blank.|
|Video output to|the directory where downloaded video files will be saved.|
|Audio output to|the directory where downloaded or extracted audio files will be saved.|
|Work directory|the directory where temporary files will be put. if not be specified, the system temporary directory is used.|
|Accept PlayList|allow to accept the url for PlayList (like `list=xxxxx`) and expand urls in the list.|
|Enable server|server for [BooDroid](https://github.com/toyota-m2k/boodroid).|


## How to use

- Append URL
  - D&D links from Web Browser
  - via Clipboard
  - Using [Barium](https://github.com/toyota-m2k/ytbrowser).
  - Sending from the Android device via [BooDroid](https://github.com/toyota-m2k/boodroid)

-  
