using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Speech.Synthesis;
using System.Windows.Forms;
using NAudio.Wave;

namespace VoiceAssistant3
{
    public class Form1 : Form
    {
        // ----- ПЕРЕМЕННЫЕ -----
        private SpeechSynthesizer synth;
        private WaveInEvent waveIn;
        private MemoryStream audioStream;
        private RichTextBox txtLog;
        private Button btnStart;
        private NotifyIcon trayIcon;
        private SmileyForm smileyForm;
        private bool isRecording = false;
        private bool isStarted = false;
        private bool isBurryMode = false;

        // Словарь для хранения путей к программам
        private Dictionary<string, string> programPaths = new Dictionary<string, string>();

        private string whisperPath = @"whisper\whisper-cli.exe";
        private string modelPath = @"whisper\ggml-small.bin";
        private string audioPath = @"temp.wav";

        public Form1()
        {
            this.Text = "Голосовой командир";
            this.Size = new System.Drawing.Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Инициализация компонентов
            InitializeComponents();
            InitializeTray();
            InitializeSpeech();
            InitializeSmiley();

            // ---- Инициализация путей по умолчанию ----
            InitializeProgramPaths();

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
        }

        private void InitializeComponents()
        {
            // Создаем лог
            txtLog = new RichTextBox()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 10),
                ReadOnly = true
            };

            // Создаем кнопку
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

            // Панель для кнопки
            Panel bottomPanel = new Panel() { Dock = DockStyle.Bottom, Height = 45 };
            bottomPanel.Controls.Add(btnStart);

            // Добавляем элементы на форму
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

