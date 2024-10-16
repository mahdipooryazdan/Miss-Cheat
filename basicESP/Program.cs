using basicESP;
using Swed64;
using System.Numerics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using ImGuiNET;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        HttpClient client = new HttpClient();
        string offsetsUrl = "https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/offsets.cs";
        string clientDllUrl = "https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/client_dll.cs";
        string offsetsFileContent = await client.GetStringAsync(offsetsUrl);
        string clientDllFileContent = await client.GetStringAsync(clientDllUrl);

        Dictionary<string, int> offsets = ExtractOffsets(offsetsFileContent);
        Dictionary<string, int> clientDll = ExtractClientDll(clientDllFileContent);

        Swed swed = new Swed("cs2");
        IntPtr clientBase = swed.GetModuleBase("client.dll");
        Renderer renderer = new Renderer();
        Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
        renderThread.Start();
        Vector2 screenSize = renderer.screensize;
        List<Entity> entities = new List<Entity>();
        Entity localPlayer = new Entity();

        //Offset
        int dwEntityList = offsets.ContainsKey("dwEntityList") ? offsets["dwEntityList"] : 0x19CA848;
        int dwViewMatrix = offsets.ContainsKey("dwViewMatrix") ? offsets["dwViewMatrix"] : 0x1A2CAD0;
        int dwLocalPlayerPawn = offsets.ContainsKey("dwLocalPlayerPawn") ? offsets["dwLocalPlayerPawn"] : 0x182FAE8;
        int dwLocalPlayerController = offsets.ContainsKey("dwLocalPlayerController") ? offsets["dwLocalPlayerController"] : 0x1A1A690;
        int dwGameRules = offsets.ContainsKey("dwGameRules") ? offsets["dwGameRules"] : 0x1A28408;
        int dwPlantedC4 = offsets.ContainsKey("dwPlantedC4") ? offsets["dwPlantedC4"] : 0x1A32040;
        int dwGlobalVars = offsets.ContainsKey("dwGlobalVars") ? offsets["dwGlobalVars"] : 0x1823CB0;

        //client
        int m_vOldOrigin = clientDll.ContainsKey("m_vOldOrigin") ? clientDll["m_vOldOrigin"] : 0x1324;
        int m_iTeamNum = clientDll.ContainsKey("m_iTeamNum") ? clientDll["m_iTeamNum"] : 0x3E3;
        int m_lifeState = clientDll.ContainsKey("m_lifeState") ? clientDll["m_lifeState"] : 0x348;
        int m_hPlayerPawn = clientDll.ContainsKey("m_hPlayerPawn") ? clientDll["m_hPlayerPawn"] : 0x80C;
        int m_vecViewOffset = clientDll.ContainsKey("C_BaseModelEntity.m_vecViewOffset") ? clientDll["C_BaseModelEntity.m_vecViewOffset"] : 0xCB0;
        int m_iszPlayerName = clientDll.ContainsKey("m_iszPlayerName") ? clientDll["m_iszPlayerName"] : 0x660;

        int m_Item = clientDll.ContainsKey("C_AttributeContainer.m_Item") ? clientDll["C_AttributeContainer.m_Item"] : 0x50;
        int m_pClippingWeapon = clientDll.ContainsKey("C_CSPlayerPawnBase.m_pClippingWeapon") ? clientDll["C_CSPlayerPawnBase.m_pClippingWeapon"] : 0x13A0;
        int m_iItemDefinitionIndex = clientDll.ContainsKey("m_iItemDefinitionIndex") ? clientDll["m_iItemDefinitionIndex"] : 0x1BA;
        int m_AttributeManager = clientDll.ContainsKey("C_EconEntity.m_AttributeManager")? clientDll["C_EconEntity.m_AttributeManager"]: 0x1148;
        int m_iHealth = clientDll.ContainsKey("m_iHealth") ? clientDll["m_iHealth"] : 0x344;
        int m_modelState = clientDll.ContainsKey("CSkeletonInstance.m_modelState") ? clientDll["CSkeletonInstance.m_modelState"] : 0x170;

        //int m_modelState = 0x170; // CModelState
        //int m_pGameSceneNode = 0x328; // CGameSceneNode*

        int m_bBombPlanted = clientDll.ContainsKey("C_CSGameRules.m_bBombPlanted") ? clientDll["C_CSGameRules.m_bBombPlanted"] : 0x9A5;
        int m_pGameSceneNode = clientDll.ContainsKey("m_pGameSceneNode") ? clientDll["m_pGameSceneNode"] : 0x328;
        int m_vecAbsOrigin = clientDll.ContainsKey("m_vecAbsOrigin") ? clientDll["m_vecAbsOrigin"] : 0xD0;
        int m_flC4Blow = clientDll.ContainsKey("m_flC4Blow") ? clientDll["m_flC4Blow"] : 0xFC0;
        int m_flTimerLength = clientDll.ContainsKey("m_flTimerLength") ? clientDll["m_flTimerLength"] : 0xFC8;
        int m_bBeingDefused = clientDll.ContainsKey("m_bBeingDefused") ? clientDll["m_bBeingDefused"] : 0xFCC;
        int m_flDefuseLength = clientDll.ContainsKey("m_flDefuseLength") ? clientDll["m_flDefuseLength"] : 0xFDC;
        int m_flDefuseCountDown = clientDll.ContainsKey("m_flDefuseCountDown") ? clientDll["m_flDefuseCountDown"] : 0xFE0;
        int m_entitySpottedState = clientDll.ContainsKey("m_entitySpottedState") ? clientDll["m_entitySpottedState"] : 0x1B48;
        int m_bSpotted = clientDll.ContainsKey("m_bSpotted") ? clientDll["m_bSpotted"] : 0x8;
        int m_hBombDefuser = clientDll.ContainsKey("m_hBombDefuser") ? clientDll["m_hBombDefuser"] : 0xFE8;

        bool bombPlanted = false;
        Task bombTimerTask = null;

        while (true)
        {
            IntPtr gameRules = swed.ReadPointer(clientBase, dwGameRules); 

            if (gameRules != IntPtr.Zero)
            {
                bombPlanted = swed.ReadBool(gameRules, m_bBombPlanted);
                bool cplantedc4 = swed.ReadBool(clientBase, dwPlantedC4 - 0x8);
                IntPtr planted_c4 = swed.ReadPointer(swed.ReadPointer(clientBase, dwPlantedC4));

                if (cplantedc4 && !renderer.bombPlanted)
                {
                    float timerc4 = swed.ReadFloat(planted_c4, m_flTimerLength);
                    bool defuse = swed.ReadBool(planted_c4, m_bBeingDefused);
                    float timerdefuse = swed.ReadFloat(planted_c4, m_flDefuseLength);

                    renderer.timerc4 = timerc4;
                    renderer.defuse = defuse;
                    renderer.timerdefuse = timerdefuse;
                }

                if (bombPlanted && (bombTimerTask == null || bombTimerTask.IsCompleted))
                {
                    bombTimerTask = Task.Run(() =>
                    {
                        double timeLeft = renderer.timerc4;
                        Stopwatch bombStopwatch = new Stopwatch();
                        bombStopwatch.Start();

                        Stopwatch defuseStopwatch = null; 
                        bool isDefusing = false;

                        while (true)
                        {
                            bombPlanted = swed.ReadBool(gameRules, m_bBombPlanted);
                            if (!bombPlanted || bombStopwatch.Elapsed.TotalSeconds >= timeLeft)
                                break;

                            double elapsed = bombStopwatch.Elapsed.TotalSeconds;
                            renderer.timerc4 = Math.Round(timeLeft - elapsed, 2);

                            bool defuse = swed.ReadBool(planted_c4, m_bBeingDefused);
                            if (defuse)
                            {
                                if (!isDefusing) 
                                {
                                    defuseStopwatch = new Stopwatch();
                                    defuseStopwatch.Start();
                                    isDefusing = true;
                                }
                                string BombDefuser = swed.ReadString(planted_c4, m_hBombDefuser, 16).Split("\0")[0];
                                Console.WriteLine(BombDefuser);
                                double defelapsed = defuseStopwatch.Elapsed.TotalSeconds;
                                float timerdefuse = swed.ReadFloat(planted_c4, m_flDefuseLength);
                                renderer.defuse = true;
                                renderer.timerdefuse = Math.Round(timerdefuse - defelapsed, 2);
                            }
                            else
                            {
                                isDefusing = false; 
                                renderer.defuse = false;
                                renderer.timerdefuse = 0;
                                defuseStopwatch = null; 
                            }

                            IntPtr c4Node = swed.ReadPointer(planted_c4, m_pGameSceneNode);
                            Vector3 c4Origin = swed.ReadVec(c4Node, m_vecAbsOrigin);

                            float[] viewMatrix = swed.ReadMatrix(clientBase + dwViewMatrix);
                            Vector2 calculatedC4Pos2D = Calculate.WorldToScreen(viewMatrix, c4Origin, screenSize);

                            renderer.c4Pos2D = calculatedC4Pos2D;

                            renderer.bombPlanted = true;
                        }

                        bombStopwatch.Stop();
                        renderer.timerc4 = -1;
                        renderer.bombPlanted = false;
                        renderer.defuse = false;
                        renderer.timerdefuse = 0;
                    });
                }
                else if (!bombPlanted)
                {
                    renderer.timerc4 = -1;
                    renderer.bombPlanted = false;
                    renderer.defuse = false;
                    renderer.timerdefuse = 0;
                }
            }


            entities.Clear();
            IntPtr entityList = swed.ReadPointer(clientBase, dwEntityList);
            IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
            IntPtr localPlayerPawn = swed.ReadPointer(clientBase, dwLocalPlayerPawn);
            IntPtr localPlayercontrol = swed.ReadPointer(clientBase, dwLocalPlayerController);

            localPlayer.team = swed.ReadInt(localPlayerPawn, m_iTeamNum);
            for (int i = 0; i < 64; i++)
            {
                IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);
                if (currentController == IntPtr.Zero) continue;
                int pawHandlle = swed.ReadInt(currentController, m_hPlayerPawn);
                if (pawHandlle == 0) continue;
                IntPtr ListEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawHandlle & 0x7FFF) >> 9) + 0x10);
                if (ListEntry2 == IntPtr.Zero) continue;
                IntPtr currentPawn = swed.ReadPointer(ListEntry2, 0x78 * (pawHandlle & 0x1FF));
                if (currentPawn == IntPtr.Zero) continue;

                int lifeState = swed.ReadInt(currentPawn, m_lifeState);
                if (lifeState != 256) continue;
                IntPtr currentWeapon = swed.ReadPointer(currentPawn, m_pClippingWeapon);
                short WeaponDefinitionIndex = swed.ReadShort(currentWeapon, m_AttributeManager + m_Item + m_iItemDefinitionIndex);
                if (WeaponDefinitionIndex == -1) continue;

                float heightOffset = 12;
                float[] viewMatrix = swed.ReadMatrix(clientBase + dwViewMatrix);

                IntPtr sceneNode = swed.ReadPointer(currentPawn, m_pGameSceneNode);
                IntPtr boneMatrix = swed.ReadPointer(sceneNode, m_modelState + 0x80);


                Entity entity = new Entity();
                entity.team = swed.ReadInt(currentPawn, m_iTeamNum);
                entity.health = swed.ReadInt(currentPawn, m_iHealth);
                entity.position = swed.ReadVec(currentPawn, m_vOldOrigin);
                entity.viewoffset = swed.ReadVec(currentPawn, m_vecViewOffset);
                Vector3 aboveHeadPosition = new Vector3(entity.position.X, entity.position.Y, entity.position.Z + heightOffset);
                entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
                entity.currentWeaponName = Enum.GetName(typeof(Weapon), WeaponDefinitionIndex);
                entity.name = swed.ReadString(currentController, m_iszPlayerName, 16).Split("\0")[0];
                entity.viewPosition2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(aboveHeadPosition, entity.viewoffset), screenSize);
                entity.distance = Vector3.Distance(entity.position, localPlayer.position);
                entity.localPlayercontrol = swed.ReadString(localPlayercontrol, m_iszPlayerName, 16).Split("\0")[0];

                String name2 = swed.ReadString(currentController, m_iszPlayerName, 16);
                bool spoted = swed.ReadBool(currentPawn, m_entitySpottedState + m_bSpotted);
                swed.WriteBool(currentPawn, m_entitySpottedState + m_bSpotted,true);
                String spottedStatus = spoted == true ? "spotted" : " ";
                //Console.WriteLine($"{name2}: {spottedStatus}");
                entity.bones = Calculate.ReadBones(boneMatrix, swed);
                entity.bones2d = Calculate.ReadBones2d(entity.bones, viewMatrix, screenSize);

                entities.Add(entity);


            }
            renderer.UpdateLocalPlayer(localPlayer);
            renderer.UpdateEntites(entities);

            Thread.Sleep((int)0.1f);

        

        }


    }
    //Offset
    static Dictionary<string, int> ExtractOffsets(string content)
    {
        Dictionary<string, int> offsets = new Dictionary<string, int>();
        string pattern = @"public\s+const\s+nint\s+(\w+)\s+=\s+0x(\w+);";
        Regex regex = new Regex(pattern);

        foreach (Match match in regex.Matches(content))
        {
            string offsetName = match.Groups[1].Value;
            string offsetValue = match.Groups[2].Value;
            offsets[offsetName] = Convert.ToInt32(offsetValue, 16);
        }
        return offsets;
    }
    //client
    static Dictionary<string, int> ExtractClientDll(string content)
    {
        Dictionary<string, int> client = new Dictionary<string, int>();
        string classPattern = @"public\s+static\s+class\s+(\w+)\s*{";
        string varPattern = @"public\s+const\s+nint\s+(\w+)\s+=\s+0x(\w+);";

        Regex classRegex = new Regex(classPattern);
        Regex varRegex = new Regex(varPattern);

        string currentClass = null;

        foreach (var line in content.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            Match classMatch = classRegex.Match(line);
            if (classMatch.Success)
            {
                currentClass = classMatch.Groups[1].Value;
                continue;
            }

            Match varMatch = varRegex.Match(line);
            if (varMatch.Success && currentClass != null)
            {
                string varName = $"{currentClass}.{varMatch.Groups[1].Value}";
                string varValue = varMatch.Groups[2].Value;
                client[varName] = Convert.ToInt32(varValue, 16);
            }
        }

        return client;
    }

}

