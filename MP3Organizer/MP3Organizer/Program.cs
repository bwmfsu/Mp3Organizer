using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagLib;
using TagLib.Id3v2;
using System.IO;
using System.Text.RegularExpressions;

namespace MP3Organizer
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo mp3Dir = new DirectoryInfo(@"F:\From External HD\Music\Albums");
            List<FileInfo> mp3s = mp3Dir.GetFiles("*.*", SearchOption.AllDirectories).ToList();
            DirectoryInfo itunesMusicDir = new DirectoryInfo(@"C:\Users\Brendan\Music\iTunes\iTunes Media\Music");
            DirectoryInfo originalMusicDir = Directory.CreateDirectory(@"F:\OrganizedMusic\OriginalMusicBeforeConvert");
            string organizedMusicParentDirectory = @"F:\OrganizedMusic\Music";

            //get music converted to .m4a by itunes.
            List<FileInfo> itunesMusic = itunesMusicDir.GetFiles("*.m4a*", SearchOption.AllDirectories).ToList();

            List<TagLib.File> taggedMp3s = new List<TagLib.File>();
            List<FileInfo> otherFormats = new List<FileInfo>();
            List<FileInfo> exceptionMp3s = new List<FileInfo>();


            //first we went to eliminate the original copies of any music files that have been converted to .m4a by itunes.
            foreach (FileInfo file in mp3s)
            {
                TagLib.File taggedMp3 = null;
                try
                {
                    taggedMp3 = TagLib.File.Create(file.FullName);
                }
                catch (Exception)
                {
                    exceptionMp3s.Add(file);
                }

                IEnumerable<FileInfo> itunesFiles = itunesMusic.Where(track => RemoveFileExtension(track) == RemoveFileExtension(file));

                foreach (FileInfo itunesFile in itunesFiles)
                {
                    try
                    {
                        TagLib.File taggedItunesMusic = TagLib.File.Create(itunesFile.FullName);

                        if (taggedItunesMusic != null && taggedMp3 != null)
                        {
                            TagLib.Tag itunesTag = taggedItunesMusic.Tag;
                            TagLib.Tag mp3Tag = taggedMp3.Tag;

                            if (itunesTag.FirstAlbumArtist == mp3Tag.FirstAlbumArtist && itunesTag.Album == mp3Tag.Album && itunesTag.Disc == mp3Tag.Disc && itunesTag.Track == mp3Tag.Track && itunesTag.Title == mp3Tag.Title) 
                            //Copy to a safe location first for verification, then manually remove.
                            //itunesFile.CopyTo(file.Directory.FullName);
                            //remove the non-itunes version of the file so that there aren't "duplicates".
                            file.CopyTo(string.Format(@"{0}\{1}", originalMusicDir.FullName, file.Name));
                        }
                    }
                    catch (Exception)
                    {
                        exceptionMp3s.Add(itunesFile);
                    }

                }
            }


            ////after moving the itunes coverted music into the music directory, we want to refresh the list of music to be organized.
            //mp3s = mp3Dir.GetFiles("*.*", SearchOption.AllDirectories).ToList();

            ////create the parent directory for the organized music.
            //Directory.CreateDirectory(organizedMusicParentDirectory);
            //List<ArtistAlbumGrouping> uniqueGroupings = new List<ArtistAlbumGrouping>();


            //foreach (FileInfo file in mp3s)
            //{
            //    //TODO:Have the list of accepted extensions as configurable values, maybe a comma delimited list.
            //    if (file.Extension != ".mp3" && file.Extension != ".MP3" && file.Extension != ".m4a" && file.Extension != ".m4p" && file.Extension != ".wma")
            //    {
            //        otherFormats.Add(file);
            //    }
            //    else
            //    {
            //        try
            //        {
            //            TagLib.File taggedMp3 = TagLib.File.Create(file.FullName);
            //            taggedMp3s.Add(taggedMp3);

            //            //string orderedArtists = string.Join(",", taggedMp3.Tag.AlbumArtists.OrderBy(artist => artist).ToArray());

            //            if (!uniqueGroupings.Exists(grouping => grouping.FirstArtist == taggedMp3.Tag.FirstArtist))
            //            {
            //                ArtistAlbumGrouping grouping = new ArtistAlbumGrouping();
            //                grouping.FirstArtist = taggedMp3.Tag.FirstAlbumArtist;
            //                grouping.Album = taggedMp3.Tag.Album;
            //                uniqueGroupings.Add(grouping);
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            exceptionMp3s.Add(file);
            //        }
            //    }
            //}

            ////we want to order our unique groupings by artist then by album name which may not be necessary but seems like it would save some processing- may take out later.
            //uniqueGroupings = uniqueGroupings.OrderBy(grouping => grouping.FirstArtist).ThenBy(grouping => grouping.Album).ToList();

            //foreach (ArtistAlbumGrouping grouping in uniqueGroupings)
            //{
            //    if (!string.IsNullOrEmpty(grouping.FirstArtist))
            //    {
            //        string artistDirectoryPath = string.Format(@"{0}\{1}", organizedMusicParentDirectory, RemoveSpecialCharacters(grouping.FirstArtist));

            //        if (!string.IsNullOrEmpty(grouping.Album))
            //        {
            //            //if the artist directory doesn't already exist then create it.
            //            if (!Directory.Exists(artistDirectoryPath))
            //            {
            //                Directory.CreateDirectory(artistDirectoryPath);
            //            }

            //            //There shouldn't be an existing album directory already for a given artist so just assume it's fine to create the album directory for this artist.
            //            Directory.CreateDirectory(string.Format(@"{0}\{1}", artistDirectoryPath, RemoveSpecialCharacters(grouping.Album)));
            //        }
            //    }
            //}

            //We will store the results of the music that was successfuly tagged in a text file at this location.
            string taggedMusicFile = @"F:\TaggedMp3s.txt";

            //We will store the results of the files that we came across which may be music file extensions taken into account, or may be pictures or text like files.
            string otherFormatsFile = @"F:\MusicInOtherFormats.txt";

            //We want to know the music files that tag information was not able to be retrieved for.  After the music organization process we will manually have to move these files over to the correct organized directory.
            string exceptionsFile = @"F:\ExceptionMp3s.txt";

            //Build the tagged results text file.
            using (TextWriter writer = new StreamWriter(taggedMusicFile, false))
            {
                foreach (TagLib.File file in taggedMp3s)
                {
                    StringBuilder sb = new StringBuilder();
                    if (file.Tag.AlbumArtists != null && file.Tag.AlbumArtists.Length > 0)
                    {
                        sb.Append("Artists: ");

                        foreach (String artist in file.Tag.AlbumArtists)
                        {
                            sb.Append(artist);
                            sb.Append(" ");
                        }
                    }

                    if (!string.IsNullOrEmpty(file.Tag.Album))
                    {
                        sb.Append("Album Name: ");
                        sb.Append(file.Tag.Album);
                    }

                    if (!string.IsNullOrEmpty(file.Tag.Title))
                    {
                        sb.Append("Song Title: ");
                        sb.Append(file.Tag.Title);
                        sb.Append(file.MimeType);
                    }

                    writer.WriteLine(sb.ToString());
                }

            }

            //Build the other file formats text file.
            using (TextWriter writer = new StreamWriter(otherFormatsFile, false))
            {
                foreach (FileInfo file in otherFormats)
                {
                    writer.WriteLine(file.FullName);
                }
            }

            //Build the tagging exceptions text file.
            using (TextWriter writer = new StreamWriter(exceptionsFile, false))
            {
                foreach (FileInfo file in exceptionMp3s)
                {
                    writer.WriteLine(file.FullName);
                }
            }
        }

        /// <summary>
        /// Remove any special characters that are reserved by Windows.
        /// </summary>
        /// <param name="str">The folder/file name to strip of special characters.</param>
        /// <returns>The folder/file name stripped of special characters.</returns>
        private static string RemoveSpecialCharacters(string str)
        {
            if (Regex.Matches(str, @"[\\?%*:|""<>]").Count > 0)
            {
                return Regex.Replace(str, @"[/\\?%*:|""<>]", "", RegexOptions.None);
            }

            return str;
        }

        private static string RemoveFileExtension(FileInfo file)
        {
            return file.Name.Substring(0, file.Name.IndexOf(file.Extension));
        }
    }
}
