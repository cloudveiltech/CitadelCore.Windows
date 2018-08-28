using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.ServiceConfigurators;

namespace CitadelCoreTest
{
    internal class Program
    {
        private static Mutex instanceMutex;

        private static void Main(string[] args)
        {
            bool createdNew;
            instanceMutex = new Mutex(true, @"Global\CitadelCore.Windows.Example", out createdNew);

            if(createdNew)
            {
                var exitCode = HostFactory.Run(x =>
                {
                    x.Service<FilterProvider>(s =>
                    {
                        s.ConstructUsing(name => new FilterProvider());
                        s.WhenStarted((fsp, hostCtl) => fsp.Start());
                        s.WhenStopped((fsp, hostCtl) => fsp.Stop());
                        s.WhenShutdown((fsp, hostCtl) =>
                        {
                            hostCtl.RequestAdditionalTime(TimeSpan.FromSeconds(30));
                            fsp.Shutdown();
                        });
                    });

                    x.EnableShutdown();
                    x.SetDescription("Content Filtering Example");
                    x.SetDisplayName("CitadelCore.Windows.Example");
                    x.SetServiceName("CitadelCore.Windows.Example");
                    x.StartManually();

                    x.RunAsLocalSystem();
                });
            }
        }
    }
}
