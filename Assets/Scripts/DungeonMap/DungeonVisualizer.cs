using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class DungeonVisualizer : MonoBehaviour
{
    public GameObject roomPrefab;
    public GameObject roomDeadEndPrefab;

    public GameObject corridorUpPrefab;
    public GameObject corridorLeftPrefab;
    public GameObject corridorRightPrefab;
    public GameObject corridorSplitPrefab;
    public GameObject corridorDeadEndPrefab;

    
    float cellSize = 100.0f; // Size of each cell in the grid

    private DungeonGenerator generator;

    public Transform mapPanel; // Parent panel for minimap images

    
    private void Awake()
    { 
        generator = new DungeonGenerator();

        Grid dungeonGrid = generator.GenerateDungeon(10);

        VisualizeDungeon(dungeonGrid);
    }


    public void VisualizeDungeon(Grid grid)
    {
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Cell cell = grid.Cells[x, y];


                Vector3 position = new Vector3((x * cellSize), (y * cellSize)-1000.0f, 0) ;
                GameObject prefab = null;

                switch (cell.Type)
                {
                    case CellType.Room:
                        prefab = roomPrefab;
                        break;
                    case CellType.CorridorUp:
                        prefab = corridorUpPrefab;
                        break;
                    case CellType.CorridorLeft:
                        prefab = corridorLeftPrefab;
                        break;
                    case CellType.CorridorRight:
                        prefab = corridorRightPrefab;
                        break;
                    case CellType.CorridorSplit:
                        prefab = corridorSplitPrefab;
                        break;

                    
                    case CellType.Empty:
                    default:
                        continue; // Skip empty cells
                }

                GameObject cellImage = Instantiate(prefab, position, Quaternion.identity);
                cellImage.transform.SetParent(mapPanel,true);
                cellImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(x * cellSize, y * cellSize);
            }
        }
    }
    
    
    /*
    public void VisualizeDungeon(Grid grid)
    {
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Cell cell = grid.Cells[x, y];
                Vector3 position = new Vector3(x * cellSize, 0, y * cellSize);
                GameObject prefab = null;

                switch (cell.Type)
                {
                    case CellType.Room:
                        prefab = roomPrefab;
                        break;
                    case CellType.Corridor:
                        prefab = corridorPrefab;
                        break;
                    case CellType.Empty:
                    default:
                        continue; // Skip empty cells
                }

                Instantiate(prefab, position, Quaternion.identity, transform);
            }
        }
    }
    */
}

