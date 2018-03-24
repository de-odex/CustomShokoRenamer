-- deodex's lua script of death 
-- oh god i put too much time into this

-- http://lua-users.org/wiki/SplitJoin, Function: true Python semantics for split
function string:split(sSeparator, nMax, bRegexp)
   assert(sSeparator ~= '')
   assert(nMax == nil or nMax >= 1)

   local aRecord = {}

   if self:len() > 0 then
      local bPlain = not bRegexp
      nMax = nMax or -1

      local nField, nStart = 1, 1
      local nFirst,nLast = self:find(sSeparator, nStart, bPlain)
      while nFirst and nMax ~= 0 do
         aRecord[nField] = self:sub(nStart, nFirst-1)
         nField = nField+1
         nStart = nLast+1
         nFirst,nLast = self:find(sSeparator, nStart, bPlain)
         nMax = nMax-1
      end
      aRecord[nField] = self:sub(nStart)
   end

   return aRecord
end

function ternary (cond, T, F)
    if cond then return T else return F end
end

-- divide resolutions and check if aspect ratio is 16 by 9, then rename to (vertres)p (even if it isnt progressive scan, sorry about that)
function x_to_p (str)
    return ternary((tonumber(str:match("%d+")) / tonumber((str:match("x%d+")):sub(2))) == (16/9), str:gsub("(%d+)x(%d+)", "%2p"), str)
end

prefix = ""
if episode.GetEpisodeTypeEnum() == EpisodeType.Credits then prefix = "C" end
if episode.GetEpisodeTypeEnum() == EpisodeType.Other then prefix = "O" end
if episode.GetEpisodeTypeEnum() == EpisodeType.Parody then prefix = "P" end
if episode.GetEpisodeTypeEnum() == EpisodeType.Special then prefix = "S" end
if episode.GetEpisodeTypeEnum() == EpisodeType.Trailer then prefix = "T" end

bit_depth = ternary(video.VideoBitDepth != "8",
                    " "..video.VideoBitDepth.."bit ", 
                    " ")

file_meta = ("%s %s %s%s%s"):format(x_to_p(string.split(file.File_VideoResolution, "'")[1]),
                                    file.File_Source,
                                    string.split(file.File_VideoCodec, "'")[1],
                                    bit_depth, 
                                    string.split(file.File_AudioCodec, "'")[1])

episode_number = ternary(anime.GetAnimeTypeRAW() != "movie",
                         (":: %s%02i%s%s "):format(prefix, 
                                                   episode.EpisodeNumber,
                                                   ternary(#episodes > 1,
                                                           ("-%02i"):format(episodes[#episodes].EpisodeNumber),
                                                           ""), 
                                                   ternary(file.FileVersion != 1, 
                                                           "v"..file.FileVersion, 
                                                           "")),
                         "")

name = ("[%s] %s %s(%s) [%s]"):format(file.Anime_GroupNameShort,
                                      anime.GetFormattedTitle(),
                                      episode_number,
                                      file_meta,
                                      string.upper(file.CRC))


name = name:gsub('H264/AVC', "x264")
name = name:gsub('HEVC', "x265")