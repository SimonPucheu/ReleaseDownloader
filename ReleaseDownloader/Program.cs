using System.CommandLine;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ReleaseDownloader
{
    class Repository
    {
        public string tag_name { get; set; }
    }

    class Program
    {
        static HttpClient Client = new();


        static void Main(string[] args)
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            Client.DefaultRequestHeaders.Add("User-Agent", "Release Downloader @ Simon Pucheu");
            Option RepositoriesListOption = new Option<List<string>>(name: "--list", description: "The list of the repositories to download.");
            Option PathOption = new Option<string>(name: "--path", description: "The location of the output files.", getDefaultValue: () => "");
            RootCommand RootCommand = new RootCommand("GitHub Release Download");
            RootCommand.AddOption(RepositoriesListOption);
            RootCommand.AddOption(PathOption);
            RootCommand.SetHandler(async (List<string> RepositoriesList, string Path) =>
            {
                await Download(RepositoriesList, Path, Client);
            }, (System.CommandLine.Binding.IValueDescriptor<List<string>>)RepositoriesListOption, (System.CommandLine.Binding.IValueDescriptor<string>)PathOption);
        }

        static async Task Download(List<string> List, string Path, HttpClient Client)
        {
            foreach (string Item in List)
            {
                string Version = "";
                string Owner = Item.Split('@')[0].Split('/')[0];
                string Repository = Item.Split('@')[0].Split('/')[1];
                if (Item.Contains('@'))
                {
                    Version = Item.Split('@')[1];
                }
                else
                {
                    await using Stream Stream = await Client.GetStreamAsync(String.Format("https://api.github.com/repos/{0}/{1}/releases/latest", Owner, Repository));
                    Version = JsonSerializer.Deserialize<Repository>(Stream).tag_name;
                }
                await DownloadZIP(String.Format("https://github.com/{0}/{1}/archive/refs/tags/{2}.zip", Owner, Repository, Version), Path, Client);
            }
        }

        static async Task DownloadZIP(string URL, string Path, HttpClient Client)
        {
            var Stream = await Client.GetStreamAsync(URL);
            FileStream FileStream = new(Path, FileMode.CreateNew);
            await Stream.CopyToAsync(FileStream);
        }
    }
}