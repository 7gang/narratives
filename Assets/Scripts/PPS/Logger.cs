using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

public static class Classifier
{
    public enum Metadata    // metadata about the application and environment, unity version, fps...
    {
        // morrored from Application: https://docs.unity3d.com/ScriptReference/Application.html
        DataPath,
        InternetReachability,
        IsFocused,
        Platform,
        SystemLanguage,
        UnityVersion,
        Version,
        FrameCount,
        LowMemory,
        Unloading,
        Quitting,
    }

    public enum Console     // everything that is logged to the console...
    {
        Error,      //
        Assert,     //
        Warning,    //      mirrored from LogType: https://docs.unity3d.com/ScriptReference/LogType.html
        Log,        //
        Exception,  //
    }

    public enum Answer      // the player's answers to the questionnaires...
    {
        ChoiceAvailable,
        Selected,
        Comitted,
    }

    public enum Player      // location of player head and hands, look direction, buttons pressed...
    {
        PlayerHeadPosition,
        PlayerHeadForwardVector,
        PlayerLeftHandPosition,
        PlayerLeftHandRotation,
        PlayerLeftHandInputEngaged,
        PlayerLeftHandInputReleased,
        PlayerRightHandPosition,
        PlayerRightHandRotation,
        PlayerRightHandInputEngaged,
        PlayerRightHandInputReleased,
    }

    public enum Prompt      // active and resolved prompts, sounds playing, amount of text on screen...
    {
        Activated,
        Resolved,
        SoundPlaying,
        SoundStopped,
        TextShowing,
        TextHidden,
    }

    public enum Task        // objects being touched...
    {
        Touching,
        NoLongerTouching,
        Grabbed,
        Released,
    }

    public enum Level       // active level, load events, completion %...
    {
        Loaded,
        Unloaded,
        Started,
        Completed,
    }

    public enum Microverse  // the things that happen in the Microverse specifically...
    {
        MicroverseGunPickup,
        MicroverseGunFired,
        MicroverseGlucoseHit,
        MicroverseSnakeSpawn,
        MicroverseSnakeHit,
        MicroverseAcetHit,
        MicroverseAimingAt,
    }
}

public class LogableEvent
{
    public string classifier;
    public string name;
    public string source;
    public string sourceLevel;
    public string sourceScene;
    public string data;

    public string timestamp;  // this field is populated automatically when the object is initialized

    public LogableEvent(string _classifier, string _name, string _source, string _sourceLevel, string _sourceScene, string _data)
    {
        this.classifier = _classifier;
        this.name = _name;
        this.source = _source;
        this.sourceLevel = _sourceLevel;
        this.sourceScene = _sourceScene;
        this.data = _data;

        this.timestamp = System.DateTime.Now
                                .ToString("yyyy-MM-dd HH:mm:ss.ffff")
                                .Replace('/', '-')
                                .Replace(":", "-");
    }

    public bool Equals(LogableEvent le)
    {
        return this.classifier == le.classifier
            && this.name == le.name
            && this.source == le.source
            && this.sourceLevel == le.sourceLevel
            && this.sourceScene == le.sourceScene
            && this.data == le.data;
        // timestamp should not be compared
    }

    public string ToStringPretty()
    {
        return $"{this.timestamp}: <({this.classifier}) {this.name}> in {this.sourceScene}->{this.sourceLevel} by {this.source}"
            + this.data != null ? $" with message: {this.data}" : "";
    }

    public override string ToString()
    {
        return $"{this.data},{this.classifier},{this.name},{this.sourceScene},{this.sourceLevel},{this.source},{this.timestamp}";
    }
}

public class Logger : MonoBehaviour  // class is almost entirely static
{
    private static bool DO_LOGGING = false;
    private static bool LOG_CONSOLE = true;
    private static bool DO_LOIP = false;
    private static float LOGGING_FREQUENCY = 2f;

