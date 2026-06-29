using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Speech.Synthesis;
using System.Windows.Forms;
using NAudio.Wave;
using WebRtcVadSharp;

namespace VoiceAssistant3
{
    public class Form1 : Form
    {
        // ----- ПЕРЕМЕННЫЕ -----
        private SpeechSynthesizer synth;
        private WaveInEvent waveIn;
        private WaveInEvent waveInContinuous;
        private MemoryStream audioStream;
        private MemoryStream speechBuffer;
        private RichTextBox txtLog;
        private Button btnStart;
        private NotifyIcon trayIcon;
        private SmileyForm smileyForm;
        private bool isRecording = false;
        private bool isStarted = false;
        private bool isBurryMode = false;
        private bool isAwake = false;
        private bool isRecordingSpeech = false;
        private int silenceCounter = 0;
        private const int SILENCE_LIMIT = 20;

        // VAD
        private WebRtcVad vad;
        private int sampleRate = 16000;

        // Словарь для хранения путей к программам
        private Dictionary<string, string> programPaths = new Dictionary<string, string>();

        // Словарь для игр Steam
        private Dictionary<string, string> steamApps = new Dictionary<string, string>();

        private string whisperPath = @"whisper\whisper-cli.exe";
        private string modelPath = @"whisper\ggml-small.bin";
        private string audioPath = @"temp.wav";
        private string triggerPath = @"trigger.wav";

        public Form1()
        {
            this.Text = "Голосовой командир v1.0";
            this.Size = new System.Drawing.Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Инициализация компонентов
            InitializeComponents();
            InitializeTray();
            InitializeSpeech();
            InitializeSmiley();
            InitializeVAD();

            // ---- Инициализация путей по умолчанию ----
            InitializeProgramPaths();
            InitializeSteamApps();

            // ---- Проверка наличия whisper-cli.exe ----
            if (!File.Exists(whisperPath))
            {
                Log($"❌ Файл не найден: {whisperPath}");
                Log("Скачайте whisper-cli.exe и модель в папку whisper/");
            }
            else if (!File.Exists(modelPath))
            {
                Log($"❌ Файл не найден: {modelPath}");
                Log("Скачайте модель ggml-small.bin в папку whisper/");
            }
            else
            {
                Log("✅ Whisper найден. Нажмите 'Старт' для записи.");
            }

            // Запускаем постоянное слушание
            StartContinuousListening();
        }

        private void InitializeComponents()
        {
            txtLog = new RichTextBox()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 10),
                ReadOnly = true
            };

            btnStart = new Button()
            {
                Text = "▶ Старт",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            Panel bottomPanel = new Panel() { Dock = DockStyle.Bottom, Height = 45 };
            bottomPanel.Controls.Add(btnStart);

            this.Controls.Add(txtLog);
            this.Controls.Add(bottomPanel);
        }

        private void InitializeSpeech()
        {
            synth = new SpeechSynthesizer();
            synth.Rate = 0;
            synth.Volume = 100;
        }

        private void InitializeSmiley()
        {
            smileyForm = new SmileyForm();
            smileyForm.OnSmileyEvent += (s, msg) => Log($"😊 {msg}");
            smileyForm.Show();
        }

        private void InitializeVAD()
        {
            vad = new WebRtcVad();
            // В WebRtcVadSharp 1.3.2 нет SetMode, режим задаётся в конструкторе
            // Используем агрессивность 2 (средняя)
            Log("✅ VAD инициализирован");
        }

        private void InitializeSteamApps()
        {
            steamApps["стандофф"] = "steam://rungameid/690";
            steamApps["standoff"] = "steam://rungameid/690";
            steamApps["кс"] = "steam://rungameid/730";
            steamApps["cs"] = "steam://rungameid/730";
            steamApps["контр страйк"] = "steam://rungameid/730";
            steamApps["дота"] = "steam://rungameid/570";
            steamApps["dota"] = "steam://rungameid/570";
            steamApps["майнкрафт"] = "steam://rungameid/2600";
            steamApps["minecraft"] = "steam://rungameid/2600";
            steamApps["террария"] = "steam://rungameid/105600";
            steamApps["terraria"] = "steam://rungameid/105600";
            steamApps["гарри мод"] = "steam://rungameid/4000";
            steamApps["gmod"] = "steam://rungameid/4000";
            steamApps["скайрим"] = "steam://rungameid/72850";
            steamApps["skyrim"] = "steam://rungameid/72850";
            steamApps["ведьмак"] = "steam://rungameid/292030";
            steamApps["witcher"] = "steam://rungameid/292030";
            steamApps["киберпанк"] = "steam://rungameid/1091500";
            steamApps["cyberpunk"] = "steam://rungameid/1091500";
            steamApps["гта"] = "steam://rungameid/271590";
            steamApps["gta"] = "steam://rungameid/271590";
            steamApps["гта 5"] = "steam://rungameid/271590";
            steamApps["ред дэд"] = "steam://rungameid/1174180";
            steamApps["rdr2"] = "steam://rungameid/1174180";
            steamApps["рейнбоу"] = "steam://rungameid/359550";
            steamApps["raimbow"] = "steam://rungameid/359550";
            steamApps["раст"] = "steam://rungameid/252490";
            steamApps["rust"] = "steam://rungameid/252490";
            steamApps["дейз"] = "steam://rungameid/221100";
            steamApps["dayz"] = "steam://rungameid/221100";
            steamApps["пабг"] = "steam://rungameid/578080";
            steamApps["pubg"] = "steam://rungameid/578080";
            steamApps["апекс"] = "steam://rungameid/1172470";
            steamApps["apex"] = "steam://rungameid/1172470";
            steamApps["фортнайт"] = "steam://rungameid/1107710";
            steamApps["fortnite"] = "steam://rungameid/1107710";
            steamApps["варфейс"] = "steam://rungameid/230410";
            steamApps["warface"] = "steam://rungameid/230410";
        }

