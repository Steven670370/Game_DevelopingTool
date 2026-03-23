using Godot;
using System;
using MyGame.Utils;

public partial class LanguageManager : Node
{
    private ConfigFile config = new ConfigFile();

    public String language;
    public event Action<string> LanguageChanged;

    public static LanguageManager Instance { get; private set; }

    public override void _Ready()
    {
        Logger.Info("[LanguageManager] Initializing translations...");

        Translation enTranslation = GD.Load<Translation>("res://Translations/language.en.translation");
        Translation zhTranslation = GD.Load<Translation>("res://Translations/language.zh.translation");

        TranslationServer.AddTranslation(enTranslation);
        TranslationServer.AddTranslation(zhTranslation);

        if (Instance != null)
        {
            Logger.Info("[LanguageManager] Duplicate instance found, freeing this node.");
            QueueFree();
            return;
        }
        Instance = this;
        AddToGroup("Localized");
        ProcessMode = ProcessModeEnum.Always; 
        Logger.Info("[LanguageManager] Ready. Instance set.");

		// Preferences
        Error err = config.Load("user://settings.cfg");
        // If value is returned
        if (err == Error.Ok)
            language = config.GetValue("general", "language", "en").ToString();
        else
            language = "en"; // Default Language

        // Set Langugae
        var currentScene = GetTree().CurrentScene;
        SetLanguage(language, currentScene);

        Logger.Info($"[LanguageManager] Default language set to {language}");
    }

    // Change languages
    public void SetLanguage(string language, Node sceneRoot)
    {
        this.language = language;
        
        Logger.Info($"[LanguageManager] Setting language to: {language}");
        TranslationServer.SetLocale(language);

        config.SetValue("general", "language", language);
        config.Save("user://settings.cfg");

        // Call help function in Helpers.cs
        if (sceneRoot != null)
            RefreshLocalized(sceneRoot);
        else
            foreach (Node node in GetTree().GetNodesInGroup("Localized"))
                LocalizationHelpers.RefreshNodeTranslations(node);

        LanguageChanged?.Invoke(language);

        Logger.Info("[LanguageManager] Language update finished.");

        CardDatabase.LoadAllCards(language);
    }

    public void RefreshLocalized(Node node)
    {
        if (node.IsInGroup("Localized"))
            LocalizationHelpers.RefreshNodeTranslations(node);

        foreach (Node child in node.GetChildren())
            RefreshLocalized(child);
    }

    public string GetLanguage() => language;
}
