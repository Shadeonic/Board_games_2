using UnityEngine;
using System.IO;

public class SaveLoadScript : MonoBehaviour
{
    public string saveFileName = "playerData.json";

    [SerializeField]
    public class GameData
    {
        public int characterIndex;
        public string characterName;
        public int playerCount;
    }

    private GameData gameData = new GameData();

    public void SavePlayerData(int charcterIndex, string characterName, int playerCount)
    {
        gameData.characterIndex = charcterIndex;
        gameData.characterName = characterName;
        gameData.playerCount = playerCount;
        string json = JsonUtility.ToJson(gameData);
        File.WriteAllText(Application.persistentDataPath + "/" + saveFileName, json);
        Debug.Log("Player data saved to " + Application.persistentDataPath + "/" + saveFileName);
    }

    public void LoadPlayerData()
    {
        string filePath = Application.persistentDataPath + "/" + saveFileName;

        if(File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            gameData = JsonUtility.FromJson<GameData>(json);
            Debug.Log("Player data loaded from " + filePath);
        } else
        {
            Debug.Log("Save file not found at " + filePath);
        }
    }
}
