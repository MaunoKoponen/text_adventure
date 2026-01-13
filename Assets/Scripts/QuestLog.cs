using System;
using System.Collections.Generic;

[Serializable]
public class QuestLogEntry
{
    public string QuestID;
    public string QuestName;
    public string QuestDescription;
    public bool IsCompleted;
}

[Serializable]
public class QuestLog
{
    public List<QuestLogEntry> Entries = new List<QuestLogEntry>();

    public void AddQuestEntry(QuestLogEntry entry)
    {
        Entries.Add(entry);
    }
}