using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CombatGrid : MonoBehaviour
{


    public List<GameObject> prefabs;
    
    public List<CombatCell> cells = new List<CombatCell>();

    
    private int itemTypesUsed = 4; 
    
    float iconSizeX = 100.0f; // Size of each cell in the grid
    float iconSizeY = 100.0f; // Size of each cell in the grid
    
    private int maxItemsInLine = 12;
    private int rows = 3;

    float rowSpeed1 = 15f;
    float rowSpeed2 = -15f;
    float rowSpeed3 = 7f;

    public Transform aParent;
    
    private void Awake()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        float posX = 0;
        float posY = 0;
        
        float speed = 0;
        

        
        for (var i = 0; i < rows; i++)
        {
            
            if (i == 0)
                speed = rowSpeed1;
            
            if (i == 1)
                speed = rowSpeed2;

            if (i == 2)
                speed = rowSpeed3;
            
            for (var k = 0; k < maxItemsInLine; k++)
            {
                Debug.Log("making item");

                GameObject  prefab = prefabs[Random.Range(0,itemTypesUsed)];

                float xCorrection = 0;
                float yCorrection = 1000;

                
                var go = Instantiate(prefab, new Vector3((k * 100)+xCorrection,  ( i * 100)+yCorrection, 0), Quaternion.identity);
                //var combatCell = new CombatCell(posX, posY, itemTypesUsed);
                go.transform.SetParent(aParent,true);

                var combatCell = go.AddComponent<CombatCell>();

                combatCell.gameObject = go;
                combatCell.baseSpeed = speed;

                go.GetComponent<CombatCell>().Initialize();

                cells.Add(combatCell);
                
                posX += iconSizeX;
            }

            posY += iconSizeY;
            
        }    
            
    }
}


public class CombatCell :MonoBehaviour
{
    public GameObject gameObject;
    public float baseSpeed;
    
    public CombatCell(float x, float y, int itemTypesUsed)
    {
        //(Random.Range(0,itemTypesUsed);
        
    }

    public void Initialize()
    {
        Button button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(Clicked);
    }

    void Clicked()
    {
        Debug.Log("Clicked..  " + transform.localPosition.x);
    }

    private void Update()
    {
        Vector3 curPos = transform.position;
        float newXpos = curPos.x + baseSpeed * Time.deltaTime;


        if (newXpos > 1300)
            newXpos = -100;
        
        if (newXpos < -100)
            newXpos = 1300;

        
        transform.position = new Vector3(newXpos , curPos.y, curPos
            .z);
        
        
        // if clicked, if conditions ok, destroy
        // mark other similar adjacent ones to be destroyed,

        // ...

        //setup which is the neighbours to update position

        // update position
        // 

        //throw new NotImplementedException();
    }
}