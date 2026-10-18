using Hacknet;
using Microsoft.Xna.Framework;
using System;
namespace RedisBreaker
{

    /*
     * Thanks for Err0r
     * Thanks for April_Crystal
     */


    public class RedisBreakerExe : Pathfinder.Executable.BaseExecutable
    {
        private Computer targetComputer;
        private int netPort;

        // 动画状态变量
        private float lifetime = 0f;
        private const float DURATION = 25f; // 延长到25秒
        private const float COMPLETION_DELAY = 3f; // 完成后保留3秒
        private float completionTimer = 0f;
        private bool isCompleted = false;

        // 二进制动画相关变量
        private string binary;
        private int binaryIndex = 0;
        private float binaryScrollTimer = 0f;
        private const float SCROLL_RATE = 0.05f; // 更快的滚动速度
        private int binaryCharsPerLine = 0;
        private int binaryLines = 0;
        private int binaryChars = 0;

        private string licenseKey = "";
        private const int KEY_LENGTH = 12; // 密钥长度
        private const int KEY_GROUP_SIZE = 4; // 每4个字符一组
        private const string KEY_PREFIX = "Key: ";

        public RedisBreakerExe(Rectangle location, OS operatingSystem, string[] args) : base(location, operatingSystem, args)
        {
            this.ramCost = 140; // 控制在150以内
            this.IdentifierName = "RedisBreaker";
        }

        public override void LoadContent()
        {
            base.LoadContent();
            targetComputer = (os.connectedComp != null) ? os.connectedComp : os.thisComputer;
            if (targetComputer == null)
            {
                os.write("ERROR: Target computer not found!");
                needsRemoval = true;
                return;
            }
            netPort = targetComputer.GetDisplayPortNumberFromCodePort(6379);
            if (Args.Length < 2 || !Int32.TryParse(Args[1], out int port) || port != netPort)
            {
                os.write("ERROR FOUND!Target Port is Closed or Missing!");
                needsRemoval = true;
                return;
            }
            GenerateLicenseKey();
            // 初始化二进制动画
            binary = Computer.generateBinaryString(1024);
            binaryCharsPerLine = (bounds.Width - 4) / 8; // 每行字符数
            binaryLines = (bounds.Height - 60) / 12; // 行数
            binaryChars = binaryCharsPerLine * binaryLines;
        }

        private void GenerateLicenseKey()
        {
            // 生成随机密钥（字母和数字混合）
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
            var random = new Random();

            // 生成随机字符序列
            char[] keyChars = new char[KEY_LENGTH];
            for (int i = 0; i < KEY_LENGTH; i++)
            {
                keyChars[i] = chars[random.Next(chars.Length)];
            }

            // 添加分组空格
            licenseKey = "";
            for (int i = 0; i < KEY_LENGTH; i++)
            {
                if (i > 0 && i % KEY_GROUP_SIZE == 0)
                {
                    licenseKey += " ";
                }
                licenseKey += keyChars[i];
            }
        }

        public override void Update(float t)
        {
            base.Update(t);

            if (isCompleted)
            {
                // 完成状态计时
                completionTimer -= t;
                if (completionTimer <= 0f)
                {
                    isExiting = true;
                }
                return;
            }

            lifetime += t;

            // 更新二进制滚动动画
            binaryScrollTimer += t;
            if (binaryScrollTimer >= SCROLL_RATE)
            {
                binaryIndex = (binaryIndex + 1) % binary.Length;
                binaryScrollTimer = 0f;
            }

            if (lifetime >= DURATION && !isCompleted)
            {
                // 破解完成
                int targetPort = targetComputer.GetDisplayPortNumberFromCodePort(6379);
                targetComputer.openPort(targetPort, os.thisComputer.ip);
                isCompleted = true;
                completionTimer = COMPLETION_DELAY;
            }
        }

