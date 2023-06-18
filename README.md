# Video Speech Check

## Functionalities:
* speech to text recognition and transcribe
* check existence of desired and prohibited phrased

## UI
### Command line application
* build/publish  **Visprech.Cmd** project
* run the built executable
* the **app.conf** file should be created with default configuration
* edit phrases in **app.conf**. Add phrases that are desired and/or prohibited
* run the executable against video file (audio fill will work too). Drag-and-Drop may be used

## Used resources:
* [ffmpeg](https://ffmpeg.org/) to extract audio stream for speech to text process
* [Whisper.net](https://github.com/sandrohanea/whisper.net) (OpenAI Whisper) to transcribe speech to text
* [Diacritics.NET](https://github.com/thomasgalliker/Diacritics.NET) used to remove diacritics before phrases check
* [Fastenshtein](https://github.com/DanHarltey/Fastenshtein) used to compare phrases by [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) 
