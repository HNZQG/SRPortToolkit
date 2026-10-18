using Microsoft.Xna.Framework;
using Hacknet;
using System;
using Pathfinder.Executable;
using System.Collections.Generic;

namespace AutoCrackFirewall
{
    /*
     * Thanks for WnCry
     * Thanks for April_Crystal
     */

    public class AutoCrackFirewallExe : BaseExecutable
    {
        private bool complete = false;
        private float elapsedTime = 0f;
        private Computer targetComputer;
        private float rotationAngle = 0f;
        private float pulseValue = 0f;
        private float pulseDirection = 1f;
        private bool connectionEstablished = false;
        private float connectionProgress = 0f;
        private float crackProgress = 0f;
        private string status = "Initializing...";

        // 正方体集合相关变量
        private List<CubeData> cubes = new List<CubeData>();
        private float cubeAddTimer = 0f;
        private const float CubeAddInterval = 0.5f; // 添加正方体的时间间隔
        private const int MaxCubes = 15; // 最大正方体数量
        private bool removingCubes = false;
        private float cubeRemoveTimer = 0f;
        private const float CubeRemoveInterval = 0.2f; // 删除正方体的时间间隔

        // 正方体数据类
        private class CubeData
        {
            public float rotationOffset; // 旋转角度偏移
            public float sizeModifier;   // 大小调整系数
            public bool isActive;       // 是否激活显示
            public float spawnTime;     // 生成时间
        }

        public AutoCrackFirewallExe(Rectangle location, OS operatingSystem, string[] args)
            : base(location, operatingSystem, args)
        {
            name = "AutoCrackFirewall";
            ramCost = 100;
            IdentifierName = "Firewall Cracker";
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // 获取目标计算机
            targetComputer = (os.connectedComp != null) ? os.connectedComp : os.thisComputer;

            if (targetComputer == null)
            {
                os.write("ERROR: Target computer not found!");
                isExiting = true;
                needsRemoval = true;
                return;
            }
            if (targetComputer.firewall == null)
            {
                os.write("Target Computer No Firewall Actived.");
                isExiting = true;
            }
            else
            {
                // 初始化第一个正方体（中心位置）
                cubes.Add(new CubeData
                {
                    rotationOffset = 0f,
                    sizeModifier = 1.0f,
                    isActive = true,
                    spawnTime = 0f
                });
            }
        }

        public override void Draw(float t)
        {
            base.Draw(t);

            // 绘制背景
            spriteBatch.Draw(Utils.white, bounds, Color.Black * 0.8f);
            DrawRectangleOutline(bounds, Color.DarkGreen, 1);

            // 定义内部绘制区域
            Rectangle drawArea = new Rectangle(
                bounds.X + 5,
                bounds.Y + 5,
                bounds.Width - 10,
                bounds.Height - 10
            );

            // 绘制标题
            Vector2 titlePos = new Vector2(drawArea.X, drawArea.Y);
            spriteBatch.DrawString(GuiData.smallfont, "FIREWALL CRACKER v2.5", titlePos, Color.Lime);

            // 绘制目标信息
            Vector2 targetPos = new Vector2(drawArea.X, drawArea.Y + 20);
            spriteBatch.DrawString(GuiData.smallfont, $"Target: {targetComputer.name}", targetPos, Color.White);

            // 绘制状态信息
            Vector2 statusPos = new Vector2(drawArea.X, drawArea.Y + 40);
            spriteBatch.DrawString(GuiData.smallfont, $"Status: {status}", statusPos, Color.Cyan);

            // 绘制进度条背景
            Rectangle progressBg = new Rectangle(
                drawArea.X,
                drawArea.Y + 60,
                drawArea.Width,
                15
            );
            spriteBatch.Draw(Utils.white, progressBg, Color.DarkSlateGray);

            // 绘制进度条
            Rectangle progressBar = new Rectangle(
                drawArea.X,
                drawArea.Y + 60,
                (int)(drawArea.Width * crackProgress),
                15
            );
            spriteBatch.Draw(Utils.white, progressBar, Color.LimeGreen);

            // 绘制进度文本
            string progressText = $"{(int)(crackProgress * 100)}%";
            Vector2 progressTextSize = GuiData.tinyfont.MeasureString(progressText);
            Vector2 progressTextPos = new Vector2(
                progressBar.X + progressBar.Width / 2 - progressTextSize.X / 2,
                progressBar.Y + progressBar.Height / 2 - progressTextSize.Y / 2
            );
            spriteBatch.DrawString(GuiData.tinyfont, progressText, progressTextPos, Color.Black);

            // 计算中心位置 - 固定在右上角
            Vector2 center = new Vector2(
                drawArea.Right - 20,
                drawArea.Top + 20
            );

            // 绘制所有正方体（共享同一个中心点）
            DrawWireframeCubes(drawArea, center);
        }

