using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Diary : MonoBehaviour
{
    private int maxCharactersOnPage = 600;
    
    
    public Button ExitButton;

    private QuestLog questLog = new QuestLog();

    public TMP_Text text1;
    public TMP_Text text2;

    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
        LoadQuestLog();
    }
    
    //--------------
    
    public void SaveQuestLog(QuestLog log)
    {
        string json = JsonUtility.ToJson(log);
        PlayerPrefs.SetString("QuestLog", json);
        PlayerPrefs.Save();
    }

    public QuestLog LoadQuestLog()
    {
        string json = PlayerPrefs.GetString("QuestLog");
        return JsonUtility.FromJson<QuestLog>(json);
    }
    
    
    public void OnQuestReceived(string questID)
    {
        QuestLogEntry newEntry = LoadQuestData(questID);

        if (newEntry != null)
        {
            // Assuming you have a questLog object already
            questLog.AddQuestEntry(newEntry);
            SaveQuestLog(questLog);

            string wholeText = "";

            foreach (var QuestLogEntry in questLog.Entries)
            {
                string entryAsText = FormatQuestLogEntryForDisplay(QuestLogEntry);
                wholeText += entryAsText + "\n---\n"; // Added a separator between entries
            }

            List<string> pages = PaginateText(wholeText, maxCharactersOnPage); // Adjust the max characters per page as needed

            // Update your UI here. Make sure to add checks if there are fewer pages than text components.
            text1.text = pages.Count > 0 ? pages[0] : "";
            text2.text = pages.Count > 1 ? pages[1] : "";
        }
    }
    
    public QuestLogEntry LoadQuestData(string questID)
    {
        TextAsset questDataAsset = Resources.Load<TextAsset>("Quests/" + questID);
        
        if (questDataAsset != null)
        {
            QuestLogEntry questData = JsonUtility.FromJson<QuestLogEntry>(questDataAsset.text);
            return questData;
        }
        else
        {
            Debug.LogError("Quest data file not found for: " + questID);
            return null;
        }
    }
    
    
    public string FormatQuestLogEntryForDisplay(QuestLogEntry entry)
    {
        string formattedEntry = "";

        // Add the quest name as a bold and underlined title
        formattedEntry += "<b><u>" + entry.QuestName + "</u></b>\n\n";

        // Add the quest description as normal text
        formattedEntry += entry.QuestDescription + "\n\n";

        // Add a completion status indicator
        string statusText = entry.IsCompleted ? "<color=green>Completed</color>" : "<color=red>In Progress</color>";
        formattedEntry += "Status: " + statusText;

        return formattedEntry;
    }
    
    
    //--------------
    
    
    public List<string> PaginateText(string text, int maxCharactersPerPage)
    {
        List<string> pages = new List<string>();

        string[] words = text.Split(' ');
        string currentPageText = "";
        int currentPageCharCount = 0;

        foreach (string word in words)
        {
            // Strip tags for length calculation
            string wordWithoutTags = Regex.Replace(word, "<.*?>", string.Empty);
            if (currentPageCharCount + wordWithoutTags.Length < maxCharactersPerPage)
            {
                currentPageText += word + " ";
                currentPageCharCount += wordWithoutTags.Length + 1; // +1 for the space
            }
            else
            {
                pages.Add(currentPageText);
                currentPageText = word + " ";
                currentPageCharCount = wordWithoutTags.Length + 1;
            }
        }

        if (!string.IsNullOrEmpty(currentPageText))
        {
            pages.Add(currentPageText);
        }

        return pages;
    }
    
    
    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
}
