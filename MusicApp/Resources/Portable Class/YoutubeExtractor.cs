using System;
using System.Collections.Generic;
using Android.Runtime;
using Android.Util;
using Java.Lang;
using Java.Util.Regex;
using Java.Net;
using Java.IO;
using Java.Util.Concurrent.Locks;
using Android.Webkit;
using MusicApp.Resources.values;

namespace MusicApp.Resources.Portable_Class
{
    public class YoutubeExtractor : Android.OS.AsyncTask<string, int, SparseArray<YtFile>>, IValueCallback
    {
        private const int dashRetries = 5;
        private bool parseDashManifest;
        private bool includeWebM;
        private Song song = null;

        private string videoID;
        private bool useHttps = true;

        private /*volatile*/ string decipheredSignature;

        private string curJsFileName;
        private const string cacheFileName = "decipher_js_funct";

        private static string decipherJsFileName;
        private static string decipherFunctions;
        private static string decipherFunctionName;

        private static ILock Ilock = new ReentrantLock();
        private ICondition jsExecution = Ilock.NewCondition();

        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36";

        private Pattern patYouTubePageLink = Pattern.Compile("(http|https)://(www\\.|m.|)youtube\\.com/watch\\?v=(.+?)( |\\z|&)");
        private Pattern patYouTubeShortLink = Pattern.Compile("(http|https)://(www\\.|)youtu.be/(.+?)( |\\z|&)");

        private Pattern patTitle = Pattern.Compile("title=(.*?)(&|\\z)");
        private Pattern patHlsvp = Pattern.Compile("hlsvp=(.+?)(&|\\z)");
        private Pattern patAuthor = Pattern.Compile("author=(.+?)(&|\\z)");
        private Pattern patChannelId = Pattern.Compile("ucid=(.+?)(&|\\z)");
        private Pattern patLength = Pattern.Compile("length_seconds=(\\d+?)(&|\\z)");
        private Pattern patViewCount = Pattern.Compile("view_count=(\\d+?)(&|\\z)");

        private Pattern patHlsItag = Pattern.Compile("/itag/(\\d+?)/");
        private Pattern patDecryptionJsFile = Pattern.Compile("jsbin\\\\/(player-(.+?).js)");

        private Pattern patDashManifest1 = Pattern.Compile("dashmpd=(.+?)(&|\\z)");
        private Pattern patDashManifest2 = Pattern.Compile("\"dashmpd\":\"(.+?)\"");
        private Pattern patDashManifestEncSig = Pattern.Compile("/s/([0-9A-F|\\.]{10,}?)(/|\\z)");

        private Pattern patItag = Pattern.Compile("itag=([0-9]+?)(&|,)");
        private Pattern patEncSig = Pattern.Compile("s=([0-9A-F|\\.]{10,}?)(&|,|\")");
        private Pattern patUrl = Pattern.Compile("url=(.+?)(&|,)");

        private Pattern patVariableFunction = Pattern.Compile("(\\{|;| |=)([a-zA-Z$][a-zA-Z0-9$]{0,2})\\.([a-zA-Z$][a-zA-Z0-9$]{0,2})\\(");
        private Pattern patFunction = Pattern.Compile("(\\{|;| |=)([a-zA-Z$_][a-zA-Z0-9$]{0,2})\\(");
        private Pattern patSignatureDecFunction = Pattern.Compile("\\(\"signature\",(.{1,3}?)\\(.{1,10}?\\)");