        // 绘制矩形边框
        private void DrawRectangleOutline(Rectangle rect, Color color, int thickness = 1)
        {
            // 上边
            spriteBatch.Draw(Utils.white, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // 左边
            spriteBatch.Draw(Utils.white, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // 下边
            spriteBatch.Draw(Utils.white, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // 右边
            spriteBatch.Draw(Utils.white, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        // 绘制线条
        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            Utils.drawLine(spriteBatch, start, end, Vector2.Zero, color, 0f);
        }

        // 绘制多个正方体（共享同一个中心点）
        private void DrawWireframeCubes(Rectangle area, Vector2 center)
        {
            // 计算大小
            float maxSize = Math.Min(area.Width, area.Height) * 0.3f;
            float baseSize = Math.Min(maxSize, 80f);

            // 绘制所有正方体
            foreach (var cube in cubes)
            {
                if (!cube.isActive) continue;

                // 计算实际大小（带脉动效果和大小调整）
                float pulseSize = baseSize * cube.sizeModifier * (0.9f + pulseValue * 0.1f);

                // 绘制单个正方体（使用共享的中心点）
                DrawWireframeCube(center, rotationAngle + cube.rotationOffset, pulseSize, area);
            }

            // 绘制流动点（如果连接已建立且第一个正方体激活）
            if (connectionEstablished && cubes.Count > 0 && cubes[0].isActive)
            {
                DrawMovingPoints(center, rotationAngle, baseSize * cubes[0].sizeModifier, area);
            }
        }

        // 绘制单个正方体
        private void DrawWireframeCube(Vector2 center, float rotation, float size, Rectangle area)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);

            // 正方体顶点
            Vector3[] vertices = {
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
            new Vector3(1, 1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, 1)
        };

            // 投影到2D
            Vector2[] projected = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float x = vertices[i].X * cos - vertices[i].Z * sin;
                float z = vertices[i].X * sin + vertices[i].Z * cos;

                projected[i] = new Vector2(
                    center.X + x * size,
                    center.Y + vertices[i].Y * size * 0.7f - z * size * 0.5f
                );
            }

            // 边连接
            int[,] edges = {
            {0, 1}, {1, 2}, {2, 3}, {3, 0}, // 后表面
            {4, 5}, {5, 6}, {6, 7}, {7, 4}, // 前表面
            {0, 4}, {1, 5}, {2, 6}, {3, 7}  // 连接边
        };

            // 边颜色
            Color edgeColor = connectionEstablished ?
                              Color.Lerp(Color.Lime, Color.Cyan, pulseValue) :
                              Color.Lerp(Color.Red, Color.Orange, pulseValue);

            // 绘制边
            for (int i = 0; i < 12; i++)
            {
                DrawLine(
                    projected[edges[i, 0]],
                    projected[edges[i, 1]],
                    edgeColor
                );
            }
        }

        // 绘制流动点
        private void DrawMovingPoints(Vector2 center, float rotation, float size, Rectangle area)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);

            Vector3[] vertices = {
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
            new Vector3(1, 1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, 1)
        };

            Vector2[] projected = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float x = vertices[i].X * cos - vertices[i].Z * sin;
                float z = vertices[i].X * sin + vertices[i].Z * cos;

                projected[i] = new Vector2(
                    center.X + x * size,
                    center.Y + vertices[i].Y * size * 0.7f - z * size * 0.5f
                );
            }

