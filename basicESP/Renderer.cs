using ClickableTransparentOverlay;
using ImGuiNET;
using SixLabors.ImageSharp.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace basicESP
{
    public class Renderer : Overlay
    {
        
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        float screenWidth = GetSystemMetrics(0);
        FileLoadException alires;
        float screenHeight = GetSystemMetrics(1);
        public Vector2 screensize;
        public Renderer()
        {
            screensize = new Vector2(screenWidth, screenHeight);
        }


        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();
        private bool enableteam = false;
        private bool enableESP = true;
        private bool enableLine = false;
        private bool enableName = true;
        private bool enableHealbar = false;
        private bool enableHealbox = false;
        private bool enableWeaponName = false;
        public bool bombPlanted =false;
        public bool enableBone = false;
        public double timerc4 = 0;
        public bool defuse = false;
        public double timerdefuse = 0;
        public Vector2 c4Pos2D = Vector2.Zero;
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1);
        private Vector4 teamColor = new Vector4(0, 1, 0, 1);
        private Vector4 nameColor = new Vector4(1, 1, 1, 1);
        private Vector4 WeaponNameColor = new Vector4(1, 1, 1, 1);
        private Vector4 boneColor = new Vector4(1, 1, 1, 1);
        private Vector4 C4ColorRed = new Vector4(1, 0, 0, 1);
        private Vector4 C4ColorGreen = new Vector4(0, 1, 0, 1);

        private Vector4 isScopedColor = new Vector4(1, 1, 0, 1);
        private Vector4 isFlashColor = new Vector4(0, 1, 0, 0.5f);
        private Vector4 IsDefusingColor = new Vector4(0, 0, 1, 1);

        float boneThickness = 4;

        ImDrawListPtr drawList;
        protected override void Render()
        {
            ImGui.Begin("Basic ESP");
            ImGui.Checkbox("enable Esp ", ref enableESP);
            ImGui.Checkbox("Enable Line", ref enableLine);
            ImGui.Checkbox("Enable Team", ref enableteam);
            ImGui.Checkbox("Enable Name", ref enableName);
            ImGui.Checkbox("Enable Heal Bar", ref enableHealbar);
            ImGui.Checkbox("Enable Heal Box", ref enableHealbox);
            ImGui.Checkbox("Enable Weapon Name", ref enableWeaponName);
            ImGui.Checkbox("Enable Bone", ref enableBone);

            if (ImGui.CollapsingHeader("Team Color"))
            {
                ImGui.ColorPicker4("##team color", ref teamColor);

            }
            if (ImGui.CollapsingHeader("Name Color"))
            {
                ImGui.ColorPicker4("##name color", ref nameColor);

            }
            if (ImGui.CollapsingHeader("Enemy Color"))
            {
                ImGui.ColorPicker4("##Enemy color", ref enemyColor);

            }
            if (ImGui.CollapsingHeader("Weapon Name Color"))
            {
                ImGui.ColorPicker4("##Weapon Name color", ref WeaponNameColor);

            }
            if (ImGui.CollapsingHeader("bone Color"))
            {
                ImGui.ColorPicker4("##bonecolor", ref boneColor);

            }
            if (ImGui.CollapsingHeader("isScoped Color"))
            {
                ImGui.ColorPicker4("##isScoped", ref isScopedColor);

            }
            if (ImGui.CollapsingHeader("isFlash Color"))
            {
                ImGui.ColorPicker4("##isFlash", ref isFlashColor);

            }
            if (ImGui.CollapsingHeader("IsDefusing Color"))
            {
                ImGui.ColorPicker4("##IsDefusing", ref IsDefusingColor);

            }





            DrawOverlay(screensize);
            drawList = ImGui.GetWindowDrawList();


                if (enableESP == true)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        DrawHealthBar(entity);
                        DrawBones(entity);
                        DrawBox(entity);
                        if (enableLine == true) DrawLine(entity);
                        NameEsp(entity, 15);
                        WeaponName(entity, 15);
                    }
                    c4Position();
                    DrawBombTimerToScreen();

                }
            }
        }
        bool EntityOnScreen(Entity entity)
        {
            if (entity.position2D.X > 0 && entity.position2D.X < screensize.X && entity.position2D.Y > 0)
            {
                return true;

            }
            return false;
        }

        private void DrawBombTimerToScreen()
        {
            float screenHeight = ImGui.GetIO().DisplaySize.Y;

            Vector2 position = new Vector2(10, screenHeight / 2);
            position.Y += 20;

            if (bombPlanted)
            {
                if (defuse)
                {
                    if (timerc4 > timerdefuse)
                    {
                        drawList.AddText(position, ImGui.ColorConvertFloat4ToU32(C4ColorGreen), $"Defuse Timer : {timerdefuse:F2}");
                    }
                    else
                    {
                        drawList.AddText(position, ImGui.ColorConvertFloat4ToU32(C4ColorRed), $"Defuse Timer : {timerdefuse:F2}");
                    }
                    position.Y += 20;
                }

                drawList.AddText(position, ImGui.ColorConvertFloat4ToU32(C4ColorRed), $"Bomb Timer : {timerc4:F2}");
            }
        }




        private void DrawBones(Entity entity) 
        {
            if (enableBone == true)
            {
                if (localPlayer.team != entity.team)
                {
                    uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);
                    float currentBoneThickness = boneThickness / entity.distance;

                    drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness);
                    drawList.AddCircle(entity.bones2d[2], 3 + currentBoneThickness, uintColor);
                }
                if (localPlayer.team == entity.team && enableteam==true)
                {
                    if(entity.localPlayercontrol==entity.name)
                    {
                        return;
                    }
                    uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);
                    float currentBoneThickness = boneThickness / entity.distance;

                    drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness);
                    drawList.AddCircle(entity.bones2d[2], 3 + currentBoneThickness, uintColor);
                    
                }
            }
        }

        public void c4Position()
        {
            if (bombPlanted)
            {
                float bombMarkerSize = 1.5f;
                Vector4 bombMarkerColor = C4ColorRed;

                if (c4Pos2D != Vector2.Zero)
                {
                    drawList.AddCircleFilled(c4Pos2D, bombMarkerSize, ImGui.ColorConvertFloat4ToU32(bombMarkerColor));
                }
            }
        }

        private void DrawHealthBar(Entity entity)
        {
            if (enableHealbar == true)
            {
                if (localPlayer.team != entity.team)
                {
                    float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;

                    float boxLeft = entity.viewPosition2D.X - entityHeight / 4 ;
                    float boxRight = entity.position2D.X + entityHeight / 4;

                    float barPercentWidth = 0.06f;
                    float barPixelWidth = barPercentWidth * (boxRight - boxLeft);

                    float barHeight = entityHeight * (entity.health / 100f);

                    Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
                    Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);

                    Vector4 barColor = GetHealthColor(entity.health);
                    drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));
                }
                if (localPlayer.team == entity.team && enableteam == true)
                {
                    if (entity.localPlayercontrol == entity.name)
                    {
                        return;
                    }
                    float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;

                    float boxLeft = entity.viewPosition2D.X - entityHeight / 4;
                    float boxRight = entity.position2D.X + entityHeight / 4;

                    float barPercentWidth = 0.03f;
                    float barPixelWidth = barPercentWidth * (boxRight - boxLeft);

                    float barHeight = entityHeight * (entity.health / 100f);

                    Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
                    Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);

                    Vector4 barColor = GetHealthColor(entity.health);
                    drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));
                }
            }


        }

        private Vector4 GetHealthColor(float health)
        {
            float normalizedHealth = health / 100f;
            float red = 1.0f - normalizedHealth;
            float green = normalizedHealth;

            return new Vector4(red, green, 0.0f, 1.0f);
        }



        private void DrawBox(Entity entity)
        {
            Vector4 boxColor;

            if (enableHealbox == true)
            {
                boxColor = GetHealthColor(entity.health);
            }
            else
            {
                boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            }

            if (enableteam && localPlayer.team == entity.team)
            {
                if (entity.viewPosition2D == new Vector2(0, 0)) return;
                if (entity.localPlayercontrol == entity.name)
                {
                    return;
                }
                else
                {
                    float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                    Vector2 recTop = new Vector2(entity.viewPosition2D.X - entityHeight / 4, entity.viewPosition2D.Y);
                    Vector2 rectBottom = new Vector2(entity.position2D.X + entityHeight / 4, entity.position2D.Y);
                    if (entity.isScoped)
                    {
                        drawList.AddRect(recTop, rectBottom, ImGui.ColorConvertFloat4ToU32(isScopedColor));
                    }
                    else if (entity.IsDefusing)
                    {
                        drawList.AddRect(recTop, rectBottom, ImGui.ColorConvertFloat4ToU32(IsDefusingColor));
                    }
                    else
                    {
                        drawList.AddRect(recTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor)); 
                    }

                }
            }
            else if (localPlayer.team != entity.team)
            {
                if (localPlayer.viewPosition2D == entity.viewPosition2D)
                {
                    return;
                }

                float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                Vector2 recTop = new Vector2(entity.viewPosition2D.X - entityHeight / 4f, entity.viewPosition2D.Y);
                Vector2 rectBottom = new Vector2(entity.position2D.X + entityHeight / 4f, entity.position2D.Y);

                if (entity.isScoped)
                {
                    drawList.AddRect(recTop, rectBottom, ImGui.ColorConvertFloat4ToU32(isScopedColor));
                }
                else if (entity.IsDefusing)
                {
                    drawList.AddRect(recTop, rectBottom, ImGui.ColorConvertFloat4ToU32(IsDefusingColor));
                }
                else
                {
                    drawList.AddRect(recTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
                }

            }
        }

        private void WeaponName(Entity entity, int offset)
        {
            if (enableWeaponName == true)
            {
                if (localPlayer.team != entity.team)
                {
                    var io = ImGui.GetIO();
                    string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Roboto-Black.ttf");
                    float fontSize = 11.0f;
                    ImFontPtr myCustomFont = io.Fonts.AddFontFromFileTTF(fontPath, fontSize);

                    float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                    float boxWidth = entityHeight / 4;

                    Vector2 weaponPosition = new Vector2(entity.viewPosition2D.X - boxWidth - 5, entity.position2D.Y + 2);
                    drawList.AddText(weaponPosition, ImGui.ColorConvertFloat4ToU32(WeaponNameColor), $"{entity.currentWeaponName}");
                }
                if (enableteam == true && localPlayer.team == entity.team)
                {
                    if (entity.localPlayercontrol == entity.name)
                    {
                        return;
                    }
                    var io = ImGui.GetIO();
                    string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Roboto-Black.ttf");
                    float fontSize = 11.0f;
                    ImFontPtr myCustomFont = io.Fonts.AddFontFromFileTTF(fontPath, fontSize);

                    float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                    float boxWidth = entityHeight / 4;

                    Vector2 weaponPosition = new Vector2(entity.viewPosition2D.X - boxWidth - 5, entity.position2D.Y + 2);
                    drawList.AddText(weaponPosition, ImGui.ColorConvertFloat4ToU32(WeaponNameColor), $"{entity.currentWeaponName}");
                }
            }
        }



        private void NameEsp(Entity entity, int offset)
        {
            if (enableName == true && enableteam == true && localPlayer.team == entity.team)
            {
                if (entity.localPlayercontrol == entity.name)
                {
                    return;
                }
                var io = ImGui.GetIO();
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Roboto-Black.ttf");
                float fontSize = 11.0f;
                ImFontPtr myCustomFont = io.Fonts.AddFontFromFileTTF(fontPath, fontSize);

                float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                float boxWidth = entityHeight / 4;

                Vector2 namepossition = new Vector2(entity.viewPosition2D.X - boxWidth - 5, entity.viewPosition2D.Y - offset);
                drawList.AddText(namepossition, ImGui.ColorConvertFloat4ToU32(nameColor), $"{entity.name}");
            }
            else if (localPlayer.team != entity.team)
            {
                var io = ImGui.GetIO();
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "Roboto-Black.ttf");
                float fontSize = 11.0f;
                ImFontPtr myCustomFont = io.Fonts.AddFontFromFileTTF(fontPath, fontSize);

                float entityHeight = entity.position2D.Y - entity.viewPosition2D.Y;
                float boxWidth = entityHeight / 4;
                Vector2 namepossition = new Vector2(entity.viewPosition2D.X - boxWidth - 5, entity.viewPosition2D.Y - offset);
                drawList.AddText(namepossition, ImGui.ColorConvertFloat4ToU32(nameColor), $"{entity.name}");
            }
        }

        private void DrawLine(Entity entity)
        {
            if (enableteam == true && localPlayer.team == entity.team)
            {
                if (entity.localPlayercontrol == entity.name)
                {
                    return;
                }
                Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
                drawList.AddLine(new Vector2(screensize.X / 2, screensize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));
            }
            else if (localPlayer.team != entity.team)
            {

                Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
                drawList.AddLine(new Vector2(screensize.X / 2, screensize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));

            }

        }

        public void UpdateEntites(IEnumerable<Entity> newentities)
        {
            entities = new ConcurrentQueue<Entity>(newentities);


        }
        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }
        public Entity GetLocalPlayer()
        {
            lock (entityLock)
            {
                return localPlayer;
            }
        }
        void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        }
    }
}