    // this is essentially a copy of the above, in case you'd like to set the settings in the Unity Inspector instead of in the code
    [Tooltip("Enable application logging to a .csv file in the user's Documents folder.")]
    public bool doLogging = DO_LOGGING;
    [Tooltip("Log console output.")]
    public bool logConsole = LOG_CONSOLE;
    [HideInInspector]
    [Tooltip("Enable Logging over IP for automatic upload of logging data to a remote server.")]
    public bool doLoIP = DO_LOIP;
    [Tooltip("How many seconds should be between file system writes.")]
    public float loggingFrequency = LOGGING_FREQUENCY;

    private static List<LogableEvent> logQueue = new List<LogableEvent>();
    private static bool fileSystemOperationInProgress = false;
    private static string logFileName = $"/LOG_MOVE_ME_{System.DateTime.Now.ToString("HHmmss-ffff")}.csv";
    private static float pointOfLastWrite = -1f;
    private static int frameCounter = 0;
    private static float frameCountTimestamp = -1f;

    private GameObject playerHead;
    private GameObject leftHand;
    private GameObject rightHand;

    protected void OnEnable()
    {
        Logger.DO_LOGGING = doLogging;
        Logger.LOG_CONSOLE = logConsole;
        Logger.DO_LOIP = doLoIP;

        Application.logMessageReceived += LogConsoleMessage;
        Application.focusChanged += LogFocusChanged;
        Application.lowMemory += LogLowMemory;
        Application.unloading += LogUnloading;
        Application.quitting += OnApplicationQuit;

        // get player hands and head
        this.playerHead = this.GetComponentInChildren<AudioListener>().gameObject;
        IEnumerator hands = this.GetComponentsInChildren<Hand>().GetEnumerator();
        hands.MoveNext();
        this.leftHand = ((Hand)hands.Current).gameObject;
        hands.MoveNext();
        this.rightHand = ((Hand)hands.Current).gameObject;

        //Debug.Log("Wrote data to file path " + Logger.GetFilePath() + Logger.logFileName);

        if (DO_LOGGING)
        {
            Logger.WriteLineToFile("data,classifier,event,scene,level,source,timestamp");

            Logger.Log(Classifier.Metadata.DataPath);
            Logger.Log(Classifier.Metadata.InternetReachability);
            Logger.Log(Classifier.Metadata.Platform);
            Logger.Log(Classifier.Metadata.SystemLanguage);
            Logger.Log(Classifier.Metadata.UnityVersion);
            Logger.Log(Classifier.Metadata.Version);
        }
    }

    protected void Update()
    {
        if (!DO_LOGGING) return;

        Logger.frameCounter++;
        Logger.frameCountTimestamp += Time.deltaTime;
        if (Logger.frameCountTimestamp == -1 || Logger.frameCountTimestamp > 1)
        {
            // log frame count
            Logger.Log(Classifier.Metadata.FrameCount, Logger.frameCounter.ToString());

            // log player data
            this.Log(Classifier.Player.PlayerHeadForwardVector);
            this.Log(Classifier.Player.PlayerHeadPosition);
            this.Log(Classifier.Player.PlayerLeftHandPosition);
            this.Log(Classifier.Player.PlayerLeftHandRotation);
            this.Log(Classifier.Player.PlayerRightHandPosition);
            this.Log(Classifier.Player.PlayerRightHandRotation);

            // reset clock
            Logger.frameCounter = 0;
            Logger.frameCountTimestamp = 0;
        }
    }

