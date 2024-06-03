using System.Collections;
using System.IO;
using System.IO.Compression;

using MaterialPainter2;

using UnityEngine;
using UnityEngine.Networking;

public class FileDownloader : MonoBehaviour
{
    // Singleton instance for easy access
    public static FileDownloader instance;

    public static FileDownloader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("FileDownloader");
                instance = obj.AddComponent<FileDownloader>();
            }
            return instance;
        }
    }

    // Coroutine to download the file
    public IEnumerator DownloadFile(string url, string path, bool extract = false)
    {
        string directoryPath = System.IO.Path.GetDirectoryName(path);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        MP2.MPDebug("DL start");
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                MP2.MPDebug("Error: " + webRequest.error);
            }
            else
            {
                byte[] fileData = webRequest.downloadHandler.data;
                File.WriteAllBytes(path, fileData);
                MP2.MPDebug("File successfully downloaded and saved to " + path);

                if (extract)
                {
                    ZipFile.ExtractToDirectory(path, System.IO.Path.GetDirectoryName(path));
                    File.Delete(path);
                }
            }
        }
    }
}
