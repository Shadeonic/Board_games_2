using UnityEngine;
using System.IO;

public class PlayerScript : MonoBehaviour
{
    public GameObject[] playerPrefabs;
    int characterIndex;
    public GameObject spawnPoint;
    int[] otherPlayers;
    int otherPlayersIndex;
    private const string playerNamesTxt = "playernames";

    void Start()
    {
        characterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        GameObject mainCharacter =
            Instantiate(playerPrefabs[characterIndex], spawnPoint.transform.position,
            Quaternion.identity);

        mainCharacter.GetComponent<NameScript>().SetPlayerName(
            PlayerPrefs.GetString("PlayerName", "PlayerName"));

        otherPlayers = new int[PlayerPrefs.GetInt("PlayerCount")];
        string[] nameArray = ReadLinesFromFile(playerNamesTxt);
        
        for(int i=0; i<otherPlayers.Length-1; i++)
        {
            spawnPoint.transform.position += new Vector3(0.2f, 0, 0.08f);
            otherPlayersIndex = Random.Range(0, playerPrefabs.Length);
            GameObject character = Instantiate(
                playerPrefabs[otherPlayersIndex],
                spawnPoint.transform.position,
                Quaternion.identity);

            character.GetComponent<NameScript>().SetPlayerName(
                nameArray[Random.Range(0, nameArray.Length)]);
        }
    }

    string[] ReadLinesFromFile(string playerNamesTxt)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(playerNamesTxt);

        if (textAsset != null)
        {
            return textAsset.text.Split(new[] { '\r', '\n'},
                System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            Debug.Log("Failed to load player names from " + playerNamesTxt);
            return new string[0];
        }
    }
}
