using System;
using System.Drawing;
using System.Speech.Synthesis;
using System.Windows.Forms;

namespace VoiceAssistant3
{
    public class SmileyForm : Form
    {
        private PictureBox smileyBox;
        private Timer animationTimer;
        private bool isSpeaking = false;
        private int frame = 0;
        private int clickCount = 0;
        private bool isSad = false;
        private bool isDead = false;
        private Timer closeTimer;
        private Timer teethTimer;
        private bool isShowingTeeth = false;
        private SpeechSynthesizer synth;

        public event EventHandler<string> OnSmileyEvent;

        public SmileyForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new System.Drawing.Size(150, 150);
            this.StartPosition = FormStartPosition.Manual;

            Screen screen = Screen.PrimaryScreen;
            this.Location = new Point(10, screen.WorkingArea.Height - this.Height - 10);

            smileyBox = new PictureBox();
            smileyBox.Dock = DockStyle.Fill;
            smileyBox.SizeMode = PictureBoxSizeMode.StretchImage;
            smileyBox.Image = CreateSmileyImage(false, false);
            smileyBox.Cursor = Cursors.Hand;
            this.Controls.Add(smileyBox);

            smileyBox.Click += SmileyBox_Click;

            animationTimer = new Timer();
            animationTimer.Interval = 200;
            animationTimer.Tick += AnimationTimer_Tick;

            closeTimer = new Timer();
            closeTimer.Interval = 3000;
            closeTimer.Tick += CloseTimer_Tick;

            synth = new SpeechSynthesizer();
            try { synth.SelectVoice("Microsoft Irina Desktop"); } catch { }
        }

        public void ShowTeeth()
        {
            if (isDead) return;
            isShowingTeeth = true;
            smileyBox.Image = CreateTeethImage();

            teethTimer = new Timer();
            teethTimer.Interval = 3000;
            teethTimer.Tick += (s, e) =>
            {
                teethTimer.Stop();
                isShowingTeeth = false;
                smileyBox.Image = CreateSmileyImage(false, isSad);
                Speak("Зачем тебе это?");
            };
            teethTimer.Start();
        }

        private void SmileyBox_Click(object sender, EventArgs e)
        {
            if (isDead) return;

            clickCount++;

            if (clickCount == 1)
            {
                Speak("Ай, больно!");
            }
            else if (clickCount == 3)
            {
                isSad = true;
                smileyBox.Image = CreateSmileyImage(false, true);
                Speak("Грустно мне...");
            }
            else if (clickCount == 5)
            {
                isDead = true;
                smileyBox.Image = CreateSmileyImage(true, isSad);
                Speak("Ты мне надоел!");
                OnSmileyEvent?.Invoke(this, "Вы плохой человек!");
                closeTimer.Start();
            }
            else
            {
                string[] phrases = { "Ой!", "Зачем?", "Не надо!", "Прекрати!" };
                Speak(phrases[clickCount % phrases.Length]);
            }
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            closeTimer.Stop();
            this.Close();
            Application.Exit();
        }

        private void Speak(string text)
        {
            synth.SpeakAsync(text);
            OnSmileyEvent?.Invoke(this, "Смайл сказал: " + text);
        }

        public void SetSpeaking(bool speaking)
        {
            if (isDead) return;
            isSpeaking = speaking;
            if (speaking)
                animationTimer.Start();
            else
            {
                animationTimer.Stop();
                UpdateSmiley(false, isSad);
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            frame++;
            UpdateSmiley(isSpeaking, isSad);
        }

        private void UpdateSmiley(bool isOpen, bool sad)
        {
            if (smileyBox.InvokeRequired)
                smileyBox.Invoke(new Action(() => smileyBox.Image = CreateSmileyImage(isOpen, sad)));
            else
                smileyBox.Image = CreateSmileyImage(isOpen, sad);
        }

        private Image CreateSmileyImage(bool speaking, bool sad)
        {
            Bitmap bmp = new Bitmap(130, 130);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Color faceColor = sad ? Color.OrangeRed : Color.Gold;
                g.FillEllipse(new SolidBrush(faceColor), 15, 15, 100, 100);

                if (sad)
                {
                    g.DrawArc(new Pen(Brushes.Black, 4), 30, 40, 18, 14, 180, 180);
                    g.DrawArc(new Pen(Brushes.Black, 4), 76, 40, 18, 14, 180, 180);
                }
                else
                {
                    g.FillEllipse(Brushes.Black, 38, 38, 12, 14);
                    g.FillEllipse(Brushes.Black, 74, 38, 12, 14);
                }

                if (speaking && !sad)
                {
                    g.FillEllipse(Brushes.Black, 44, 60, 34, 28);
                }
                else if (sad)
                {
                    g.DrawArc(new Pen(Brushes.Black, 4), 44, 60, 34, 28, 180, 180);
                }
                else
                {
                    g.DrawArc(new Pen(Brushes.Black, 4), 44, 50, 34, 28, 0, 180);
                }

                if (sad)
                {
                    g.FillEllipse(Brushes.LightBlue, 32, 52, 6, 10);
                    g.FillEllipse(Brushes.LightBlue, 84, 52, 6, 10);
                    g.DrawLine(new Pen(Brushes.LightBlue, 2), 35, 62, 33, 72);
                    g.DrawLine(new Pen(Brushes.LightBlue, 2), 87, 62, 89, 72);
                }
            }
            return bmp;
        }

        private Image CreateTeethImage()
        {
            Bitmap bmp = new Bitmap(130, 130);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Color faceColor = isSad ? Color.OrangeRed : Color.Gold;
                g.FillEllipse(new SolidBrush(faceColor), 15, 15, 100, 100);

                if (isSad)
                {
                    g.DrawArc(new Pen(Brushes.Black, 4), 30, 40, 18, 14, 180, 180);
                    g.DrawArc(new Pen(Brushes.Black, 4), 76, 40, 18, 14, 180, 180);
                }
                else
                {
                    g.FillEllipse(Brushes.Black, 38, 38, 12, 14);
                    g.FillEllipse(Brushes.Black, 74, 38, 12, 14);
                }

                g.FillRectangle(Brushes.Black, 44, 58, 34, 26);
                g.FillRectangle(Brushes.White, 48, 60, 6, 14);
                g.FillRectangle(Brushes.White, 56, 60, 6, 14);
                g.FillRectangle(Brushes.White, 64, 60, 6, 14);

                if (isSad)
                {
                    g.FillEllipse(Brushes.LightBlue, 32, 52, 6, 10);
                    g.FillEllipse(Brushes.LightBlue, 84, 52, 6, 10);
                    g.DrawLine(new Pen(Brushes.LightBlue, 2), 35, 62, 33, 72);
                    g.DrawLine(new Pen(Brushes.LightBlue, 2), 87, 62, 89, 72);
                }
            }
            return bmp;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            animationTimer.Stop();
            closeTimer.Stop();
            if (teethTimer != null) teethTimer.Stop();
            base.OnFormClosing(e);
        }
    }
}