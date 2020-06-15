using Topshelf;

namespace Mirage
{
    internal static class ConfigureService
    {
        internal static void Configure()
        {
            HostFactory.Run(configure =>
            {
                configure.Service<MirageService>(service =>
                {
                    service.ConstructUsing(s => new MirageService());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                //Setup Account that window service use to run.  
                configure.RunAsLocalSystem();
                configure.SetServiceName("MirageService");
                configure.SetDisplayName("Mirage");
                configure.SetDescription("Mirage Data Harvester for MiR Fleet");
            });
        }
    }
}