    // ===== THE DIFFERENT CLASSIFIERS- AND SUB-CLASSIFIERS =====
    public static void Log(Classifier.Metadata category, string data = "null")
    {
        string eventData = data;
        switch (category)
        {
            case Classifier.Metadata.DataPath:
                eventData = Application.dataPath;
                break;
            case Classifier.Metadata.FrameCount:
                eventData = Logger.frameCounter.ToString();
                break;
            case Classifier.Metadata.InternetReachability:
                eventData = Application.internetReachability.ToString();
                break;
            case Classifier.Metadata.IsFocused:
                eventData = Application.isFocused ? "1" : "0";
                break;
            case Classifier.Metadata.Platform:
                eventData = Application.platform.ToString();
                break;
            case Classifier.Metadata.SystemLanguage:
                eventData = Application.systemLanguage.ToString();
                break;
            case Classifier.Metadata.UnityVersion:
                eventData = Application.unityVersion.ToString();
                break;
            case Classifier.Metadata.Version:
                eventData = Application.version.ToString();
                break;
        }

        Logger.Log(new LogableEvent(
            "Metadata",
            category.ToString(),
            "null",
            "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            eventData
        ));
    }

    public static void Log(Classifier.Console category, string message)
    {
        // ...
    }

    public static void Log(Classifier.Answer category, string data)
    {
        // ...
    }

    public static void Log(Classifier.Player category, string data = "null")
    {
        Logger.Log(new LogableEvent(
            "Player",
            category.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            data.Replace(',', ';')
        ));
    }

    public void Log(Classifier.Player category)
    {
        string dataString = "null";
        switch (category)
        {
            case Classifier.Player.PlayerHeadForwardVector:
                // ...
                break;
            case Classifier.Player.PlayerHeadPosition:
                dataString = this.playerHead.transform.position.ToString();
                break;
            case Classifier.Player.PlayerLeftHandPosition:
                dataString = this.leftHand.transform.position.ToString();
                break;
            case Classifier.Player.PlayerLeftHandRotation:
                dataString = this.leftHand.transform.rotation.ToString();
                break;
            case Classifier.Player.PlayerRightHandPosition:
                dataString = this.rightHand.transform.position.ToString();
                break;
            case Classifier.Player.PlayerRightHandRotation:
                dataString = this.rightHand.transform.rotation.ToString();
                break;
        }

        Logger.Log(new LogableEvent(
            "Player",
            category.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            dataString.Replace(',', ';')
        ));
    }

    public static void Log(Classifier.Prompt category, Prompt prompt)
    {
        Logger.Log(new LogableEvent(
            "Prompt",
            category.ToString(),
            prompt.name,
            prompt.GetParentLevel().name,
            prompt.gameObject.scene != null ? prompt.gameObject.scene.name : "null",
            prompt.gameObject.GetComponent<Task>() != null ? "Has Task" : "null"
        ));
    }

    public static void Log(Classifier.Task category, Task task)
    {
        Logger.Log(new LogableEvent(
            "Task",
            category.ToString(),
            task.name,
            task.GetParentLevel().name,
            task.gameObject.scene != null ? task.gameObject.scene.name : "null",
            "null"
        ));
    }

    public static void Log(Classifier.Level category, Level level)
    {
        bool levelInitialized = level != null;
        Logger.Log(new LogableEvent(
            "level",
            category.ToString(),
            levelInitialized ? level.name : "null",
            "null",
            levelInitialized && level.gameObject.scene != null ? level.gameObject.scene.name : "null",
            "null"
        ));
    }

    public static void Log(Classifier.Microverse category, GameObject source)
    {
        string dataString = "null";
        switch (category)
        {
            case Classifier.Microverse.MicroverseAcetHit:
            case Classifier.Microverse.MicroverseGlucoseHit:
            case Classifier.Microverse.MicroverseSnakeHit:
            case Classifier.Microverse.MicroverseSnakeSpawn:
            case Classifier.Microverse.MicroverseAimingAt:
                dataString = source.transform.position.ToString();
                break;
        }

        Logger.Log(new LogableEvent(
            "Microverse",
            category.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            dataString.Replace(',', ';')
        ));
    }

    public static void Log(Classifier.Microverse category, Vector3 source)
    {
        Logger.Log(new LogableEvent(
            "Microverse",
            category.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            source.ToString().Replace(',', ';')
        ));
    }

