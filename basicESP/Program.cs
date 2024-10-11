using basicESP;
using Swed64;
using System.Numerics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

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
        int dwEntityList = offsets.ContainsKey("dwEntityList") ? offsets["dwEntityList"] : 0x19BDE30;
        int dwViewMatrix = offsets.ContainsKey("dwViewMatrix") ? offsets["dwViewMatrix"] : 0x1A1FF40;
        int dwLocalPlayerPawn = offsets.ContainsKey("dwLocalPlayerPawn") ? offsets["dwLocalPlayerPawn"] : 0x1825138;
        //client
        int m_vOldOrigin = clientDll.ContainsKey("m_vOldOrigin") ? clientDll["m_vOldOrigin"] : 0x131C;
        int m_iTeamNum = clientDll.ContainsKey("m_iTeamNum") ? clientDll["m_iTeamNum"] : 0x3E3;
        int m_lifeState = clientDll.ContainsKey("m_lifeState") ? clientDll["m_lifeState"] : 0x348;
        int m_hPlayerPawn = clientDll.ContainsKey("m_hPlayerPawn") ? clientDll["m_hPlayerPawn"] : 0x80C;
        int m_vecViewOffset = clientDll.ContainsKey("m_vecViewOffset") ? clientDll["m_vecViewOffset"] : 0xCA8;
        int m_iszPlayerName = clientDll.ContainsKey("m_iszPlayerName") ? clientDll["m_iszPlayerName"] : 0x660;

        int m_Item = clientDll.ContainsKey("m_Item") ? clientDll["m_Item"] : 0x50;
        int m_pClippingWeapon = clientDll.ContainsKey("m_pClippingWeapon") ? clientDll["m_pClippingWeapon"] : 0x1398;
        int m_iItemDefinitionIndex = clientDll.ContainsKey("m_iItemDefinitionIndex") ? clientDll["m_iItemDefinitionIndex"] : 0x1BA;
        int m_AttributeManager = clientDll.ContainsKey("C_EconEntity.m_AttributeManager")? clientDll["C_EconEntity.m_AttributeManager"]: 0x1140;
        int m_iHealth = clientDll.ContainsKey("m_iHealth") ? clientDll["m_iHealth"] : 0x344;

        while (true)
        {
            entities.Clear();
            IntPtr entityList = swed.ReadPointer(clientBase, dwEntityList);
            IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
            IntPtr localPlayerPawn = swed.ReadPointer(clientBase, dwLocalPlayerPawn);
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
                entities.Add(entity);
                Console.WriteLine($"ent : {entity.currentWeaponName}");
                renderer.UpdateEntites(entities);


            }
            renderer.UpdateLocalPlayer(localPlayer);

            Thread.Sleep(2);
            Console.Clear();
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