        private void InitializeProgramPaths()
        {
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

            programPaths["хром"] = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            programPaths["файрфокс"] = @"C:\Program Files\Mozilla Firefox\firefox.exe";
            programPaths["опера"] = @"C:\Program Files\Opera\launcher.exe";
            programPaths["брейв"] = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe";
            programPaths["эдж"] = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

            programPaths["дискорд"] = @"C:\Users\АЛЕХ\AppData\Local\Discord\Discord.exe";
            programPaths["зум"] = @"C:\Users\АЛЕХ\AppData\Roaming\Zoom\bin\Zoom.exe";
            programPaths["скайп"] = @"C:\Program Files (x86)\Microsoft\Skype for Desktop\Skype.exe";
            programPaths["телеграм"] = @"C:\Users\АЛЕХ\AppData\Roaming\Telegram Desktop\Telegram.exe";

            programPaths["влс"] = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
            programPaths["спотифай"] = @"C:\Users\АЛЕХ\AppData\Roaming\Spotify\Spotify.exe";
            programPaths["аудасити"] = @"C:\Program Files\Audacity\Audacity.exe";

            programPaths["гимп"] = @"C:\Program Files\GIMP 2\bin\gimp-2.10.exe";
            programPaths["пейнт"] = @"C:\Program Files\paint.net\paintdotnet.exe";
            programPaths["инкскейп"] = @"C:\Program Files\Inkscape\bin\inkscape.exe";

            programPaths["ворд"] = @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE";
            programPaths["эксель"] = @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE";
            programPaths["пауэрпоинт"] = @"C:\Program Files\Microsoft Office\root\Office16\POWERPNT.EXE";
            programPaths["либреофис"] = @"C:\Program Files\LibreOffice\program\soffice.exe";
            programPaths["фокс"] = @"C:\Program Files\Foxit Software\Foxit Reader\FoxitReader.exe";

            programPaths["зип"] = @"C:\Program Files\7-Zip\7zFM.exe";
            programPaths["блокнот++"] = @"C:\Program Files\Notepad++\notepad++.exe";
            programPaths["эвритинг"] = @"C:\Program Files\Everything\Everything.exe";
            programPaths["рево"] = @"C:\Program Files\VS Revo Group\Revo Uninstaller\RevoUninstaller.exe";
            programPaths["клин"] = @"C:\Program Files\CCleaner\CCleaner.exe";
            programPaths["глэри"] = @"C:\Program Files\Glary Utilities\GlaryUtilities.exe";
            programPaths["теракопи"] = @"C:\Program Files\TeraCopy\TeraCopy.exe";

            programPaths["вижуал студио код"] = @"C:\Users\АЛЕХ\AppData\Local\Programs\Microsoft VS Code\Code.exe";
            programPaths["пайтон"] = @"C:\Users\АЛЕХ\AppData\Local\Programs\Python\Python312\python.exe";
            programPaths["гит"] = @"C:\Program Files\Git\bin\git.exe";

            programPaths["гугл диск"] = @"C:\Program Files\Google\Drive File Stream\GoogleDriveFS.exe";
            programPaths["дропбокс"] = @"C:\Users\АЛЕХ\AppData\Roaming\Dropbox\bin\Dropbox.exe";

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

            // ---- ОТКРЫТИЕ ПРОГРАММ ----
            if (text.Contains("открой") || text.Contains("открыть"))
            {
                if (text.Contains("теардовн") || text.Contains("teardown")) OpenApp("теардовн", "Teardown");
                else if (text.Contains("легаси") || text.Contains("legacy")) OpenApp("легаси", "Legacy Launcher");
                else if (text.Contains("тимспик") || text.Contains("teamspeak")) OpenApp("тимспик", "TeamSpeak");
                else if (text.Contains("лаунчер") || text.Contains("launcher")) OpenApp("лаунчер", "TLauncher");
                else if (text.Contains("вижуал студио") || text.Contains("visual studio")) OpenApp("вижуал студио", "Visual Studio");
                else if (text.Contains("фотошоп") || text.Contains("photoshop")) OpenApp("фотошоп", "Photoshop");
                else if (text.Contains("премиер") || text.Contains("premiere")) OpenApp("премиер", "Premiere Pro");
                else if (text.Contains("афтер") || text.Contains("after effects")) OpenApp("афтер", "After Effects");
                else if (text.Contains("иллюстратор") || text.Contains("illustrator")) OpenApp("иллюстратор", "Illustrator");
                else if (text.Contains("индизайн") || text.Contains("indesign")) OpenApp("индизайн", "InDesign");
                else if (text.Contains("лайтрум") || text.Contains("lightroom")) OpenApp("лайтрум", "Lightroom");
                else if (text.Contains("акробат") || text.Contains("acrobat")) OpenApp("акробат", "Acrobat Reader");
                else if (text.Contains("аудишн") || text.Contains("audition")) OpenApp("аудишн", "Audition");
                else if (text.Contains("анимейт") || text.Contains("animate")) OpenApp("анимейт", "Animate");
                else if (text.Contains("дримвивер") || text.Contains("dreamweaver")) OpenApp("дримвивер", "Dreamweaver");
                else if (text.Contains("бридж") || text.Contains("bridge")) OpenApp("бридж", "Bridge");
                else if (text.Contains("медиа энкодер") || text.Contains("media encoder")) OpenApp("медиа энкодер", "Media Encoder");
                else if (text.Contains("хром") || text.Contains("chrome")) OpenApp("хром", "Chrome");
                else if (text.Contains("файрфокс") || text.Contains("firefox")) OpenApp("файрфокс", "Firefox");
                else if (text.Contains("опера") || text.Contains("opera")) OpenApp("опера", "Opera");
                else if (text.Contains("брейв") || text.Contains("brave")) OpenApp("брейв", "Brave");
                else if (text.Contains("эдж") || text.Contains("edge")) OpenApp("эдж", "Edge");
                else if (text.Contains("дискорд") || text.Contains("discord")) OpenApp("дискорд", "Discord");
                else if (text.Contains("зум") || text.Contains("zoom")) OpenApp("зум", "Zoom");
                else if (text.Contains("скайп") || text.Contains("skype")) OpenApp("скайп", "Skype");
                else if (text.Contains("телеграм") || text.Contains("telegram")) OpenApp("телеграм", "Telegram");
                else if (text.Contains("влс") || text.Contains("vlc")) OpenApp("влс", "VLC");
                else if (text.Contains("спотифай") || text.Contains("spotify")) OpenApp("спотифай", "Spotify");
                else if (text.Contains("аудасити") || text.Contains("audacity")) OpenApp("аудасити", "Audacity");
                else if (text.Contains("гимп") || text.Contains("gimp")) OpenApp("гимп", "GIMP");
                else if (text.Contains("пейнт") || text.Contains("paint.net")) OpenApp("пейнт", "Paint.NET");
                else if (text.Contains("инкскейп") || text.Contains("inkscape")) OpenApp("инкскейп", "Inkscape");
                else if (text.Contains("ворд") || text.Contains("word")) OpenApp("ворд", "Word");
                else if (text.Contains("эксель") || text.Contains("excel")) OpenApp("эксель", "Excel");
                else if (text.Contains("пауэрпоинт") || text.Contains("powerpoint") || text.Contains("презентация")) OpenApp("пауэрпоинт", "PowerPoint");
                else if (text.Contains("либреофис") || text.Contains("libreoffice")) OpenApp("либреофис", "LibreOffice");
                else if (text.Contains("фокс") || text.Contains("foxit")) OpenApp("фокс", "Foxit Reader");
                else if (text.Contains("блокнот") || text.Contains("notepad")) { Process.Start("notepad.exe"); Speak("Открываю блокнот"); return; }
                else if (text.Contains("зип") || text.Contains("7zip")) OpenApp("зип", "7-Zip");
                else if (text.Contains("блокнот++") || text.Contains("notepad++")) OpenApp("блокнот++", "Notepad++");
                else if (text.Contains("эвритинг") || text.Contains("everything")) OpenApp("эвритинг", "Everything");
                else if (text.Contains("рево") || text.Contains("revo")) OpenApp("рево", "Revo");
                else if (text.Contains("клин") || text.Contains("ccleaner")) OpenApp("клин", "CCleaner");
                else if (text.Contains("глэри") || text.Contains("glary")) OpenApp("глэри", "Glary");
                else if (text.Contains("теракопи") || text.Contains("teracopy")) OpenApp("теракопи", "TeraCopy");
                else if (text.Contains("вижуал студио код") || text.Contains("vs code")) OpenApp("вижуал студио код", "VS Code");
                else if (text.Contains("пайтон") || text.Contains("python")) OpenApp("пайтон", "Python");
                else if (text.Contains("гит") || text.Contains("git")) OpenApp("гит", "Git");
                else if (text.Contains("гугл диск") || text.Contains("google drive")) OpenApp("гугл диск", "Google Drive");
                else if (text.Contains("дропбокс") || text.Contains("dropbox")) OpenApp("дропбокс", "Dropbox");
                else if (text.Contains("стим") || text.Contains("steam")) OpenApp("стим", "Steam");
                else if (text.Contains("эпик") || text.Contains("epic")) OpenApp("эпик", "Epic Games");
                else if (text.Contains("торрент") || text.Contains("torrent")) OpenApp("торрент", "Torrent");
                else if (text.Contains("файлзилла") || text.Contains("filezilla")) OpenApp("файлзилла", "FileZilla");
                else if (text.Contains("винмердж") || text.Contains("winmerge")) OpenApp("винмердж", "WinMerge");
                else if (text.Contains("гриншот") || text.Contains("greenshot")) OpenApp("гриншот", "Greenshot");
                else if (text.Contains("инфранвью") || text.Contains("irfanview")) OpenApp("инфранвью", "IrfanView");
                else { Speak("Не знаю такую программу"); }
                return;
            }

            // Громкость
            if (text.Contains("громче") || text.Contains("увеличь громкость"))
            {
                Log("🔊 Увеличиваем громкость");
                Speak("Громче!");
                return;
            }

            if (text.Contains("тише") || text.Contains("уменьши громкость"))
            {
                Log("🔉 Уменьшаем громкость");
                Speak("Тише!");
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

        // ----- МЕТОД ДЛЯ ОТКРЫТИЯ ПРИЛОЖЕНИЙ -----
        private void OpenApp(string key, string name)
        {
            try
            {
                if (programPaths.ContainsKey(key))
                {
                    string path = programPaths[key];
                    if (File.Exists(path))
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
            }
        }

        // ----- ЗАПИСЬ АУДИО -----
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
            if (audioStream.Length > 0)
            {
                using (var writer = new WaveFileWriter(audioPath, new WaveFormat(16000, 1)))
                {
                    audioStream.Position = 0;
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

        // ----- РАСПОЗНАВАНИЕ -----
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
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Log($"📝 Распознано: {output}");
                        ProcessCommand(output.Trim());
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
        }

        // ----- ТРЕЙ -----
        private void InitializeTray()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                Text = "Голосовой командир",
                Visible = true
            };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Показать окно", null, (s, e) => ShowWindow());
            menu.Items.Add("Я картавый!", null, (s, e) => ToggleBurryMode());
            menu.Items.Add("Я пират!", null, (s, e) => ShowPirateWarning());
            menu.Items.Add("📸 Скриншот", null, (s, e) => TakeScreenshot());
            menu.Items.Add("Изменить путь к программе...", null, (s, e) => ChangeProgramPath());
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
                trayIcon.ShowBalloonTip(3000, "Голосовой командир", "Приложение свернуто в трей", ToolTipIcon.Info);
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
}