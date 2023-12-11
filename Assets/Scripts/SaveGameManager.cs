using System;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveGameManager
{
    private static string saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.dat");

    public static void SaveGame(PlayerData playerData)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(saveFilePath, FileMode.Create))
        {
            formatter.Serialize(stream, playerData);
        }
    }

    
    public static void LoadGame()
    {
        Debug.Log("File path for game save: " + saveFilePath);
        
        try
        {
            if (File.Exists(saveFilePath))
            {
                using (FileStream stream = new FileStream(saveFilePath, FileMode.Open))
                {
                    if(stream.Length == 0)
                    {
                        Debug.LogError("Save file is empty.");
                        //return null;
                    }
                    BinaryFormatter formatter = new BinaryFormatter();
                    RoomManager.playerData =  formatter.Deserialize(stream) as PlayerData;
                    Debug.Log("Loading data should be ok.");
                }
            }
            else
            {
                Debug.LogWarning("Save file not found, returning null.");
                //return null;
            }
        }
        catch (SerializationException ex)
        {
            Debug.LogError("Failed to deserialize. Reason: " + ex.Message);
            //return null;
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred: " + ex.Message);
            //return null;
        }
    }
    
    
    /*
    public static PlayerData LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            Debug.Log("File exists in " + saveFilePath);
            
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(saveFilePath, FileMode.Open))
            {
                return formatter.Deserialize(stream) as PlayerData;
            }
        }
        else
        {
            Debug.LogWarning("Save file not found, returning null.");
            return null;
        }
    }

*/
    public static bool SaveFileExists()
    {
        return File.Exists(saveFilePath);
    }
}