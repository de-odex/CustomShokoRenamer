using System;
using System.Collections.Generic;
using System.IO;
using Shoko.Models.Server;
using Shoko.Server;
using Shoko.Server.Models;
using Shoko.Server.Renamer;
using Shoko.Server.Repositories;
using Shoko.Commons.Extensions;
using Shoko.Models.Enums;
using Shoko.Server.API.v2.Models.common;
using MoonSharp.Interpreter;        


namespace Renamer.deodex {  

    [Renamer("LuaRenamer", Description = "Lua Renamer (deodex)")]
    public class LuaRenamer : IRenamer {

        // set up scripting stuff                     
        private readonly RenameScript _rawScript;
        private Script _script = new Script(MoonSharp.Interpreter.CoreModules.Preset_SoftSandbox);

        public LuaRenamer(RenameScript rawScript) {
            this._rawScript = rawScript;
        }

        // implemented classes
        public string GetFileName(SVR_VideoLocal_Place video) => GetFileName(video.VideoLocal);

        public string GetFileName(SVR_VideoLocal video) {
            if (video == null) return "*Error: Unable to access file";

            // base data         
            string scriptCode = _rawScript.Script;
            SVR_AniDB_File file = video.GetAniDBFile();
            List<AniDB_Episode> episode = new List<AniDB_Episode>();
            SVR_AniDB_Anime anime;
   
            // error handling (not all is in this)
            if (file == null) {
                var epIntmdry = video.GetAnimeEpisodes();
                if (epIntmdry.Count == 0) return "*Error: Unable to get episode for file";
                episode.Add(epIntmdry[0].AniDB_Episode);
                anime = RepoFactory.AniDB_Anime.GetByAnimeID(episode[0].AnimeID);
                if (anime == null) return "*Error: Unable to get anime for file";
                
            }
            else {
                episode = file.Episodes;
                if (episode.Count == 0) return "*Error: Unable to get episode for file";
                anime = RepoFactory.AniDB_Anime.GetByAnimeID(episode[0].AnimeID);
                if (anime == null) return "*Error: Unable to get anime for file";
            }                      

            if (scriptCode == null) return "*Error: No script available for renamer";

            // TODO: start filling up tables (crudely, will implement better later (maybe (probably (hopefully))))
            // future me: actually this makes for a very strong renamer
            UserData.RegisterType<SVR_VideoLocal>();
            UserData.RegisterType<SVR_AniDB_File>();
            UserData.RegisterType<AniDB_Episode>();
            UserData.RegisterType<SVR_AniDB_Anime>();
            UserData.RegisterType<AniDB_Anime_Title>();
            UserData.RegisterExtensionType(typeof(Models)); // extends SVR_AniDB_Anime
            
            UserData.RegisterType<EpisodeType>();

            DynValue videoLuaObject = UserData.Create(video);
            DynValue fileLuaObject = UserData.Create(file);
            DynValue episodeLuaObject = UserData.Create(episode[0]);
            DynValue animeLuaObject = UserData.Create(anime);

            _script.Globals["EpisodeType"] = UserData.CreateStatic<EpisodeType>();
            _script.Globals.Set("video", videoLuaObject);
            _script.Globals.Set("file", fileLuaObject);
            _script.Globals.Set("episode", episodeLuaObject);
            _script.Globals.Set("anime", animeLuaObject);
            // (I have)/(deodex has) no idea if this is dangerous. It's as if the interpreter is as strong as the C# method 
            // (as long as the private-public keywords are actually used correctly, it should be fine)

            // make sure name is a string by initializing it as an empty one (in case its not used)
            _script.Globals["name"] = "";

            // run the script, get the variable "name" (str)
            _script.DoString(scriptCode);
            string scriptResult = _script.Globals.Get("name").ToString();  
            if (string.IsNullOrEmpty(scriptResult.Substring(1, scriptResult.Length - 2)))   
                return "*Error: the new filename is empty (script error)";

            //copy-pasted
            string pathToVid = video.GetBestVideoLocalPlace().FilePath;
            if (string.IsNullOrEmpty(pathToVid)) return "*Error: Unable to get the file's old filename";
            string ext = Path.GetExtension(pathToVid); //Prefer VideoLocal_Place as this is more accurate.
            if (string.IsNullOrEmpty(ext))
                return "*Error: Unable to get the file's extension"; // fail if we get a blank extension, something went wrong

            string renameTo = $"{scriptResult.Substring(1, scriptResult.Length - 2).Replace("`", "'")}{ext}";

            if (File.Exists(Path.Combine(Path.GetDirectoryName(pathToVid), renameTo))) // Has potential null error, im bad pls fix ty 
                return "*Error: A file with this filename already exists";
            return Utils.ReplaceInvalidFolderNameCharacters(renameTo);
        }
               
        public (ImportFolder dest, string folder) GetDestinationFolder(SVR_VideoLocal_Place video) {
            return (new LegacyRenamer(_rawScript)).GetDestinationFolder(video);
        }
    }
}
                                                                                                      