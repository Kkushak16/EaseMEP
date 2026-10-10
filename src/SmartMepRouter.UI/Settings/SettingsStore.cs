using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace SmartMepRouter.UI.Settings
{
    /// <summary>Loads and saves <see cref="UserSettings"/> as JSON in %APPDATA%\SmartMepRouter.</summary>
    public class SettingsStore
    {
        private readonly string _path;

        public SettingsStore(string? path = null)
        {
            _path = path ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SmartMepRouter", "settings.json");
        }

        /// <summary>Returns stored settings, or defaults if the file is missing or unreadable.</summary>
        public UserSettings Load()
        {
            try
            {
                if (!File.Exists(_path)) return new UserSettings();
                using (var stream = File.OpenRead(_path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(UserSettings));
                    return (UserSettings?)serializer.ReadObject(stream) ?? new UserSettings();
                }
            }
            catch (Exception ex) when (ex is IOException || ex is System.Runtime.Serialization.SerializationException || ex is UnauthorizedAccessException)
            {
                return new UserSettings();
            }
        }

        /// <summary>Writes settings; I/O problems are ignored because settings are a convenience, not state.</summary>
        public void Save(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                using (var stream = File.Create(_path))
                {
                    new DataContractJsonSerializer(typeof(UserSettings)).WriteObject(stream, settings);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }
    }
}
