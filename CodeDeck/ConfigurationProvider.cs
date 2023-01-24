using CodeDeck.Models.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeDeck
{
    public class ConfigurationProvider
    {
        public event EventHandler? ConfigurationChanged;

        public const string CONFIGURATION_FILE_NAME = "deck.json";
        public const string CONFIGURATION_FOLDER_NAME = ".codedeck";

        public static readonly string UserFolder;
        public static readonly string ConfigFolder;
        public static readonly string ConfigFile;

        private readonly ILogger<ConfigurationProvider> _logger;
        private readonly FileSystemWatcher _fileSystemWatcher;

        static ConfigurationProvider()
        {
            UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            ConfigFolder = Path.Combine(UserFolder, CONFIGURATION_FOLDER_NAME);
            ConfigFile = Path.Combine(ConfigFolder, CONFIGURATION_FILE_NAME);
        }

        public ConfigurationProvider(ILogger<ConfigurationProvider> logger)
        {
            _logger = logger;

            // Make sure configuration folder exists
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            // Set up file system watcher
            _fileSystemWatcher = new FileSystemWatcher(ConfigFolder)
            {
                Filter = CONFIGURATION_FILE_NAME,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            // Handle file system watcher changed event
            _fileSystemWatcher.Changed += (sender, e) =>
            {
                ConfigurationChanged?.Invoke(this, e);
            };
        }

        public bool DoesConfigurationFileExists()
        {
            return File.Exists(ConfigFile);
        }

        public StreamDeckConfiguration LoadConfiguration()
        {
            try
            {
                if (!DoesConfigurationFileExists())
                {
                    _logger.LogError($"Configuration file does not exist. Filename: {ConfigFile}");
                    var defaultConfiguration = CreateDefaultConfiguration();
                    var defaultJson = JsonSerializer.Serialize(defaultConfiguration,
                        new JsonSerializerOptions()
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                    File.WriteAllText(ConfigFile, defaultJson);
                    _logger.LogWarning($"Default configuration file created. Filename: {ConfigFile}");
                }

                var json = File.ReadAllText(ConfigFile);

                var configuration = JsonSerializer.Deserialize<StreamDeckConfiguration>(json, new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                });

                if (configuration is not null) return configuration;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not load configuration. Exception: {e.Message}");
            }

            return CreateDefaultConfiguration();
        }


        public static StreamDeckConfiguration CreateDefaultConfiguration()
        {
            var config = new StreamDeckConfiguration
            {
                Brightness = 100,

                Profiles = new List<Profile>()
                {
                    new Profile()
                    {
                        Name = Profile.PROFILE_DEFAULT_NAME,
                        Pages = new List<Page>()
                        {
                            new Page()
                            {
                                Name = Page.PAGE_DEFAULT_NAME,
                                Keys = new List<Key>()
                                {
                                    new Key()
                                    {
                                        Index = 1,
                                        Text = "CODE",
                                        BackgroundColor = "#0b367f"
                                    },
                                    new Key()
                                    {
                                        Index = 2,
                                        Plugin = "Runner",
                                        Tile = "OpenWebsiteTile",
                                        Image = "Image/icon.png",
                                        ImagePadding = 5,
                                        Settings = new() {
                                            { "Url", "https://heinandre.no/code-deck" }
                                        },
                                    },
                                    new Key()
                                    {
                                        Index = 3,
                                        Text = "DECK",
                                        BackgroundColor = "#47750b"
                                    },
                                    new Key()
                                    {
                                        Index = 11,
                                        Plugin = "Counter",
                                        Tile = "CounterTile",
                                        BackgroundColor = "#b52610"
                                    },
                                    new Key()
                                    {
                                        Index = 13,
                                        KeyType = Key.KEY_TYPE_GOTO_PAGE,
                                        Profile = "DefaultProfile",
                                        Page = "Page2",
                                        Text = "GO TO\nPAGE 2",
                                    },
                                }
                            },
                            new Page()
                            {
                                Name = "Page2",
                                Keys = new List<Key>()
                                {
                                    new Key()
                                    {
                                        Index = 0,
                                        Text = "BACK",
                                        KeyType = Key.KEY_TYPE_GO_BACK,
                                        BackgroundColor = "#333333"
                                    },
                                    new Key()
                                    {
                                        Index = 1,
                                        Text = "THIS IS\nPAGE 2",
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return config;
        }
    }
}
