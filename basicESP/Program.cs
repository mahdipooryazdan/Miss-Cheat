using basicESP;
using Swed64;
using System.Numerics;


Swed swed = new Swed("cs2");


IntPtr client = swed.GetModuleBase("client.dll");
Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();
Vector2 screenSize = renderer.screensize;
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();

int dwEntityList = 0x19BDE30;
int dwViewMatrix = 0x1A1FF40;
int dwLocalPlayerPawn = 0x1825138;

int m_vOldOrigin = 0x131C;
int m_iTeamNum = 0x3E3;
int m_lifeState = 0x348;
int m_hPlayerPawn = 0x80C;
int m_vecViewOffset = 0xCA8;
int m_iszPlayerName = 0x660;

int m_Item = 0x50;
int m_pClippingWeapon = 0x1398;
int m_iItemDefinitionIndex = 0x1BA;
int m_AttributeManager = 0x1140;

int m_iHealth = 0x344;
while (true)
{
    entities.Clear();
    IntPtr entityList = swed.ReadPointer(client, dwEntityList);
    IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
    IntPtr localPlayerPawn = swed.ReadPointer(client, dwLocalPlayerPawn);
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
        Console.WriteLine(currentWeapon);

        float heightOffset = 12;
        float[] viewMatrix = swed.ReadMatrix(client + dwViewMatrix);
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


    }
    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntites(entities);
    Thread.Sleep(5);
    Console.Clear();
}