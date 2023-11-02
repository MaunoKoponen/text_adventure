using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Diary : MonoBehaviour
{
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
        QuestLogEntry newEntry = new QuestLogEntry()
        {
            QuestID = questID,
            QuestName = "The Enchanted Estuary",
            QuestDescription = "Map the Enchanted Estuary. Map the Enchanted Estuary. Map the Enchanted Estuary. Map the Enchanted Estuary. Map the Enchanted Estuary. Map the Enchanted Estuary. Map the Enchanted Estuary. Map the Enchanted Estuary.",
            IsCompleted = false
        };

        // Assuming you have a questLog object already
        questLog.AddQuestEntry(newEntry);
        SaveQuestLog(questLog);

        
        string wholeText = "";
        
        foreach (var QuestLogEntry in questLog.Entries)
        {
           string entryAsText =  FormatQuestLogEntryForDisplay(QuestLogEntry);
           wholeText += entryAsText;
        }

        List<string> pages = PaginateText(wholeText, 40);

        text1.text = pages[0];
        text2.text = pages[1];


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
