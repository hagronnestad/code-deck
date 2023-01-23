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
        private readonly ILogger<ConfigurationProvider> _logger;

        private readonly FileSystemWatcher _fileSystemWatcher;

        public ConfigurationProvider(ILogger<ConfigurationProvider> logger)
        {
            _logger = logger;

            _fileSystemWatcher = new FileSystemWatcher(@".")
            {
                Filter = CONFIGURATION_FILE_NAME,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            _fileSystemWatcher.Changed += (sender, e) => {
                ConfigurationChanged?.Invoke(this, e);
            };
        }

        public bool DoesConfigurationFileExists()
        {
            return File.Exists(CONFIGURATION_FILE_NAME);
        }

        public StreamDeckConfiguration LoadConfiguration()
        {
            try
            {
                if (!DoesConfigurationFileExists())
                {
                    _logger.LogError($"Configuration file does not exist. Filename: {CONFIGURATION_FILE_NAME}");
                    var defaultConfiguration = CreateDefaultConfiguration();
                    var defaultJson = JsonSerializer.Serialize(defaultConfiguration,
                        new JsonSerializerOptions()
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                    File.WriteAllText(CONFIGURATION_FILE_NAME, defaultJson);
                    _logger.LogWarning($"Default configuration file created. Filename: {CONFIGURATION_FILE_NAME}");
                }

                var json = File.ReadAllText(CONFIGURATION_FILE_NAME);

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
                        Name = "DefaultProfile",
                        Pages = new List<Page>()
                        {
                            new Page()
                            {
                                Name = "Home",
                                Keys = new List<Key>()
                                {
                                    new Key()
                                    {
                                        Index = 0,
                                        Text = "Code\nDeck",
                                    },

                                    new Key()
                                    {
                                        Index = 1,
                                        Plugin = "Runner",
                                        Tile = "ShellRunTile",
                                        Text = "Calc",
                                        Settings = new()
                                        {
                                            { "program", "calc.exe" }
                                        },
                                    },

                                    new Key()
                                    {
                                        Index = 2,
                                        Plugin = "Runner",
                                        Tile = "OpenWebsiteTile",
                                        ImagePadding = 5,
                                        Settings = new() {
                                            { "url", "https://youtube.com" }
                                        },
                                    },

                                    new Key()
                                    {
                                        Index = 3,
                                        Plugin = "Counter",
                                        Tile = "CounterTile",
                                    },

                                    new Key()
                                    {
                                        Index = 4,
                                        KeyType = Key.KEY_TYPE_GOTO_PAGE,
                                        Profile = "DefaultProfile",
                                        Page = "Gaming",
                                        Text = "Gaming\nFolder",
                                    },
                                }
                            },

                            new Page()
                            {
                                Name = "Gaming",
                                Keys = new List<Key>()
                                {
                                    new Key()
                                    {
                                        Index = 0,
                                        Text = "BACK",
                                        KeyType = Key.KEY_TYPE_GO_BACK
                                    },

                                    new Key()
                                    {
                                        Index = 1,
                                        Text = "Gaming...",
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
