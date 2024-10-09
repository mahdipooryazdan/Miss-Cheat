﻿using basicESP;
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

int dwEntityList = 0x19BDCF0;
int dwViewMatrix = 0x1A1FF70;
int dwLocalPlayerPawn = 0x1825158;

int m_vOldOrigin = 0x131C;
int m_iTeamNum = 0x3E3;
int m_lifeState = 0x348;
int m_hPlayerPawn = 0x80C;
int m_vecViewOffset = 0xCA8;
int m_iszPlayerName = 0x660;

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

        float[] viewMatrix = swed.ReadMatrix(client + dwViewMatrix );
        Entity entity = new Entity();
        entity.team = swed.ReadInt(currentPawn, m_iTeamNum);
        entity.health = swed.ReadInt(currentPawn, m_iHealth);

        entity.position = swed.ReadVec(currentPawn, m_vOldOrigin);
        entity.viewoffset = swed.ReadVec(currentPawn,m_vecViewOffset);
        entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
        entity.name = swed.ReadString(currentController, m_iszPlayerName, 16).Split("\0")[0];
        entity.viewPosition2D = Calculate.WorldToScreen(viewMatrix,Vector3.Add(entity.position,entity.viewoffset),screenSize);  
        entities.Add(entity);


    }
    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntites(entities);

}