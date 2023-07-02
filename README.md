# Video Speech Check

![Video Speech Check Logo](https://github.com/wit-eks/VideoSpeechCheck/blob/master/_assets/icons/icon-128.png)

## Builds

![Test Develop](https://github.com/wit-eks/VideoSpeechCheck/actions/workflows/test-develop.yml/badge.svg)

![Command Line UI publish](https://github.com/wit-eks/VideoSpeechCheck/actions/workflows/publish-cmd.yml/badge.svg)


## Functionalities:
* speech to text recognition and transcribe
* check existence of desired and prohibited phrases

## UI
### Command line application
* build/publish  **Visprech.Cmd** project or download latest release
* run the built executable
    * if not exists `ffmpeg` will be download (needed for audio extraction)
    * if not exists `Whisper ggml` will be downloaded (needed for transcript creation)
* the **app.conf** file should be created with default configuration
* edit phrases in **app.conf**. Add phrases that are desired and/or prohibited. **DesiredPhrases** and **ProhibitedPhrases** respectively.
* run the executable against video file (audio file will work too). 
	* Run it in the command line. E.g. `VideoSpeechCheck.exe <file to check>`
	* Drag-and-Drop a video file on `VideoSpeechCheck.exe`

## Used resources:
* [ffmpeg](https://ffmpeg.org/) to extract audio stream for speech to text process
* [Whisper.net](https://github.com/sandrohanea/whisper.net) (OpenAI Whisper) to transcribe speech to text
* [Diacritics.NET](https://github.com/thomasgalliker/Diacritics.NET) used to remove diacritics before phrases check
* [Fastenshtein](https://github.com/DanHarltey/Fastenshtein) used to compare phrases by [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) 

## Planned improvements
* some more user friendly UI