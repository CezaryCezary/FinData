using System;
using System.IO;
using log4net;
using Newtonsoft.Json;

namespace NewsService.Helpers
{
    public interface ISerializer
    {
        void Serialize(DateTime lastDateTime, string path);
        T Deserialize<T>(string path);
    }

    public class Serializer : ISerializer
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Serialize(DateTime lastDateTime, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(lastDateTime, Formatting.Indented));
            Log.DebugFormat("Properly serialized to file: {0}", lastDateTime);
        }

        public T Deserialize<T>(string path)
        {
            try
            {
                var content = File.ReadAllText(path);
                Log.Debug("Deserialized content: " + content);
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.ErrorFormat("Unauthorized access {0}. Check access rights to path: {1}", ex.Message, path);
                throw;
            }
            catch (DirectoryNotFoundException ex)
            {
                Log.ErrorFormat("Directory does not exists {0}. Creating...", ex.Message);
                //TODO: remove potential bug from here
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            catch (FileNotFoundException ex)
            {
                Log.ErrorFormat("File does not exists {0}. Creating...", ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                Log.ErrorFormat("Path is null: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Exception thrown in Deserialize: {0}", ex.Message);
            }

            File.WriteAllText(path, 0.ToString());
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
    }
}
