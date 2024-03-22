using UnityEngine;

public class DungeonGenerator
{
    private int maxRooms;
    private int rooms;
    
    public Grid GenerateDungeon(int maxRooms,int width=15, int height=15)
    {
        this.maxRooms = maxRooms;
        
        Grid dungeonGrid = new Grid(width, height);
        GenerateBranch(new Vector2Int(width / 2, height/2), dungeonGrid, height);
        return dungeonGrid;
    }

    private void GenerateBranch(Vector2Int currentPosition, Grid grid, int height, int depth = 0)
    {

        Debug.Log("generateBranch, depth: " + depth  +  " height: " + height);
        
        grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.Room;

        if (currentPosition.y >= height - 1 || depth > 10) // depth limit to avoid excessive recursion
        {
            Debug.Log("----> returning");
            return;
        }        
            
        float randomValue = Random.value;
        Debug.Log(">>> " + randomValue);
        
        // Branching logic:
        
        currentPosition.y++;
        
        if (randomValue > 0.7f)
        {
            if (currentPosition.x > 0 &&
                grid.Cells[currentPosition.x, currentPosition.y].Type == CellType.Empty)
            {
                grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.CorridorUp;
                
                if (rooms < maxRooms)
                    GenerateBranch(new Vector2Int(currentPosition.x, currentPosition.y+1), grid, height, depth);
            }
                
        }    
        else if (randomValue > 0.4f)
        {
            if (currentPosition.x > 0 && grid.Cells[currentPosition.x - 1, currentPosition.y].Type == CellType.Empty)
            {
                grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.CorridorLeft;
                //currentPosition.x--;
                //grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.CorridorUp;
                GenerateBranch(new Vector2Int(currentPosition.x - 1, currentPosition.y), grid, height, depth + 1);
            }
            
        }
        else if (randomValue > 0.2f)
        {
            if (currentPosition.x < grid.Width - 1 && grid.Cells[currentPosition.x + 1, currentPosition.y].Type == CellType.Empty)
            {
                grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.CorridorRight;
                //currentPosition.x++;
                //grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.CorridorUp;
                GenerateBranch(new Vector2Int(currentPosition.x + 1, currentPosition.y), grid, height, depth + 1);
            }
        }
        else
        {
            if (currentPosition.x > 0 && grid.Cells[currentPosition.x - 1, currentPosition.y].Type == CellType.Empty && currentPosition.x < grid.Width - 1 && grid.Cells[currentPosition.x + 1, currentPosition.y].Type == CellType.Empty)
            {
                grid.Cells[currentPosition.x, currentPosition.y].Type = CellType.CorridorSplit;
                GenerateBranch(new Vector2Int(currentPosition.x - 1, currentPosition.y), grid, height, depth + 1);
                GenerateBranch(new Vector2Int(currentPosition.x + 1, currentPosition.y), grid, height, depth + 1);
            }
        }
    
    }
}




public enum CellType
{
    Empty, 
    Room,
    RoomDeadEnd,
    CorridorUp,
    CorridorLeft,
    CorridorRight,
    CorridorSplit,
    CorridorDeadEnd,
}

public class Cell
{
    public CellType Type { get; set; }
    public Vector2Int Position { get; set; }

    public Cell(Vector2Int position, CellType type)
    {
        Position = position;
        Type = type;
    }
}

public class Grid
{
    public Cell[,] Cells;
    public int Width, Height;

    public Grid(int width, int height)
    {
        Width = width;
        Height = height;
        Cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cells[x, y] = new Cell(new Vector2Int(x, y), CellType.Empty);
            }
        }
    }
}
