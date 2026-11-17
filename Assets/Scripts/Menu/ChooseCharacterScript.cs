using UnityEngine;
using UnityEngine.UI;

public class ChooseCharacterScript : MonoBehaviour
{
    public GameObject[] characters;
    int characterIndex;

    public GameObject inputField;
    string characterName;
    public GameObject playerDropdownCount;
    public int playerCount = 3;
    public SceneChangerScript sceneChangerScript;


    void Awake()
    {
        characterIndex = 0;
        foreach(GameObject character in characters)
        {
            character.SetActive(false);
        }

        characters[characterIndex].SetActive(true);
    }
    
    public void NextCharacter()
    {
        characters[characterIndex].SetActive(false);
        characterIndex++;
        if(characterIndex == characters.Length) 
        {
            characterIndex = 0;
        }

        characters[characterIndex].SetActive(true);
    }

    public void PreviousCharacter()
    {
        characters[characterIndex].SetActive(false);
        characterIndex--;
        if (characterIndex == -1)
        {
            characterIndex = characters.Length - 1;
        }

        characters[characterIndex].SetActive(true);
    }

    public void Play()
    {
        characterName = inputField.GetComponent<TMPro.TMP_InputField>().text;
        playerCount = playerDropdownCount.GetComponent<TMPro.TMP_Dropdown>().value + 1;
        print(playerCount);
        if (characterName.Length > 2)
        {
            PlayerPrefs.SetInt("SelectedCharacter", characterIndex);
            PlayerPrefs.SetString("PlayerName", characterName);
            PlayerPrefs.SetInt("PlayerCount", playerCount);
            StartCoroutine(sceneChangerScript.Delay("play", characterIndex, characterName, playerCount));
        }
        else
            inputField.GetComponent<TMPro.TMP_InputField>().Select();
    }
}