    // ===== THE BOTTLENECK LOG METHOD =====
    private static void Log(LogableEvent le)
    {
        if (!DO_LOGGING) return;

        Logger.logQueue.Add(le);
        if (Logger.pointOfLastWrite != -1 && Time.realtimeSinceStartup - Logger.pointOfLastWrite < LOGGING_FREQUENCY) return;

        if (!Logger.fileSystemOperationInProgress)
        {
            List<LogableEvent> logQueueCopy = new List<LogableEvent>(Logger.logQueue);
            Logger.logQueue.Clear();

            Logger.WriteLogableEventsToFile(logQueueCopy);
            // TODO: add LoIP call here...
        }
    }

    // ===== THE SUBSCRIBED LOG EVENTS =====
    private static void LogConsoleMessage(string logString, string stackTrace, LogType type)
    {
        if (!LOG_CONSOLE) return;

        string messageData = $"\"{logString.Replace(',', ' ')}\" in {stackTrace.Replace(',', ' ').Replace("\n", " // ")}";
        Logger.Log(new LogableEvent(
            "Console",
            type.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene().name,
            messageData.Substring(0, messageData.Length - 1)
        ));
    }

    private static void LogFocusChanged(bool isFocused)
    {
        Logger.Log(new LogableEvent(
            "Metadata",
            Classifier.Metadata.IsFocused.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene().name,
            isFocused ? "1" : "0"
        ));
    }

    private static void LogLowMemory()
    {
        Logger.Log(new LogableEvent(
            "Metadata",
            Classifier.Metadata.LowMemory.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene().name,
            "null"
        ));
    }

    private static void LogUnloading()
    {
        Logger.Log(new LogableEvent(
            "Metadata",
            Classifier.Metadata.Unloading.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            "null"
        ));
    }

    private static void OnApplicationQuit()
    {
        Logger.Log(new LogableEvent(
            "Metadata",
            Classifier.Metadata.Quitting.ToString(),
            "null",
            Level.activeLevel != null ? Level.activeLevel.name : "null",
            SceneManager.GetActiveScene() != null ? SceneManager.GetActiveScene().name : "null",
            "null"
        ));

        List<LogableEvent> logQueueCopy = new List<LogableEvent>(Logger.logQueue);
        Logger.WriteLogableEventsToFile(logQueueCopy);
    }

    // ===== THE LOWER-LEVEL FILE SYSTEM AND NETWORKING OPERATIONS =====
    private static bool WriteLogableEventsToFile(List<LogableEvent> eventQueue)
    {
        List<string> LogableStrings = new List<string>();
        foreach (LogableEvent le in eventQueue)
            LogableStrings.Add(le.ToString());

        return Logger.WriteLinesToFile(LogableStrings);
    }

    private static string GetFilePath()
    {
        return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
    }

    private static bool LogFileExists()
    {
        return File.Exists(Logger.GetFullFilePath());
    }

    private static string GetFullFilePath()
    {
        return Path.Combine(Logger.GetFilePath() + Logger.logFileName);
    }

    private static bool WriteLinesToFile(List<string> lines)
    {
        Logger.fileSystemOperationInProgress = true;

        try
        {
            using (StreamWriter file = File.AppendText(Logger.GetFullFilePath()))
            {
                foreach (string line in lines)
                    file.WriteLine(line);
            }

            Logger.fileSystemOperationInProgress = false;
            Logger.pointOfLastWrite = Time.realtimeSinceStartup;
            return true;
        }
        catch (InvalidDataException e)
        {
            Debug.LogError("Target log path exists but is read-only\n" + e);
        }
        catch (PathTooLongException e)
        {
            Debug.LogError("Target log path name may be too long\n" + e);
        }
        catch (IOException e)
        {
            Debug.LogError("The disk may be full\n" + e);
        }

        // TODO: revert log file if write operations fail...

        Logger.fileSystemOperationInProgress = false;
        Logger.pointOfLastWrite = Time.realtimeSinceStartup;
        return false;
    }

    private static bool WriteLineToFile(string line)
    {
        return Logger.WriteLinesToFile(new List<string>() { line });
    }
}