        private void InitializeProgramPaths()
        {
            // ---- СТАРЫЕ ПРОГРАММЫ ----
            // Adobe
            programPaths["фотошоп"] = @"C:\Program Files\Adobe\Adobe Photoshop 2026\Photoshop.exe";
            programPaths["премиер"] = @"C:\Program Files\Adobe\Adobe Premiere Pro 2026\PremierePro.exe";
            programPaths["афтер"] = @"C:\Program Files\Adobe\Adobe After Effects 2026\AfterFX.exe";
            programPaths["иллюстратор"] = @"C:\Program Files\Adobe\Adobe Illustrator 2026\Illustrator.exe";
            programPaths["индизайн"] = @"C:\Program Files\Adobe\Adobe InDesign 2026\InDesign.exe";
            programPaths["лайтрум"] = @"C:\Program Files\Adobe\Adobe Lightroom\Lightroom.exe";
            programPaths["акробат"] = @"C:\Program Files\Adobe\Acrobat DC\Acrobat.exe";
            programPaths["аудишн"] = @"C:\Program Files\Adobe\Adobe Audition 2026\Audition.exe";
            programPaths["анимейт"] = @"C:\Program Files\Adobe\Adobe Animate 2026\Animate.exe";
            programPaths["дримвивер"] = @"C:\Program Files\Adobe\Adobe Dreamweaver 2026\Dreamweaver.exe";
            programPaths["бридж"] = @"C:\Program Files\Adobe\Adobe Bridge 2026\Bridge.exe";
            programPaths["медиа энкодер"] = @"C:\Program Files\Adobe\Adobe Media Encoder 2026\MediaEncoder.exe";

            // Браузеры
            programPaths["хром"] = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            programPaths["файрфокс"] = @"C:\Program Files\Mozilla Firefox\firefox.exe";
            programPaths["опера"] = @"C:\Program Files\Opera\launcher.exe";
            programPaths["брейв"] = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe";
            programPaths["эдж"] = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

            // Мессенджеры
            programPaths["дискорд"] = @"C:\Users\АЛЕХ\AppData\Local\Discord\Discord.exe";
            programPaths["зум"] = @"C:\Users\АЛЕХ\AppData\Roaming\Zoom\bin\Zoom.exe";
            programPaths["скайп"] = @"C:\Program Files (x86)\Microsoft\Skype for Desktop\Skype.exe";
            programPaths["телеграм"] = @"C:\Users\АЛЕХ\AppData\Roaming\Telegram Desktop\Telegram.exe";

            // Медиа
            programPaths["влс"] = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
            programPaths["спотифай"] = @"C:\Users\АЛЕХ\AppData\Roaming\Spotify\Spotify.exe";
            programPaths["аудасити"] = @"C:\Program Files\Audacity\Audacity.exe";

            // Графика
            programPaths["гимп"] = @"C:\Program Files\GIMP 2\bin\gimp-2.10.exe";
            programPaths["пейнт"] = @"C:\Program Files\paint.net\paintdotnet.exe";
            programPaths["инкскейп"] = @"C:\Program Files\Inkscape\bin\inkscape.exe";
            programPaths["блендер"] = @"C:\Program Files\Blender Foundation\Blender 4.2\blender.exe";
            programPaths["blender"] = @"C:\Program Files\Blender Foundation\Blender 4.2\blender.exe";

            // Стриминг
            programPaths["обс"] = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe";
            programPaths["obs"] = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe";

            // Обои
            programPaths["валлпапер"] = @"C:\Program Files\Wallpaper Engine\wallpaper64.exe";
            programPaths["wallpaper"] = @"C:\Program Files\Wallpaper Engine\wallpaper64.exe";
            programPaths["валлпапер енджин"] = @"C:\Program Files\Wallpaper Engine\wallpaper64.exe";

            // Офис
            programPaths["ворд"] = @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE";
            programPaths["эксель"] = @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE";
            programPaths["пауэрпоинт"] = @"C:\Program Files\Microsoft Office\root\Office16\POWERPNT.EXE";
            programPaths["либреофис"] = @"C:\Program Files\LibreOffice\program\soffice.exe";
            programPaths["фокс"] = @"C:\Program Files\Foxit Software\Foxit Reader\FoxitReader.exe";

            // Утилиты
            programPaths["зип"] = @"C:\Program Files\7-Zip\7zFM.exe";
            programPaths["блокнот++"] = @"C:\Program Files\Notepad++\notepad++.exe";
            programPaths["эвритинг"] = @"C:\Program Files\Everything\Everything.exe";
            programPaths["рево"] = @"C:\Program Files\VS Revo Group\Revo Uninstaller\RevoUninstaller.exe";
            programPaths["клин"] = @"C:\Program Files\CCleaner\CCleaner.exe";
            programPaths["глэри"] = @"C:\Program Files\Glary Utilities\GlaryUtilities.exe";
            programPaths["теракопи"] = @"C:\Program Files\TeraCopy\TeraCopy.exe";

            // Разработка
            programPaths["вижуал студио код"] = @"C:\Users\АЛЕХ\AppData\Local\Programs\Microsoft VS Code\Code.exe";
            programPaths["пайтон"] = @"C:\Users\АЛЕХ\AppData\Local\Programs\Python\Python312\python.exe";
            programPaths["гит"] = @"C:\Program Files\Git\bin\git.exe";

            // Облака
            programPaths["гугл диск"] = @"C:\Program Files\Google\Drive File Stream\GoogleDriveFS.exe";
            programPaths["дропбокс"] = @"C:\Users\АЛЕХ\AppData\Roaming\Dropbox\bin\Dropbox.exe";

            // Игры и лаунчеры
            programPaths["стим"] = @"C:\Program Files (x86)\Steam\steam.exe";
            programPaths["эпик"] = @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe";
            programPaths["торрент"] = @"C:\Program Files\qBittorrent\qbittorrent.exe";
            programPaths["файлзилла"] = @"C:\Program Files\FileZilla FTP Client\filezilla.exe";
            programPaths["винмердж"] = @"C:\Program Files\WinMerge\WinMerge.exe";
            programPaths["гриншот"] = @"C:\Program Files\Greenshot\Greenshot.exe";
            programPaths["инфранвью"] = @"C:\Program Files\IrfanView\i_view64.exe";
            programPaths["теардовн"] = @"F:\Teardown\teardown.exe";
            programPaths["легаси"] = @"C:\Users\АЛЕХ\AppData\Roaming\.tlauncher\legacy\Minecraft\LL.exe";
            programPaths["тимспик"] = @"C:\Users\АЛЕХ\AppData\Local\TeamSpeak 3 Client\ts3client_win64.exe";
            programPaths["лаунчер"] = @"C:\Users\АЛЕХ\AppData\Roaming\.tlauncher\TLauncher.exe";
            programPaths["вижуал студио"] = @"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe";

            // ---- САЙТЫ ----
            programPaths["ютуб"] = "https://www.youtube.com";
            programPaths["youtube"] = "https://www.youtube.com";
            programPaths["you tube"] = "https://www.youtube.com";
            programPaths["тикток"] = "https://www.tiktok.com";
            programPaths["tiktok"] = "https://www.tiktok.com";
            programPaths["тт"] = "https://www.tiktok.com";
            programPaths["вк"] = "https://vk.com";
            programPaths["vk"] = "https://vk.com";
            programPaths["вконтакте"] = "https://vk.com";
            programPaths["телеграм сайт"] = "https://web.telegram.org";
            programPaths["telegram сайт"] = "https://web.telegram.org";
            programPaths["тг сайт"] = "https://web.telegram.org";
            programPaths["гугл"] = "https://www.google.com";
            programPaths["google"] = "https://www.google.com";
            programPaths["яндекс"] = "https://ya.ru";
            programPaths["yandex"] = "https://ya.ru";
            programPaths["рамблер"] = "https://www.rambler.ru";
            programPaths["rambler"] = "https://www.rambler.ru";
            programPaths["новости"] = "https://news.yandex.ru";
            programPaths["news"] = "https://news.yandex.ru";
            programPaths["почта"] = "https://mail.ru";
            programPaths["mail"] = "https://mail.ru";
            programPaths["мейл"] = "https://mail.ru";
            programPaths["авито"] = "https://www.avito.ru";
            programPaths["avito"] = "https://www.avito.ru";
            programPaths["озон"] = "https://www.ozon.ru";
            programPaths["ozon"] = "https://www.ozon.ru";
            programPaths["вайлдберриз"] = "https://www.wildberries.ru";
            programPaths["wildberries"] = "https://www.wildberries.ru";
            programPaths["вб"] = "https://www.wildberries.ru";
            programPaths["алиэкспресс"] = "https://www.aliexpress.ru";
            programPaths["aliexpress"] = "https://www.aliexpress.ru";
            programPaths["али"] = "https://www.aliexpress.ru";
            programPaths["гитхаб"] = "https://github.com";
            programPaths["github"] = "https://github.com";
            programPaths["чатгпт"] = "https://chat.openai.com";
            programPaths["чат гпт"] = "https://chat.openai.com";
            programPaths["гпт"] = "https://chat.openai.com";
            programPaths["яндекс музыка"] = "https://music.yandex.ru";
            programPaths["yandex music"] = "https://music.yandex.ru";
            programPaths["музыка"] = "https://music.yandex.ru";
            programPaths["спотифай сайт"] = "https://open.spotify.com";
            programPaths["spotify сайт"] = "https://open.spotify.com";
            programPaths["кино"] = "https://www.kinopoisk.ru";
            programPaths["кинопоиск"] = "https://www.kinopoisk.ru";
            programPaths["kinopoisk"] = "https://www.kinopoisk.ru";
            programPaths["иви"] = "https://www.ivi.ru";
            programPaths["ivi"] = "https://www.ivi.ru";
            programPaths["окко"] = "https://okko.tv";
            programPaths["okko"] = "https://okko.tv";
            programPaths["нетфликс"] = "https://www.netflix.com";
            programPaths["netflix"] = "https://www.netflix.com";
            programPaths["твич"] = "https://www.twitch.tv";
            programPaths["twitch"] = "https://www.twitch.tv";
            programPaths["картинки"] = "https://yandex.ru/images";
            programPaths["яндекс картинки"] = "https://yandex.ru/images";
            programPaths["погода"] = "https://yandex.ru/pogoda";
            programPaths["weather"] = "https://yandex.ru/pogoda";
            programPaths["дуолинго"] = "https://www.duolingo.com";
            programPaths["duolingo"] = "https://www.duolingo.com";
            programPaths["гейтс"] = "https://www.gatesnotes.com";
            programPaths["gates"] = "https://www.gatesnotes.com";

            // ---- НОВЫЕ ПРОДЮСЕРСКИЕ / МОНТАЖНЫЕ / КОДЕРСКИЕ ----
            programPaths["да винчи"] = @"C:\Program Files\Blackmagic Design\DaVinci Resolve\Resolve.exe";
            programPaths["resolve"] = @"C:\Program Files\Blackmagic Design\DaVinci Resolve\Resolve.exe";
            programPaths["сонар"] = @"C:\Program Files\Vegas\VEGAS Pro 21.0\vegas210.exe";
            programPaths["vegas"] = @"C:\Program Files\Vegas\VEGAS Pro 21.0\vegas210.exe";
            programPaths["финал кат"] = @"C:\Program Files\Final Cut Pro\Final Cut Pro.exe";
            programPaths["final cut"] = @"C:\Program Files\Final Cut Pro\Final Cut Pro.exe";
            programPaths["фрути лупс"] = @"C:\Program Files\Image-Line\FL Studio 21\FL64.exe";
            programPaths["fl studio"] = @"C:\Program Files\Image-Line\FL Studio 21\FL64.exe";
            programPaths["аблетон"] = @"C:\Program Files\Ableton\Live 11\Program\Ableton Live 11.exe";
            programPaths["ableton"] = @"C:\Program Files\Ableton\Live 11\Program\Ableton Live 11.exe";
            programPaths["максимус"] = @"C:\Program Files\Autodesk\3ds Max 2026\3dsmax.exe";
            programPaths["3ds max"] = @"C:\Program Files\Autodesk\3ds Max 2026\3dsmax.exe";
            programPaths["майя"] = @"C:\Program Files\Autodesk\Maya2026\bin\maya.exe";
            programPaths["maya"] = @"C:\Program Files\Autodesk\Maya2026\bin\maya.exe";
            programPaths["райдер"] = @"C:\Program Files\JetBrains\Rider 2024.1\bin\rider64.exe";
            programPaths["rider"] = @"C:\Program Files\JetBrains\Rider 2024.1\bin\rider64.exe";
            programPaths["интелиж"] = @"C:\Program Files\JetBrains\IntelliJ IDEA 2024.1\bin\idea64.exe";
            programPaths["idea"] = @"C:\Program Files\JetBrains\IntelliJ IDEA 2024.1\bin\idea64.exe";
            programPaths["пичарм"] = @"C:\Program Files\JetBrains\PyCharm 2024.1\bin\pycharm64.exe";
            programPaths["pycharm"] = @"C:\Program Files\JetBrains\PyCharm 2024.1\bin\pycharm64.exe";
            programPaths["ноут"] = @"C:\Program Files\Notion\Notion.exe";
            programPaths["notion"] = @"C:\Program Files\Notion\Notion.exe";
            programPaths["фигма"] = @"C:\Program Files\Figma\Figma.exe";
            programPaths["figma"] = @"C:\Program Files\Figma\Figma.exe";
            programPaths["стримлабс"] = @"C:\Program Files\Streamlabs OBS\Streamlabs OBS.exe";
            programPaths["streamlabs"] = @"C:\Program Files\Streamlabs OBS\Streamlabs OBS.exe";
            programPaths["консоль"] = @"C:\Windows\System32\cmd.exe";
            programPaths["cmd"] = @"C:\Windows\System32\cmd.exe";
            programPaths["пауэршелл"] = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
            programPaths["powershell"] = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
        }