        public override void Draw(float t)
        {
            base.Draw(t);
            drawTarget();
            drawOutline();

            // 获取当前RAM栏的位置和尺寸
            Rectangle ramRect = bounds;
            ramRect.Width -= 2;
            ramRect.Height -= 2;

            // 绘制二进制滚动背景 - 向下偏移20像素
            Vector2 textPos = new Vector2(ramRect.X + 2, ramRect.Y + 22); // 向下挪20像素
            for (int i = 0; i < binaryLines; i++)
            {
                for (int j = 0; j < binaryCharsPerLine; j++)
                {
                    int charIndex = (binaryIndex + j + i * binaryCharsPerLine) % (binary.Length - 1);
                    Color charColor = Color.Lerp(Color.DarkSlateGray, Color.DarkGreen,
                                                (float)Math.Sin(lifetime * 2f + i * 0.3f + j * 0.1f) * 0.5f + 0.5f);

                    spriteBatch.DrawString(GuiData.UITinyfont,
                                          binary[charIndex].ToString(),
                                          textPos,
                                          charColor);
                    textPos.X += 8f;
                }
                textPos.Y += 12f;
                textPos.X = ramRect.X + 2;

                // 如果超出边界则停止绘制
                if (textPos.Y > ramRect.Y + ramRect.Height - 70) break;
            }

            // 计算破解进度 (0-1)
            float progress = Math.Min(1f, lifetime / DURATION);

            // 在进度条上方绘制密钥破解动画 - 位置调整到进度条正上方
            string fullKeyText = KEY_PREFIX + licenseKey; // 添加Key:前缀
            Vector2 keyPos = new Vector2(
                ramRect.X + (ramRect.Width - GuiData.UISmallfont.MeasureString(fullKeyText).X) / 2, // 水平居中
                ramRect.Y + ramRect.Height - 60 // 进度条上方位置
            );

            // 完成状态显示
            if (isCompleted)
            {
                // 绘制绿色横幅
                Rectangle bannerRect = new Rectangle(
                    ramRect.X,
                    ramRect.Y + ramRect.Height / 3,
                    ramRect.Width,
                    40
                );
                spriteBatch.Draw(Utils.white, bannerRect, Color.LimeGreen * 0.9f);

                // 绘制横幅文字
                string bannerText = "Port Access!";
                Vector2 bannerSize = GuiData.font.MeasureString(bannerText);
                Vector2 bannerPos = new Vector2(
                    ramRect.X + (ramRect.Width - bannerSize.X) / 2,
                    ramRect.Y + ramRect.Height / 3 + (40 - bannerSize.Y) / 2
                );
                spriteBatch.DrawString(GuiData.font, bannerText, bannerPos, Color.White);

                // 显示完整密钥
                Vector2 fullKeyPos = new Vector2(
                    ramRect.X + (ramRect.Width - GuiData.UISmallfont.MeasureString(fullKeyText).X) / 2,
                    bannerRect.Bottom + 5
                );
                spriteBatch.DrawString(GuiData.UISmallfont, fullKeyText, fullKeyPos, Color.LimeGreen);

                // 显示完成消息
                string completeText = "Redit Port Opened!";
                Vector2 completePos = new Vector2(
                    ramRect.X + (ramRect.Width - GuiData.smallfont.MeasureString(completeText).X) / 2,
                    fullKeyPos.Y + 15
                );
                spriteBatch.DrawString(GuiData.smallfont, completeText, completePos, Color.Cyan);


            }
            else
            {
                // 未完成状态显示进度条和其他元素
                // 绘制进度条背景
                Rectangle progressBg = new Rectangle(
                    ramRect.X + 2,
                    ramRect.Y + ramRect.Height - 30,
                    ramRect.Width - 4,
                    20
                );
                GuiData.spriteBatch.Draw(Utils.white, progressBg, Color.Black * 0.8f);

                // 绘制进度条
                Rectangle progressBar = new Rectangle(
                    progressBg.X,
                    progressBg.Y,
                    (int)(progressBg.Width * progress),
                    progressBg.Height
                );

                // 渐变色进度条（红->黄->绿）
                Color progressColor = Color.Lerp(
                    Color.Red,
                    Color.Lerp(Color.Yellow, Color.Green, progress * 2f),
                    progress
                );
                GuiData.spriteBatch.Draw(Utils.white, progressBar, progressColor);

                // 绘制端口信息和进度文本
                Vector2 infoPos = new Vector2(ramRect.X + 5, ramRect.Y + ramRect.Height - 28);
                string portText = $"REDIT-6379 : {(int)(progress * 100)}%";
                spriteBatch.DrawString(GuiData.UISmallfont, portText, infoPos, Color.Cyan);

                // 在进度条上方绘制密钥破解动画
                if (progress > 0.10f)
                {
                    // 始终显示密钥前缀
                    string prefix = KEY_PREFIX;
                    Vector2 prefixPos = keyPos;
                    spriteBatch.DrawString(
                        GuiData.UISmallfont,
                        prefix,
                        prefixPos,
                        Color.Lerp(Color.Yellow, Color.LimeGreen, progress)
                    );

                    // 计算密钥显示长度（从0开始，最多显示完整密钥）
                    int keysToShow = Math.Min(
                        licenseKey.Length,
                        (int)((progress - 0.2f) / 0.75f * licenseKey.Length)
                    );

                    // 确保显示完整的12位密钥（包括空格）
                    keysToShow = Math.Min(keysToShow, licenseKey.Length);

                    string partialLicense = licenseKey.Substring(0, keysToShow);

                    // 计算密钥部分的位置（紧跟前缀）
                    Vector2 licensePos = prefixPos + new Vector2(
                        GuiData.UISmallfont.MeasureString(prefix).X,
                        0
                    );

                    // 绘制密钥文本
                    spriteBatch.DrawString(
                        GuiData.UISmallfont,
                        partialLicense,
                        licensePos,
                        Color.Lerp(Color.Yellow, Color.LimeGreen, progress)
                    );

                    // 绘制闪烁的光标
                    if ((int)(lifetime * 5) % 2 == 0) // 每0.2秒闪烁一次
                    {
                        Vector2 cursorPos = licensePos + new Vector2(
                            GuiData.UISmallfont.MeasureString(partialLicense).X,
                            0
                        );

                        spriteBatch.DrawString(
                            GuiData.UISmallfont,
                            "|",
                            cursorPos,
                            Color.Cyan
                        );
                    }
                }
                else
                {
                    // 在进度达到20%之前，只显示"Key:"
                    spriteBatch.DrawString(
                        GuiData.UISmallfont,
                        KEY_PREFIX,
                        keyPos,
                        Color.Lerp(Color.Yellow, Color.LimeGreen, progress)
                    );
                }
            }
        }
    }
}
