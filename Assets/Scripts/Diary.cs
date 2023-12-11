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
    public Button prevButton;
    public Button nextButton;

    
    public QuestLog questLog;

    public TMP_Text text1;
    //public TMP_Text text2;

    private int currentPage = 0;
    private List<string> pages;
    public TMP_Text pageNumber;
    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);

        questLog = LoadQuestLog();
        RewriteDiary();
    }
    
    private void OnEnable()
    {
        questLog = LoadQuestLog();
        RewriteDiary();
    }
    
    
    public void SaveQuestLog(QuestLog log)
    {
        string json = JsonUtility.ToJson(log);
        PlayerPrefs.SetString("QuestLog", json);
        PlayerPrefs.Save();
    }

    public void ResetQuestLog()
    {
        PlayerPrefs.SetString("QuestLog", "");
        PlayerPrefs.Save();
    }

    
    public QuestLog LoadQuestLog()
    {
        string json = PlayerPrefs.GetString("QuestLog");
        if (string.IsNullOrEmpty(json)) // game reseted
        {
            QuestLog newLog =  new QuestLog();
            questLog = newLog;
            return questLog;
        }
        
        return JsonUtility.FromJson<QuestLog>(json);
    }
    
    
    public void OnQuestReceived(string questID)
    {
        QuestLogEntry newEntry = LoadQuestData(questID);

        if (questLog == null)
        {
            questLog =  new QuestLog();
        }
            
        if (newEntry != null)
        {
            // Assuming you have a questLog object already
            questLog.AddQuestEntry(newEntry);
            SaveQuestLog(questLog);

            RewriteDiary();
        }
        else
        {
            Debug.LogError("Ooops, no quest data found with questID " + questID);
        }
    }
    
    public void OnQuestConcluded(string questID)
    {
        questLog = LoadQuestLog();
        
        foreach (var questLogEntry in questLog.Entries)
        {
            Debug.Log("Comparing " + questLogEntry.QuestID  + " <----> " + questID );
            
            if (questLogEntry.QuestID == questID)
            {
                Debug.Log("Found the quest entry, changing it to concluded");
                questLogEntry.IsCompleted = true;
            }
           
        }
        SaveQuestLog(questLog);
        RewriteDiary();
    }

    void RewriteDiary()
    {
        string wholeText = "";
        foreach (var QuestLogEntry in questLog.Entries)
        {
            Debug.Log("-------> GuestLogEntry: " + QuestLogEntry.QuestName +  " completed?: " + QuestLogEntry.IsCompleted);
            
            string entryAsText = FormatQuestLogEntryForDisplay(QuestLogEntry);
            wholeText += entryAsText + "\n---\n"; // Added a separator between entries
        }

        pages = PaginateText(wholeText, maxCharactersOnPage); // Adjust the max characters per page as needed
        
        // Update your UI here. Make sure to add checks if there are fewer pages than text components.

        text1.text = pages[currentPage];

        //text1.text = pages.Count > 0 ? pages[0] : "";
        //text2.text = pages.Count > 1 ? pages[1] : "";
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
       pages = new List<string>();

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
    
    void PrevPage()
    {
        currentPage--;
        text1.text = pages[currentPage];
        pageNumber.text = (currentPage + 1).ToString();
    }
    void NextPage()
    {
       
        currentPage++;
        text1.text = pages[currentPage];
        pageNumber.text = (currentPage + 1).ToString();
    }
    
    
    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
}