        #region formats
        private Dictionary<int, Format> formats = new Dictionary<int, Format>
        {
            // http://en.wikipedia.org/wiki/YouTube#Quality_and_formats

            // Video and Audio
            { 17, new Format(17, "3gp", 144, Format.VCodec.MPEG4, Format.ACodec.AAC, 24, false) },
            {36, new Format(36, "3gp", 240, Format.VCodec.MPEG4, Format.ACodec.AAC, 32, false)},
            {5, new Format(5, "flv", 240, Format.VCodec.H263, Format.ACodec.MP3, 64, false)},
            {43, new Format(43, "webm", 360, Format.VCodec.VP8, Format.ACodec.VORBIS, 128, false)},
            {18, new Format(18, "mp4", 360, Format.VCodec.H264, Format.ACodec.AAC, 96, false)},
            {22, new Format(22, "mp4", 720, Format.VCodec.H264, Format.ACodec.AAC, 192, false)},

            // Dash Video
            {160, new Format(160, "mp4", 144, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {133, new Format(133, "mp4", 240, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {134, new Format(134, "mp4", 360, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {135, new Format(135, "mp4", 480, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {136, new Format(136, "mp4", 720, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {137, new Format(137, "mp4", 1080, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {264, new Format(264, "mp4", 1440, Format.VCodec.H264, Format.ACodec.NONE, true)},
            {266, new Format(266, "mp4", 2160, Format.VCodec.H264, Format.ACodec.NONE, true)},

            {298, new Format(298, "mp4", 720, Format.VCodec.H264, 60, Format.ACodec.NONE, true)},
            {299, new Format(299, "mp4", 1080, Format.VCodec.H264, 60, Format.ACodec.NONE, true)},

            // Dash Audio
            {140, new Format(140, "m4a", Format.VCodec.NONE, Format.ACodec.AAC, 128, true)},
            {141, new Format(141, "m4a", Format.VCodec.NONE, Format.ACodec.AAC, 256, true)},

            // WEBM Dash Video
            {278, new Format(278, "webm", 144, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {242, new Format(242, "webm", 240, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {243, new Format(243, "webm", 360, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {244, new Format(244, "webm", 480, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {247, new Format(247, "webm", 720, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {248, new Format(248, "webm", 1080, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {271, new Format(271, "webm", 1440, Format.VCodec.VP9, Format.ACodec.NONE, true)},
            {313, new Format(313, "webm", 2160, Format.VCodec.VP9, Format.ACodec.NONE, true)},

            {302, new Format(302, "webm", 720, Format.VCodec.VP9, 60, Format.ACodec.NONE, true)},
            {308, new Format(308, "webm", 1440, Format.VCodec.VP9, 60, Format.ACodec.NONE, true)},
            {303, new Format(303, "webm", 1080, Format.VCodec.VP9, 60, Format.ACodec.NONE, true)},
            {315, new Format(315, "webm", 2160, Format.VCodec.VP9, 60, Format.ACodec.NONE, true)},

            // WEBM Dash Audio
            {171, new Format(171, "webm", Format.VCodec.NONE, Format.ACodec.VORBIS, 128, true)},

            {249, new Format(249, "webm", Format.VCodec.NONE, Format.ACodec.OPUS, 48, true)},
            {250, new Format(250, "webm", Format.VCodec.NONE, Format.ACodec.OPUS, 64, true)},
            {251, new Format(251, "webm", Format.VCodec.NONE, Format.ACodec.OPUS, 160, true)},

            // HLS Live Stream
            {91, new Format(91, "mp4", 144 , Format.VCodec.H264, Format.ACodec.AAC, 48, false, true)},
            {92, new Format(92, "mp4", 240 , Format.VCodec.H264, Format.ACodec.AAC, 48, false, true)},
            {93, new Format(93, "mp4", 360 , Format.VCodec.H264, Format.ACodec.AAC, 128, false, true)},
            {94, new Format(94, "mp4", 480 , Format.VCodec.H264, Format.ACodec.AAC, 128, false, true)},
            {95, new Format(95, "mp4", 720 , Format.VCodec.H264, Format.ACodec.AAC, 256, false, true)},
            {96, new Format(96, "mp4", 1080 , Format.VCodec.H264, Format.ACodec.AAC, 256, false, true)},
        };
#endregion


        public YoutubeExtractor()
        {

        }

        public YoutubeExtractor(IntPtr doNotUse, JniHandleOwnership transfer) : base(doNotUse, transfer)
        {

        }

        public void Extract(string youtubeURL, bool parseDashManifest, bool includeWebM, Song song = null)
        {
            this.parseDashManifest = parseDashManifest;
            this.includeWebM = includeWebM;
            this.song = song;
            this.Execute(youtubeURL);
        }

        protected override void OnPreExecute()
        {
            base.OnPreExecute();
        }

        protected override void OnPostExecute(Java.Lang.Object result)
        {
            base.OnPostExecute(result);
            OnExtractionComplete((SparseArray<YtFile>) result, song);
        }

        protected override void OnPostExecute(SparseArray<YtFile> ytFiles)
        {
            System.Console.WriteLine("Post Execute");
            base.OnPostExecute(ytFiles);
            OnExtractionComplete(ytFiles, song);
        }

        public delegate void ExtractionComplete(SparseArray<YtFile> ytFiles, Song song);
        public event ExtractionComplete OnExtractionComplete;

        protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] native_parms)
        {
            videoID = null;
            string ytUrl = native_parms[0].ToString();
            if (ytUrl == null)
                return null;

            Matcher matcher = patYouTubePageLink.Matcher(ytUrl);
            if (matcher.Find())
                videoID = matcher.Group(3);
            else
            {
                matcher = patYouTubeShortLink.Matcher(ytUrl);
                if (matcher.Find())
                    videoID = matcher.Group(3);
                else if (new Java.Lang.String(videoID).Matches("\\p{Graph}+?"))
                    videoID = ytUrl;
            }
            if (videoID != null)
            {
                try
                {
                    var urls = GetStreamUrls();
                    System.Console.WriteLine("Finial Size: " + urls.Size());
                    return urls;
                }
                catch (Java.Lang.Exception e)
                {
                    e.PrintStackTrace();
                }
            }
            else
                System.Console.WriteLine("Youtube link not supported");
            return null;
        }

        private SparseArray<YtFile> GetStreamUrls()
        {
            string ytInfoUrl = (useHttps) ? "https://" : "http://";
            ytInfoUrl += "www.youtube.com/get_video_info?video_id=" + videoID + "&eurl=" + URLEncoder.Encode("https://youtube.googleapis.com/v/" + videoID, "UTF-8");

            //string dashMpdUrl = null;
            string streamMap = null;
            BufferedReader reader = null;
            URL url = new URL(ytInfoUrl);
            HttpURLConnection urlConnection = (HttpURLConnection)url.OpenConnection();
            urlConnection.SetRequestProperty("User-Agent", USER_AGENT);

            try
            {
                reader = new BufferedReader(new InputStreamReader(urlConnection.InputStream));
                streamMap = reader.ReadLine();
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                urlConnection.Disconnect();
            }

            VideoMeta videoMeta = ParseVideoMeta(streamMap);
            System.Console.WriteLine(videoMeta.title);
            if (videoMeta.isLiveStream)
            {
                return GetLiveStreamUrls(streamMap);
            }

            SparseArray<string> encSignatures = new SparseArray<string>();
            string dashMpdUrl = null;

            if (streamMap == null || !streamMap.Contains("use_cipher_signature=False"))
                encSignatures = DecipherJsFile(streamMap);
            else
            {
                if (parseDashManifest)
                {
                    Matcher matcher = patDashManifest1.Matcher(streamMap);
                    if (matcher.Find())
                    {
                        dashMpdUrl = URLDecoder.Decode(matcher.Group(1), "UTF-8");
                    }
                }
                streamMap = URLDecoder.Decode(streamMap, "UTF-8");
            }
            Java.Lang.String streamMapJL = new Java.Lang.String(streamMap);
            string[] streams = streamMapJL.Split(",|url_encoded_fmt_stream_map|&adaptive_fmts=");
            SparseArray<YtFile> ytFiles = new SparseArray<YtFile>();

            foreach (string foo in streams)
            {
                string encStream = foo + ",";
                if (!encStream.Contains("itag%3D"))
                    continue;

                string stream = URLDecoder.Decode(encStream, "UTF-8");

                Matcher matcher = patItag.Matcher(stream);
                int itag;
                if (matcher.Find())
                {
                    itag = int.Parse(matcher.Group(1));
                    if (formats[itag] == null)
                        continue;
                    else if (!includeWebM && formats[itag].GetExt().Equals("webm"))
                        continue;
                }
                else
                    continue;

                if (curJsFileName != null)
                {
                    matcher = patEncSig.Matcher(stream);
                    if (matcher.Find())
                    {
                        encSignatures.Append(itag, matcher.Group(1));
                    }
                }
                matcher = patUrl.Matcher(encStream);
                string uri = null;
                if (matcher.Find())
                    uri = matcher.Group(1);

                if(uri != null)
                {
                    Format format = formats[itag];
                    string finalUrl = URLDecoder.Decode(uri, "UTF-8");
                    YtFile newVideo = new YtFile(format, finalUrl);
                    ytFiles.Put(itag, newVideo);
                }
            }
            System.Console.WriteLine("Size: " + ytFiles.Size());
            if(encSignatures != null)
            {
                decipheredSignature = null;
                bool boo = DecipherSignature(encSignatures);
                System.Console.WriteLine("booleanbOO : " + boo);
                if (boo)
                {
                    Ilock.Lock();
                    try
                    {
                        jsExecution.Await(7, Java.Util.Concurrent.TimeUnit.Seconds);
                    }
                    finally
                    {
                        Ilock.Unlock();
                    }
                }
                if (decipheredSignature == null)
                    return null;
                else
                {
                    string[] signatures = new Java.Lang.String(decipheredSignature).Split("\n");
                    for (int i = 0; i < encSignatures.Size() && i < signatures.Length; i++)
                    {
                        int key = encSignatures.KeyAt(i);
                        if (key == 0)
                            dashMpdUrl = dashMpdUrl.Replace("/s/" + encSignatures.Get(key), "/signature/" + signatures[i]);
                        else
                        {
                            string uri = ytFiles.Get(key).url;
                            uri += "&signature=" + signatures[i];
                            YtFile newFile = new YtFile(formats[key], uri);
                            ytFiles.Put(key, newFile);
                        }
                    }
                }
            }
            if(parseDashManifest && dashMpdUrl != null)
            {
                for (int i = 0; i < dashRetries; i++)
                {
                    try
                    {
                        ParseDashManifest(dashMpdUrl, ytFiles);
                        break;
                    }
                    catch(IOException)
                    {
                        Thread.Sleep(5);
                    }
                }
            }
            if (ytFiles.Size() == 0)
                return null;
            return ytFiles;
        }

        private bool DecipherSignature(SparseArray<string> encSignatures)
        {
            bool dfn = decipherFunctionName == null;
            bool dfs = decipherFunctions == null;
            System.Console.WriteLine("DecipherFunctionName: " + dfn + " DecipherFunctions: " + dfs);
            if(decipherFunctionName == null || decipherFunctions == null)
            {
                string decipherFunctUrl = "https://s.ytimg.com/yts/jsbin/" + decipherJsFileName;
                URL url = new URL(decipherFunctUrl);
                HttpURLConnection urlConnection = (HttpURLConnection)url.OpenConnection();
                urlConnection.SetRequestProperty("User-Agent", USER_AGENT);
                BufferedReader reader = null;
                string javascriptFile = null;
                try
                {
                    reader = new BufferedReader(new InputStreamReader(urlConnection.InputStream));
                    StringBuilder sb = new StringBuilder("");
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        sb.Append(line);
                        sb.Append(" ");
                    }
                    javascriptFile = sb.ToString();
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                    urlConnection.Disconnect();
                }

                Matcher matcher = patSignatureDecFunction.Matcher(javascriptFile);
                if (matcher.Find())
                {
                    decipherFunctionName = matcher.Group(1);
                    Pattern patMainVariable = Pattern.Compile("(var |\\s|,|;)" + decipherFunctionName.Replace("$", "\\$") + "(=function\\((.{1,3})\\)\\{)");
                    string mainDecipherFunct;

                    matcher = patMainVariable.Matcher(javascriptFile);
                    if (matcher.Find())
                        mainDecipherFunct = "var " + decipherFunctionName + matcher.Group(2);
                    else
                    {
                        Pattern patMainFunction = Pattern.Compile("function " + decipherFunctionName.Replace("$", "\\$") + "(\\((.{1,3})\\)\\{)");
                        matcher = patMainFunction.Matcher(javascriptFile);
                        if (!matcher.Find())
                            return false;
                        mainDecipherFunct = "function " + decipherFunctionName + matcher.Group(2);
                    }

                    System.Console.WriteLine("Etap 1 succed");
                    int startIndex = matcher.End();
                    char[] javascriptChars = javascriptFile.ToCharArray();
                    for (int braces = 1, i = 0; i < javascriptFile.Length; i++)
                    {
                        if (braces == 0 && startIndex + 5 < i)
                        {
                            mainDecipherFunct += javascriptFile.Substring(startIndex, i) + ";";
                            break;
                        }
                        if (javascriptChars[i] == '{')
                            braces++;
                        else if (javascriptChars[i] == '}')
                            braces--;
                    }
                    decipherFunctions = mainDecipherFunct;
                    System.Console.WriteLine("Etap 2 succed");
                    matcher = patVariableFunction.Matcher(mainDecipherFunct);
                    while (matcher.Find())
                    {
                        string variableDef = "var " + matcher.Group(2) + "={";
                        if (decipherFunctions.Contains(variableDef))
                            continue;

                        startIndex = javascriptFile.IndexOf(variableDef) + variableDef.Length;
                        for (int braces = 1, i = 0; i < javascriptFile.Length - startIndex; i++)
                        {
                            if (braces == 0)
                            {
                                decipherFunctions += variableDef + javascriptFile.Substring(startIndex, i) + ";";
                                break;
                            }
                            if (javascriptFile[i] == '{')
                                braces++;
                            else if (javascriptFile[i] == '}')
                                braces--;
                        }
                    }

                    System.Console.WriteLine("Etap 3 succed");
                    matcher = patFunction.Matcher(mainDecipherFunct);
                    while (matcher.Find())
                    {
                        string functionDef = "function " + matcher.Group(2) + "(";
                        if (decipherFunctions.Contains(functionDef))
                            continue;

                        startIndex = javascriptFile.IndexOf(functionDef) + functionDef.Length;
                        for (int braces = 0, i = 0; i < javascriptFile.Length - startIndex; i++)
                        {
                            if (braces == 0 && startIndex + 5 < i)
                            {
                                decipherFunctions += functionDef + javascriptFile.Substring(startIndex, i) + ";";
                                break;
                            }
                            if (javascriptFile[i] == '{')
                                braces++;
                            else if (javascriptFile[i] == '}')
                                braces--;
                        }
                    }
                    System.Console.WriteLine("Work fine");
                    DecipherViaWebView(encSignatures);
                    System.Console.WriteLine("Deciphered via Web");
                    WriteDeciperFunctToCache();
                }
                else
                    return false;
            }
            else
                DecipherViaWebView(encSignatures);
            return true;
        }

        private void DecipherViaWebView(SparseArray<string> encSignatures)
        {
            StringBuilder stringBuilder = new StringBuilder(decipherFunctions + " function decipher(");
            stringBuilder.Append("){return ");
            for (int i = 0; i < encSignatures.Size(); i++)
            {
                int key = encSignatures.KeyAt(i);
                if (i < encSignatures.Size() - 1)
                    stringBuilder.Append(decipherFunctionName).Append("('").Append(encSignatures.Get(key)).Append("')+\"\\n\"+");
                else
                    stringBuilder.Append(decipherFunctionName).Append("('").Append(encSignatures.Get(key)).Append("')");
            }
            stringBuilder.Append("};decipher();");

            Android.OS.Handler handler = new Android.OS.Handler(MainActivity.instance.MainLooper);
            handler.Post(() =>
            {
                WebView webView = new WebView(Android.App.Application.Context);
                webView.EvaluateJavascript(stringBuilder.ToString(), this);
            });

            //Android.OS.Handler handler = new Android.OS.Handler((sender) => 
            //{
            //    WebView webView = new WebView(Android.App.Application.Context);
            //    webView.EvaluateJavascript(stringBuilder.ToString(), this);
            //});
        }

        public void OnReceiveValue(Java.Lang.Object value)
        {
            System.Console.WriteLine("Value receive");
            Ilock.Lock();
            try
            {
                decipheredSignature = value.ToString();
            }
            finally
            {
                Ilock.Unlock();
            }
        }

        private void WriteDeciperFunctToCache()
        {
            File cacheFile = new File(Android.App.Application.Context.CacheDir.AbsolutePath + "/" + cacheFileName);
            BufferedWriter writer = null;
            try
            {
                writer = new BufferedWriter(new FileWriter(cacheFile));
                writer.Write(decipherJsFileName + "\n");
                writer.Write(decipherFunctionName + "\n");
                writer.Write(decipherFunctions);
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private void ParseDashManifest(string dashMpdUrl, SparseArray<YtFile> ytFiles)
        {
            Pattern patBaseUrl = Pattern.Compile("<BaseURL yt:contentLength=\"[0-9]+?\">(.+?)</BaseURL>");
            URL url = new URL(dashMpdUrl);
            HttpURLConnection urlConnection = (HttpURLConnection)url.OpenConnection();
            urlConnection.SetRequestProperty("User-Agent", USER_AGENT);
            BufferedReader reader = null;
            string dashManifest;
            try
            {
                reader = new BufferedReader(new InputStreamReader(urlConnection.InputStream));
                reader.ReadLine();
                dashManifest = reader.ReadLine();
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                urlConnection.Disconnect();
            }
            if (dashManifest == null)
                return;

            Matcher matcher = patBaseUrl.Matcher(dashManifest);
            while (matcher.Find())
            {
                string foo = matcher.Group(1);
                Matcher matcherBis = patItag.Matcher(foo);
                int itag;
                if (matcherBis.Find())
                {
                    itag = int.Parse(matcherBis.Group(1));
                    if (formats[itag] != null)
                        continue;
                    if (!includeWebM && formats[itag].GetExt().Equals("webm"))
                        continue;
                }
                else
                    continue;

                foo = foo.Replace("&amp;", "&").Replace(",", "%2C").Replace("mime=audio/", "mime=audio%2F").Replace("mime=video/", "mime=video%2F");
                YtFile newFile = new YtFile(formats[itag], foo);
                ytFiles.Append(itag, newFile);
            }
        }

        private void ReadDecipherFunctFromCache()
        {
            File cacheFile = new File(Android.App.Application.Context.CacheDir.AbsolutePath + "/" + cacheFileName);
            if(cacheFile.Exists() && JavaSystem.CurrentTimeMillis() - cacheFile.LastModified() < 1209600000)
            {
                BufferedReader reader = null;
                try
                {
                    reader = new BufferedReader(new FileReader(cacheFile));
                    decipherJsFileName = reader.ReadLine();
                    decipherFunctionName = reader.ReadLine();
                    decipherFunctions = reader.ReadLine();
                }
                catch(Java.Lang.Exception ex)
                {
                    ex.PrintStackTrace();
                }
                finally
                {
                    if(reader != null)
                        reader.Close();
                }
            }
        }

        private SparseArray<string> DecipherJsFile(string streamMap)
        {
            if (decipherJsFileName == null || decipherFunctions == null || decipherFunctionName == null)
                ReadDecipherFunctFromCache();

            URL url = new URL("https://youtube.com/watch?v=" + videoID);
            HttpURLConnection urlConnection = (HttpURLConnection)url.OpenConnection();
            urlConnection.SetRequestProperty("User-Agent", USER_AGENT);

            BufferedReader reader = null;
            try
            {
                reader = new BufferedReader(new InputStreamReader(urlConnection.InputStream));
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("url_encoded_fmt_stream_map"))
                    {
                        streamMap = line.Replace("\\u0026", "&");
                        break;
                    }
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                urlConnection.Disconnect();
            }
            SparseArray<string> encSignatures = new SparseArray<string>();

            Matcher matcher = patDecryptionJsFile.Matcher(streamMap);
            if (matcher.Find())
            {
                curJsFileName = matcher.Group(1).Replace("\\/", "/");
                if (decipherJsFileName == null || !decipherJsFileName.Equals(curJsFileName))
                {
                    decipherFunctions = null;
                    decipherFunctionName = null;
                }
                decipherJsFileName = curJsFileName;
            }

            if (parseDashManifest)
            {
                matcher = patDashManifest2.Matcher(streamMap);
                if (matcher.Find())
                {
                    string dashMpdUrl = matcher.Group(1).Replace("\\/", "/");
                    matcher = patDashManifestEncSig.Matcher(dashMpdUrl);
                    if (matcher.Find())
                    {
                        encSignatures.Append(0, matcher.Group(1));
                    }
                    else
                    {
                        dashMpdUrl = null;
                    }
                }
            }
            return encSignatures;
        }

        private SparseArray<YtFile> GetLiveStreamUrls(string streamMap)
        {
            Matcher matcher = patHlsvp.Matcher(streamMap);
            if (matcher.Find())
            {
                string hlsvp = URLDecoder.Decode(matcher.Group(1), "UTF-8");
                SparseArray<YtFile> ytFiles = new SparseArray<YtFile>();

                URL url = new URL(hlsvp);
                HttpURLConnection urlConnection = (HttpURLConnection)url.OpenConnection();
                urlConnection.SetRequestProperty("User-Agent", USER_AGENT);

                BufferedReader reader = null;
                try
                {
                    reader = new BufferedReader(new InputStreamReader(urlConnection.InputStream));
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("https://") || line.StartsWith("http://"))
                        {
                            matcher = patHlsItag.Matcher(line);
                            if (matcher.Find())
                            {
                                int itag = int.Parse(matcher.Group(1));
                                YtFile newFile = new YtFile(formats[itag], line);
                                ytFiles.Put(itag, newFile);
                            }
                        }
                    }
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                    urlConnection.Disconnect();
                }

                if (ytFiles.Size() == 0)
                    return null;
                return ytFiles;
            }
            return null;
        }

        private VideoMeta ParseVideoMeta(string videoInfo)
        {
            bool isLiveStream = false;
            string title = null;
            string author = null;
            string channelID = null;
            long viewCount = 0;
            long length = 0;

            Matcher matcher = patTitle.Matcher(videoInfo);
            if (matcher.Find())
                title = URLDecoder.Decode(matcher.Group(1), "UTF-8");

            matcher = patHlsvp.Matcher(videoInfo);
            if (matcher.Find())
                isLiveStream = true;

            matcher = patAuthor.Matcher(videoInfo);
            if (matcher.Find())
                author = URLDecoder.Decode(matcher.Group(1), "UTF-8");

            matcher = patChannelId.Matcher(videoInfo);
            if (matcher.Find())
                channelID = matcher.Group(1);

            matcher = patLength.Matcher(videoInfo);
            if (matcher.Find())
                length = Long.ParseLong(matcher.Group(1));

            matcher = patViewCount.Matcher(videoInfo);
            if (matcher.Find())
                viewCount = Long.ParseLong(matcher.Group(1));

            return new VideoMeta(videoID, title, author, channelID, length, viewCount, isLiveStream);
        }

        protected override SparseArray<YtFile> RunInBackground(params string[] @params)
        {
            throw new NotImplementedException();
        }
    }
}
 