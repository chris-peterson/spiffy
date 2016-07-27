using Kekiri.IoC.Autofac;
using NUnit.Framework;

namespace UnitTests
{
    [SetUpFixture]
    public class BeforeTestRun
    {
        public BeforeTestRun()
        {
            AutofacBootstrapper.Initialize();
        }
    }
}