            int[,] edges = {
            {0, 1}, {1, 2}, {2, 3}, {3, 0},
            {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };

            // 绘制流动点
            for (int i = 0; i < 20; i++)
            {
                float timeOffset = (elapsedTime * 2f + i * 0.3f) % 1f;
                int edgeIndex = (int)(timeOffset * 12) % 12;

                Vector2 start = projected[edges[edgeIndex, 0]];
                Vector2 end = projected[edges[edgeIndex, 1]];
                Vector2 position = Vector2.Lerp(start, end, (timeOffset * 12) % 1f);

                if (position.X >= area.Left && position.X <= area.Right &&
                    position.Y >= area.Top && position.Y <= area.Bottom)
                {
                    Rectangle pointRect = new Rectangle(
                        (int)position.X - 2,
                        (int)position.Y - 2,
                        4, 4
                    );
                    spriteBatch.Draw(Utils.white, pointRect, Color.Lime);
                }
            }
        }

        public override void Update(float t)
        {
            base.Update(t);
            elapsedTime += t;

            // 更新动画状态
            rotationAngle += t * 0.5f;
            pulseValue += pulseDirection * t * 2f;

            if (pulseValue > 1f)
            {
                pulseValue = 1f;
                pulseDirection = -1f;
            }
            else if (pulseValue < 0f)
            {
                pulseValue = 0f;
                pulseDirection = 1f;
            }

            // 正方体管理
            if (!complete)
            {
                // 添加正方体（连接建立后）
                if (connectionEstablished && !removingCubes && cubes.Count < MaxCubes)
                {
                    cubeAddTimer += t;
                    if (cubeAddTimer >= CubeAddInterval)
                    {
                        cubeAddTimer = 0f;

                        // 添加新正方体（带旋转偏移和大小调整）
                        cubes.Add(new CubeData
                        {
                            rotationOffset = cubes.Count * 0.2f, // 每个正方体有不同的旋转偏移
                            sizeModifier = 1.0f - cubes.Count * 0.05f, // 每个正方体稍微小一点
                            isActive = true,
                            spawnTime = elapsedTime
                        });
                    }
                }

                // 当进度达到90%时开始删除正方体
                if (crackProgress >= 0.9f && !removingCubes)
                {
                    removingCubes = true;
                    cubeRemoveTimer = 0f;
                }

                // 删除正方体
                if (removingCubes)
                {
                    cubeRemoveTimer += t;
                    if (cubeRemoveTimer >= CubeRemoveInterval && cubes.Count > 1)
                    {
                        cubeRemoveTimer = 0f;

                        // 从列表末尾开始删除（保留第一个）
                        for (int i = cubes.Count - 1; i >= 1; i--)
                        {
                            if (cubes[i].isActive)
                            {
                                cubes[i].isActive = false;
                                break;
                            }
                        }
                    }
                }
            }

            // 更新破解进度
            if (!complete)
            {
                if (!connectionEstablished)
                {
                    connectionProgress += t / 5f;
                    status = "Establishing connection...";

                    if (connectionProgress >= 1f)
                    {
                        connectionEstablished = true;
                        status = "Analyzing firewall...";
                    }
                }
                else
                {
                    crackProgress += t / 35f;

                    if (crackProgress < 0.3f)
                    {
                        status = "Mapping firewall structure...";
                    }
                    else if (crackProgress < 0.6f)
                    {
                        status = "Bypassing security protocols...";
                    }
                    else if (crackProgress < 0.9f)
                    {
                        status = "Injecting payload...";
                    }
                    else
                    {
                        status = "Finalizing exploit...";
                    }

                    if (crackProgress >= 1f)
                    {
                        crackProgress = 1f;
                        status = "Firewall compromised!";
                        complete = true;
                        Completed();
                    }
                }
            }
        }

        public override void Completed()
        {
            base.Completed();
            if (targetComputer == null) return;

            // 破解防火墙
            targetComputer.firewall.solved = true;

            // 显示成功消息
            os.terminal.writeLine("Firewall successfully cracked!");
            os.terminal.writeLine("Security protocols bypassed");
            os.terminal.writeLine($"Target: {targetComputer.name} is now vulnerable");
        }
    }
}