        // ----- МЕТОДЫ ЛОГИРОВАНИЯ И ОЗВУЧКИ -----
        private void Log(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.ScrollToCaret();
        }

        private void Speak(string text)
        {
            if (isBurryMode)
            {
                text = text.Replace("р", "л").Replace("Р", "Л");
            }
            synth.SpeakAsync(text);
            smileyForm?.SetSpeaking(true);
        }

        // ----- ОТКРЫТИЕ САЙТОВ -----
        private void OpenWebsite(string name, string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                Speak($"Открываю {name}!");
                Log($"🌐 Открыт сайт: {name} ({url})");
            }
            catch (Exception ex)
            {
                Speak($"Не удалось открыть {name}, бро!");
                Log($"❌ Ошибка открытия сайта {name}: {ex.Message}");
            }
        }

        // ----- ОТКРЫТИЕ STEAM ИГР -----
        private void OpenSteamApp(string gameName, string key)
        {
            try
            {
                if (steamApps.ContainsKey(key))
                {
                    string url = steamApps[key];
                    Process.Start(url);
                    Speak($"Открываю {gameName}!");
                    Log($"🎮 Запущена игра: {gameName} через Steam");
                }
                else
                {
                    Speak($"Не знаю такой игры в Steam, бро!");
                    Log($"❌ Игра {gameName} не найдена в словаре Steam");
                }
            }
            catch (Exception ex)
            {
                Speak($"Не удалось открыть {gameName}, бро!");
                Log($"❌ Ошибка запуска Steam игры {gameName}: {ex.Message}");
            }
        }

        // ----- МЕТОД ДЛЯ ОТКРЫТИЯ ПРИЛОЖЕНИЙ -----
        private void OpenApp(string key, string name)
        {
            try
            {
                if (programPaths.ContainsKey(key))
                {
                    string path = programPaths[key];
                    if (path.StartsWith("https://") || path.StartsWith("http://"))
                    {
                        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                        Speak($"Открываю {name}!");
                        Log($"🌐 Открыт сайт: {name} ({path})");
                    }
                    else if (File.Exists(path))
                    {
                        Process.Start(path);
                        Speak($"Открываю {name}");
                        Log($"📂 Открыт {name}: {path}");
                    }
                    else
                    {
                        Speak($"А хули не удалось открыть {name}?");
                        Log($"❌ Ошибка: файл не найден: {path}");
                    }
                }
                else
                {
                    Speak($"Не знаю, где находится {name}");
                    Log($"❌ Путь для {name} не задан");
                }
            }
            catch (Exception ex)
            {
                Speak($"А хули не удалось открыть {name}?");
                Log($"❌ Ошибка открытия {name}: {ex.Message}");
            }
        }

        // ----- УПРАВЛЕНИЕ МУЗЫКОЙ -----
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private void SendMediaKey(int keyCode)
        {
            const uint KEYEVENTF_EXTENDEDKEY = 0x1;
            const uint KEYEVENTF_KEYUP = 0x2;
            keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private void ControlMusic(string action)
        {
            try
            {
                switch (action)
                {
                    case "playpause":
                        SendMediaKey(0xB3);
                        Speak("Пауза!");
                        Log("🎵 Пауза/Воспроизведение");
                        break;
                    case "next":
                        SendMediaKey(0xB0);
                        Speak("Дальше!");
                        Log("⏭️ Следующий трек");
                        break;
                    case "prev":
                        SendMediaKey(0xB1);
                        Speak("Назад!");
                        Log("⏮️ Предыдущий трек");
                        break;
                    case "volup":
                        SendMediaKey(0xAF);
                        Speak("Громче!");
                        Log("🔊 Громче");
                        break;
                    case "voldown":
                        SendMediaKey(0xAE);
                        Speak("Тише!");
                        Log("🔉 Тише");
                        break;
                    default:
                        Speak("Не знаю такой команды, бро!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Speak("Не удалось управлять музыкой, бро!");
                Log($"❌ Ошибка управления музыкой: {ex.Message}");
            }
        }

        // ----- ПОСТОЯННОЕ СЛУШАНИЕ (VAD + ТРИГГЕР) -----
        private void StartContinuousListening()
        {
            try
            {
                waveInContinuous = new WaveInEvent();
                waveInContinuous.DeviceNumber = 0;
                waveInContinuous.WaveFormat = new WaveFormat(sampleRate, 1);
                waveInContinuous.DataAvailable += WaveInContinuous_DataAvailable;
                waveInContinuous.RecordingStopped += WaveInContinuous_RecordingStopped;
                waveInContinuous.StartRecording();
                Log("🔊 Постоянное слушание запущено. Скажи 'компьютер' чтобы активировать.");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка запуска постоянного слушания: {ex.Message}");
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД
        private bool IsSpeech(byte[] buffer, int bytesRecorded)
        {
            try
            {
                // Работает в ЛЮБОЙ версии WebRtcVadSharp
                var sampleRate = (WebRtcVadSharp.SampleRate)16000;
                var frameLength = (WebRtcVadSharp.FrameLength)30;

                return vad.HasSpeech(buffer, sampleRate, frameLength);
            }
            catch (Exception ex)
            {
                Log($"❌ VAD ошибка: {ex.Message}");
                return false;
            }
        }

        private void WaveInContinuous_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (isAwake) return;

            // Проверяем VAD
            if (IsSpeech(e.Buffer, e.BytesRecorded))
            {
                if (!isRecordingSpeech)
                {
                    isRecordingSpeech = true;
                    speechBuffer = new MemoryStream();
                    Log("🔊 Голос обнаружен...");
                }
                speechBuffer.Write(e.Buffer, 0, e.BytesRecorded);
                silenceCounter = 0;
            }
            else if (isRecordingSpeech)
            {
                silenceCounter++;
                if (silenceCounter > SILENCE_LIMIT)
                {
                    isRecordingSpeech = false;
                    silenceCounter = 0;

                    if (speechBuffer != null && speechBuffer.Length > 0)
                    {
                        speechBuffer.Position = 0;
                        using (var writer = new WaveFileWriter(triggerPath, new WaveFormat(sampleRate, 1)))
                        {
                            byte[] buffer = new byte[speechBuffer.Length];
                            speechBuffer.Read(buffer, 0, buffer.Length);
                            writer.Write(buffer, 0, buffer.Length);
                        }
                        speechBuffer.Dispose();
                        speechBuffer = null;
                        Log("🎤 Буфер сохранён, распознаём...");
                        RecognizeTrigger();
                    }
                }
            }
        }

        private void WaveInContinuous_RecordingStopped(object sender, StoppedEventArgs e)
        {
            Log("⏹ Постоянное слушание остановлено");
        }

        private void RecognizeTrigger()
        {
            try
            {
                if (!File.Exists(whisperPath) || !File.Exists(modelPath))
                {
                    Log("❌ Whisper не найден");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = whisperPath,
                    Arguments = $"-m \"{modelPath}\" -f \"{triggerPath}\" -l ru --no-gpu",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Log($"🔊 Услышал: {output}");

                        if (output.ToLower().Contains("компьютер") ||
                            output.ToLower().Contains("комп") ||
                            output.ToLower().Contains("computer"))
                        {
                            isAwake = true;
                            Speak("Слушаю, командир!");
                            smileyForm?.ShowTeeth();
                            StartRecording();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка распознавания триггера: {ex.Message}");
            }
        }

        // ----- ОСНОВНАЯ ЗАПИСЬ -----
        private void StartRecording()
        {
            try
            {
                waveIn = new WaveInEvent();
                waveIn.DeviceNumber = 0;
                waveIn.WaveFormat = new WaveFormat(16000, 1);
                audioStream = new MemoryStream();
                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;
                waveIn.StartRecording();
                Log("🎤 Запись команды...");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка записи: {ex.Message}");
            }
        }

        private void StopRecording()
        {
            try
            {
                waveIn?.StopRecording();
                waveIn?.Dispose();
                waveIn = null;
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка остановки: {ex.Message}");
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            audioStream.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (audioStream != null && audioStream.Length > 0)
            {
                audioStream.Position = 0;
                using (var writer = new WaveFileWriter(audioPath, new WaveFormat(16000, 1)))
                {
                    byte[] buffer = new byte[audioStream.Length];
                    audioStream.Read(buffer, 0, buffer.Length);
                    writer.Write(buffer, 0, buffer.Length);
                }
                Log($"🎵 Аудио сохранено: {audioPath}");
                RecognizeSpeech();
            }
            audioStream?.Dispose();
            audioStream = null;
        }

        // ----- РАСПОЗНАВАНИЕ КОМАНД -----
        private void RecognizeSpeech()
        {
            try
            {
                if (!File.Exists(whisperPath) || !File.Exists(modelPath))
                {
                    Log("❌ Whisper не найден");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = whisperPath,
                    Arguments = $"-m \"{modelPath}\" -f \"{audioPath}\" -l ru --no-gpu",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Log($"📝 Распознано: {output}");
                        ProcessCommand(output);
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
                        Log($"❌ Ошибка Whisper: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка распознавания: {ex.Message}");
            }

            // Возврат в режим ожидания
            isAwake = false;
            smileyForm?.SetSpeaking(false);
            Log("⏳ Режим ожидания...");
        }

        // ----- ОБРАБОТКА КОМАНД -----
        private void ProcessCommand(string text)
        {
            Log($"Распознано: {text}");

            // Активация
            if (text.Contains("активация") || text.Contains("активируйся"))
            {
                Speak("Слушаю, командир!");
                isStarted = true;
                smileyForm?.ShowTeeth();
                return;
            }

            // Выключение
            if (text.Contains("выключись") || text.Contains("отключись") || text.Contains("спи") || text.Contains("замолчи"))
            {
                Speak("А хули, отключаюсь");
                isStarted = false;
                smileyForm?.SetSpeaking(false);
                return;
            }

            // Время
            if (text.Contains("время") || text.Contains("сколько времени"))
            {
                Speak($"Сейчас {DateTime.Now:HH:mm}");
                return;
            }

            // День
            if (text.Contains("день") || text.Contains("дата") || text.Contains("число"))
            {
                Speak($"Сегодня {DateTime.Now:dd MMMM yyyy}");
                return;
            }

            // Скриншот
            if (text.Contains("скриншот") || text.Contains("скрин") || text.Contains("снимок"))
            {
                TakeScreenshot();
                return;
            }

            // Режим картавости
            if (text.Contains("я картавый") || text.Contains("я шепелявый"))
            {
                ToggleBurryMode();
                return;
            }

            // Зубы
            if (text.Contains("зубы") || text.Contains("покажи зубы"))
            {
                smileyForm?.ShowTeeth();
                Speak("На, смотри!");
                return;
            }

            // Список игр
            if (text.Contains("игры") || text.Contains("какие игры"))
            {
                string gameList = "Я знаю эти игры: ";
                foreach (var game in steamApps.Keys)
                {
                    gameList += game + ", ";
                }
                Speak(gameList);
                Log($"📋 Список игр: {gameList}");
                return;
            }

            // ----- ПОДДЕРЖИ СОЗДАТЕЛЯ (ДОНАТ) -----
            if (text.Contains("поддержи создателя") || text.Contains("донат") || text.Contains("поддержать") || text.Contains("кофе"))
            {
                string donateUrl = "https://www.donationalerts.com/r/olegplay9909";
                Process.Start(new ProcessStartInfo { FileName = donateUrl, UseShellExecute = true });
                Speak("Открываю страницу поддержки, бро! Спасибо!");
                Log("💸 Открыта страница доната: https://www.donationalerts.com/r/olegplay9909");
                return;
            }

            // ----- УПРАВЛЕНИЕ МУЗЫКОЙ -----
            if (text.Contains("пауза") || text.Contains("стоп") || text.Contains("останови"))
            {
                ControlMusic("playpause");
                return;
            }
            if (text.Contains("дальше") || text.Contains("следующий") || text.Contains("вперед") || text.Contains("next"))
            {
                ControlMusic("next");
                return;
            }
            if (text.Contains("назад") || text.Contains("предыдущий") || text.Contains("прошлый"))
            {
                ControlMusic("prev");
                return;
            }
            if (text.Contains("включи музыку") || (text.Contains("музыка") && text.Contains("включи")))
            {
                ControlMusic("playpause");
                return;
            }
            if ((text.Contains("громче") || text.Contains("увеличь громкость")) && !text.Contains("комп") && !text.Contains("компьютер"))
            {
                ControlMusic("volup");
                return;
            }
            if ((text.Contains("тише") || text.Contains("уменьши громкость")) && !text.Contains("комп") && !text.Contains("компьютер"))
            {
                ControlMusic("voldown");
                return;
            }

            // ---- ОТКРЫТИЕ ПРОГРАММ, ИГР И САЙТОВ ----
            if (text.Contains("открой") || text.Contains("открыть"))
            {
                string lowerText = text.ToLower();

                // ----- ИГРЫ STEAM -----
                if (lowerText.Contains("стандофф") || lowerText.Contains("standoff"))
                {
                    OpenSteamApp("Standoff 2", "стандофф");
                    return;
                }
                else if (lowerText.Contains("кс") || lowerText.Contains("cs") || lowerText.Contains("контр страйк"))
                {
                    OpenSteamApp("Counter-Strike 2", "кс");
                    return;
                }
                else if (lowerText.Contains("дота") || lowerText.Contains("dota"))
                {
                    OpenSteamApp("Dota 2", "дота");
                    return;
                }
                else if (lowerText.Contains("майнкрафт") || lowerText.Contains("minecraft"))
                {
                    OpenSteamApp("Minecraft", "майнкрафт");
                    return;
                }
                else if (lowerText.Contains("гта") || lowerText.Contains("gta"))
                {
                    OpenSteamApp("GTA V", "гта");
                    return;
                }
                else if (lowerText.Contains("киберпанк") || lowerText.Contains("cyberpunk"))
                {
                    OpenSteamApp("Cyberpunk 2077", "киберпанк");
                    return;
                }
                else if (lowerText.Contains("ведьмак") || lowerText.Contains("witcher"))
                {
                    OpenSteamApp("The Witcher 3", "ведьмак");
                    return;
                }
                else if (lowerText.Contains("раст") || lowerText.Contains("rust"))
                {
                    OpenSteamApp("Rust", "раст");
                    return;
                }
                else if (lowerText.Contains("пабг") || lowerText.Contains("pubg"))
                {
                    OpenSteamApp("PUBG", "пабг");
                    return;
                }
                else if (lowerText.Contains("фортнайт") || lowerText.Contains("fortnite"))
                {
                    OpenSteamApp("Fortnite", "фортнайт");
                    return;
                }
                else if (lowerText.Contains("апекс") || lowerText.Contains("apex"))
                {
                    OpenSteamApp("Apex Legends", "апекс");
                    return;
                }

                // ----- САЙТЫ -----
                else if (lowerText.Contains("ютуб") || lowerText.Contains("youtube") || lowerText.Contains("you tube"))
                {
                    OpenWebsite("YouTube", "https://www.youtube.com");
                    return;
                }
                else if (lowerText.Contains("тикток") || lowerText.Contains("tiktok") || lowerText.Contains("тт"))
                {
                    OpenWebsite("TikTok", "https://www.tiktok.com");
                    return;
                }
                else if (lowerText.Contains("вк") || lowerText.Contains("вконтакте") || lowerText.Contains("vk"))
                {
                    OpenWebsite("ВКонтакте", "https://vk.com");
                    return;
                }
                else if (lowerText.Contains("гугл") || lowerText.Contains("google"))
                {
                    OpenWebsite("Google", "https://www.google.com");
                    return;
                }
                else if (lowerText.Contains("яндекс") || lowerText.Contains("yandex"))
                {
                    OpenWebsite("Яндекс", "https://ya.ru");
                    return;
                }
                else if (lowerText.Contains("чатгпт") || lowerText.Contains("чат гпт") || lowerText.Contains("гпт"))
                {
                    OpenWebsite("ChatGPT", "https://chat.openai.com");
                    return;
                }
                else if (lowerText.Contains("гитхаб") || lowerText.Contains("github"))
                {
                    OpenWebsite("GitHub", "https://github.com");
                    return;
                }
                else if (lowerText.Contains("озон") || lowerText.Contains("ozon"))
                {
                    OpenWebsite("Ozon", "https://www.ozon.ru");
                    return;
                }
                else if (lowerText.Contains("вайлдберриз") || lowerText.Contains("wildberries") || lowerText.Contains("вб"))
                {
                    OpenWebsite("Wildberries", "https://www.wildberries.ru");
                    return;
                }
                else if (lowerText.Contains("авито") || lowerText.Contains("avito"))
                {
                    OpenWebsite("Avito", "https://www.avito.ru");
                    return;
                }
                else if (lowerText.Contains("кинопоиск") || lowerText.Contains("кино"))
                {
                    OpenWebsite("Кинопоиск", "https://www.kinopoisk.ru");
                    return;
                }
                else if (lowerText.Contains("нетфликс") || lowerText.Contains("netflix"))
                {
                    OpenWebsite("Netflix", "https://www.netflix.com");
                    return;
                }
                else if (lowerText.Contains("твич") || lowerText.Contains("twitch"))
                {
                    OpenWebsite("Twitch", "https://www.twitch.tv");
                    return;
                }
                else if (lowerText.Contains("погода") || lowerText.Contains("weather"))
                {
                    OpenWebsite("Погода", "https://yandex.ru/pogoda");
                    return;
                }

                // ----- ПРОГРАММЫ -----
                else if (lowerText.Contains("фотошоп") || lowerText.Contains("photoshop")) { OpenApp("фотошоп", "Photoshop"); return; }
                else if (lowerText.Contains("премиер") || lowerText.Contains("premiere")) { OpenApp("премиер", "Premiere Pro"); return; }
                else if (lowerText.Contains("афтер") || lowerText.Contains("after effects")) { OpenApp("афтер", "After Effects"); return; }
                else if (lowerText.Contains("хром") || lowerText.Contains("chrome")) { OpenApp("хром", "Chrome"); return; }
                else if (lowerText.Contains("дискорд") || lowerText.Contains("discord")) { OpenApp("дискорд", "Discord"); return; }
                else if (lowerText.Contains("телеграм") || lowerText.Contains("telegram")) { OpenApp("телеграм", "Telegram"); return; }
                else if (lowerText.Contains("влс") || lowerText.Contains("vlc")) { OpenApp("влс", "VLC"); return; }
                else if (lowerText.Contains("спотифай") || lowerText.Contains("spotify")) { OpenApp("спотифай", "Spotify"); return; }
                else if (lowerText.Contains("блендер") || lowerText.Contains("blender")) { OpenApp("блендер", "Blender"); return; }
                else if (lowerText.Contains("обс") || lowerText.Contains("obs")) { OpenApp("обс", "OBS Studio"); return; }
                else if (lowerText.Contains("валлпапер") || lowerText.Contains("wallpaper")) { OpenApp("валлпапер", "Wallpaper Engine"); return; }
                else if (lowerText.Contains("ворд") || lowerText.Contains("word")) { OpenApp("ворд", "Word"); return; }
                else if (lowerText.Contains("эксель") || lowerText.Contains("excel")) { OpenApp("эксель", "Excel"); return; }
                else if (lowerText.Contains("стим") || lowerText.Contains("steam")) { Process.Start("steam://open/games"); Speak("Открываю Steam!"); return; }
                else if (lowerText.Contains("да винчи") || lowerText.Contains("resolve")) { OpenApp("да винчи", "DaVinci Resolve"); return; }
                else if (lowerText.Contains("сонар") || lowerText.Contains("vegas")) { OpenApp("сонар", "VEGAS Pro"); return; }
                else if (lowerText.Contains("фрути лупс") || lowerText.Contains("fl studio")) { OpenApp("фрути лупс", "FL Studio"); return; }
                else if (lowerText.Contains("аблетон") || lowerText.Contains("ableton")) { OpenApp("аблетон", "Ableton Live"); return; }
                else if (lowerText.Contains("максимус") || lowerText.Contains("3ds max")) { OpenApp("максимус", "3ds Max"); return; }
                else if (lowerText.Contains("майя") || lowerText.Contains("maya")) { OpenApp("майя", "Maya"); return; }
                else if (lowerText.Contains("райдер") || lowerText.Contains("rider")) { OpenApp("райдер", "Rider"); return; }
                else if (lowerText.Contains("интелиж") || lowerText.Contains("idea")) { OpenApp("интелиж", "IntelliJ IDEA"); return; }
                else if (lowerText.Contains("пичарм") || lowerText.Contains("pycharm")) { OpenApp("пичарм", "PyCharm"); return; }
                else if (lowerText.Contains("фигма") || lowerText.Contains("figma")) { OpenApp("фигма", "Figma"); return; }
                else if (lowerText.Contains("ноут") || lowerText.Contains("notion")) { OpenApp("ноут", "Notion"); return; }
                else if (lowerText.Contains("стримлабс") || lowerText.Contains("streamlabs")) { OpenApp("стримлабс", "Streamlabs OBS"); return; }
                else if (lowerText.Contains("консоль") || lowerText.Contains("cmd")) { OpenApp("консоль", "Командная строка"); return; }
                else if (lowerText.Contains("пауэршелл") || lowerText.Contains("powershell")) { OpenApp("пауэршелл", "PowerShell"); return; }
                else if (lowerText.Contains("блокнот") || lowerText.Contains("notepad")) { Process.Start("notepad.exe"); Speak("Открываю блокнот"); return; }
                else { Speak("Не знаю такую программу, бро!"); }
                return;
            }

            // Выключение компьютера
            if (text.Contains("выключи компьютер") || text.Contains("выключи комп"))
            {
                Speak("Выключаю компьютер через 10 секунд!");
                var psi = new ProcessStartInfo("shutdown", "/s /t 10");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
                return;
            }
        }

        // ----- МЕТОД ДЛЯ ИЗМЕНЕНИЯ ПУТИ -----
        private void ChangeProgramPath()
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "Изменить путь к программе";
                inputForm.Size = new Size(400, 150);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                Label label = new Label()
                {
                    Text = "Введите название программы (например, фотошоп, стим, хром):",
                    Location = new Point(10, 10),
                    Size = new Size(370, 30),
                    AutoSize = false
                };

                TextBox textBox = new TextBox()
                {
                    Location = new Point(10, 45),
                    Size = new Size(260, 20)
                };

                Button okButton = new Button()
                {
                    Text = "OK",
                    Location = new Point(280, 43),
                    Size = new Size(100, 25),
                    DialogResult = DialogResult.OK
                };

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(okButton);
                inputForm.AcceptButton = okButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string programName = textBox.Text.ToLower().Trim();
                    if (string.IsNullOrEmpty(programName)) return;

                    if (!programPaths.ContainsKey(programName))
                    {
                        MessageBox.Show($"Программа '{programName}' не найдена в списке. Добавьте её вручную в код.",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    using (var dialog = new OpenFileDialog())
                    {
                        dialog.Title = $"Выберите исполняемый файл для {programName}";
                        dialog.Filter = "Исполняемые файлы (*.exe)|*.exe";
                        if (File.Exists(programPaths[programName]))
                        {
                            dialog.InitialDirectory = Path.GetDirectoryName(programPaths[programName]);
                        }

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            programPaths[programName] = dialog.FileName;
                            Log($"📁 Путь к {programName} изменён: {dialog.FileName}");
                            MessageBox.Show($"Путь к {programName} изменён!\nНовый путь: {dialog.FileName}",
                                "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        // ----- ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ -----
        private void TakeScreenshot()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }
                    string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                    bitmap.Save(fileName, ImageFormat.Png);
                    Log($"📸 Скриншот сохранён: {fileName}");
                    Speak("Скриншот сделан");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка создания скриншота: {ex.Message}");
                Speak("Не удалось сделать скриншот");
            }
        }

        private void ToggleBurryMode()
        {
            isBurryMode = !isBurryMode;
            Speak(isBurryMode ? "Я тепель калтавый" : "Я больше не калтавый");
            Log($"Режим картавости: {(isBurryMode ? "ВКЛ" : "ВЫКЛ")}");
        }

        private void ShowPirateWarning()
        {
            Speak("АХТУНГ! ВАС ПОЙМАЮТ!");
            Log("🏴‍☠️ Пиратское предупреждение!");
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void ExitApp()
        {
            synth?.Dispose();
            smileyForm?.Close();
            trayIcon?.Dispose();
            Application.Exit();
        }

        // ----- КНОПКА СТАРТ -----
        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!isStarted)
            {
                isStarted = true;
                btnStart.Text = "⏹ Стоп";
                btnStart.BackColor = Color.Red;
                Log("🎤 Запись запущена...");
                StartRecording();
            }
            else
            {
                isStarted = false;
                btnStart.Text = "▶ Старт";
                btnStart.BackColor = Color.Green;
                Log("⏹ Запись остановлена");
                StopRecording();
                isAwake = false;
            }
        }

        // ----- ТРЕЙ -----
        private void InitializeTray()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                Text = "Голосовой командир v0.94",
                Visible = true
            };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Показать окно", null, (s, e) => ShowWindow());
            menu.Items.Add("Я картавый!", null, (s, e) => ToggleBurryMode());
            menu.Items.Add("Я пират!", null, (s, e) => ShowPirateWarning());
            menu.Items.Add("📸 Скриншот", null, (s, e) => TakeScreenshot());
            menu.Items.Add("Изменить путь к программе...", null, (s, e) => ChangeProgramPath());
            menu.Items.Add("💸 Поддержать создателя", null, (s, e) =>
            {
                Process.Start("https://www.donationalerts.com/r/olegplay9909");
                Log("💸 Открыта страница доната");
            });
            menu.Items.Add("Выход", null, (s, e) => ExitApp());
            trayIcon.ContextMenuStrip = menu;

            trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        // ----- ЗАКРЫТИЕ ФОРМЫ -----
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(3000, "Голосовой командир v0.94", "Приложение свернуто в трей", ToolTipIcon.Info);
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
}